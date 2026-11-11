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
        private bool _modoAgregarTalle;
        private string? _talleOriginalEdicion;
        private int? _categoriaOriginalEdicion;
        private readonly HashSet<string> _tallesSeleccionados = new(StringComparer.OrdinalIgnoreCase);
        private readonly FlowLayoutPanel _panelTalles = new();
        private readonly Label _avisoSeleccionCategoria = new();
        private readonly Guna2Button _talleFijoEdicion = new();
        private int? _idProductoBaseAgregarTalle;
        private Guna2Button? _btnNuevaCategoria;

        // Mapeo por nombre: los ID pueden variar entre instalaciones.
        // Para habilitar categorías nuevas, agregar su nombre y talles aquí.
        private static string[] TallesPorCategoria(string categoria)
        {
            switch (categoria.Trim().ToLowerInvariant())
            {
                case "remeras":
                case "buzos":
                case "camperas":
                case "camisas":
                    return new[] { "XS", "S", "M", "L", "XL", "XXL" };
                case "pantalones":
                case "bermudas":
                    return new[] { "38", "40", "42", "44", "46", "48", "50" };
                default:
                    return Array.Empty<string>();
            }
        }

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
            ConfigurarSelectorTalles();
            ConfigurarEventos();
            ActualizarSelectorTalles();
            ModalHelper.ConfigurarTransparencia(this);
        }

        public FormRegistrarProducto(int idProducto) : this(idProducto, false) { }

        // agregarTalle=true: copia los datos del producto indicado y solo permite
        // escoger talles que todavía no existen (incluidos los inactivos).
        public FormRegistrarProducto(int idProducto, bool agregarTalle) : this()
        {
            _modoAgregarTalle = agregarTalle;
            if (agregarTalle) _idProductoBaseAgregarTalle = idProducto;
            if (!agregarTalle)
                _idProductoOriginal = idProducto;

            CargarDatosProducto(idProducto);
            ActualizarSelectorTalles();

            if (agregarTalle)
            {
                // En este modo solo se pueden pulsar los talles nuevos.
                foreach (Control campo in new Control[] { textBoxNombre, textBoxDescripcion,
                    textBoxProveedor, comboBoxCategoria, textBoxCosto, textBoxPrecioVenta,
                    textBoxStock })
                {
                    campo.Enabled = false;
                    campo.TabStop = false;
                }
                textBoxStock.Text = "0";
                btnTituloProducto2.Text = "AGREGAR TALLE";
                buttonLimpiar.Visible = false;
                if (_btnNuevaCategoria != null) _btnNuevaCategoria.Visible = false;
            }
            else
            {
                btnTituloProducto2.Text = "EDITAR PRODUCTO";
                buttonLimpiar.Visible = false;
                if (_btnNuevaCategoria != null) _btnNuevaCategoria.Visible = false;
            }
        }

        private void ConfigurarStockInicial()
        {
            textBoxStock.ReadOnly = true;
            textBoxStock.Text = "0";
        }

        private void ConfigurarSelectorTalles()
        {
            // Alineado con Nombre, Categoría y Descripción (x = 52).
            lblTalle.Text = "Talles";
            lblTalle.Location = new Point(52, 451);

            _panelTalles.Location = new Point(49, 479); // +3 de margen del botón = 52.
            _panelTalles.Size = new Size(guna2Panel4.ClientSize.Width - 66, 44);
            _panelTalles.BackColor = Color.Transparent;
            _panelTalles.WrapContents = false;
            _panelTalles.AutoScroll = true;
            guna2Panel4.Controls.Add(_panelTalles);

            // CORRECCIÓN: Coordenada Y subida a 485 para que quede pegado a "Talles"
            _avisoSeleccionCategoria.AutoSize = true;
            _avisoSeleccionCategoria.Location = new Point(52, 485);
            _avisoSeleccionCategoria.ForeColor = Color.FromArgb(191, 199, 218);
            _avisoSeleccionCategoria.BackColor = Color.Transparent;
            _avisoSeleccionCategoria.Font = new Font("Segoe UI", 9F);
            _avisoSeleccionCategoria.Text = "Seleccioná una categoría para ver los talles disponibles.";
            guna2Panel4.Controls.Add(_avisoSeleccionCategoria);

            // Editar un registro no cambia su talle: es una variante identificada por su ID.
            _talleFijoEdicion.Name = "talleFijoEdicion";
            _talleFijoEdicion.Location = new Point(52, 479);
            _talleFijoEdicion.Size = new Size(72, 36);
            _talleFijoEdicion.BorderRadius = 12;
            _talleFijoEdicion.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            _talleFijoEdicion.Enabled = false;
            _talleFijoEdicion.DisabledState.FillColor = Color.FromArgb(156, 160, 169);
            _talleFijoEdicion.DisabledState.ForeColor = Color.FromArgb(78, 84, 96);
            _talleFijoEdicion.Visible = false;
            guna2Panel4.Controls.Add(_talleFijoEdicion);
        }

        private void ActualizarSelectorTalles(bool limpiarSeleccion = false)
        {
            if (limpiarSeleccion) _tallesSeleccionados.Clear();

            bool edicion = _idProductoOriginal.HasValue && !_modoAgregarTalle;
            _avisoSeleccionCategoria.Visible = !edicion && !_modoAgregarTalle &&
                comboBoxCategoria.SelectedIndex < 0;
            _panelTalles.Visible = !edicion;
            _talleFijoEdicion.Visible = edicion;
            if (edicion)
            {
                _talleFijoEdicion.Text = _talleOriginalEdicion ?? "Único";
                return;
            }

            var talles = TallesPorCategoria(comboBoxCategoria.Text);
            _panelTalles.SuspendLayout();
            _panelTalles.Controls.Clear();

            if (talles.Length == 0)
            {
                _panelTalles.ResumeLayout();
                return;
            }

            HashSet<string> existentes = new(StringComparer.OrdinalIgnoreCase);
            if (_idProveedorSeleccionado is int idProveedor && !string.IsNullOrWhiteSpace(textBoxNombre.Text))
            {
                try
                {
                    existentes = new HashSet<string>(
                        _lProducto.ObtenerTallesRegistrados(textBoxNombre.Text.Trim(), idProveedor),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    _panelTalles.ResumeLayout();
                    MessageBox.Show("No se pudieron consultar los talles registrados: " + ex.Message,
                        "Error de consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            foreach (var talle in talles)
            {
                bool yaExiste = existentes.Contains(talle);
                if (yaExiste) _tallesSeleccionados.Remove(talle);

                var boton = new Guna2Button
                {
                    Text = yaExiste ? talle + " ✓" : talle,
                    Tag = talle,
                    Width = 68,
                    Height = 36,
                    Margin = new Padding(3, 1, 3, 1),
                    BorderRadius = 12,
                    BorderThickness = 1,
                    BorderColor = Color.FromArgb(109, 124, 163),
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = yaExiste ? Cursors.Default : Cursors.Hand,
                    Enabled = !yaExiste
                };
                boton.DisabledState.FillColor = Color.FromArgb(156, 160, 169);
                boton.DisabledState.ForeColor = Color.FromArgb(78, 84, 96);
                boton.Click += (_, __) =>
                {
                    if (!_tallesSeleccionados.Add(talle)) _tallesSeleccionados.Remove(talle);
                    PintarTalle(boton, _tallesSeleccionados.Contains(talle));
                };
                PintarTalle(boton, _tallesSeleccionados.Contains(talle) && !yaExiste);
                _panelTalles.Controls.Add(boton);
            }
            _panelTalles.ResumeLayout();
        }

        // No reconstruir botones cuando un TextBox pierde el foco: si el usuario
        // hace clic en un talle, recrearlos durante Leave perdería ese clic.
        private void RefrescarDisponibilidadTalles()
        {
            if (_modoAgregarTalle || _idProductoOriginal.HasValue) return;
            HashSet<string> existentes = new(StringComparer.OrdinalIgnoreCase);
            if (_idProveedorSeleccionado is int idProveedor && !string.IsNullOrWhiteSpace(textBoxNombre.Text))
            {
                try
                {
                    existentes = new HashSet<string>(
                        _lProducto.ObtenerTallesRegistrados(textBoxNombre.Text.Trim(), idProveedor),
                        StringComparer.OrdinalIgnoreCase);
                }
                catch (Exception ex)
                {
                    // Ante una falla de consulta, no permitir agregar talles a ciegas.
                    foreach (var b in _panelTalles.Controls.OfType<Guna2Button>())
                        b.Enabled = false;
                    MessageBox.Show("No se pudieron consultar los talles registrados: " + ex.Message,
                        "Error de consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }

            foreach (var boton in _panelTalles.Controls.OfType<Guna2Button>())
            {
                string talle = (string)boton.Tag;
                bool yaExiste = existentes.Contains(talle);
                if (yaExiste) _tallesSeleccionados.Remove(talle);
                boton.Enabled = !yaExiste;
                boton.Cursor = yaExiste ? Cursors.Default : Cursors.Hand;
                boton.Text = yaExiste ? talle + " ✓" : talle;
                PintarTalle(boton, _tallesSeleccionados.Contains(talle));
            }
        }

        private static void PintarTalle(Guna2Button boton, bool seleccionado)
        {
            // Claro = disponible; oscuro = elegido. Gris = ya registrado (DisabledState).
            boton.FillColor = seleccionado
                ? Color.FromArgb(58, 65, 84)
                : Color.FromArgb(109, 124, 163);
            boton.HoverState.FillColor = seleccionado
                ? Color.FromArgb(58, 65, 84)
                : Color.FromArgb(125, 140, 177);
            boton.ForeColor = Color.White;
        }

        private void ConfigurarBotonNuevaCategoria()
        {

            var btnNuevaCat = new Guna2Button
            {
                Text = "+",
                Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                FillColor = Color.FromArgb(109, 124, 163),
                ForeColor = Color.White,
                Location = new Point(comboBoxCategoria.Right + 8, comboBoxCategoria.Top),
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
            _btnNuevaCategoria = btnNuevaCat;

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
                comboBoxCategoria.SelectedValue = idGenerado;
                RestaurarBordeCombo(comboBoxCategoria);
                if (TallesPorCategoria(nuevaCat).Length == 0)
                    MessageBox.Show("La categoría fue creada, pero todavía no tiene talles configurados. " +
                        "Para registrar productos de esta categoría, agregá su mapeo en TallesPorCategoria().",
                        "Configuración pendiente", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
            textBoxProveedor.Leave += (_, __) =>
            {
                ValidarYFijarProveedor();
                RefrescarDisponibilidadTalles();
            };

            textBoxNombre.TextChanged += (_, __) => RestaurarBorde(textBoxNombre);
            textBoxDescripcion.TextChanged += (_, __) => RestaurarBorde(textBoxDescripcion);
            textBoxPrecioVenta.TextChanged += (_, __) => RestaurarBorde(textBoxPrecioVenta);
            textBoxCosto.TextChanged += (_, __) => RestaurarBorde(textBoxCosto);
            textBoxProveedor.TextChanged += (_, __) => RestaurarBorde(textBoxProveedor);
            textBoxNombre.Leave += (_, __) => RefrescarDisponibilidadTalles();
            comboBoxCategoria.SelectedIndexChanged += (_, __) =>
            {
                RestaurarBordeCombo(comboBoxCategoria);
                ActualizarSelectorTalles(limpiarSeleccion: true);
            };
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
            comboBoxCategoria.DataSource = _lProducto.ObtenerCategorias();
            comboBoxCategoria.DisplayMember = "nombre";
            comboBoxCategoria.ValueMember = "id_categoria";
            comboBoxCategoria.SelectedIndex = -1;
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
            comboBoxCategoria.SelectedValue = prod.IdCategoria;

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

            if (!_modoAgregarTalle)
            {
                // El talle existente es fijo: nunca se ofrecen talles inexistentes.
                _talleOriginalEdicion = prod.Talle ?? "Único";
                _categoriaOriginalEdicion = prod.IdCategoria;
                _talleFijoEdicion.Text = _talleOriginalEdicion;
            }
        }

        private void ButtonGuardar_Click(object sender, EventArgs e)
        {
            ValidarYFijarProveedor();

            Control? primerInvalido = null;
            var obligatorios = new[] { textBoxNombre, textBoxPrecioVenta, textBoxCosto };
            foreach (var campo in obligatorios)
            {
                if (string.IsNullOrWhiteSpace(campo.Text))
                {
                    MarcarCampoInvalido(campo);
                    primerInvalido ??= campo;
                }
                else RestaurarBorde(campo);
            }

            if (comboBoxCategoria.SelectedIndex < 0)
            {
                MarcarComboInvalido(comboBoxCategoria);
                primerInvalido ??= comboBoxCategoria;
            }
            if (!_idProveedorSeleccionado.HasValue || string.IsNullOrWhiteSpace(textBoxProveedor.Text))
            {
                MarcarCampoInvalido(textBoxProveedor);
                primerInvalido ??= textBoxProveedor;
            }
            if (primerInvalido != null)
            {
                primerInvalido.Focus();
                MessageBox.Show("Completá los campos obligatorios resaltados.",
                    "Campos incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string[] permitidos = TallesPorCategoria(comboBoxCategoria.Text);
            bool edicion = _idProductoOriginal.HasValue && !_modoAgregarTalle;
            var talles = edicion
                ? new List<string> { _talleOriginalEdicion ?? string.Empty }
                : _tallesSeleccionados.OrderBy(t => Array.IndexOf(permitidos, t)).ToList();

            if (talles.Count == 0 || talles.Any(string.IsNullOrWhiteSpace) ||
                (!edicion && permitidos.Length == 0))
            {
                MessageBox.Show(permitidos.Length == 0
                        ? "La categoría no tiene talles configurados."
                        : "Seleccioná al menos un talle.",
                    "Talle requerido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!edicion && talles.Any(t => !permitidos.Contains(t, StringComparer.OrdinalIgnoreCase)))
            {
                MessageBox.Show("El talle seleccionado no corresponde a la categoría.",
                    "Talle inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // Los talles históricos pueden conservarse en su categoría original,
            // pero no trasladarse a una categoría que no los admite.
            int categoriaElegida = Convert.ToInt32(comboBoxCategoria.SelectedValue);
            if (edicion && categoriaElegida != _categoriaOriginalEdicion &&
                !permitidos.Contains(_talleOriginalEdicion ?? "", StringComparer.OrdinalIgnoreCase))
            {
                MessageBox.Show("El talle del producto no corresponde a la nueva categoría.",
                    "Categoría incompatible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cul = CultureInfo.CurrentCulture;
            if (!decimal.TryParse(textBoxCosto.Text.Trim(), NumberStyles.Number, cul, out var pc) || pc <= 0)
            {
                MarcarCampoInvalido(textBoxCosto);
                textBoxCosto.Focus();
                MessageBox.Show("El costo debe ser un número mayor a cero.",
                    "Precio inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!decimal.TryParse(textBoxPrecioVenta.Text.Trim(), NumberStyles.Number, cul, out var pv) || pv <= pc)
            {
                MarcarCampoInvalido(textBoxPrecioVenta);
                textBoxPrecioVenta.Focus();
                MessageBox.Show("El precio de venta debe ser mayor al costo.",
                    "Precio inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int stock = edicion && int.TryParse(textBoxStock.Text, out int actual) ? actual : 0;
            var productos = talles.Select(talle => new EProducto
            {
                IdProducto = edicion ? _idProductoOriginal!.Value : 0,
                IdCategoria = Convert.ToInt32(comboBoxCategoria.SelectedValue),
                IdProveedor = _idProveedorSeleccionado!.Value,
                Nombre = textBoxNombre.Text.Trim(),
                Descripcion = string.IsNullOrWhiteSpace(textBoxDescripcion.Text) ? null : textBoxDescripcion.Text.Trim(),
                Talle = talle,
                Stock = stock,
                PrecioCosto = pc,
                PrecioVenta = pv,
                Activo = true
            }).ToList();

            // Se confirma explícitamente si los cambios se propagan a talles existentes.
            bool aplicarAOtrosTalles = false;
            if (edicion)
            {
                List<string> otrosTalles;
                try
                {
                    otrosTalles = _lProducto.ObtenerOtrosTalles(_idProductoOriginal!.Value);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("No se pudieron consultar las variantes: " + ex.Message,
                        "Error de consulta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (otrosTalles.Count > 0)
                {
                    var opcion = MessageBox.Show(
                        $"Este producto también existe en los talles: {string.Join(", ", otrosTalles)}.\n\n" +
                        $"¿Aplicar los cambios también a esos talles?\n\n" +
                        $"Sí: todos los talles  |  No: solo talle {_talleOriginalEdicion}",
                        "Aplicar cambios", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                    if (opcion == DialogResult.Cancel) return;
                    aplicarAOtrosTalles = opcion == DialogResult.Yes;

                    if (aplicarAOtrosTalles && categoriaElegida != _categoriaOriginalEdicion &&
                        otrosTalles.Any(t => !permitidos.Contains(t, StringComparer.OrdinalIgnoreCase)))
                    {
                        MessageBox.Show("Hay otros talles que no corresponden a la nueva categoría.",
                            "Categoría incompatible", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }
            }

            buttonGuardar.Enabled = false;
            try
            {
                bool guardado;
                string error;
                if (edicion)
                    guardado = _lProducto.ModificarVariantes(productos[0], aplicarAOtrosTalles, out error);
                else if (_modoAgregarTalle && _idProductoBaseAgregarTalle.HasValue)
                    guardado = _lProducto.RegistrarTallesParaProducto(
                        _idProductoBaseAgregarTalle.Value, talles, out error);
                else
                    guardado = _lProducto.RegistrarVariantes(productos, out error);

                if (!guardado)
                {
                    MessageBox.Show(string.IsNullOrEmpty(error) ? "No se pudo guardar." : error,
                        "No se guardó", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                MessageBox.Show(edicion ? "Producto actualizado correctamente." :
                    productos.Count == 1 ? "Se registró 1 talle correctamente." :
                    $"Se registraron {productos.Count} talles correctamente.",
                    "Guardado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron guardar los datos: " + ex.Message,
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!IsDisposed) buttonGuardar.Enabled = true;
            }
        }

        private void ButtonLimpiar_Click(object sender, EventArgs e)
        {
            if (_idProductoOriginal.HasValue || _modoAgregarTalle) return;

            var campos = new List<Guna2TextBox> { textBoxNombre, textBoxDescripcion, textBoxPrecioVenta, textBoxCosto };
            if (!textBoxProveedor.ReadOnly) campos.Add(textBoxProveedor);
            foreach (var c in campos)
            {
                c.Clear();
                RestaurarBorde(c);
            }
            if (!textBoxProveedor.ReadOnly) _idProveedorSeleccionado = null;
            _tallesSeleccionados.Clear();
            RestaurarBordeCombo(comboBoxCategoria);
            comboBoxCategoria.SelectedIndex = -1;
            textBoxStock.Text = "0";
            ActualizarSelectorTalles();
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

    }
}