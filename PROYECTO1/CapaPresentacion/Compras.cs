using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PROYECTO1;
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class Compras : Form
    {
        private readonly LCompra _lCompra = new();

        private class ProveedorDTO
        {
            public int IdProveedor { get; set; }
            public string Cuit { get; set; } = string.Empty;
            public string RazonSocial { get; set; } = string.Empty;
            public string Telefono { get; set; } = string.Empty;

            public string Display => $"{Cuit} - {RazonSocial}".Trim();
            public string DisplayNombre => $"{RazonSocial} (CUIT: {Cuit})".Trim();
        }

        private class ProductoCompraDTO
        {
            public int IdProducto { get; set; }
            public int IdProveedor { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public string Talle { get; set; } = string.Empty;
            public int Stock { get; set; }
            public decimal PrecioCosto { get; set; }
            public decimal PrecioVenta { get; set; }

            public string DisplayId => $"{IdProducto} - {Nombre} - {Categoria}".Trim();
            public string DisplayNombre => $"{Nombre} - {Categoria} (ID {IdProducto})".Trim();
        }

        public class ItemCarritoCompra
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Talle { get; set; } = string.Empty;
            public int Cantidad { get; set; }
            public decimal PrecioCosto { get; set; }
            public decimal PrecioVenta { get; set; }
            public decimal SubTotal => Cantidad * PrecioCosto;
        }

        private readonly BindingList<ItemCarritoCompra> _carrito = new();
        private List<ProveedorDTO> _listaProveedores = new();
        private List<ProductoCompraDTO> _listaProductos = new();

        private ProveedorDTO? _proveedorSeleccionado = null;
        private ProductoCompraDTO? _productoSeleccionado = null;

        private readonly ToolTip _ayudaMargen = new();

        public Compras()
        {
            InitializeComponent();
            ConfigurarCamposRentabilidad();

            dataGridViewVentas.CellClick += DataGridViewVentas_CellClick;
            buttonAgregar.Click += ButtonAgregar_Click;
            buttonFinalizar.Click += ButtonFinalizar_Click;
            buttonCancelar.Click += ButtonCancelar_Click;
            buttonAgregarCliente.Click += ButtonNuevoProveedor_Click;
            buttonNuevoProdcuto.Click += ButtonNuevoProducto_Click;

            textBoxBuscadorProveedor.KeyDown += TextBoxBuscadorProveedor_KeyDown;
            TextBoxBuscadorProducto.KeyDown += TextBoxBuscadorProducto_KeyDown;

            comboBoxTalle.SelectedIndexChanged += ComboBoxTalle_SelectedIndexChanged;
            textBoxPrecioCosto.TextChanged += (_, __) => ActualizarMargen();
            textBoxPrecioVentaCompra.TextChanged += (_, __) => ActualizarMargen();
            textBoxPrecioCosto.Leave += (_, __) => NormalizarPrecio(textBoxPrecioCosto);
            textBoxPrecioVentaCompra.Leave += (_, __) => NormalizarPrecio(textBoxPrecioVentaCompra);

            textBoxCantidad.KeyPress += TextBoxCantidad_KeyPress;
            textBoxCantidad.KeyDown += TextBoxCantidad_KeyDown;

            Load += Compras_Load;
        }

        private void ConfigurarCamposRentabilidad()
        {
            textBoxPrecioCosto.Enabled = false;
            textBoxPrecioVentaCompra.Enabled = false;
            textBoxMargenCompra.ReadOnly = true;

            _ayudaMargen.SetToolTip(labelMargenCompra,
                "Margen bruto = (precio de venta - costo) / precio de venta × 100.");
            _ayudaMargen.SetToolTip(textBoxMargenCompra,
                "El porcentaje de ganancia bruta calculado sobre el precio de venta.");
        }

        private static bool LeerPrecio(string texto, out decimal precio)
        {
            return decimal.TryParse(texto.Trim(), NumberStyles.Number,
                CultureInfo.CurrentCulture, out precio);
        }

        private static void NormalizarPrecio(Guna.UI2.WinForms.Guna2TextBox campo)
        {
            if (LeerPrecio(campo.Text, out decimal precio) && precio >= 0 &&
                decimal.Round(precio, 2) == precio)
                campo.Text = precio.ToString("N2", CultureInfo.CurrentCulture);
        }

        private void ActualizarMargen()
        {
            if (LeerPrecio(textBoxPrecioCosto.Text, out decimal costo) &&
                LeerPrecio(textBoxPrecioVentaCompra.Text, out decimal venta) && venta > 0)
            {
                // Fórmula estándar de margen de contribución comercial
                decimal margen = ((venta - costo) / venta) * 100m;
                textBoxMargenCompra.Text = margen.ToString("N2") + "%";
                textBoxMargenCompra.ForeColor = margen > 0
                    ? Color.FromArgb(130, 225, 176)
                    : margen < 0 ? Color.FromArgb(255, 130, 130) : Color.White;
                decimal utilidad = venta - costo;
                _ayudaMargen.SetToolTip(textBoxMargenCompra,
                    $"Ganancia bruta por unidad: $ {utilidad:N2}. Margen bruto: {margen:N2}%.");
            }
            else
            {
                textBoxMargenCompra.Clear();
                textBoxMargenCompra.ForeColor = Color.White;
            }
        }

        private void Compras_Load(object? sender, EventArgs e)
        {
            ConfigurarGrilla();
            CargarMetodosPago();
            CargarProveedoresDesdeBD();
            CargarProductosDesdeBD();
            InicializarFormulario();

            this.BeginInvoke(new Action(LimpiarMetodoPago));
        }

        private void ConfigurarGrilla()
        {
            dataGridViewVentas.AutoGenerateColumns = false;
            CidProducto.DataPropertyName = "IdProducto";
            CNombre.DataPropertyName = "Nombre";
            CTalle.DataPropertyName = "Talle";
            CCantidad.DataPropertyName = "Cantidad";
            CPrecioUnitario.DataPropertyName = "PrecioCosto";
            CSubTotal.DataPropertyName = "SubTotal";

            CPrecioUnitario.HeaderText = "Costo unit.";
            CSubTotal.HeaderText = "Subtotal";

            var pesos = new[]
            {
                ("CidProducto", 65f, 54), ("CNombre", 185f, 130),
                ("CTalle", 90f, 65), ("CCantidad", 90f, 75),
                ("CPrecioUnitario", 125f, 105), ("CSubTotal", 135f, 112),
                ("CQuitar", 75f, 65)
            };
            foreach (var (nombre, peso, minimo) in pesos)
            {
                DataGridViewColumn col = dataGridViewVentas.Columns[nombre];
                col.MinimumWidth = minimo;
                col.FillWeight = peso;
            }

            CPrecioUnitario.DefaultCellStyle.Format = "N2";
            CSubTotal.DefaultCellStyle.Format = "N2";
            CPrecioUnitario.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CSubTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CCantidad.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dataGridViewVentas.DataSource = _carrito;

            textBoxNombreCliente.ReadOnly = true;
            textBoxApellidoCliente.ReadOnly = true;
            textBoxNombreProducto.ReadOnly = true;
            textBoxStock.ReadOnly = true;
            textBoxImporteTotal.ReadOnly = true;
        }

        private void CargarMetodosPago()
        {
            DataTable dt = _lCompra.ObtenerMetodosPago();
            comboBoxMetodoPago.DisplayMember = "nombre";
            comboBoxMetodoPago.ValueMember = "id_metodo_pago";
            comboBoxMetodoPago.DataSource = dt;
            LimpiarMetodoPago();
        }

        private void LimpiarMetodoPago()
        {
            comboBoxMetodoPago.StartIndex = -1;
            comboBoxMetodoPago.SelectedIndex = -1;
            comboBoxMetodoPago.SelectedItem = null;
            comboBoxMetodoPago.ResetText();
        }

        private void TextBoxCantidad_KeyPress(object? sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && (e.KeyChar < '0' || e.KeyChar > '9'))
            {
                e.Handled = true;
            }
        }

        private void TextBoxCantidad_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ButtonAgregar_Click(sender, EventArgs.Empty);
            }
        }

        private void InicializarFormulario()
        {
            _carrito.Clear();
            LimpiarCamposProveedor();
            LimpiarCamposProducto();
            LimpiarMetodoPago();
            CalcularTotalCompra();
        }

        private void ActualizarEstadoPanelProductos(bool habilitado)
        {
            Color colorPanelFondo = habilitado ? Color.FromArgb(43, 50, 68) : Color.FromArgb(30, 34, 46);
            Color colorPanelBorde = habilitado ? Color.FromArgb(43, 50, 68) : Color.FromArgb(24, 28, 38);
            Color colorTextoTitulo = habilitado ? Color.White : Color.FromArgb(120, 130, 150);
            Color colorLabels = habilitado ? Color.White : Color.FromArgb(100, 110, 130);

            guna2Panel1.FillColor = colorPanelFondo;
            guna2Panel1.BorderColor = colorPanelBorde;

            tituloProducto.FillColor = colorPanelFondo;
            tituloProducto.BorderColor = colorPanelBorde;
            tituloProducto.ForeColor = colorTextoTitulo;

            labelNombre.ForeColor = colorLabels;
            labelPrecioCosto.ForeColor = colorLabels;
            labelCantidad.ForeColor = colorLabels;
            labelTalle.ForeColor = colorLabels;
            lblStock.ForeColor = colorLabels;

            TextBoxBuscadorProducto.Enabled = habilitado;
            TextBoxBuscadorProducto.DisabledState.FillColor = Color.FromArgb(24, 28, 38);
            TextBoxBuscadorProducto.DisabledState.BorderColor = Color.FromArgb(45, 52, 70);
            TextBoxBuscadorProducto.DisabledState.ForeColor = Color.FromArgb(100, 110, 130);
            TextBoxBuscadorProducto.DisabledState.PlaceholderForeColor = Color.FromArgb(100, 110, 130);
            TextBoxBuscadorProducto.PlaceholderText = habilitado ? "Buscar" : "Seleccione un proveedor primero...";

            buttonNuevoProdcuto.Enabled = habilitado;
            buttonNuevoProdcuto.DisabledState.FillColor = Color.FromArgb(45, 52, 70);
            buttonNuevoProdcuto.DisabledState.ForeColor = Color.FromArgb(110, 120, 140);

            comboBoxTalle.Enabled = habilitado;

            textBoxCantidad.Enabled = habilitado;
            textBoxCantidad.DisabledState.FillColor = Color.FromArgb(24, 28, 38);
            textBoxCantidad.DisabledState.BorderColor = Color.FromArgb(45, 52, 70);
            textBoxCantidad.DisabledState.ForeColor = Color.FromArgb(100, 110, 130);

            buttonAgregar.Enabled = habilitado;
            labelPrecioCosto.ForeColor = colorLabels;
            labelPrecioVentaCompra.ForeColor = colorLabels;
            labelMargenCompra.ForeColor = colorLabels;
            textBoxPrecioCosto.Enabled = habilitado && _productoSeleccionado != null;
            textBoxPrecioVentaCompra.Enabled = habilitado && _productoSeleccionado != null;
            buttonAgregar.DisabledState.FillColor = Color.FromArgb(45, 52, 70);
            buttonAgregar.DisabledState.ForeColor = Color.FromArgb(110, 120, 140);
        }

        private static string LeerTexto(DataRow dr, params string[] columnas)
        {
            foreach (var col in columnas)
            {
                if (dr.Table.Columns.Contains(col) && dr[col] != DBNull.Value)
                {
                    string valor = dr[col].ToString()?.Trim() ?? "";
                    if (valor.Length > 0) return valor;
                }
            }
            return string.Empty;
        }

        private void CargarProveedoresDesdeBD()
        {
            try
            {
                _listaProveedores.Clear();
                DataTable dt = _lCompra.ObtenerProveedoresParaCompra();

                foreach (DataRow dr in dt.Rows)
                {
                    _listaProveedores.Add(new ProveedorDTO
                    {
                        IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
                        Cuit = LeerTexto(dr, "cuit", "cuit_proveedor", "cuil", "cuit_cuil"),
                        RazonSocial = LeerTexto(dr, "razon_social", "razonsocial", "nombre", "nombre_proveedor", "empresa"),
                        Telefono = LeerTexto(dr, "telefono", "telefono_proveedor", "celular")
                    });
                }

                var coleccion = new AutoCompleteStringCollection();
                foreach (var prov in _listaProveedores)
                {
                    coleccion.Add(prov.Display);
                    coleccion.Add(prov.DisplayNombre);
                }

                textBoxBuscadorProveedor.AutoCompleteCustomSource = coleccion;
                textBoxBuscadorProveedor.AutoCompleteMode = AutoCompleteMode.Suggest;
                textBoxBuscadorProveedor.AutoCompleteSource = AutoCompleteSource.CustomSource;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar proveedores: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void TextBoxBuscadorProveedor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ConfirmarSeleccionProveedor();
            }
        }

        private void ConfirmarSeleccionProveedor()
        {
            string texto = textBoxBuscadorProveedor.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            var prov = _listaProveedores.FirstOrDefault(p =>
                    p.Display.Equals(texto, StringComparison.OrdinalIgnoreCase) ||
                    p.DisplayNombre.Equals(texto, StringComparison.OrdinalIgnoreCase))
                ?? _listaProveedores.FirstOrDefault(p =>
                    p.Display.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                    p.Cuit.StartsWith(texto, StringComparison.OrdinalIgnoreCase));

            if (prov != null)
            {
                EstablecerProveedor(prov);
            }
            else
            {
                MessageBox.Show("No se encontró ningún proveedor con ese dato.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EstablecerProveedor(ProveedorDTO prov)
        {
            if (_proveedorSeleccionado != null &&
                _proveedorSeleccionado.IdProveedor != prov.IdProveedor &&
                _carrito.Count > 0)
            {
                var r = MessageBox.Show(
                    "El pedido actual tiene productos de otro proveedor. Si cambia de proveedor, se vaciará el pedido. ¿Desea continuar?",
                    "Cambiar de proveedor", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                {
                    textBoxBuscadorProveedor.Clear();
                    return;
                }

                _carrito.Clear();
                CalcularTotalCompra();
            }

            _proveedorSeleccionado = prov;
            textBoxNombreCliente.Text = prov.RazonSocial;
            textBoxApellidoCliente.Text = prov.Cuit;
            textBoxBuscadorProveedor.Clear();

            LimpiarCamposProducto();
            ActualizarSugerenciasProducto();
            ActualizarEstadoPanelProductos(true);
            TextBoxBuscadorProducto.Focus();
        }

        private void LimpiarCamposProveedor()
        {
            _proveedorSeleccionado = null;
            textBoxBuscadorProveedor.Clear();
            textBoxNombreCliente.Clear();
            textBoxApellidoCliente.Clear();
            LimpiarMetodoPago();
            ActualizarSugerenciasProducto();
            ActualizarEstadoPanelProductos(false);
        }

        private void ButtonNuevoProveedor_Click(object? sender, EventArgs e)
        {
            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formProv = new FormRegistrarProveedor())
            {
                formProv.StartPosition = FormStartPosition.CenterScreen;
                if (formProv.ShowDialog(this) == DialogResult.OK)
                {
                    CargarProveedoresDesdeBD();
                    if (_listaProveedores.Count > 0)
                    {
                        var ultimo = _listaProveedores.OrderByDescending(p => p.IdProveedor).First();
                        EstablecerProveedor(ultimo);
                    }
                }
            }
            overlay.Close();
        }

        private void CargarProductosDesdeBD()
        {
            try
            {
                _listaProductos.Clear();
                DataTable dt = _lCompra.ObtenerProductosParaCompra();

                foreach (DataRow dr in dt.Rows)
                {
                    _listaProductos.Add(new ProductoCompraDTO
                    {
                        IdProducto = Convert.ToInt32(dr["id_producto"]),
                        IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
                        Nombre = dr["nombre"].ToString() ?? "",
                        Categoria = dr["categoria"].ToString() ?? "",
                        Talle = dr["talla"].ToString() ?? "Único",
                        Stock = Convert.ToInt32(dr["stock"]),
                        PrecioCosto = Convert.ToDecimal(dr["precio_costo"]),
                        PrecioVenta = Convert.ToDecimal(dr["precio_venta"])
                    });
                }

                ActualizarSugerenciasProducto();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private IEnumerable<ProductoCompraDTO> ProductosDelProveedor()
        {
            if (_proveedorSeleccionado == null) return Enumerable.Empty<ProductoCompraDTO>();
            return _listaProductos.Where(p => p.IdProveedor == _proveedorSeleccionado.IdProveedor);
        }

        private void ActualizarSugerenciasProducto()
        {
            var coleccion = new AutoCompleteStringCollection();
            foreach (var prod in ProductosDelProveedor().GroupBy(p => p.Nombre).Select(g => g.First()))
            {
                coleccion.Add(prod.DisplayId);
                coleccion.Add(prod.DisplayNombre);
            }

            TextBoxBuscadorProducto.AutoCompleteCustomSource = coleccion;
            TextBoxBuscadorProducto.AutoCompleteMode = AutoCompleteMode.Suggest;
            TextBoxBuscadorProducto.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        private void TextBoxBuscadorProducto_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ConfirmarSeleccionProducto();
            }
        }

        private void ConfirmarSeleccionProducto()
        {
            string busqueda = TextBoxBuscadorProducto.Text.Trim();
            if (string.IsNullOrWhiteSpace(busqueda)) return;

            if (_proveedorSeleccionado == null)
            {
                MessageBox.Show("Primero seleccione un proveedor para ver sus productos.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxBuscadorProveedor.Focus();
                return;
            }

            var prodEncontrado = ProductosDelProveedor().FirstOrDefault(p =>
                    p.DisplayId.Equals(busqueda, StringComparison.OrdinalIgnoreCase) ||
                    p.DisplayNombre.Equals(busqueda, StringComparison.OrdinalIgnoreCase))
                ?? ProductosDelProveedor().FirstOrDefault(p =>
                    p.Nombre.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    p.Categoria.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0);

            if (prodEncontrado != null)
            {
                SeleccionarProducto(prodEncontrado);
            }
            else
            {
                MessageBox.Show("No se encontró ningún producto de este proveedor con ese criterio.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void SeleccionarProducto(ProductoCompraDTO prodEncontrado)
        {
            string nombreProducto = prodEncontrado.Nombre;
            textBoxNombreProducto.Text = nombreProducto;

            var variantes = ProductosDelProveedor()
                .Where(p => p.Nombre.Equals(nombreProducto, StringComparison.OrdinalIgnoreCase))
                .ToList();

            comboBoxTalle.Items.Clear();
            foreach (var v in variantes)
            {
                comboBoxTalle.Items.Add(v.Talle);
            }

            if (comboBoxTalle.Items.Count > 0)
                comboBoxTalle.SelectedIndex = 0;

            TextBoxBuscadorProducto.Clear();
            comboBoxTalle.Focus();
        }

        private void ComboBoxTalle_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (comboBoxTalle.SelectedItem == null) return;

            string nombre = textBoxNombreProducto.Text.Trim();
            string talle = comboBoxTalle.SelectedItem.ToString()!;

            _productoSeleccionado = ProductosDelProveedor().FirstOrDefault(p =>
                p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                p.Talle.Equals(talle, StringComparison.OrdinalIgnoreCase));

            if (_productoSeleccionado != null)
            {
                textBoxStock.Text = _productoSeleccionado.Stock.ToString();
                textBoxPrecioCosto.Enabled = true;
                textBoxPrecioVentaCompra.Enabled = true;
                textBoxPrecioCosto.Text = _productoSeleccionado.PrecioCosto.ToString("N2");
                textBoxPrecioVentaCompra.Text = _productoSeleccionado.PrecioVenta.ToString("N2");
                ActualizarMargen();
            }
        }

        private void LimpiarCamposProducto()
        {
            _productoSeleccionado = null;
            TextBoxBuscadorProducto.Clear();
            textBoxNombreProducto.Clear();
            textBoxPrecioCosto.Clear();
            textBoxPrecioVentaCompra.Clear();
            textBoxMargenCompra.Clear();
            textBoxPrecioCosto.Enabled = false;
            textBoxPrecioVentaCompra.Enabled = false;
            textBoxStock.Clear();
            comboBoxTalle.Items.Clear();
            textBoxCantidad.Text = "1";
        }

        private void ButtonNuevoProducto_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null)
            {
                MessageBox.Show("Primero seleccione un proveedor para registrar un producto suyo.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxBuscadorProveedor.Focus();
                return;
            }

            int idProveedor = _proveedorSeleccionado.IdProveedor;
            int maxIdAntes = _listaProductos.Select(p => p.IdProducto).DefaultIfEmpty(0).Max();
            bool creado = false;

            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formProd = new FormRegistrarProducto())
            {
                formProd.PreseleccionarProveedor(idProveedor);
                formProd.StartPosition = FormStartPosition.CenterScreen;
                if (formProd.ShowDialog(this) == DialogResult.OK)
                {
                    CargarProductosDesdeBD();
                    creado = true;
                }
            }
            overlay.Close();

            if (creado) SeleccionarProductoRecienCreado(idProveedor, maxIdAntes);
        }

        private void SeleccionarProductoRecienCreado(int idProveedor, int maxIdAntes)
        {
            var nuevo = _listaProductos
                .Where(p => p.IdProveedor == idProveedor && p.IdProducto > maxIdAntes)
                .OrderByDescending(p => p.IdProducto)
                .FirstOrDefault();

            if (nuevo == null) return;

            SeleccionarProducto(nuevo);

            int idx = comboBoxTalle.Items.IndexOf(nuevo.Talle);
            if (idx >= 0) comboBoxTalle.SelectedIndex = idx;

            textBoxCantidad.Focus();
            textBoxCantidad.SelectAll();
        }

        private void ButtonAgregar_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null)
            {
                MessageBox.Show("Seleccione un producto para agregar al pedido.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!int.TryParse(textBoxCantidad.Text.Trim(), out int cantidad) || cantidad <= 0)
            {
                MessageBox.Show("Ingrese una cantidad válida mayor a cero.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxCantidad.Focus();
                textBoxCantidad.SelectAll();
                return;
            }

            if (!LeerPrecio(textBoxPrecioCosto.Text, out decimal costo) || costo <= 0 ||
                decimal.Round(costo, 2) != costo)
            {
                MessageBox.Show("Ingresá un costo positivo con hasta dos decimales.",
                    "Costo inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxPrecioCosto.Focus();
                return;
            }
            if (!LeerPrecio(textBoxPrecioVentaCompra.Text, out decimal venta) || venta <= 0 ||
                decimal.Round(venta, 2) != venta)
            {
                MessageBox.Show("Ingresá un precio de venta positivo con hasta dos decimales.",
                    "Venta inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxPrecioVentaCompra.Focus();
                return;
            }

            // Nueva validación sugerida: asegurar que la compra sea rentable
            if (venta <= costo)
            {
                MessageBox.Show("El precio de venta debe ser estrictamente mayor al precio de costo.",
                    "Rentabilidad negativa", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxPrecioVentaCompra.Focus();
                return;
            }

            var itemExistente = _carrito.FirstOrDefault(c => c.IdProducto == _productoSeleccionado.IdProducto);
            if (itemExistente != null)
            {
                if (itemExistente.PrecioCosto != costo || itemExistente.PrecioVenta != venta)
                {
                    var respuesta = MessageBox.Show(
                        "Este talle ya está en el pedido con otros precios. " +
                        "Si continuás, los precios nuevos se usarán para todas sus unidades. ¿Continuar?",
                        "Actualizar precios del renglón", MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    if (respuesta != DialogResult.Yes) return;
                }
                itemExistente.Cantidad += cantidad;
                itemExistente.PrecioCosto = costo;
                itemExistente.PrecioVenta = venta;
                _carrito.ResetBindings();
            }
            else
            {
                _carrito.Add(new ItemCarritoCompra
                {
                    IdProducto = _productoSeleccionado.IdProducto,
                    Nombre = _productoSeleccionado.Nombre,
                    Talle = _productoSeleccionado.Talle,
                    Cantidad = cantidad,
                    PrecioCosto = costo,
                    PrecioVenta = venta
                });
            }

            CalcularTotalCompra();
            LimpiarCamposProducto();
            TextBoxBuscadorProducto.Focus();
        }

        private void DataGridViewVentas_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _carrito.Count && e.ColumnIndex >= 0 &&
                dataGridViewVentas.Columns[e.ColumnIndex].Name == "CQuitar")
            {
                _carrito.RemoveAt(e.RowIndex);
                CalcularTotalCompra();
            }
        }

        private decimal CalcularTotalCompra()
        {
            decimal total = _carrito.Sum(i => i.SubTotal);
            textBoxImporteTotal.Text = $"$ {total:N2}";
            return total;
        }

        private void ButtonCancelar_Click(object? sender, EventArgs e)
        {
            var r = MessageBox.Show("¿Está seguro de cancelar el pedido actual?", "Confirmar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (r == DialogResult.Yes) InicializarFormulario();
        }

        private void ButtonFinalizar_Click(object? sender, EventArgs e)
        {
            if (_proveedorSeleccionado == null)
            {
                MessageBox.Show("Debe seleccionar un proveedor para registrar el ingreso.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                textBoxBuscadorProveedor.Focus();
                return;
            }

            if (_carrito.Count == 0)
            {
                MessageBox.Show("El detalle de la compra no contiene ningún producto.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBoxMetodoPago.SelectedIndex == -1 || comboBoxMetodoPago.SelectedValue == null)
            {
                MessageBox.Show("Seleccione el método de pago utilizado.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBoxMetodoPago.Focus();
                return;
            }

            decimal totalFinal = CalcularTotalCompra();
            int idMetodo = Convert.ToInt32(comboBoxMetodoPago.SelectedValue);
            if (SesionActual.Usuario == null)
            {
                MessageBox.Show("Tu sesión no tiene un usuario válido. Volvé a iniciar sesión.",
                    "Sesión requerida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            int idUsuario = SesionActual.Usuario.Id;

            // Se elimina la verificación de "margen bruto cero o negativo" antes del DialogResult
            // porque ya se bloqueó en el ButtonAgregar_Click de acuerdo a la nueva validación.

            if (MessageBox.Show(
                    $"Total a comprar: $ {totalFinal:N2}\n\n" +
                    "Al finalizar se sumará el stock y se actualizarán el costo y el precio " +
                    "de venta de cada talle comprado.\n\n¿Confirmás la compra?",
                    "Confirmar compra", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var compra = new ECompra
            {
                IdUsuario = idUsuario,
                IdProveedor = _proveedorSeleccionado.IdProveedor,
                IdMetodoPago = idMetodo,
                Total = totalFinal
            };

            foreach (var item in _carrito)
            {
                compra.Detalles.Add(new EDetalleCompra
                {
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioCosto = item.PrecioCosto,
                    PrecioVenta = item.PrecioVenta
                });
            }

            if (_lCompra.RegistrarCompra(compra, out int idCompra, out string mensajeError))
            {
                string ticket = GenerarTextoTicketCompra(idCompra, totalFinal);

                using (Form overlay = ModalHelper.CrearOverlay(this))
                {
                    overlay.Show();
                    overlay.BringToFront();

                    using (var frmTicket = new FormTicket(ticket))
                    {
                        frmTicket.StartPosition = FormStartPosition.CenterScreen;
                        frmTicket.ShowDialog(this);
                    }

                    overlay.Close();
                }

                CargarProductosDesdeBD();
                InicializarFormulario();
            }
            else
            {
                MessageBox.Show(mensajeError, "Error al registrar compra", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerarTextoTicketCompra(int idCompra, decimal totalFinal)
        {
            string usuario = SesionActual.Usuario != null
                ? $"{SesionActual.Usuario.Nombre} {SesionActual.Usuario.Apellido}"
                : "Administrador";

            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("             TIENDA DE ROPA             ");
            sb.AppendLine("           REGISTRO DE COMPRA           ");
            sb.AppendLine("========================================");
            sb.AppendLine($" Compra N°: #{idCompra:D6}");
            sb.AppendLine($" Fecha:     {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($" Usuario:   {usuario}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($" Proveedor: {_proveedorSeleccionado!.RazonSocial}");
            sb.AppendLine($" CUIT:      {_proveedorSeleccionado.Cuit}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine(" CANT. DESCRIPCION      TALLE   SUBTOTAL");
            sb.AppendLine("----------------------------------------");

            foreach (var item in _carrito)
            {
                string desc = item.Nombre.Length > 15 ? item.Nombre.Substring(0, 15) : item.Nombre.PadRight(15);
                string tal = item.Talle.PadRight(6);
                sb.AppendLine($" {item.Cantidad,2}    {desc} {tal} ${item.SubTotal,7:N2}");
            }

            sb.AppendLine("----------------------------------------");
            sb.AppendLine($" Metodo Pago: {comboBoxMetodoPago.Text}");
            sb.AppendLine($" TOTAL:                    ${totalFinal,8:N2}");

            return sb.ToString();
        }
    }
}