using System;
using System.Drawing;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PROYECTO1;
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class InicioSesion : Form
    {
        private readonly LUsuario _lUsuario = new();

        // Colores para el borde de los controles
        private readonly Color _colorBordeNormal = Color.FromArgb(208, 208, 208);
        private readonly Color _colorBordeError = Color.FromArgb(230, 57, 70);

        public InicioSesion()
        {
            InitializeComponent();
            this.AcceptButton = buttonIngresar;
            ConfigurarEventosValidacion();
        }

        private void ConfigurarEventosValidacion()
        {
            // Restaura el color normal apenas el usuario empieza a escribir
            textBoxUsuario.TextChanged += (_, __) => RestaurarBorde(textBoxUsuario);
            textBoxContraseña.TextChanged += (_, __) => RestaurarBorde(textBoxContraseña);
        }

        private void RestaurarBorde(Guna2TextBox campo)
        {
            if (campo.BorderColor == _colorBordeError)
            {
                campo.BorderColor = _colorBordeNormal;
                campo.BorderThickness = 1;
            }
        }

        // Marca el borde del campo en rojo para indicar que es inválido
        private void MarcarInvalido(Guna2TextBox campo)
        {
            campo.BorderColor = _colorBordeError;
            campo.BorderThickness = 2;
        }

        private void buttonIngresar_Click(object? sender, EventArgs e)
        {
            // Variable para almacenar el primer control inválido encontrado
            Control? primerInvalido = null;

            // 1. Validación de usuario vacío
            if (string.IsNullOrWhiteSpace(textBoxUsuario.Text))
            {
                MarcarInvalido(textBoxUsuario);
                primerInvalido ??= textBoxUsuario;
            }
            else
            {
                RestaurarBorde(textBoxUsuario);
            }

            // 2. Validación de contraseña vacía
            if (string.IsNullOrWhiteSpace(textBoxContraseña.Text))
            {
                MarcarInvalido(textBoxContraseña);
                primerInvalido ??= textBoxContraseña;
            }
            else
            {
                RestaurarBorde(textBoxContraseña);
            }

            // Si falta alguno, enfoca el primero y muestra la advertencia
            if (primerInvalido != null)
            {
                primerInvalido.Focus();
                MessageBox.Show("Por favor, complete todos los campos obligatorios resaltados en rojo.",
                                "Campos Incompletos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string usuario = textBoxUsuario.Text.Trim();
            string clave = textBoxContraseña.Text;

            var usuarioLogueado = _lUsuario.IniciarSesion(usuario, clave, out string mensajeError);

            if (usuarioLogueado == null)
            {
                // Resalta ambos campos si las credenciales fallan en la base de datos
                MarcarInvalido(textBoxUsuario);
                MarcarInvalido(textBoxContraseña);
                textBoxContraseña.Clear();
                textBoxContraseña.Focus();

                MessageBox.Show(mensajeError, "Inicio de Sesión", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SesionActual.Usuario = usuarioLogueado;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}