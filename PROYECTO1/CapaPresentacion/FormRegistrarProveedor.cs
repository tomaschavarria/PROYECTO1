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
    public partial class FormRegistrarProveedor : Form
    {
        private readonly LProveedor _lProveedor = new();
        private int? _idProveedorOriginal = null;

        private readonly Color _colorBordeNormal = Color.FromArgb(213, 218, 223);
        private readonly Color _colorBordeError = Color.FromArgb(230, 57, 70);

        public FormRegistrarProveedor()
        {
            InitializeComponent();
            ConfigurarEventos();
            ModalHelper.ConfigurarTransparencia(this);
        }

        public FormRegistrarProveedor(int idProveedor)
        {
            InitializeComponent();
            ConfigurarEventos();
            _idProveedorOriginal = idProveedor;
            CargarDatosProveedor(idProveedor);
            ModalHelper.ConfigurarTransparencia(this);
        }

        private void ConfigurarEventos()
        {
            buttonCancelar.Click += (_, __) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            buttonGuardar.Click += ButtonGuardar_Click;
            buttonLimpiar.Click += ButtonLimpiar_Click;

            // Restricción numérica y longitud para CUIT (11 dígitos) y Teléfono
            textBoxNombre.MaxLength = 11; // textBoxNombre representa CUIT
            textBoxNombre.KeyPress += SoloNumeros_KeyPress;
            textBoxTelefono.KeyPress += SoloNumeros_KeyPress;

            // Quita el borde rojo en tiempo real al escribir
            textBoxNombre.TextChanged += (_, __) => RestaurarBorde(textBoxNombre);
            textBoxApellido.TextChanged += (_, __) => RestaurarBorde(textBoxApellido);
            textBoxDescripcion.TextChanged += (_, __) => RestaurarBorde(textBoxDescripcion);
            textBOxCorreo.TextChanged += (_, __) => RestaurarBorde(textBOxCorreo);
            textBoxTelefono.TextChanged += (_, __) => RestaurarBorde(textBoxTelefono);
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

        private void CargarDatosProveedor(int id)
        {
            var prov = _lProveedor.ObtenerPorId(id);
            if (prov == null) return;

            textBoxNombre.Text = prov.Cuit;
            textBoxApellido.Text = prov.RazonSocial;
            textBoxDescripcion.Text = prov.Direccion ?? string.Empty;
            textBOxCorreo.Text = prov.Correo ?? string.Empty;
            textBoxTelefono.Text = prov.Telefono ?? string.Empty;

            btnTituloProveedor.Text = "EDITAR PROVEEDOR";
        }

        private void ButtonGuardar_Click(object? sender, EventArgs e)
        {
            Control? primerVacio = null;
            var campos = new List<Guna2TextBox>
            {
                textBoxNombre,       // CUIT
                textBoxApellido,     // Razón Social
                textBoxDescripcion,  // Dirección
                textBOxCorreo,       // Correo
                textBoxTelefono      // Teléfono
            };

            // 1. Verificación de campos vacíos
            foreach (var campo in campos)
            {
                if (string.IsNullOrWhiteSpace(campo.Text))
                {
                    MarcarCampoInvalido(campo);
                    primerVacio ??= campo;
                }
                else
                {
                    RestaurarBorde(campo);
                }
            }

            if (primerVacio != null)
            {
                primerVacio.Focus();
                MessageBox.Show("Por favor, complete todos los campos obligatorios resaltados en rojo.",
                                "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string cuit = textBoxNombre.Text.Trim();
            string correo = textBOxCorreo.Text.Trim();

            // 2. Validación de longitud y formato de CUIT (debe tener 11 dígitos)
            if (cuit.Length != 11)
            {
                MarcarCampoInvalido(textBoxNombre);
                textBoxNombre.Focus();
                textBoxNombre.SelectAll();
                MessageBox.Show("El CUIT debe contener exactamente 11 dígitos numéricos sin guiones.",
                                "CUIT Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 3. Validación de formato de Correo
            if (!EsEmailValido(correo))
            {
                MarcarCampoInvalido(textBOxCorreo);
                textBOxCorreo.Focus();
                textBOxCorreo.SelectAll();
                MessageBox.Show("El correo electrónico ingresado no tiene un formato válido (ejemplo: contacto@proveedor.com).",
                                "Correo Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var prov = new EProveedor
            {
                IdProveedor = _idProveedorOriginal ?? 0,
                Cuit = cuit,
                RazonSocial = textBoxApellido.Text.Trim(),
                Direccion = textBoxDescripcion.Text.Trim(),
                Correo = correo,
                Telefono = textBoxTelefono.Text.Trim(),
                Activo = true
            };

            bool resultado;
            string mensajeError;

            if (_idProveedorOriginal.HasValue)
            {
                resultado = _lProveedor.Modificar(prov, out mensajeError);
            }
            else
            {
                resultado = _lProveedor.Registrar(prov, out mensajeError);
            }

            if (resultado)
            {
                MessageBox.Show(_idProveedorOriginal.HasValue ? "Proveedor actualizado correctamente." : "Proveedor registrado con éxito.",
                                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                // 4. Si la capa lógica rechaza el registro, enfoca y resalta el campo conflictivo
                string msgLower = (mensajeError ?? "").ToLower();

                if (msgLower.Contains("cuit"))
                {
                    MarcarCampoInvalido(textBoxNombre);
                    textBoxNombre.Focus();
                    textBoxNombre.SelectAll();
                }
                else if (msgLower.Contains("correo") || msgLower.Contains("email"))
                {
                    MarcarCampoInvalido(textBOxCorreo);
                    textBOxCorreo.Focus();
                    textBOxCorreo.SelectAll();
                }
                else if (msgLower.Contains("razon") || msgLower.Contains("razón") || msgLower.Contains("social"))
                {
                    MarcarCampoInvalido(textBoxApellido);
                    textBoxApellido.Focus();
                    textBoxApellido.SelectAll();
                }

                MessageBox.Show(mensajeError, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ButtonLimpiar_Click(object? sender, EventArgs e)
        {
            var campos = new[] { textBoxNombre, textBoxApellido, textBoxDescripcion, textBOxCorreo, textBoxTelefono };
            foreach (var c in campos)
            {
                c.Clear();
                RestaurarBorde(c);
            }
            textBoxNombre.Focus();
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