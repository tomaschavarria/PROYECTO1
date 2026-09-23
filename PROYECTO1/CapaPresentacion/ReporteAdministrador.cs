using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Guna.UI2.WinForms;
using PROYECTO1;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class ReporteAdministrador : Form
    {
        private readonly LReporte _lReporte = new();
        private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
        private readonly ToolTip _toolTip = new();
        private readonly Font _fontEmoji = new("Segoe UI Emoji", 11F, FontStyle.Regular);

        // Binding para Ventas
        private readonly BindingSource _bsVentas = new();
        private DataTable _dtVentas = null!;

        // Binding para Compras
        private readonly BindingSource _bsCompras = new();
        private DataTable _dtCompras = null!;

        private readonly List<(string Categoria, int Cantidad, float Porcentaje, Color ColorSegmento)> _datosDonut = new();

        private Label lblUsuariosInactivos = null!;
        private Label lblProveedoresInactivos = null!;
        private Label lblClientesInactivos = null!;
        private Label lblProductosInactivos = null!;

        public ReporteAdministrador()
        {
            InitializeComponent();
            this.Load += ReporteAdministrador_Load;

            // Filtros y exportar de Ventas
            textBoxBuscador.TextChanged += (_, __) => AplicarFiltroVentas();
            comboBoxColumna.SelectedIndexChanged += (_, __) => AplicarFiltroVentas();
            btnExportarVentas.Click += BtnExportarVentas_Click;
            btnExportarBajoStock.Click += BtnExportarBajoStock_Click;

            // Filtros y exportar de Compras
            textBoxCompras.TextChanged += (_, __) => AplicarFiltroCompras();
            comboBoxCompras.SelectedIndexChanged += (_, __) => AplicarFiltroCompras();
            btnExportarCompras.Click += BtnExportarCompras_Click;

            panelNProdxCategoria.Paint += PanelNProdxCategoria_Paint;
        }

        private void ReporteAdministrador_Load(object? sender, EventArgs e)
        {
            ConfigurarTarjetasKpi();
            ConfigurarGrillaVentas();
            ConfigurarGrillaBajoStock();
            ConfigurarGrillaCompras();
            ConfigurarBuscadorVentas();
            ConfigurarBuscadorCompras();

            CargarKpis();
            CargarGraficoDonut();
            CargarBarrasProveedores();
            CargarTablaBajoStock();
            CargarTablaVentas();
            CargarTablaCompras();
        }

        #region 1. Tarjetas KPI

        private void ConfigurarTarjetasKpi()
        {
            void InicializarTarjeta(Guna2Panel panel, Label lblImg, Label lblTexto, out Label lblInactivo)
            {
                lblImg.AutoSize = false;
                lblImg.Dock = DockStyle.None;
                lblImg.Text = string.Empty;
                lblImg.Size = new Size(60, 60);
                lblImg.Location = new Point(18, (panel.Height - 60) / 2);
                lblImg.ImageAlign = ContentAlignment.MiddleCenter;
                lblImg.TextAlign = ContentAlignment.MiddleCenter;
                lblImg.BackColor = Color.Transparent;
                lblImg.BringToFront();

                lblTexto.AutoSize = true;
                lblTexto.Font = new Font("Segoe UI", 28F, FontStyle.Bold);
                lblTexto.ForeColor = Color.White;
                lblTexto.BackColor = Color.Transparent;
                lblTexto.BringToFront();

                lblInactivo = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(230, 57, 70),
                    BackColor = Color.Transparent,
                    Visible = false
                };

                panel.Controls.Add(lblInactivo);
                lblInactivo.BringToFront();
            }

            InicializarTarjeta(panelUsuarios, imagenUsuarios, lblUsuarios, out lblUsuariosInactivos);
            InicializarTarjeta(panelProveedores, imagenProveedores, lblProveedores, out lblProveedoresInactivos);
            InicializarTarjeta(panelClientes, imagenClientes, lblClientes, out lblClientesInactivos);
            InicializarTarjeta(panelProductos, imagenProductos, lblProductos, out lblProductosInactivos);
        }

        private void ActualizarPosicionKpi(Guna2Panel panel, Label lblTexto, Label lblInactivo, int total, int inactivos, string descripcion)
        {
            lblTexto.Text = total.ToString("N0", cultura);
            _toolTip.SetToolTip(lblTexto, $"{total} {descripcion} activo(s)");

            if (inactivos > 0)
            {
                lblInactivo.Text = $"▼ {inactivos}";
                lblInactivo.Visible = true;

                int anchoTotal = lblTexto.PreferredWidth + 5 + lblInactivo.PreferredWidth;
                int startX = 85 + Math.Max(0, (panel.Width - 95 - anchoTotal) / 2);

                lblTexto.Location = new Point(startX, (panel.Height - lblTexto.PreferredHeight) / 2);
                lblInactivo.Location = new Point(lblTexto.Right + 5, lblTexto.Top + (lblTexto.PreferredHeight - lblInactivo.PreferredHeight) / 2 + 3);

                _toolTip.SetToolTip(lblInactivo, $"{inactivos} {descripcion} inactivo(s)");
            }
            else
            {
                lblInactivo.Text = string.Empty;
                lblInactivo.Visible = false;

                int startX = 85 + Math.Max(0, (panel.Width - 95 - lblTexto.PreferredWidth) / 2);
                lblTexto.Location = new Point(startX, (panel.Height - lblTexto.PreferredHeight) / 2);
            }
        }

        private void CargarKpis()
        {
            try
            {
                var kpis = _lReporte.ObtenerKpisAdmin();

                ActualizarPosicionKpi(panelUsuarios, lblUsuarios, lblUsuariosInactivos, kpis.TotalUsers, kpis.InactUsers, "usuario(s)");
                ActualizarPosicionKpi(panelProveedores, lblProveedores, lblProveedoresInactivos, kpis.TotalProv, kpis.InactProv, "proveedor(es)");
                ActualizarPosicionKpi(panelClientes, lblClientes, lblClientesInactivos, kpis.TotalCli, kpis.InactCli, "cliente(s)");
                ActualizarPosicionKpi(panelProductos, lblProductos, lblProductosInactivos, kpis.TotalProd, kpis.InactProd, "producto(s)");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar KPIs: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 2. Gráfico Donut

        private void CargarGraficoDonut()
        {
            _datosDonut.Clear();
            try
            {
                DataTable dt = _lReporte.ObtenerProductosPorCategoria();
                if (dt.Rows.Count == 0) return;

                int totalProd = 0;
                var listaTemporal = new List<(string Cat, int Cant)>();

                foreach (DataRow row in dt.Rows)
                {
                    int cant = Convert.ToInt32(row["Cantidad"]);
                    listaTemporal.Add((row["Categoria"].ToString()!, cant));
                    totalProd += cant;
                }

                if (totalProd == 0) return;

                Color[] paleta = new[]
                {
                    Color.FromArgb(109, 124, 163),
                    Color.FromArgb(136, 152, 194),
                    Color.FromArgb(85, 98, 132),
                    Color.FromArgb(162, 175, 212),
                    Color.FromArgb(63, 74, 102),
                    Color.FromArgb(48, 56, 78)
                };

                if (listaTemporal.Count <= 6)
                {
                    for (int i = 0; i < listaTemporal.Count; i++)
                    {
                        float pct = (float)listaTemporal[i].Cant / totalProd * 100f;
                        _datosDonut.Add((listaTemporal[i].Cat, listaTemporal[i].Cant, pct, paleta[i % paleta.Length]));
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float pct = (float)listaTemporal[i].Cant / totalProd * 100f;
                        _datosDonut.Add((listaTemporal[i].Cat, listaTemporal[i].Cant, pct, paleta[i]));
                    }

                    int sumaOtros = listaTemporal.Skip(5).Sum(x => x.Cant);
                    float pctOtros = (float)sumaOtros / totalProd * 100f;
                    _datosDonut.Add(("Otros", sumaOtros, pctOtros, paleta[5]));
                }

                panelNProdxCategoria.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar dona de categorías: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PanelNProdxCategoria_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = panelNProdxCategoria.ClientSize.Width;
            int h = panelNProdxCategoria.ClientSize.Height;

            if (_datosDonut.Count == 0)
            {
                using var fontVacio = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                g.DrawString("Sin datos de categorías disponibles.", fontVacio, Brushes.LightGray, 30, h / 2 - 10);
                return;
            }

            int diametro = Math.Min(h - 40, 200);
            Rectangle rectDona = new Rectangle(15, (h - diametro) / 2, diametro, diametro);

            float startAngle = 0f;
            foreach (var item in _datosDonut)
            {
                float sweepAngle = (item.Porcentaje / 100f) * 360f;
                using var brush = new SolidBrush(item.ColorSegmento);
                g.FillPie(brush, rectDona, startAngle, sweepAngle);
                startAngle += sweepAngle;
            }

            int diametroHueco = (int)(diametro * 0.58f);
            Rectangle rectHueco = new Rectangle(
                rectDona.X + (diametro - diametroHueco) / 2,
                rectDona.Y + (diametro - diametroHueco) / 2,
                diametroHueco,
                diametroHueco
            );
            using (var brushFondo = new SolidBrush(panelNProdxCategoria.BackColor))
            {
                g.FillEllipse(brushFondo, rectHueco);
            }

            int totalItems = _datosDonut.Sum(x => x.Cantidad);
            using (var fontTotalNum = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (var fontTotalSub = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            {
                string txtNum = totalItems.ToString("N0", cultura);
                string txtSub = "TOTAL";

                SizeF szNum = g.MeasureString(txtNum, fontTotalNum);
                SizeF szSub = g.MeasureString(txtSub, fontTotalSub);

                float cy = rectHueco.Y + (rectHueco.Height - szNum.Height - szSub.Height) / 2;
                g.DrawString(txtNum, fontTotalNum, Brushes.White, rectHueco.X + (rectHueco.Width - szNum.Width) / 2, cy);
                g.DrawString(txtSub, fontTotalSub, Brushes.LightGray, rectHueco.X + (rectHueco.Width - szSub.Width) / 2, cy + szNum.Height);
            }

            int posX = rectDona.Right + 20;
            int posY = 20;
            int altoItem = Math.Max(26, (h - 40) / _datosDonut.Count);

            using var fontLeyenda = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            using var fontPct = new Font("Segoe UI", 8.5F, FontStyle.Bold);

            foreach (var item in _datosDonut)
            {
                using (var brushBox = new SolidBrush(item.ColorSegmento))
                {
                    g.FillRectangle(brushBox, posX, posY + 4, 12, 12);
                }

                string nombreCat = item.Categoria.Length > 13 ? item.Categoria.Substring(0, 11) + "..." : item.Categoria;
                g.DrawString(nombreCat, fontLeyenda, Brushes.White, posX + 18, posY + 2);
                g.DrawString($"{item.Porcentaje:0.#}%", fontPct, Brushes.LightGray, posX + 160, posY + 2);

                posY += altoItem;
            }
        }

        #endregion

        #region 3. Barras Horizontales

        private void CargarBarrasProveedores()
        {
            panelProvXProd.Controls.Clear();
            try
            {
                DataTable dt = _lReporte.ObtenerTopProveedoresPorProductos();

                if (dt.Rows.Count == 0)
                {
                    var lblVacio = new Label
                    {
                        Text = "Sin productos asociados a proveedores",
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                        ForeColor = Color.FromArgb(160, 174, 192),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    panelProvXProd.Controls.Add(lblVacio);
                    return;
                }

                int maxValor = 0;
                foreach (DataRow row in dt.Rows)
                {
                    int val = Convert.ToInt32(row["Cantidad"]);
                    if (val > maxValor) maxValor = val;
                }
                if (maxValor == 0) maxValor = 1;

                int posY = 8;
                int anchoDisponible = 310;

                foreach (DataRow row in dt.Rows)
                {
                    string prov = row["Proveedor"].ToString()!;
                    int cantidad = Convert.ToInt32(row["Cantidad"]);

                    var lblNom = new Label
                    {
                        Text = prov,
                        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                        ForeColor = Color.FromArgb(220, 225, 235),
                        Location = new Point(0, posY + 5),
                        Size = new Size(130, 24),
                        TextAlign = ContentAlignment.MiddleRight,
                        AutoEllipsis = true
                    };
                    panelProvXProd.Controls.Add(lblNom);

                    int anchoBarra = (int)((double)cantidad / maxValor * anchoDisponible);
                    if (anchoBarra < 45) anchoBarra = 45;

                    var barra = new Guna2Panel
                    {
                        Location = new Point(138, posY),
                        Size = new Size(anchoBarra, 30),
                        BorderRadius = 6,
                        FillColor = Color.FromArgb(25, 29, 36),
                        BackColor = Color.Transparent
                    };

                    var lblVal = new Label
                    {
                        Text = cantidad.ToString("N0", cultura),
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.White,
                        Dock = DockStyle.Right,
                        TextAlign = ContentAlignment.MiddleRight,
                        Padding = new Padding(0, 0, 8, 0),
                        AutoSize = false,
                        Width = 55
                    };
                    barra.Controls.Add(lblVal);

                    panelProvXProd.Controls.Add(barra);
                    posY += 40;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gráfico de proveedores: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region 4. Tabla de Productos con Bajo Stock

        private void ConfigurarGrillaBajoStock()
        {
            dataGridViewBajoStock.AutoGenerateColumns = false;

            CID_Producto.DataPropertyName = "id_producto";
            CNombre.DataPropertyName = "Nombre";
            CStock.DataPropertyName = "Stock";

            CStock.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            CStock.DefaultCellStyle.Format = "N0";

            dataGridViewBajoStock.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CID_Producto.FillWeight = 20;
            CNombre.FillWeight = 55;
            CStock.FillWeight = 25;

            dataGridViewBajoStock.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && dataGridViewBajoStock.Columns[e.ColumnIndex].Name == "CStock" && e.Value != null)
                {
                    if (int.TryParse(e.Value.ToString(), out int stk))
                    {
                        if (stk == 0)
                        {
                            Color colorAgotado = Color.FromArgb(245, 101, 101);
                            e.CellStyle.ForeColor = colorAgotado;
                            e.CellStyle.SelectionForeColor = colorAgotado;
                            e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        }
                        else if (stk <= 5)
                        {
                            Color colorAlerta = Color.FromArgb(246, 173, 85);
                            e.CellStyle.ForeColor = colorAlerta;
                            e.CellStyle.SelectionForeColor = colorAlerta;
                            e.CellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                        }
                    }
                }
            };
        }

        private void CargarTablaBajoStock()
        {
            try
            {
                DataTable dt = _lReporte.ObtenerProductosBajoStock();
                dataGridViewBajoStock.DataSource = dt;
                dataGridViewBajoStock.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar productos de bajo stock: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportarBajoStock_Click(object? sender, EventArgs e)
        {
            ExportarGrillaACSV(dataGridViewBajoStock, "ProductosBajoStock");
        }

        #endregion

        #region 5. Tabla de Ventas General

        private void ConfigurarGrillaVentas()
        {
            dataGridViewVentas.AutoGenerateColumns = false;

            CNVenta.DataPropertyName = "NroVenta";
            CFecha.DataPropertyName = "Fecha";
            CCliente.DataPropertyName = "Cliente";
            CDNI.DataPropertyName = "DNI";
            CVendedor.DataPropertyName = "Vendedor";
            CMetodo.DataPropertyName = "MetodoPago";
            CTotal.DataPropertyName = "Total";

            CFecha.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            CTotal.DefaultCellStyle.Format = "C2";
            CTotal.DefaultCellStyle.FormatProvider = cultura;

            dataGridViewVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CNVenta.MinimumWidth = 70;
            CFecha.MinimumWidth = 105;
            CCliente.MinimumWidth = 120;
            CDNI.MinimumWidth = 85;
            CVendedor.MinimumWidth = 120;
            CMetodo.MinimumWidth = 100;
            CTotal.MinimumWidth = 85;
            CFactura.MinimumWidth = 55;

            CNVenta.FillWeight = 9;
            CFecha.FillWeight = 14;
            CCliente.FillWeight = 20;
            CDNI.FillWeight = 11;
            CVendedor.FillWeight = 18;
            CMetodo.FillWeight = 14;
            CTotal.FillWeight = 9;
            CFactura.FillWeight = 5;

            CFactura.FlatStyle = FlatStyle.Flat;
            CFactura.Text = "🧾";
            CFactura.UseColumnTextForButtonValue = true;

            dataGridViewVentas.CellPainting += DataGridViewVentas_CellPainting;
            dataGridViewVentas.CellContentClick += DataGridViewVentas_CellContentClick;
        }

        private void ConfigurarBuscadorVentas()
        {
            var columnas = new List<KeyValuePair<string, string>>
            {
                new("Vendedor",        "Vendedor"),
                new("Cliente",         "Cliente"),
                new("Fecha",           "Fecha"),
                new("DNI",             "DNI"),
                new("N° de Venta",     "NroVenta"),
                new("Método de Pago",  "MetodoPago")
            };

            comboBoxColumna.DataSource = columnas;
            comboBoxColumna.DisplayMember = "Key";
            comboBoxColumna.ValueMember = "Value";
            comboBoxColumna.SelectedIndex = 0;
        }

        private void CargarTablaVentas()
        {
            try
            {
                _dtVentas = _lReporte.ObtenerTodasLasVentas();
                _bsVentas.DataSource = _dtVentas;
                dataGridViewVentas.DataSource = _bsVentas;
                AplicarFiltroVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar ventas: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AplicarFiltroVentas()
        {
            if (_dtVentas == null) return;

            string texto = EscaparLike((textBoxBuscador.Text ?? string.Empty).Trim());
            if (string.IsNullOrEmpty(texto))
            {
                _bsVentas.Filter = string.Empty;
                return;
            }

            string columna = comboBoxColumna.SelectedValue?.ToString() ?? "Vendedor";

            if (columna == "Fecha")
            {
                if (DateTime.TryParse(texto, cultura, DateTimeStyles.None, out DateTime fechaFiltro) ||
                    DateTime.TryParse(texto, out fechaFiltro))
                {
                    DateTime desde = fechaFiltro.Date;
                    DateTime hasta = desde.AddDays(1);
                    _bsVentas.Filter = $"([Fecha] >= #{desde:yyyy-MM-dd 00:00:00}# AND [Fecha] < #{hasta:yyyy-MM-dd 00:00:00}#) OR CONVERT([Fecha], 'System.String') LIKE '%{texto}%'";
                }
                else
                {
                    _bsVentas.Filter = $"CONVERT([Fecha], 'System.String') LIKE '%{texto}%'";
                }
            }
            else if (columna == "NroVenta" || columna == "DNI")
            {
                _bsVentas.Filter = $"CONVERT([{columna}], 'System.String') LIKE '%{texto}%'";
            }
            else
            {
                _bsVentas.Filter = $"[{columna}] LIKE '%{texto}%'";
            }
        }

        private void DataGridViewVentas_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == CFactura.Index)
            {
                using var brushFila = new SolidBrush(Color.FromArgb(43, 50, 68));
                e.Graphics.FillRectangle(brushFila, e.CellBounds);

                var rectBoton = new Rectangle(
                    e.CellBounds.X + 6,
                    e.CellBounds.Y + 3,
                    e.CellBounds.Width - 12,
                    e.CellBounds.Height - 6
                );

                using var brushBoton = new SolidBrush(Color.FromArgb(43, 50, 68));
                e.Graphics.FillRectangle(brushBoton, rectBoton);

                TextRenderer.DrawText(
                    e.Graphics,
                    "🧾",
                    _fontEmoji,
                    rectBoton,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );

                e.Handled = true;
            }
        }

        private void DataGridViewVentas_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewVentas.Columns[e.ColumnIndex] == CFactura)
            {
                int idVenta = Convert.ToInt32(dataGridViewVentas.Rows[e.RowIndex].Cells["CNVenta"].Value);
                string textoTicket = _lReporte.GenerarTextoComprobante(idVenta);

                if (!string.IsNullOrEmpty(textoTicket))
                {
                    using Form overlay = ModalHelper.CrearOverlay(this);
                    overlay.Show();

                    using var frm = new FormTicket(textoTicket);
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    frm.ShowDialog(overlay);

                    overlay.Close();
                }
            }
        }

        private void BtnExportarVentas_Click(object? sender, EventArgs e)
        {
            ExportarGrillaACSV(dataGridViewVentas, "Ventas_Admin", omitirColumnaBoton: true);
        }

        #endregion

        #region 6. Tabla de Compras (Administrador)

        private void ConfigurarGrillaCompras()
        {
            dataGridViewCompras.AutoGenerateColumns = false;

            CNCompra.DataPropertyName = "NroCompra";
            CFechaCompra.DataPropertyName = "Fecha";
            CProveedor.DataPropertyName = "Proveedor";
            CCuit.DataPropertyName = "CUIT";
            CComprador.DataPropertyName = "Comprador";
            CMetodoCompra.DataPropertyName = "MetodoPago";
            CTotalCompra.DataPropertyName = "Total";

            CFechaCompra.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            CTotalCompra.DefaultCellStyle.Format = "C2";
            CTotalCompra.DefaultCellStyle.FormatProvider = cultura;

            dataGridViewCompras.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CNCompra.MinimumWidth = 70;
            CFechaCompra.MinimumWidth = 105;
            CProveedor.MinimumWidth = 120;
            CCuit.MinimumWidth = 85;
            CComprador.MinimumWidth = 120;
            CMetodoCompra.MinimumWidth = 100;
            CTotalCompra.MinimumWidth = 85;
            CFacturaCompra.MinimumWidth = 55;

            CNCompra.FillWeight = 9;
            CFechaCompra.FillWeight = 14;
            CProveedor.FillWeight = 20;
            CCuit.FillWeight = 11;
            CComprador.FillWeight = 18;
            CMetodoCompra.FillWeight = 14;
            CTotalCompra.FillWeight = 9;
            CFacturaCompra.FillWeight = 5;

            CFacturaCompra.FlatStyle = FlatStyle.Flat;
            CFacturaCompra.Text = "🧾";
            CFacturaCompra.UseColumnTextForButtonValue = true;

            dataGridViewCompras.CellPainting += DataGridViewCompras_CellPainting;
            dataGridViewCompras.CellContentClick += DataGridViewCompras_CellContentClick;
        }

        private void ConfigurarBuscadorCompras()
        {
            var columnas = new List<KeyValuePair<string, string>>
            {
                new("Proveedor",       "Proveedor"),
                new("Comprador",       "Comprador"),
                new("Fecha",           "Fecha"),
                new("CUIT",            "CUIT"),
                new("N° de Compra",    "NroCompra"),
                new("Método de Pago",  "MetodoPago")
            };

            comboBoxCompras.DataSource = columnas;
            comboBoxCompras.DisplayMember = "Key";
            comboBoxCompras.ValueMember = "Value";
            comboBoxCompras.SelectedIndex = 0;
        }

        private void CargarTablaCompras()
        {
            try
            {
                _dtCompras = _lReporte.ObtenerTodasLasCompras();
                _bsCompras.DataSource = _dtCompras;
                dataGridViewCompras.DataSource = _bsCompras;
                AplicarFiltroCompras();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar compras: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AplicarFiltroCompras()
        {
            if (_dtCompras == null) return;

            string texto = EscaparLike((textBoxCompras.Text ?? string.Empty).Trim());
            if (string.IsNullOrEmpty(texto))
            {
                _bsCompras.Filter = string.Empty;
                return;
            }

            string columna = comboBoxCompras.SelectedValue?.ToString() ?? "Proveedor";

            if (columna == "Fecha")
            {
                if (DateTime.TryParse(texto, cultura, DateTimeStyles.None, out DateTime fechaFiltro) ||
                    DateTime.TryParse(texto, out fechaFiltro))
                {
                    DateTime desde = fechaFiltro.Date;
                    DateTime hasta = desde.AddDays(1);
                    _bsCompras.Filter = $"([Fecha] >= #{desde:yyyy-MM-dd 00:00:00}# AND [Fecha] < #{hasta:yyyy-MM-dd 00:00:00}#) OR CONVERT([Fecha], 'System.String') LIKE '%{texto}%'";
                }
                else
                {
                    _bsCompras.Filter = $"CONVERT([Fecha], 'System.String') LIKE '%{texto}%'";
                }
            }
            else if (columna == "NroCompra" || columna == "CUIT")
            {
                _bsCompras.Filter = $"CONVERT([{columna}], 'System.String') LIKE '%{texto}%'";
            }
            else
            {
                _bsCompras.Filter = $"[{columna}] LIKE '%{texto}%'";
            }
        }

        private void DataGridViewCompras_CellPainting(object? sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == CFacturaCompra.Index)
            {
                using var brushFila = new SolidBrush(Color.FromArgb(43, 50, 68));
                e.Graphics.FillRectangle(brushFila, e.CellBounds);

                var rectBoton = new Rectangle(
                    e.CellBounds.X + 6,
                    e.CellBounds.Y + 3,
                    e.CellBounds.Width - 12,
                    e.CellBounds.Height - 6
                );

                using var brushBoton = new SolidBrush(Color.FromArgb(43, 50, 68));
                e.Graphics.FillRectangle(brushBoton, rectBoton);

                TextRenderer.DrawText(
                    e.Graphics,
                    "🧾",
                    _fontEmoji,
                    rectBoton,
                    Color.White,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter
                );

                e.Handled = true;
            }
        }

        private void DataGridViewCompras_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dataGridViewCompras.Columns[e.ColumnIndex] == CFacturaCompra)
            {
                int idCompra = Convert.ToInt32(dataGridViewCompras.Rows[e.RowIndex].Cells["CNCompra"].Value);
                string textoTicket = _lReporte.GenerarTextoComprobanteCompra(idCompra);

                if (!string.IsNullOrEmpty(textoTicket))
                {
                    using Form overlay = ModalHelper.CrearOverlay(this);
                    overlay.Show();

                    using var frm = new FormTicket(textoTicket);
                    frm.StartPosition = FormStartPosition.CenterScreen;
                    frm.ShowDialog(overlay);

                    overlay.Close();
                }
            }
        }

        private void BtnExportarCompras_Click(object? sender, EventArgs e)
        {
            ExportarGrillaACSV(dataGridViewCompras, "Compras_Admin", omitirColumnaBoton: true);
        }

        #endregion

        #region Utilidad de Búsqueda y Exportación

        private static string EscaparLike(string valor)
        {
            var sb = new System.Text.StringBuilder(valor.Length);
            foreach (char c in valor)
            {
                switch (c)
                {
                    case '[':
                    case ']':
                    case '%':
                    case '*':
                        sb.Append('[').Append(c).Append(']');
                        break;
                    case '\'':
                        sb.Append("''");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
            return sb.ToString();
        }

        private void ExportarGrillaACSV(DataGridView dgv, string prefijoArchivo, bool omitirColumnaBoton = false)
        {
            if (dgv.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv|Archivo de Texto (*.txt)|*.txt",
                Title = "Guardar tabla",
                FileName = $"{prefijoArchivo}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string separador = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";

                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();

                foreach (DataGridViewColumn col in dgv.Columns)
                {
                    if (omitirColumnaBoton && col is DataGridViewButtonColumn) continue;
                    cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(separador, cabeceras));

                foreach (DataGridViewRow row in dgv.Rows)
                {
                    if (row.IsNewRow) continue;

                    var celdas = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (omitirColumnaBoton && cell.OwningColumn is DataGridViewButtonColumn) continue;

                        string valor = cell.FormattedValue?.ToString()?.Replace("\"", "\"\"") ?? "";
                        if (separador == ",")
                            celdas.Add($"\"{valor}\"");
                        else
                            celdas.Add(valor);
                    }
                    sw.WriteLine(string.Join(separador, celdas));
                }

                MessageBox.Show("Tabla exportada con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        #endregion
    }
}