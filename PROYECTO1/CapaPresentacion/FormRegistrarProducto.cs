using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class FormRegistrarProducto : Form
    {
        private readonly LProducto _lProducto = new();
        private int? _idProductoOriginal = null;

        private class ProveedorItem
        {
            public int IdProveedor { get; set; }
            public string Cuit { get; set; } = string.Empty;
            public string RazonSocial { get; set; } = string.Empty;
            public string Display => $"{Cuit} - {RazonSocial}".Trim();
            public string DisplayInverso => $"{RazonSocial} - {Cuit}".Trim();
        }

        private readonly List<ProveedorItem> _listaProveedores = new();
        private int? _idProveedorSeleccionado = null;

        private readonly Color _colorBordeNormal = Color.FromArgb(213, 218, 223);
        private readonly Color _colorBordeError = Color.FromArgb(230, 57, 70);

        public FormRegistrarProducto()
        {
            InitializeComponent();
            ConfigurarStockInicial();
            CargarCategorias();
            ConfigurarBuscadorProveedor();
            ConfigurarBotonNuevaCategoria();
            ConfigurarEventos();
            ModalHelper.ConfigurarTransparencia(this);
        }

        public FormRegistrarProducto(int idProducto)
        {
            InitializeComponent();
            ConfigurarStockInicial();
            CargarCategorias();
            ConfigurarBuscadorProveedor();
            ConfigurarBotonNuevaCategoria();
            ConfigurarEventos();
            _idProductoOriginal = idProducto;
            CargarDatosProducto(idProducto);
            ModalHelper.ConfigurarTransparencia(this);
        }

        private void ConfigurarStockInicial()
        {
            textBoxStock.ReadOnly = true;
            textBoxStock.Text = "0";
        }

        private void ConfigurarBotonNuevaCategoria()
        {
            comboBoxCantidad.Width = 142;

            var btnNuevaCat = new Guna2Button
            {
                Text = "+",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                FillColor = Color.FromArgb(109, 124, 163),
                ForeColor = Color.White,
                Location = new Point(comboBoxCantidad.Right + 8, comboBoxCantidad.Top),
                Size = new Size(36, 36),
                BorderRadius = 18,
                Cursor = Cursors.Hand,
                Animated = true,
                BackColor = Color.FromArgb(43, 50, 68),
                UseTransparentBackground = false,
                BorderThickness = 0
            };

            btnNuevaCat.HoverState.FillColor = Color.FromArgb(130, 145, 185);
            btnNuevaCat.Click += (_, __) => AgregarNuevaCategoria();

            guna2Panel4.Controls.Add(btnNuevaCat);
            btnNuevaCat.BringToFront();
        }

        private void AgregarNuevaCategoria()
        {
            string? nuevaCat = SolicitarNombreCategoriaModal();
            if (string.IsNullOrWhiteSpace(nuevaCat)) return;

            if (_lProducto.RegistrarCategoria(nuevaCat, out int idGenerado, out string error))
            {
                CargarCategorias();
                comboBoxCantidad.SelectedValue = idGenerado;
                RestaurarBordeCombo(comboBoxCantidad);
            }
            else
            {
                MessageBox.Show(error, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private string? SolicitarNombreCategoriaModal()
        {
            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using var formInput = new Form
            {
                Size = new Size(430, 260),
                FormBorderStyle = FormBorderStyle.None,
                StartPosition = FormStartPosition.CenterParent,
                BackColor = Color.FromArgb(25, 29, 36),
                TransparencyKey = Color.FromArgb(25, 29, 36),
                ShowInTaskbar = false
            };

            ModalHelper.ConfigurarTransparencia(formInput);

            var panelCard = new Guna2Panel
            {
                Location = new Point(15, 30),
                Size = new Size(400, 215),
                BorderRadius = 25,
                FillColor = Color.FromArgb(43, 50, 68),
                BorderThickness = 0
            };

            var btnTitulo = new Guna2Button
            {
                Text = "NUEVA CATEGORÍA",
                Font = new Font("Arial Black", 10.5F, FontStyle.Bold),
                ForeColor = Color.White,
                FillColor = Color.FromArgb(43, 50, 68),
                BorderColor = Color.FromArgb(43, 50, 68),
                BorderThickness = 1,
                AutoRoundedCorners = true,
                Size = new Size(250, 40),
                Location = new Point((formInput.Width - 250) / 2, 10),
                BackColor = Color.Transparent,
                UseTransparentBackground = true,
                Cursor = Cursors.Default
            };
            btnTitulo.HoverState.FillColor = Color.FromArgb(43, 50, 68);
            btnTitulo.HoverState.ForeColor = Color.White;

            var lblPrompt = new Label
            {
                Text = "Nombre de la categoría",
                Font = new Font("Segoe UI", 9.2F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Location = new Point(35, 42),
                AutoSize = true
            };

            var txtNombre = new Guna2TextBox
            {
                AutoRoundedCorners = true,
                FillColor = Color.White,
                BorderThickness = 0,
                ForeColor = Color.Black,
                Font = new Font("Segoe UI", 9.5F),
                PlaceholderText = "Ej. Camperas, Buzos...",
                PlaceholderForeColor = Color.Gray,
                Location = new Point(35, 68),
                Size = new Size(330, 36),
                BackColor = Color.Transparent
            };

            var btnGuardarModal = new Guna2Button
            {
                Text = "GUARDAR",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FillColor = SystemColors.ActiveCaption,
                ForeColor = SystemColors.Highlight,
                BorderRadius = 16,
                Size = new Size(130, 42),
                Location = new Point(55, 135),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            var btnCancelarModal = new Guna2Button
            {
                Text = "CANCELAR",
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                FillColor = Color.FromArgb(255, 192, 192),
                ForeColor = Color.FromArgb(230, 57, 70),
                BorderRadius = 16,
                Size = new Size(130, 42),
                Location = new Point(215, 135),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };

            btnGuardarModal.Click += (_, __) =>
            {
                if (string.IsNullOrWhiteSpace(txtNombre.Text))
                {
                    MessageBox.Show("El nombre de la categoría no puede estar vacío.", "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    txtNombre.Focus();
                    return;
                }
                formInput.DialogResult = DialogResult.OK;
            };

            btnCancelarModal.Click += (_, __) => formInput.DialogResult = DialogResult.Cancel;

            panelCard.Controls.Add(lblPrompt);
            panelCard.Controls.Add(txtNombre);
            panelCard.Controls.Add(btnGuardarModal);
            panelCard.Controls.Add(btnCancelarModal);

            formInput.Controls.Add(btnTitulo);
            formInput.Controls.Add(panelCard);
            btnTitulo.BringToFront();

            formInput.AcceptButton = btnGuardarModal;
            formInput.CancelButton = btnCancelarModal;

            string? resultado = null;
            if (formInput.ShowDialog(this) == DialogResult.OK)
            {
                resultado = txtNombre.Text.Trim();
            }

            overlay.Close();
            return resultado;
        }

        private void ConfigurarEventos()
        {
            buttonCancelar.Click += (_, __) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            textBoxProveedor.KeyDown += TextBoxProveedor_KeyDown;
            textBoxProveedor.Leave += (_, __) => ValidarYFijarProveedor();

            textBoxNombre.TextChanged += (_, __) => RestaurarBorde(textBoxNombre);
            textBoxDescripcion.TextChanged += (_, __) => RestaurarBorde(textBoxDescripcion);
            textBoxPrecioVenta.TextChanged += (_, __) => RestaurarBorde(textBoxPrecioVenta);
            textBoxCosto.TextChanged += (_, __) => RestaurarBorde(textBoxCosto);
            textBoxTalle.TextChanged += (_, __) => RestaurarBorde(textBoxTalle);
            textBoxProveedor.TextChanged += (_, __) => RestaurarBorde(textBoxProveedor);
            comboBoxCantidad.SelectedIndexChanged += (_, __) => RestaurarBordeCombo(comboBoxCantidad);
        }

        private void RestaurarBorde(Guna2TextBox campo)
        {
            if (campo.BorderColor == _colorBordeError)
            {
                campo.BorderColor = _colorBordeNormal;
                campo.BorderThickness = 1;
            }
        }

        private void RestaurarBordeCombo(Guna2ComboBox combo)
        {
            if (combo.BorderColor == _colorBordeError)
            {
                combo.BorderColor = _colorBordeNormal;
                combo.BorderThickness = 1;
            }
        }

        private void MarcarCampoInvalido(Guna2TextBox campo)
        {
            campo.BorderColor = _colorBordeError;
            campo.BorderThickness = 2;
        }

        private void MarcarComboInvalido(Guna2ComboBox combo)
        {
            combo.BorderColor = _colorBordeError;
            combo.BorderThickness = 2;
        }

        private void CargarCategorias()
        {
            comboBoxCantidad.DataSource = _lProducto.ObtenerCategorias();
            comboBoxCantidad.DisplayMember = "nombre";
            comboBoxCantidad.ValueMember = "id_categoria";
            comboBoxCantidad.SelectedIndex = -1;
        }

        private void ConfigurarBuscadorProveedor()
        {
            _listaProveedores.Clear();
            DataTable dt = _lProducto.ObtenerProveedores();
            var coleccion = new AutoCompleteStringCollection();

            foreach (DataRow dr in dt.Rows)
            {
                var item = new ProveedorItem
                {
                    IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
                    Cuit = dr["cuit"] != DBNull.Value ? dr["cuit"].ToString()! : string.Empty,
                    RazonSocial = dr["razon_social"].ToString() ?? string.Empty
                };

                _listaProveedores.Add(item);

                coleccion.Add(item.Display);
                if (!item.DisplayInverso.Equals(item.Display, StringComparison.OrdinalIgnoreCase))
                    coleccion.Add(item.DisplayInverso);
            }

            textBoxProveedor.AutoCompleteCustomSource = coleccion;
            textBoxProveedor.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            textBoxProveedor.AutoCompleteSource = AutoCompleteSource.CustomSource;
        }

        private void TextBoxProveedor_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ValidarYFijarProveedor();
            }
        }

        private void ValidarYFijarProveedor()
        {
            string texto = textBoxProveedor.Text.Trim();
            if (string.IsNullOrWhiteSpace(texto))
            {
                _idProveedorSeleccionado = null;
                return;
            }

            var encontrado = _listaProveedores.FirstOrDefault(p =>
                p.Display.Equals(texto, StringComparison.OrdinalIgnoreCase) ||
                p.DisplayInverso.Equals(texto, StringComparison.OrdinalIgnoreCase) ||
                p.RazonSocial.Equals(texto, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrEmpty(p.Cuit) && p.Cuit.Equals(texto, StringComparison.OrdinalIgnoreCase)));

            if (encontrado != null)
            {
                _idProveedorSeleccionado = encontrado.IdProveedor;
                textBoxProveedor.Text = encontrado.Display;
                RestaurarBorde(textBoxProveedor);
            }
            else
            {
                _idProveedorSeleccionado = null;
            }
        }

        public void PreseleccionarProveedor(int idProveedor)
        {
            var prov = _listaProveedores.FirstOrDefault(p => p.IdProveedor == idProveedor);
            if (prov == null) return;

            _idProveedorSeleccionado = prov.IdProveedor;
            textBoxProveedor.Text = prov.Display;
            textBoxProveedor.ReadOnly = true;
        }

        private void CargarDatosProducto(int id)
        {
            var prod = _lProducto.ObtenerPorId(id);
            if (prod == null) return;

            textBoxNombre.Text = prod.Nombre;
            textBoxDescripcion.Text = prod.Descripcion ?? string.Empty;
            textBoxStock.Text = prod.Stock.ToString();

            var cul = CultureInfo.CurrentCulture;
            textBoxPrecioVenta.Text = prod.PrecioVenta.ToString("N2", cul);
            textBoxCosto.Text = prod.PrecioCosto.ToString("N2", cul);
            textBoxTalle.Text = prod.Talle ?? string.Empty;

            comboBoxCantidad.SelectedValue = prod.IdCategoria;

            var provActual = _listaProveedores.FirstOrDefault(p => p.IdProveedor == prod.IdProveedor);
            if (provActual == null)
            {
                DataTable dtProv = _lProducto.ObtenerProveedorPorId(prod.IdProveedor);
                if (dtProv.Rows.Count > 0)
                {
                    var drProv = dtProv.Rows[0];
                    provActual = new ProveedorItem
                    {
                        IdProveedor = Convert.ToInt32(drProv["id_proveedor"]),
                        Cuit = drProv["cuit"] != DBNull.Value ? drProv["cuit"].ToString()! : string.Empty,
                        RazonSocial = drProv["razon_social"].ToString() ?? string.Empty
                    };
                    _listaProveedores.Add(provActual);
                }
            }

            if (provActual != null)
            {
                _idProveedorSeleccionado = provActual.IdProveedor;
                textBoxProveedor.Text = provActual.Display;
            }

            btnTituloProducto2.Text = "EDITAR PRODUCTO";
        }

        private void ButtonGuardar_Click(object sender, EventArgs e)
        {
            ValidarYFijarProveedor();

            Control? primerInvalido = null;
            var camposTexto = new List<Guna2TextBox>
            {
                textBoxNombre,
                textBoxDescripcion,
                textBoxTalle,
                textBoxPrecioVenta,
                textBoxCosto
            };

            foreach (var campo in camposTexto)
            {
                if (string.IsNullOrWhiteSpace(campo.Text))
                {
                    MarcarCampoInvalido(campo);
                    primerInvalido ??= campo;
                }
                else
                {
                    RestaurarBorde(campo);
                }
            }

            if (comboBoxCantidad.SelectedIndex == -1)
            {
                MarcarComboInvalido(comboBoxCantidad);
                primerInvalido ??= comboBoxCantidad;
            }
            else
            {
                RestaurarBordeCombo(comboBoxCantidad);
            }

            if (!_idProveedorSeleccionado.HasValue || _idProveedorSeleccionado.Value <= 0 || string.IsNullOrWhiteSpace(textBoxProveedor.Text))
            {
                MarcarCampoInvalido(textBoxProveedor);
                primerInvalido ??= textBoxProveedor;
            }
            else
            {
                RestaurarBorde(textBoxProveedor);
            }

            if (primerInvalido != null)
            {
                primerInvalido.Focus();
                MessageBox.Show("Por favor, complete todos los campos obligatorios resaltados en rojo.",
                                "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cul = CultureInfo.CurrentCulture;

            // 1. Validación de parseo numérico y valor > 0 para Precio de Costo
            if (!decimal.TryParse(textBoxCosto.Text.Trim(), NumberStyles.Number, cul, out var pc) || pc <= 0)
            {
                MarcarCampoInvalido(textBoxCosto);
                textBoxCosto.Focus();
                textBoxCosto.SelectAll();
                MessageBox.Show("El precio de costo debe ser un valor numérico mayor a cero.",
                                "Precio Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Validación de parseo numérico y valor > 0 para Precio de Venta
            if (!decimal.TryParse(textBoxPrecioVenta.Text.Trim(), NumberStyles.Number, cul, out var pv) || pv <= 0)
            {
                MarcarCampoInvalido(textBoxPrecioVenta);
                textBoxPrecioVenta.Focus();
                textBoxPrecioVenta.SelectAll();
                MessageBox.Show("El precio de venta debe ser un valor numérico mayor a cero.",
                                "Precio Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Validación de negocio: Precio de Venta estrictamente mayor a Costo
            if (pv <= pc)
            {
                MarcarCampoInvalido(textBoxPrecioVenta);
                textBoxPrecioVenta.Focus();
                textBoxPrecioVenta.SelectAll();
                MessageBox.Show("El precio de venta debe ser mayor al precio de costo.",
                                "Regla de Negocio", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int stockFinal = _idProductoOriginal.HasValue
                ? (int.TryParse(textBoxStock.Text.Trim(), out int s) ? s : 0)
                : 0;

            var producto = new EProducto
            {
                IdProducto = _idProductoOriginal ?? 0,
                IdCategoria = Convert.ToInt32(comboBoxCantidad.SelectedValue),
                IdProveedor = _idProveedorSeleccionado!.Value,
                Nombre = textBoxNombre.Text.Trim(),
                Descripcion = textBoxDescripcion.Text.Trim(),
                Talle = textBoxTalle.Text.Trim(),
                Stock = stockFinal,
                PrecioVenta = pv,
                PrecioCosto = pc,
                Activo = true
            };

            bool resultado;
            string mensajeError;

            if (_idProductoOriginal.HasValue)
            {
                resultado = _lProducto.Modificar(producto, out mensajeError);
            }
            else
            {
                resultado = _lProducto.Registrar(producto, out mensajeError);
            }

            if (resultado)
            {
                MessageBox.Show(_idProductoOriginal.HasValue ? "Producto actualizado correctamente." : "Producto registrado correctamente.",
                                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show(mensajeError, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ButtonLimpiar_Click(object sender, EventArgs e)
        {
            var campos = new[] { textBoxNombre, textBoxDescripcion, textBoxPrecioVenta, textBoxCosto, textBoxTalle, textBoxProveedor };
            foreach (var c in campos)
            {
                c.Clear();
                RestaurarBorde(c);
            }

            RestaurarBordeCombo(comboBoxCantidad);
            comboBoxCantidad.SelectedIndex = -1;
            textBoxStock.Text = "0";
            _idProveedorSeleccionado = null;
        }

        private void SoloEnteros_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }

        private void SoloDecimal_KeyPress(object sender, KeyPressEventArgs e)
        {
            var sep = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar.ToString() != sep)
                e.Handled = true;

            if (e.KeyChar.ToString() == sep && sender is TextBox tb && tb.Text.Contains(sep))
                e.Handled = true;
        }

        private void textBoxNombre_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetter(e.KeyChar) && e.KeyChar != ' ')
            {
                e.Handled = true;
            }
        }
    }
}