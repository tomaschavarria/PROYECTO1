using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Guna.UI2.WinForms;

namespace PROYECTO1.CapaPresentacion
{
    public partial class Bienvenida : Form
    {
        private Form? formularioActual;

        private Label labelNombreMenu = null!;
        private Label labelRolMenu = null!;
        private Image? _imagenPerfilPorDefecto;
        private readonly ToolTip _toolTip = new();

        // Subbotones dinámicos para el rol Gerente
        private Guna2Button btnSubReporteVentas = null!;
        private Guna2Button btnSubReporteClientes = null!;
        private Guna2Button btnSubReporteVendedores = null!;
        private bool subMenuGerenteVisible = false;

        private readonly Color colorNormal = Color.FromArgb(43, 50, 68);
        private readonly Color colorActivo = Color.FromArgb(109, 124, 163);

        public Bienvenida()
        {
            InitializeComponent();
            btnProveedor.Click += btnProveedor_Click;
            btnCompras.Click += btnCompras_Click; // Conectado correctamente al evento de abrir compras
            buttonLogOut.Click += buttonLogOut_Click;
        }

        private void Bienvenida_Load(object? sender, EventArgs e)
        {
            _imagenPerfilPorDefecto = pictureBoxPerfil.Image;
            _toolTip.SetToolTip(buttonLogOut, "Cerrar Sesión");

            panelMenu.AutoScroll = false;

            buttonLogOut.Location = new Point(155, 81);
            buttonLogOut.Size = new Size(41, 39);
            buttonLogOut.BringToFront();

            CrearEtiquetasMenuLateral();
            CrearBotonesSubMenuGerente();
            CargarDatosUsuarioMenu();
            AplicarPermisosYPantallaInicio();
        }

        private void CrearEtiquetasMenuLateral()
        {
            labelNombreMenu = new Label
            {
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Width = panelMenu.Width - 10,
                Height = 24,
                Left = 5,
                Top = pictureBoxPerfil.Bottom + 8
            };

            labelRolMenu = new Label
            {
                ForeColor = Color.FromArgb(160, 165, 177),
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                TextAlign = ContentAlignment.MiddleCenter,
                Width = panelMenu.Width - 10,
                Height = 20,
                Left = 5,
                Top = labelNombreMenu.Bottom + 1
            };

            panelMenu.Controls.Add(labelNombreMenu);
            panelMenu.Controls.Add(labelRolMenu);
        }

        private void CrearBotonesSubMenuGerente()
        {
            Guna2Button CrearBotonSub(string texto, EventHandler onClick)
            {
                var btn = new Guna2Button
                {
                    Text = texto,
                    Size = new Size(panelMenu.Width - 6, 38),
                    Left = 3,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(205, 215, 230),
                    FillColor = colorNormal,
                    TextAlign = HorizontalAlignment.Left,
                    TextOffset = new Point(25, 0),
                    BorderRadius = 0,
                    BorderThickness = 0,
                    Animated = true,
                    Visible = false,
                    Cursor = Cursors.Hand
                };
                btn.HoverState.FillColor = Color.FromArgb(55, 65, 88);
                btn.HoverState.ForeColor = Color.White;
                btn.Click += onClick;
                panelMenu.Controls.Add(btn);
                return btn;
            }

            btnSubReporteVentas = CrearBotonSub("    ▸  Ventas", (s, e) => AbrirFormulario(new ReporteGerenteVentas(), btnSubReporteVentas));
            btnSubReporteClientes = CrearBotonSub("    ▸  Clientes", (s, e) => AbrirFormulario(new ReporteGerenteClientes(), btnSubReporteClientes));
            btnSubReporteVendedores = CrearBotonSub("    ▸  Vendedores", (s, e) => AbrirFormulario(new ReporteGerenteVendedores(), btnSubReporteVendedores));
        }

        private void CargarDatosUsuarioMenu()
        {
            labelNombreMenu.Text = $"{SesionActual.Usuario.Nombre} {SesionActual.Usuario.Apellido}";

            string rolTexto = "Administrador";
            if (SesionActual.Usuario.IdRol == 2) rolTexto = "Gerente";
            else if (SesionActual.Usuario.IdRol == 3) rolTexto = "Vendedor";
            labelRolMenu.Text = rolTexto;

            string rutaFoto = SesionActual.Usuario.Foto;
            if (!string.IsNullOrEmpty(rutaFoto) && File.Exists(rutaFoto))
            {
                try
                {
                    using var imgTemp = Image.FromFile(rutaFoto);
                    pictureBoxPerfil.Image = new Bitmap(imgTemp);
                }
                catch
                {
                    pictureBoxPerfil.Image = _imagenPerfilPorDefecto;
                }
            }
            else
            {
                pictureBoxPerfil.Image = _imagenPerfilPorDefecto;
            }
        }

