using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using PROYECTO1;
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class Ventas : Form
    {
        private readonly LVenta _lVenta = new();

        private class ClienteDTO
        {
            public int IdCliente { get; set; }
            public string Dni { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string Apellido { get; set; } = string.Empty;

            public string Display => $"{Dni} - {Apellido}, {Nombre}".Trim();
        }

        private class ProductoDTO
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Descripcion { get; set; } = string.Empty;
            public string Categoria { get; set; } = string.Empty;
            public string Talle { get; set; } = string.Empty;
            public int Stock { get; set; }
            public decimal PrecioVenta { get; set; }

            public string DisplayId => $"{IdProducto} - {Nombre} - {Categoria}".Trim();
            public string DisplayNombre => $"{Nombre} - {Categoria} (ID {IdProducto})".Trim();
        }

        public class ItemCarrito
        {
            public int IdProducto { get; set; }
            public string Nombre { get; set; } = string.Empty;
            public string Talle { get; set; } = string.Empty;
            public decimal PrecioUnitario { get; set; }
            public int Cantidad { get; set; }
            public decimal SubTotal => Cantidad * PrecioUnitario;
        }

        private readonly BindingList<ItemCarrito> _carrito = new();
        private List<ClienteDTO> _listaClientes = new();
        private List<ProductoDTO> _listaProductos = new();

        private ClienteDTO? _clienteSeleccionado = null;
        private ProductoDTO? _productoSeleccionado = null;

        public Ventas()
        {
            InitializeComponent();

            dataGridViewVentas.CellClick += DataGridViewVentas_CellClick;
            comboBoxMetodoPago.SelectedIndexChanged += (_, __) => ConfigurarMetodoPago();
            ComboBoxCuotas.SelectedIndexChanged += (_, __) => CalcularTotalVenta();
        }

        private void Ventas_Load(object sender, EventArgs e)
        {
            ConfigurarGrilla();
            CargarMetodosPago();
            CargarClientesDesdeBD();
            CargarProductosDesdeBD();
            InicializarFormulario();

            // Asegura que WinForms no fuerce la selección del primer elemento al terminar de cargar el formulario
            this.BeginInvoke(new Action(LimpiarMetodoPago));
        }

        private void ConfigurarGrilla()
        {
            dataGridViewVentas.AutoGenerateColumns = false;
            CidProducto.DataPropertyName = "IdProducto";
            CNombre.DataPropertyName = "Nombre";
            CTalle.DataPropertyName = "Talle";
            CPrecioUnitario.DataPropertyName = "PrecioUnitario";
            CCantidad.DataPropertyName = "Cantidad";
            CSubTotal.DataPropertyName = "SubTotal";

            CPrecioUnitario.DefaultCellStyle.Format = "N2";
            CSubTotal.DefaultCellStyle.Format = "N2";
            CPrecioUnitario.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CSubTotal.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CCantidad.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            dataGridViewVentas.DataSource = _carrito;

            textBoxNombreCliente.ReadOnly = true;
            textBoxApellidoCliente.ReadOnly = true;
            textBoxDNICliente.ReadOnly = true;
            textBoxNombreProducto.ReadOnly = true;
            textBoxPrecioVenta.ReadOnly = true;
            textBoxStock.ReadOnly = true;
            textBoxImporteTotal.ReadOnly = true;
        }

        private void CargarMetodosPago()
        {
            DataTable dt = _lVenta.ObtenerMetodosPago();
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

            ComboBoxCuotas.Items.Clear();
            ComboBoxCuotas.StartIndex = -1;
            ComboBoxCuotas.SelectedIndex = -1;
            ComboBoxCuotas.SelectedItem = null;
            ComboBoxCuotas.ResetText();
            ComboBoxCuotas.Enabled = false;
        }

        private void InicializarFormulario()
        {
            _carrito.Clear();
            LimpiarCamposCliente();
            LimpiarCamposProducto();
            LimpiarMetodoPago();
            CalcularTotalVenta();
        }

        // ==========================================
        // BUSCADOR DE CLIENTES
        // ==========================================
        private void CargarClientesDesdeBD()
        {
            _listaClientes.Clear();
            DataTable dt = _lVenta.ObtenerClientesParaVenta();

            foreach (DataRow dr in dt.Rows)
            {
                _listaClientes.Add(new ClienteDTO
                {
                    IdCliente = Convert.ToInt32(dr["id_cliente"]),
                    Dni = dr["DNI_cliente"].ToString() ?? "",
                    Nombre = dr["nombre"].ToString() ?? "",
                    Apellido = dr["apellido"].ToString() ?? ""
                });
            }

            var coleccion = new AutoCompleteStringCollection();
            foreach (var cliente in _listaClientes)
            {
                coleccion.Add(cliente.Display);
            }

            textBoxBuscadorCliente.AutoCompleteCustomSource = coleccion;
            textBoxBuscadorCliente.AutoCompleteMode = AutoCompleteMode.Suggest;
            textBoxBuscadorCliente.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        private void TextBoxBuscadorCliente_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ConfirmarSeleccionCliente();
            }
        }

        private void ConfirmarSeleccionCliente()
        {
            string texto = textBoxBuscadorCliente.Text.Trim();
            if (string.IsNullOrEmpty(texto)) return;

            var cliente = _listaClientes.FirstOrDefault(c =>
                c.Display.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                c.Dni.StartsWith(texto, StringComparison.OrdinalIgnoreCase));

            if (cliente != null)
            {
                EstablecerCliente(cliente);
            }
            else
            {
                MessageBox.Show("No se encontró ningún cliente con ese dato.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void EstablecerCliente(ClienteDTO cliente)
        {
            _clienteSeleccionado = cliente;
            textBoxNombreCliente.Text = cliente.Nombre;
            textBoxApellidoCliente.Text = cliente.Apellido;
            textBoxDNICliente.Text = cliente.Dni;
            textBoxBuscadorCliente.Clear();
        }

        private void LimpiarCamposCliente()
        {
            _clienteSeleccionado = null;
            textBoxBuscadorCliente.Clear();
            textBoxNombreCliente.Clear();
            textBoxApellidoCliente.Clear();
            textBoxDNICliente.Clear();
            LimpiarMetodoPago();
        }

        private void ButtonAgregarCliente_Click(object? sender, EventArgs e)
        {
            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formReg = new FormRegistrarCliente())
            {
                formReg.StartPosition = FormStartPosition.CenterScreen;

                if (formReg.ShowDialog(this) == DialogResult.OK)
                {
                    CargarClientesDesdeBD();

                    if (_listaClientes.Count > 0)
                    {
                        var nuevoCliente = _listaClientes.OrderByDescending(c => c.IdCliente).First();
                        EstablecerCliente(nuevoCliente);
                    }
                }
            }

            overlay.Close();
        }

        // ==========================================
        // BUSCADOR DE PRODUCTOS
        // ==========================================
        private void CargarProductosDesdeBD()
        {
            _listaProductos.Clear();
            DataTable dt = _lVenta.ObtenerProductosParaVenta();

            foreach (DataRow dr in dt.Rows)
            {
                _listaProductos.Add(new ProductoDTO
                {
                    IdProducto = Convert.ToInt32(dr["id_producto"]),
                    Nombre = dr["nombre"].ToString() ?? "",
                    Descripcion = dr["descripcion"].ToString() ?? "",
                    Categoria = dr["categoria"].ToString() ?? "",
                    Talle = dr["talla"].ToString() ?? "Único",
                    Stock = Convert.ToInt32(dr["stock"]),
                    PrecioVenta = Convert.ToDecimal(dr["precio_venta"])
                });
            }

            var coleccion = new AutoCompleteStringCollection();
            foreach (var prod in _listaProductos.GroupBy(p => p.Nombre).Select(g => g.First()))
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

            var prodEncontrado = _listaProductos.FirstOrDefault(p =>
                p.DisplayId.Equals(busqueda, StringComparison.OrdinalIgnoreCase) ||
                p.DisplayNombre.Equals(busqueda, StringComparison.OrdinalIgnoreCase) ||
                p.Nombre.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0 ||
                p.Categoria.IndexOf(busqueda, StringComparison.OrdinalIgnoreCase) >= 0);

            if (prodEncontrado != null)
            {
                string nombreProducto = prodEncontrado.Nombre;
                textBoxNombreProducto.Text = nombreProducto;

                var variantes = _listaProductos
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
            else
            {
                MessageBox.Show("No se encontró ningún producto con ese criterio.", "Búsqueda", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ComboBoxTalle_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (comboBoxTalle.SelectedItem == null) return;

            string nombre = textBoxNombreProducto.Text.Trim();
            string talle = comboBoxTalle.SelectedItem.ToString()!;

            _productoSeleccionado = _listaProductos.FirstOrDefault(p =>
                p.Nombre.Equals(nombre, StringComparison.OrdinalIgnoreCase) &&
                p.Talle.Equals(talle, StringComparison.OrdinalIgnoreCase));

            if (_productoSeleccionado != null)
            {
                textBoxPrecioVenta.Text = _productoSeleccionado.PrecioVenta.ToString("N2");

                int enCarrito = _carrito
                    .Where(c => c.IdProducto == _productoSeleccionado.IdProducto)
                    .Sum(c => c.Cantidad);

                int stockDisponible = Math.Max(0, _productoSeleccionado.Stock - enCarrito);
                textBoxStock.Text = stockDisponible.ToString();

                comboBoxCantidad.Items.Clear();
                for (int i = 1; i <= stockDisponible; i++)
                {
                    comboBoxCantidad.Items.Add(i);
                }

                if (comboBoxCantidad.Items.Count > 0)
                    comboBoxCantidad.SelectedIndex = 0;
                else
                    comboBoxCantidad.Text = "0";
            }
        }

        private void LimpiarCamposProducto()
        {
            _productoSeleccionado = null;
            TextBoxBuscadorProducto.Clear();
            textBoxNombreProducto.Clear();
            comboBoxTalle.Items.Clear();
            comboBoxCantidad.Items.Clear();
            textBoxPrecioVenta.Clear();
            textBoxStock.Clear();
        }

        // ==========================================
        // ACCIONES DEL CARRITO
        // ==========================================
        private void ButtonAgregar_Click(object? sender, EventArgs e)
        {
            if (_productoSeleccionado == null || comboBoxCantidad.SelectedItem == null)
            {
                MessageBox.Show("Seleccione un producto, talle y cantidad válida.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int cantidad = Convert.ToInt32(comboBoxCantidad.SelectedItem);
            if (cantidad <= 0) return;

            var itemExistente = _carrito.FirstOrDefault(c => c.IdProducto == _productoSeleccionado.IdProducto);
            if (itemExistente != null)
            {
                itemExistente.Cantidad += cantidad;
                _carrito.ResetBindings();
            }
            else
            {
                _carrito.Add(new ItemCarrito
                {
                    IdProducto = _productoSeleccionado.IdProducto,
                    Nombre = _productoSeleccionado.Nombre,
                    Talle = _productoSeleccionado.Talle,
                    PrecioUnitario = _productoSeleccionado.PrecioVenta,
                    Cantidad = cantidad
                });
            }

            CalcularTotalVenta();
            LimpiarCamposProducto();
            TextBoxBuscadorProducto.Focus();
        }

        private void DataGridViewVentas_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < _carrito.Count && dataGridViewVentas.Columns[e.ColumnIndex].Name == "CQuitar")
            {
                _carrito.RemoveAt(e.RowIndex);
                CalcularTotalVenta();
            }
        }

        // ==========================================
        // CONFIGURACIÓN DE PAGO Y TOTALES
        // ==========================================
        private static bool EsMetodoCredito(string metodo)
        {
            string normalizado = metodo.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalizado)
            {
                var categoria = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (categoria != System.Globalization.UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }
            return string.Equals(sb.ToString(), "Credito", StringComparison.OrdinalIgnoreCase);
        }

        private void ConfigurarMetodoPago()
        {
            if (comboBoxMetodoPago.SelectedIndex == -1 || comboBoxMetodoPago.SelectedItem == null)
            {
                ComboBoxCuotas.Items.Clear();
                ComboBoxCuotas.StartIndex = -1;
                ComboBoxCuotas.SelectedIndex = -1;
                ComboBoxCuotas.SelectedItem = null;
                ComboBoxCuotas.ResetText();
                ComboBoxCuotas.Enabled = false;
                CalcularTotalVenta();
                return;
            }

            string metodo = comboBoxMetodoPago.Text;

            ComboBoxCuotas.Items.Clear();

            if (EsMetodoCredito(metodo))
            {
                ComboBoxCuotas.Enabled = true;
                ComboBoxCuotas.Items.AddRange(new object[] { "1 Cuota", "3 Cuotas (+10%)", "6 Cuotas (+20%)", "12 Cuotas (+40%)" });
                ComboBoxCuotas.SelectedIndex = 0;
            }
            else
            {
                ComboBoxCuotas.Items.Add("1 Cuota");
                ComboBoxCuotas.SelectedIndex = 0;
                ComboBoxCuotas.Enabled = false;
            }

            CalcularTotalVenta();
        }

        private decimal ObtenerMontoRecargo(decimal subtotal)
        {
            decimal porcentaje = 0m;
            if (comboBoxMetodoPago.SelectedIndex != -1 && EsMetodoCredito(comboBoxMetodoPago.Text))
            {
                string seleccion = ComboBoxCuotas.SelectedItem?.ToString() ?? "";
                if (seleccion.Contains("3")) porcentaje = 0.10m;
                else if (seleccion.Contains("6")) porcentaje = 0.20m;
                else if (seleccion.Contains("12")) porcentaje = 0.40m;
            }
            return subtotal * porcentaje;
        }

        private decimal CalcularTotalVenta()
        {
            decimal subtotal = _carrito.Sum(i => i.SubTotal);
            decimal recargo = ObtenerMontoRecargo(subtotal);
            decimal total = subtotal + recargo;

            textBoxImporteTotal.Text = $"$ {total:N2}";
            return total;
        }

        private void ButtonCancelar_Click(object? sender, EventArgs e)
        {
            var resultado = MessageBox.Show("¿Está seguro de cancelar la venta actual y limpiar todos los datos?",
                "Confirmar Cancelación", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (resultado == DialogResult.Yes)
            {
                InicializarFormulario();
            }
        }

        private void ButtonFinalizar_Click(object? sender, EventArgs e)
        {
            if (_carrito.Count == 0)
            {
                MessageBox.Show("El detalle de venta está vacío. Agregue productos antes de continuar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBoxMetodoPago.SelectedIndex == -1 || comboBoxMetodoPago.SelectedValue == null)
            {
                MessageBox.Show("Seleccione un método de pago para continuar.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                comboBoxMetodoPago.Focus();
                return;
            }

            int idMetodo = Convert.ToInt32(comboBoxMetodoPago.SelectedValue);
            string textoCuotas = ComboBoxCuotas.SelectedItem?.ToString() ?? "1";
            int cuotas = int.TryParse(textoCuotas.Split(' ')[0], out int c) ? c : 1;

            decimal subtotal = _carrito.Sum(i => i.SubTotal);
            decimal recargo = ObtenerMontoRecargo(subtotal);
            decimal totalFinal = subtotal + recargo;
            int idUsuarioVendedor = SesionActual.Usuario?.Id ?? 1;

            var venta = new EVenta
            {
                IdUsuario = idUsuarioVendedor,
                IdCliente = _clienteSeleccionado?.IdCliente,
                IdMetodoPago = idMetodo,
                Cuotas = cuotas,
                Recargo = recargo,
                Total = totalFinal
            };

            foreach (var item in _carrito)
            {
                venta.Detalles.Add(new EDetalleVenta
                {
                    IdProducto = item.IdProducto,
                    Cantidad = item.Cantidad,
                    PrecioUnitario = item.PrecioUnitario
                });
            }

            if (_lVenta.RegistrarVenta(venta, out int idVenta, out string mensajeError))
            {
                string textoTicket = GenerarTextoTicket(idVenta, totalFinal);

                using (Form overlay = ModalHelper.CrearOverlay(this))
                {
                    overlay.Show();
                    overlay.BringToFront();

                    using (var frmTicket = new FormTicket(textoTicket))
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
                MessageBox.Show(mensajeError, "Error en Venta", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GenerarTextoTicket(int idVenta, decimal totalFinal)
        {
            string clienteNombre = _clienteSeleccionado != null
                ? $"{_clienteSeleccionado.Nombre} {_clienteSeleccionado.Apellido}"
                : "Consumidor Final";

            string clienteDni = _clienteSeleccionado?.Dni ?? "00000000";
            string vendedor = SesionActual.Usuario != null
                ? $"{SesionActual.Usuario.Nombre} {SesionActual.Usuario.Apellido}"
                : "Cajero 1";

            var sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("             TIENDA DE ROPA             ");
            sb.AppendLine("         COMPROBANTE DE VENTA           ");
            sb.AppendLine("========================================");
            sb.AppendLine($" Venta N°: #{idVenta:D6}");
            sb.AppendLine($" Fecha:    {DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($" Vendedor: {vendedor}");
            sb.AppendLine("----------------------------------------");
            sb.AppendLine($" Cliente:  {clienteNombre}");
            sb.AppendLine($" DNI:      {clienteDni}");
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
            sb.AppendLine($" Metodo Pago: {comboBoxMetodoPago.Text} ({ComboBoxCuotas.SelectedItem})");
            sb.AppendLine($" TOTAL:                    ${totalFinal,8:N2}");
            sb.AppendLine("========================================");
            sb.AppendLine("        ¡GRACIAS POR SU COMPRA!         ");
            sb.AppendLine("========================================");

            return sb.ToString();
        }
    }
}