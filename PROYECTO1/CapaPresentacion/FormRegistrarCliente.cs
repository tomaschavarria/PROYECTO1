using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class FormRegistrarCliente : Form
    {
        private readonly LCliente _lCliente = new();
        private int? _idClienteOriginal = null;

        // Colores para el estado de validación
        private readonly Color _colorBordeNormal = Color.FromArgb(213, 218, 223);
        private readonly Color _colorBordeError = Color.FromArgb(230, 57, 70);

        public FormRegistrarCliente()
        {
            InitializeComponent();
            ConfigurarEventos();
            fecha.Value = DateTime.Today;
            ModalHelper.ConfigurarTransparencia(this);
        }

        public FormRegistrarCliente(int dni)
        {
            InitializeComponent();
            ConfigurarEventos();
            CargarDatosCliente(dni);
            ModalHelper.ConfigurarTransparencia(this);
        }

        public FormRegistrarCliente(string dni) : this(Convert.ToInt32(dni))
        {
        }

        private void ConfigurarEventos()
        {
            buttonCancelar.Click += (_, __) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            // Quita el borde rojo en tiempo real mientras el usuario escribe
            textBoxNombre.TextChanged += (_, __) => RestaurarBorde(textBoxNombre);
            textBoxApellido.TextChanged += (_, __) => RestaurarBorde(textBoxApellido);
            textBoxDNI.TextChanged += (_, __) => RestaurarBorde(textBoxDNI);
            textBoxTelefono.TextChanged += (_, __) => RestaurarBorde(textBoxTelefono);
            textBoxDescripcion.TextChanged += (_, __) => RestaurarBorde(textBoxDescripcion);
            textBOxCorreo.TextChanged += (_, __) => RestaurarBorde(textBOxCorreo);
        }

        private void RestaurarBorde(Guna2TextBox campo)
        {
            if (campo.BorderColor == _colorBordeError)
            {
                campo.BorderColor = _colorBordeNormal;
                campo.BorderThickness = 1;
            }
        }

        private void MarcarCampoInvalido(Guna2TextBox campo)
        {
            campo.BorderColor = _colorBordeError;
            campo.BorderThickness = 2;
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

        private void CargarDatosCliente(int dni)
        {
            var cliente = _lCliente.ObtenerPorDni(dni);
            if (cliente == null)
            {
                MessageBox.Show("No se encontró el cliente solicitado. Puede que ya no exista.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                this.DialogResult = DialogResult.Cancel;
                Load += (_, __) => Close();
                return;
            }

            _idClienteOriginal = cliente.IdCliente;
            textBoxNombre.Text = cliente.Nombre;
            textBoxApellido.Text = cliente.Apellido;
            textBoxDNI.Text = cliente.Dni.ToString();
            textBoxTelefono.Text = cliente.Telefono ?? string.Empty;
            textBoxDescripcion.Text = cliente.Direccion ?? string.Empty;
            textBOxCorreo.Text = cliente.Correo ?? string.Empty;

            if (cliente.Nacimiento.HasValue)
                fecha.Value = cliente.Nacimiento.Value;

            btnTituloCliente2.Text = "EDITAR CLIENTE";
        }

        private void buttonGuardar_Click(object sender, EventArgs e)
        {
            Control? primerInvalido = null;
            var campos = new List<Guna2TextBox>
            {
                textBoxNombre,
                textBoxApellido,
                textBoxDNI,
                textBoxTelefono,
                textBoxDescripcion,
                textBOxCorreo
            };

            // 1. Evalúa todos los campos y resalta visualmente los vacíos
            foreach (var campo in campos)
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

            if (primerInvalido != null)
            {
                primerInvalido.Focus();
                MessageBox.Show("Por favor, complete todos los campos obligatorios resaltados en rojo.",
                                "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Validación de formato de DNI
            if (!int.TryParse(textBoxDNI.Text.Trim(), out int dniIngresado) ||
                dniIngresado < 1000000 ||
                dniIngresado > 99999999)
            {
                MarcarCampoInvalido(textBoxDNI);
                textBoxDNI.Focus();
                textBoxDNI.SelectAll();

                MessageBox.Show(
                    "El DNI debe contener entre 7 y 8 dígitos numéricos.",
                    "DNI Inválido",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            // 3. Validación de formato de correo electrónico
            string correo = textBOxCorreo.Text.Trim();
            if (!EsEmailValido(correo))
            {
                MarcarCampoInvalido(textBOxCorreo);
                textBOxCorreo.Focus();
                textBOxCorreo.SelectAll();
                MessageBox.Show("El correo electrónico ingresado no tiene un formato válido (ejemplo: cliente@correo.com).",
                                "Correo Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var cliente = new ECliente
            {
                IdCliente = _idClienteOriginal ?? 0,
                Nombre = textBoxNombre.Text.Trim(),
                Apellido = textBoxApellido.Text.Trim(),
                Dni = dniIngresado,
                Telefono = textBoxTelefono.Text.Trim(),
                Direccion = textBoxDescripcion.Text.Trim(),
                Correo = correo,
                Nacimiento = fecha.Value.Date,
                Activo = true
            };

            bool resultado;
            string mensajeError;

            if (_idClienteOriginal.HasValue)
            {
                resultado = _lCliente.Modificar(cliente, out mensajeError);
            }
            else
            {
                resultado = _lCliente.Registrar(cliente, out mensajeError);
            }

            if (resultado)
            {
                MessageBox.Show(_idClienteOriginal.HasValue ? "Cliente actualizado correctamente." : "Cliente insertado correctamente.",
                                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                string msgLower = (mensajeError ?? "").ToLower();

                if (msgLower.Contains("correo") || msgLower.Contains("email"))
                {
                    MarcarCampoInvalido(textBOxCorreo);
                    textBOxCorreo.Focus();
                    textBOxCorreo.SelectAll();
                }
                else if (msgLower.Contains("dni"))
                {
                    MarcarCampoInvalido(textBoxDNI);
                    textBoxDNI.Focus();
                    textBoxDNI.SelectAll();
                }

                MessageBox.Show(mensajeError, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void buttonLimpiar_Click(object sender, EventArgs e)
        {
            var campos = new[] { textBoxNombre, textBoxApellido, textBoxDNI, textBoxTelefono, textBoxDescripcion, textBOxCorreo };
            foreach (var c in campos)
            {
                c.Clear();
                RestaurarBorde(c);
            }
            fecha.Value = DateTime.Today;
            textBoxNombre.Focus();
        }

        private void SoloLetras_KeyPress(object sender, KeyPressEventArgs e)
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