        private void AplicarPermisosYPantallaInicio()
        {
            int rol = SesionActual.Usuario.IdRol;

            Guna2Button[] todosLosBotones =
            {
                buttonUsuarios,
                btnCompras,
                btnProveedor,
                buttonVentas,
                buttonProductos,
                buttonClientes,
                buttonReportes,
                btnSubReporteVentas,
                btnSubReporteClientes,
                btnSubReporteVendedores,
                buttonBackUp
            };

            foreach (var b in todosLosBotones)
            {
                if (b != null) b.Visible = false;
            }

            if (rol == 3) // Rol Vendedor
            {
                Guna2Button[] botonesVendedor =
                {
                    buttonVentas,
                    buttonProductos,
                    buttonClientes,
                    buttonReportes
                };
                ApilarMenu(botonesVendedor, altoPrincipal: 110);
                AbrirFormulario(new Ventas(), buttonVentas);
            }
            else if (rol == 2) // Rol Gerente
            {
                subMenuGerenteVisible = true;
                Guna2Button[] botonesGerente =
                {
                    buttonUsuarios,
                    btnProveedor,
                    buttonProductos,
                    buttonClientes,
                    buttonReportes
                };
                ApilarMenu(botonesGerente, altoPrincipal: 105);
                AbrirFormulario(new ReporteGerenteVentas(), btnSubReporteVentas);
            }
            else // Rol Administrador (Tamaño agrandado a 105 px para aprovechar el espacio)
            {
                Guna2Button[] botonesAdmin =
                {
                    buttonUsuarios,
                    btnProveedor,
                    buttonProductos,
                    buttonClientes,
                    btnCompras,
                    buttonReportes,
                    buttonBackUp
                };
                ApilarMenu(botonesAdmin, altoPrincipal: 105);
                AbrirFormulario(new Usuarios(), buttonUsuarios);
            }
        }

        private void ApilarMenu(Guna2Button[] botonesPrincipales, int altoPrincipal)
        {
            int posY = labelRolMenu.Bottom + 10;
            int espaciado = -1;

            buttonUsuarios.Visible = false;
            btnCompras.Visible = false;
            btnProveedor.Visible = false;
            buttonVentas.Visible = false;
            buttonProductos.Visible = false;
            buttonClientes.Visible = false;
            buttonReportes.Visible = false;
            buttonBackUp.Visible = false;
            if (btnSubReporteVentas != null) btnSubReporteVentas.Visible = false;
            if (btnSubReporteClientes != null) btnSubReporteClientes.Visible = false;
            if (btnSubReporteVendedores != null) btnSubReporteVendedores.Visible = false;

            foreach (var boton in botonesPrincipales)
            {
                if (boton == null) continue;

                boton.Visible = true;
                boton.BorderRadius = 0;
                boton.BorderThickness = 0;
                boton.Left = 3;
                boton.Width = panelMenu.Width - 6;
                boton.Height = altoPrincipal;
                boton.Top = posY;
                posY += boton.Height + espaciado;

                if (boton == buttonReportes && SesionActual.Usuario.IdRol == 2 && subMenuGerenteVisible)
                {
                    Guna2Button[] subBotones = { btnSubReporteVentas, btnSubReporteClientes, btnSubReporteVendedores };
                    foreach (var sub in subBotones)
                    {
                        sub.Visible = true;
                        sub.Left = 3;
                        sub.Width = panelMenu.Width - 6;
                        sub.Top = posY;
                        posY += sub.Height + espaciado;
                    }
                }
            }
        }

