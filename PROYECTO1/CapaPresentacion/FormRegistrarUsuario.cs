using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class FormRegistrarUsuario : Form
    {
        private readonly LUsuario _lUsuario = new();
        private int? _idUsuarioOriginal = null;
        private string _rutaImagenSeleccionada = string.Empty;
        private string _hashContraseñaOriginal = string.Empty;

        private readonly Color _colorBordeNormal = Color.FromArgb(213, 218, 223);
        private readonly Color _colorBordeError = Color.FromArgb(230, 57, 70);

        public FormRegistrarUsuario()
        {
            InitializeComponent();
            CargarRolesEnCombo();
            ConfigurarEventos();
            dateTimePickerNacimiento.Value = DateTime.Today;
            ModalHelper.ConfigurarTransparencia(this);
        }

        public FormRegistrarUsuario(int idUsuario)
        {
            InitializeComponent();
            CargarRolesEnCombo();
            ConfigurarEventos();
            _idUsuarioOriginal = idUsuario;
            CargarDatosUsuario(idUsuario);
            ModalHelper.ConfigurarTransparencia(this);
        }

        private void ConfigurarEventos()
        {
            buttonCancelar.Click += (_, __) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Limpia el borde rojo en tiempo real al escribir o seleccionar
            textBoxNombre.TextChanged += (_, __) => RestaurarBorde(textBoxNombre);
            textBoxApellido.TextChanged += (_, __) => RestaurarBorde(textBoxApellido);
            textBoxUsuario.TextChanged += (_, __) => RestaurarBorde(textBoxUsuario);
            textBoxContraseña.TextChanged += (_, __) => RestaurarBorde(textBoxContraseña);
            textBoxDNI.TextChanged += (_, __) => RestaurarBorde(textBoxDNI);
            textBoxDirección.TextChanged += (_, __) => RestaurarBorde(textBoxDirección);
            textBoxTelefono.TextChanged += (_, __) => RestaurarBorde(textBoxTelefono);
            textBoxEmail.TextChanged += (_, __) => RestaurarBorde(textBoxEmail);
            comboBoxRol.SelectedIndexChanged += (_, __) => RestaurarBordeCombo(comboBoxRol);
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

        private void MarcarInvalido(Guna2TextBox campo)
        {
            campo.BorderColor = _colorBordeError;
            campo.BorderThickness = 2;
        }

        private void MarcarInvalidoCombo(Guna2ComboBox combo)
        {
            combo.BorderColor = _colorBordeError;
            combo.BorderThickness = 2;
        }

        private bool EsEmailValido(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            try
            {
                return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void CargarRolesEnCombo()
        {
            var dt = _lUsuario.ObtenerRoles();
            comboBoxRol.DisplayMember = "nombre";
            comboBoxRol.ValueMember = "id_rol";
            comboBoxRol.DataSource = dt;
            comboBoxRol.SelectedIndex = -1;
        }

        private void CargarDatosUsuario(int id)
        {
            var usuario = _lUsuario.ObtenerPorId(id);
            if (usuario == null) return;

            textBoxNombre.Text = usuario.Nombre;
            textBoxApellido.Text = usuario.Apellido;
            textBoxUsuario.Text = usuario.NombreUsuario;

            _hashContraseñaOriginal = usuario.Contraseña;
            textBoxContraseña.Text = string.Empty;
            textBoxContraseña.PlaceholderText = "Ingrese nueva clave";

            textBoxDNI.Text = usuario.Dni.ToString();
            textBoxEmail.Text = usuario.Correo ?? string.Empty;
            textBoxDirección.Text = usuario.Direccion ?? string.Empty;
            textBoxTelefono.Text = usuario.Telefono ?? string.Empty;
            dateTimePickerNacimiento.Value = usuario.Nacimiento ?? DateTime.Today;
            comboBoxRol.SelectedValue = usuario.IdRol;

            if (!string.IsNullOrEmpty(usuario.Foto))
            {
                _rutaImagenSeleccionada = usuario.Foto;
                if (File.Exists(_rutaImagenSeleccionada))
                {
                    try { pictureBoxUsuario.Image = Image.FromFile(_rutaImagenSeleccionada); }
                    catch { pictureBoxUsuario.Image = null; }
                }
            }

            btnTituloUsuario2.Text = "EDITAR USUARIO";
        }

        private void buttonCargarFoto_Click(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog
            {
                Title = "Seleccionar imagen de perfil",
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                _rutaImagenSeleccionada = ofd.FileName;
                pictureBoxUsuario.Image = Image.FromFile(_rutaImagenSeleccionada);
            }
        }

        private void buttonGuardar_Click(object sender, EventArgs e)
        {
            Control? primerInvalido = null;
            var camposTexto = new List<Guna2TextBox>
            {
                textBoxNombre,
                textBoxApellido,
                textBoxUsuario,
                textBoxDNI,
                textBoxDirección,
                textBoxTelefono,
                textBoxEmail
            };

            bool esUsuarioNuevo = !_idUsuarioOriginal.HasValue;
            if (esUsuarioNuevo) camposTexto.Add(textBoxContraseña);

            // 1. Validar campos de texto vacíos
            foreach (var campo in camposTexto)
            {
                if (string.IsNullOrWhiteSpace(campo.Text))
                {
                    MarcarInvalido(campo);
                    primerInvalido ??= campo;
                }
                else
                {
                    RestaurarBorde(campo);
                }
            }

            // 2. Validar selección de rol
            if (comboBoxRol.SelectedIndex == -1)
            {
                MarcarInvalidoCombo(comboBoxRol);
                primerInvalido ??= comboBoxRol;
            }
            else
            {
                RestaurarBordeCombo(comboBoxRol);
            }

            if (primerInvalido != null)
            {
                primerInvalido.Focus();
                MessageBox.Show("Por favor, complete todos los campos obligatorios resaltados en rojo.",
                                "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Regla de contraseña
            bool seEscribioContraseñaNueva = !string.IsNullOrEmpty(textBoxContraseña.Text);
            string clave = textBoxContraseña.Text;

            if (esUsuarioNuevo || seEscribioContraseñaNueva)
            {
                bool cumpleLongitud = clave.Length >= 8;
                bool tieneNumero = clave.Any(char.IsDigit);
                bool tieneMayuscula = clave.Any(char.IsUpper);

                if (!cumpleLongitud || !tieneNumero || !tieneMayuscula)
                {
                    MarcarInvalido(textBoxContraseña);
                    textBoxContraseña.Focus();
                    textBoxContraseña.SelectAll();
                    MessageBox.Show("La contraseña no cumple con los requisitos mínimos de seguridad:\n\n" +
                                    "• Mínimo 8 caracteres.\n" +
                                    "• Al menos un número.\n" +
                                    "• Al menos una letra mayúscula.",
                                    "Contraseña Inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            // 4. Validación de formato de DNI
            if (!int.TryParse(textBoxDNI.Text.Trim(), out int dniIngresado) ||
                dniIngresado < 1000000 ||
                dniIngresado > 99999999)
            {
                MarcarInvalido(textBoxDNI);
                textBoxDNI.Focus();
                textBoxDNI.SelectAll();

                MessageBox.Show(
                    "El DNI debe contener entre 7 y 8 dígitos numéricos.",
                    "DNI Inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }
            // 5. Validación de formato de correo electrónico
            string correo = textBoxEmail.Text.Trim();
            if (!EsEmailValido(correo))
            {
                MarcarInvalido(textBoxEmail);
                textBoxEmail.Focus();
                textBoxEmail.SelectAll();
                MessageBox.Show("El correo electrónico ingresado no tiene un formato válido (ejemplo: usuario@correo.com).",
                                "Correo Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var usuario = new EUsuario
            {
                IdUsuario = _idUsuarioOriginal ?? 0,
                Nombre = textBoxNombre.Text.Trim(),
                Apellido = textBoxApellido.Text.Trim(),
                NombreUsuario = textBoxUsuario.Text.Trim(),
                Contraseña = (!esUsuarioNuevo && !seEscribioContraseñaNueva) ? _hashContraseñaOriginal : clave,
                Dni = dniIngresado,
                Direccion = textBoxDirección.Text.Trim(),
                Telefono = textBoxTelefono.Text.Trim(),
                Correo = correo,
                Nacimiento = dateTimePickerNacimiento.Value.Date,
                IdRol = Convert.ToInt32(comboBoxRol.SelectedValue),
                Estado = true,
                Foto = string.IsNullOrEmpty(_rutaImagenSeleccionada) ? null : _rutaImagenSeleccionada
            };

            bool resultado;
            string mensajeError;

            if (_idUsuarioOriginal.HasValue)
            {
                resultado = _lUsuario.Modificar(usuario, out mensajeError);
            }
            else
            {
                resultado = _lUsuario.Registrar(usuario, out mensajeError);
            }

            if (resultado)
            {
                MessageBox.Show(_idUsuarioOriginal.HasValue ? "Usuario actualizado correctamente." : "Usuario insertado correctamente.",
                                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                string msgLower = (mensajeError ?? "").ToLower();

                if (msgLower.Contains("correo") || msgLower.Contains("email"))
                {
                    MarcarInvalido(textBoxEmail);
                    textBoxEmail.Focus();
                    textBoxEmail.SelectAll();
                }
                else if (msgLower.Contains("usuario") || msgLower.Contains("login"))
                {
                    MarcarInvalido(textBoxUsuario);
                    textBoxUsuario.Focus();
                    textBoxUsuario.SelectAll();
                }
                else if (msgLower.Contains("dni"))
                {
                    MarcarInvalido(textBoxDNI);
                    textBoxDNI.Focus();
                    textBoxDNI.SelectAll();
                }

                MessageBox.Show(mensajeError, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonLimpiar_Click(object sender, EventArgs e)
        {
            textBoxContraseña.PlaceholderText = string.Empty;
            _hashContraseñaOriginal = string.Empty;

            var camposTexto = new[] { textBoxNombre, textBoxApellido, textBoxUsuario, textBoxContraseña, textBoxDNI, textBoxEmail, textBoxDirección, textBoxTelefono };
            foreach (var c in camposTexto)
            {
                c.Clear();
                RestaurarBorde(c);
            }

            RestaurarBordeCombo(comboBoxRol);
            comboBoxRol.SelectedIndex = -1;
            dateTimePickerNacimiento.Value = DateTime.Today;
            pictureBoxUsuario.Image = null;
            _rutaImagenSeleccionada = string.Empty;
            textBoxNombre.Focus();
        }

        private void textBoxNombre_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsLetter(e.KeyChar) && e.KeyChar != ' ')
            {
                e.Handled = true;
            }
        }

        private void SoloNumeros_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }
    }
}