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
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class ReporteGerenteVentas : Form
    {
        private readonly LReporte _lReporte = new();
        private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
        private readonly List<(DateTime Fecha, decimal Total)> datosHistoricos = new();
        private readonly ToolTip _toolTip = new();

        private Label lblVenMesComp = null!;
        private Label lblGananciasMesComp = null!;
        private Label lblOrdenesMesComp = null!;
        private Label lblProdVendidosMesComp = null!;

        public ReporteGerenteVentas()
        {
            InitializeComponent();
            this.Load += ReporteGerenteVentas_Load;
            panelGraficoLineasDefault.Paint += PanelGraficoLineasDefault_Paint;
            btnExportar.Click += BtnExportar_Click;
        }

        private void ReporteGerenteVentas_Load(object? sender, EventArgs e)
        {
            ConfigurarEstiloLabels();
            ConfigurarColumnasTabla();
            CargarMetricasGerente();
            CargarMetricasMesGerente();
            CargarTop10Productos();
            CargarGraficoCategorias();
            CargarGraficoHistorico();
        }

        private void ConfigurarEstiloLabels()
        {
            // Corrige texto de la etiqueta si vino por defecto del diseñador
            tituloGananciasMes.Text = "GANANCIAS DEL MES";

            void ConfigurarTarjetaKPI(Guna2Panel panel, Label lblImg, Label lblTexto, float tamanoFuente)
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

                lblTexto.AutoSize = false;
                lblTexto.Dock = DockStyle.None;
                lblTexto.Location = new Point(85, 10);
                lblTexto.Size = new Size(panel.Width - 95, panel.Height - 20);
                lblTexto.TextAlign = ContentAlignment.MiddleCenter;
                lblTexto.Font = new Font("Segoe UI", tamanoFuente, FontStyle.Bold);
                lblTexto.ForeColor = Color.White;
                lblTexto.BackColor = Color.Transparent;
                lblTexto.BringToFront();
            }

            ConfigurarTarjetaKPI(panelIngresos, imagenIngresos, lblIngresos, 18F);
            ConfigurarTarjetaKPI(panelGanancias, imagenGanancias, lblGanancias, 18F);
            ConfigurarTarjetaKPI(panelNOrdenes, imagenNOrdenes, lblNOrdenes, 26F);
            ConfigurarTarjetaKPI(panelTicketPromedio, imagenTicketPromedio, lblTicketPromedio, 18F);

            void ConfigurarTarjetaMes(Guna2Panel panel, Label lblPrincipal, out Label lblComparativa)
            {
                lblPrincipal.AutoSize = false;
                lblPrincipal.Dock = DockStyle.Top;
                lblPrincipal.Height = 44;
                lblPrincipal.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
                lblPrincipal.ForeColor = Color.White;
                lblPrincipal.TextAlign = ContentAlignment.MiddleCenter;
                lblPrincipal.BackColor = Color.Transparent;

                lblComparativa = new Label
                {
                    AutoSize = false,
                    Dock = DockStyle.Bottom,
                    Height = 28,
                    Font = new Font("Segoe UI", 8.5F, FontStyle.Bold),
                    ForeColor = Color.FromArgb(180, 190, 205),
                    TextAlign = ContentAlignment.MiddleCenter,
                    BackColor = Color.Transparent
                };

                panel.Controls.Add(lblComparativa);
                lblPrincipal.BringToFront();
            }

            ConfigurarTarjetaMes(pnlVenMes, lblVenMes, out lblVenMesComp);
            ConfigurarTarjetaMes(pnlGananciasMes, lblGananciasMes, out lblGananciasMesComp);
            ConfigurarTarjetaMes(pnlOrdenesMes, lblOrdenesMes, out lblOrdenesMesComp);
            ConfigurarTarjetaMes(pnlProductosVendidosXMes, lblProductosVendidosXMes, out lblProdVendidosMesComp);
        }

        private void ConfigurarColumnasTabla()
        {
            dataGridViewVentas.AutoGenerateColumns = false;

            CID_producto.DataPropertyName = "IdProducto";
            CNombre.DataPropertyName = "Nombre";
            COrdenes.DataPropertyName = "Ordenes";
            CIngresos.DataPropertyName = "Ingresos";

            CID_producto.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            COrdenes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CIngresos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            COrdenes.DefaultCellStyle.Format = "N0";
            CIngresos.DefaultCellStyle.Format = "C2";
            CIngresos.DefaultCellStyle.FormatProvider = cultura;

            dataGridViewVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CID_producto.FillWeight = 15;
            CNombre.FillWeight = 45;
            COrdenes.FillWeight = 20;
            CIngresos.FillWeight = 20;
        }

        private void CargarMetricasGerente()
        {
            try
            {
                EReporteKpiHistoricoGerente kpi = _lReporte.ObtenerKpisHistoricosGerente();

                lblIngresos.Text = FormatearMonedaCompacta(kpi.Ingresos);
                lblGanancias.Text = FormatearMonedaCompacta(kpi.Ganancias);
                lblNOrdenes.Text = kpi.Ordenes.ToString("N0", cultura);
                lblTicketPromedio.Text = FormatearMonedaCompacta(kpi.TicketPromedio);

                _toolTip.SetToolTip(lblIngresos, $"Ingresos totales: $ {kpi.Ingresos.ToString("N2", cultura)}");
                _toolTip.SetToolTip(lblGanancias, $"Ganancias totales: $ {kpi.Ganancias.ToString("N2", cultura)}");
                _toolTip.SetToolTip(lblTicketPromedio, $"Ticket promedio: $ {kpi.TicketPromedio.ToString("N2", cultura)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar KPIs generales: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarMetricasMesGerente()
        {
            DateTime ahora = DateTime.Now;

            try
            {
                EReporteKpiMesGerente kpi = _lReporte.ObtenerKpisMesGerente();

                lblVenMes.Text = FormatearMonedaCompacta(kpi.VentasActual);
                lblGananciasMes.Text = FormatearMonedaCompacta(kpi.GananciasActual);
                lblOrdenesMes.Text = kpi.OrdenesActual.ToString("N0", cultura);
                lblProductosVendidosXMes.Text = kpi.ProdVendidosActual.ToString("N0", cultura);

                AsignarComparativa(lblVenMesComp, kpi.VentasActual, kpi.VentasAnterior, esMoneda: true);
                AsignarComparativa(lblGananciasMesComp, kpi.GananciasActual, kpi.GananciasAnterior, esMoneda: true);
                AsignarComparativa(lblOrdenesMesComp, kpi.OrdenesActual, kpi.OrdenesAnterior, esMoneda: false);
                AsignarComparativa(lblProdVendidosMesComp, kpi.ProdVendidosActual, kpi.ProdVendidosAnterior, esMoneda: false);

                _toolTip.SetToolTip(lblVenMes, $"Este mes (al día {ahora.Day}): $ {kpi.VentasActual:N2}\nMes anterior (mismo corte): $ {kpi.VentasAnterior:N2}");
                _toolTip.SetToolTip(lblGananciasMes, $"Este mes: $ {kpi.GananciasActual:N2}\nMes anterior: $ {kpi.GananciasAnterior:N2}");
                _toolTip.SetToolTip(lblOrdenesMes, $"Este mes: {kpi.OrdenesActual:N0}\nMes anterior: {kpi.OrdenesAnterior:N0}");
                _toolTip.SetToolTip(lblProductosVendidosXMes, $"Este mes: {kpi.ProdVendidosActual:N0}\nMes anterior: {kpi.ProdVendidosAnterior:N0}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar KPIs del mes: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AsignarComparativa(Label lblComp, decimal actual, decimal anterior, bool esMoneda)
        {
            string textoAnt = esMoneda
                ? FormatearMonedaCompacta(anterior)
                : (anterior >= 1000 ? (anterior / 1000m).ToString("0.#", cultura) + "K" : anterior.ToString("N0", cultura));

            if (anterior == 0)
            {
                lblComp.Text = actual > 0 ? $"Ant: {textoAnt}  ▲ +100%" : $"Ant: {textoAnt}  0.0%";
                lblComp.ForeColor = actual > 0 ? Color.FromArgb(46, 204, 113) : Color.FromArgb(160, 174, 192);
                return;
            }

            double pct = (double)((actual - anterior) / anterior) * 100.0;

            if (pct > 0)
            {
                lblComp.Text = $"Ant: {textoAnt}  ▲ +{pct:0.0}%";
                lblComp.ForeColor = Color.FromArgb(46, 204, 113);
            }
            else if (pct < 0)
            {
                lblComp.Text = $"Ant: {textoAnt}  ▼ {pct:0.0}%";
                lblComp.ForeColor = Color.FromArgb(245, 101, 101);
            }
            else
            {
                lblComp.Text = $"Ant: {textoAnt}  = 0.0%";
                lblComp.ForeColor = Color.FromArgb(160, 174, 192);
            }
        }

        private void CargarTop10Productos()
        {
            try
            {
                DataTable dt = _lReporte.ObtenerTop10ProductosGerente();
                dataGridViewVentas.DataSource = dt;
                dataGridViewVentas.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar Top 10 Productos: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGraficoHistorico()
        {
            datosHistoricos.Clear();
            try
            {
                datosHistoricos.AddRange(_lReporte.ObtenerGraficoHistoricoGerente());
                panelGraficoLineasDefault.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar ventas mensuales: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PanelGraficoLineasDefault_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = panelGraficoLineasDefault.ClientSize.Width;
            int h = panelGraficoLineasDefault.ClientSize.Height;

            if (datosHistoricos.Count < 2)
            {
                using var fontVacio = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                g.DrawString("No hay suficientes datos históricos registrados.", fontVacio, Brushes.LightGray, 30, h / 2 - 10);
                return;
            }

            float padLeft = 65;
            float padRight = 25;
            float padTop = 20;
            float padBottom = 35;

            float plotW = w - padLeft - padRight;
            float plotH = h - padTop - padBottom;

            decimal maxVal = datosHistoricos.Max(d => d.Total);
            if (maxVal <= 0) maxVal = 1000m;
            maxVal *= 1.15m;

            int n = datosHistoricos.Count;
            int lineasY = 4;

            using (var fontAxis = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (var brushAxis = new SolidBrush(Color.FromArgb(180, 190, 205)))
            {
                for (int i = 0; i <= lineasY; i++)
                {
                    float y = padTop + (plotH / lineasY) * i;
                    decimal valorNivel = maxVal - (maxVal / lineasY) * i;

                    string textoY = valorNivel >= 1000000m
                        ? $"${(valorNivel / 1000000m):0.0}M"
                        : valorNivel >= 1000m
                            ? $"${(valorNivel / 1000m):0}K"
                            : $"${valorNivel:0}";

                    SizeF sz = g.MeasureString(textoY, fontAxis);
                    g.DrawString(textoY, fontAxis, brushAxis, padLeft - sz.Width - 8, y - sz.Height / 2);
                }
            }

            PointF[] puntos = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                float px = padLeft + (plotW / (n - 1)) * i;
                float py = padTop + plotH - (float)(datosHistoricos[i].Total / maxVal) * plotH;
                puntos[i] = new PointF(px, py);
            }

            int pasoEtiquetas = Math.Max(1, n / 5);
            using (var fontAxisX = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (var brushAxisX = new SolidBrush(Color.FromArgb(180, 190, 205)))
            {
                for (int i = 0; i < n; i += pasoEtiquetas)
                {
                    string labelFecha = datosHistoricos[i].Fecha.ToString("MMM yyyy", cultura);
                    SizeF sz = g.MeasureString(labelFecha, fontAxisX);
                    g.DrawString(labelFecha, fontAxisX, brushAxisX, puntos[i].X - sz.Width / 2, padTop + plotH + 8);
                }
            }

            using var penLinea = new Pen(Color.White, 2.5f);
            g.DrawLines(penLinea, puntos);
        }

        private void CargarGraficoCategorias()
        {
            panelContenedorBarras.Controls.Clear();

            try
            {
                DataTable dt = _lReporte.ObtenerTopCategoriasGerente();

                if (dt.Rows.Count == 0)
                {
                    var lblVacio = new Label
                    {
                        Text = "Sin órdenes registradas",
                        Font = new Font("Segoe UI", 9.5F, FontStyle.Italic),
                        ForeColor = Color.FromArgb(160, 174, 192),
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter
                    };
                    panelContenedorBarras.Controls.Add(lblVacio);
                    return;
                }

                int maxValor = 0;
                foreach (DataRow row in dt.Rows)
                {
                    int val = Convert.ToInt32(row["CantidadOrdenes"]);
                    if (val > maxValor) maxValor = val;
                }
                if (maxValor == 0) maxValor = 1;

                int posY = 8;
                int anchoDisponibleBarras = 330;

                foreach (DataRow row in dt.Rows)
                {
                    string categoria = row["Categoria"].ToString()!;
                    int cantidad = Convert.ToInt32(row["CantidadOrdenes"]);

                    var lblCat = new Label
                    {
                        Text = categoria,
                        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                        ForeColor = Color.FromArgb(220, 225, 235),
                        Location = new Point(0, posY + 5),
                        Size = new Size(115, 24),
                        TextAlign = ContentAlignment.MiddleRight,
                        AutoEllipsis = true
                    };
                    panelContenedorBarras.Controls.Add(lblCat);

                    int anchoBarra = (int)((double)cantidad / maxValor * anchoDisponibleBarras);
                    if (anchoBarra < 50) anchoBarra = 50;

                    var barra = new Guna2Panel
                    {
                        Location = new Point(125, posY),
                        Size = new Size(anchoBarra, 32),
                        BorderRadius = 6,
                        FillColor = Color.FromArgb(25, 29, 36),
                        BackColor = Color.Transparent
                    };

                    string textoCantidad = cantidad >= 1000
                        ? (cantidad / 1000.0).ToString("0.0", cultura) + "K"
                        : cantidad.ToString("N0", cultura);

                    var lblVal = new Label
                    {
                        Text = textoCantidad,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = Color.White,
                        Dock = DockStyle.Right,
                        TextAlign = ContentAlignment.MiddleRight,
                        Padding = new Padding(0, 0, 8, 0),
                        AutoSize = false,
                        Width = 60
                    };
                    barra.Controls.Add(lblVal);

                    panelContenedorBarras.Controls.Add(barra);
                    posY += 42;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar categorías: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (dataGridViewVentas.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv|Archivo de Texto (*.txt)|*.txt",
                Title = "Guardar Top 10 Productos",
                FileName = $"Top10Productos_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string separador = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";

                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();

                foreach (DataGridViewColumn col in dataGridViewVentas.Columns)
                {
                    cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(separador, cabeceras));

                foreach (DataGridViewRow row in dataGridViewVentas.Rows)
                {
                    if (row.IsNewRow) continue;

                    var celdas = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        string valor = cell.FormattedValue?.ToString()?.Replace("\"", "\"\"") ?? "";
                        if (separador == ",")
                            celdas.Add($"\"{valor}\"");
                        else
                            celdas.Add(valor);
                    }
                    sw.WriteLine(string.Join(separador, celdas));
                }

                MessageBox.Show("Top 10 Productos exportado con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private string FormatearMonedaCompacta(decimal valor)
        {
            if (valor >= 1_000_000m)
                return $"$ {(valor / 1_000_000m).ToString("0.#", cultura)}M";

            if (valor >= 1_000m)
                return $"$ {(valor / 1_000m).ToString("0.#", cultura)}K";

            return $"$ {valor.ToString("N0", cultura)}";
        }
    }
}