        private void ResaltarBotonActivo(Guna2Button botonActivo)
        {
            buttonUsuarios.FillColor = colorNormal;
            btnCompras.FillColor = colorNormal;
            btnProveedor.FillColor = colorNormal;
            buttonVentas.FillColor = colorNormal;
            buttonProductos.FillColor = colorNormal;
            buttonClientes.FillColor = colorNormal;
            buttonReportes.FillColor = colorNormal;
            buttonBackUp.FillColor = colorNormal;

            if (btnSubReporteVentas != null) btnSubReporteVentas.FillColor = colorNormal;
            if (btnSubReporteClientes != null) btnSubReporteClientes.FillColor = colorNormal;
            if (btnSubReporteVendedores != null) btnSubReporteVendedores.FillColor = colorNormal;

            botonActivo.FillColor = colorActivo;

            if (botonActivo == btnSubReporteVentas || botonActivo == btnSubReporteClientes || botonActivo == btnSubReporteVendedores)
            {
                buttonReportes.FillColor = Color.FromArgb(65, 75, 100);
            }
        }

        private void AbrirFormulario(Form formHijo, Guna2Button btnAsociado)
        {
            int rol = SesionActual.Usuario.IdRol;
            if (rol == 2)
            {
                bool esReporte = (btnAsociado == buttonReportes || btnAsociado == btnSubReporteVentas || btnAsociado == btnSubReporteClientes || btnAsociado == btnSubReporteVendedores);

                if (subMenuGerenteVisible != esReporte)
                {
                    subMenuGerenteVisible = esReporte;
                    Guna2Button[] botonesGerente = { buttonUsuarios, btnProveedor, buttonProductos, buttonClientes, buttonReportes };
                    ApilarMenu(botonesGerente, altoPrincipal: 105);
                }
            }

            if (formularioActual != null)
            {
                formularioActual.Close();
                formularioActual.Dispose();
            }

            formularioActual = formHijo;

            formHijo.TopLevel = false;
            formHijo.FormBorderStyle = FormBorderStyle.None;
            formHijo.Dock = DockStyle.Fill;
            panelContenido.Controls.Add(formHijo);

            formHijo.Show();
            formHijo.BringToFront();
            formHijo.Focus();
            ResaltarBotonActivo(btnAsociado);
        }

        private void buttonUsuarios_Click(object sender, EventArgs e) => AbrirFormulario(new Usuarios(), buttonUsuarios);
        private void btnCompras_Click(object sender, EventArgs e) => AbrirFormulario(new Compras(), btnCompras); // Conectado al formulario Compras
        private void btnProveedor_Click(object? sender, EventArgs e) => AbrirFormulario(new Proveedor(), btnProveedor);
        private void buttonVentas_Click(object sender, EventArgs e) => AbrirFormulario(new Ventas(), buttonVentas);
        private void buttonProductos_Click(object sender, EventArgs e) => AbrirFormulario(new Productos(), buttonProductos);
        private void buttonClientes_Click(object sender, EventArgs e) => AbrirFormulario(new Clientes(), buttonClientes);
        private void buttonBackUp_Click(object sender, EventArgs e) => AbrirFormulario(new BackUp(), buttonBackUp);

        private void buttonReportes_Click(object sender, EventArgs e)
        {
            int rol = SesionActual.Usuario.IdRol;

            if (rol == 3) // Vendedor
            {
                AbrirFormulario(new ReporteVendedor(), buttonReportes);
            }
            else if (rol == 1) // Administrador
            {
                AbrirFormulario(new ReporteAdministrador(), buttonReportes);
            }
            else // Gerente
            {
                subMenuGerenteVisible = true;
                Guna2Button[] botonesGerente = { buttonUsuarios, btnProveedor, buttonProductos, buttonClientes, buttonReportes };
                ApilarMenu(botonesGerente, altoPrincipal: 105);
                AbrirFormulario(new ReporteGerenteVentas(), btnSubReporteVentas);
            }
        }

        private void buttonLogOut_Click(object? sender, EventArgs e)
        {
            var respuesta = MessageBox.Show(
                "¿Está seguro de que desea cerrar la sesión actual?",
                "Cerrar Sesión",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (respuesta != DialogResult.Yes) return;

            if (formularioActual != null)
            {
                formularioActual.Close();
                formularioActual.Dispose();
                formularioActual = null;
            }

            SesionActual.Usuario = null!;
            this.Hide();

            using (var formLogin = new InicioSesion())
            {
                formLogin.StartPosition = FormStartPosition.CenterScreen;

                if (formLogin.ShowDialog() == DialogResult.OK)
                {
                    CargarDatosUsuarioMenu();
                    AplicarPermisosYPantallaInicio();
                    this.Show();
                }
                else
                {
                    this.Close();
                }
            }
        }
    }
}