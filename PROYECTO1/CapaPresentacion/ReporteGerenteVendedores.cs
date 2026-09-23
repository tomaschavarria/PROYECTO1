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
    public partial class ReporteGerenteVendedores : Form
    {
        private readonly LReporte _lReporte = new();
        private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
        private readonly List<(string Vendedor, decimal Total, float Porcentaje, Color ColorSegmento)> _datosDonut = new();
        private readonly ToolTip _toolTip = new();

        public ReporteGerenteVendedores()
        {
            InitializeComponent();
            this.Load += ReporteGerenteVendedores_Load;

            panelGraficoDonutDefault.Paint += PanelGraficoDonutDefault_Paint;
            btnExportar.Click += BtnExportar_Click;
        }

        private void ReporteGerenteVendedores_Load(object? sender, EventArgs e)
        {
            ConfigurarEstiloLabels();
            ConfigurarColumnasTabla();

            CargarMetricasVendedores();
            CargarTablaVendedores();
            CargarGraficoDonutVendedores();
            CargarGraficoBarrasVendedores();
        }

        private void ConfigurarEstiloLabels()
        {
            tituloVendedores.Text = "VENDEDORES ACTIVOS";
            tituloVentasXVendedor.Text = "INGRESO PROM. X VEND.";
            tituloNOrdenesXVendedor.Text = "ORDENES PROM. X VEND.";

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

            ConfigurarTarjetaKPI(panelVendedores, imagenVendedores, lblVendedores, 26F);
            ConfigurarTarjetaKPI(panelVentasXVendedor, imagenVxVendedor, lblVentasXVendedor, 18F);
            ConfigurarTarjetaKPI(panelNOrdenesXVendedor, imagenNOrdenesXVendedor, lblNOrdenesXVendedor, 24F);

            lblNombreVENDEDORMes.AutoSize = false;
            lblNombreVENDEDORMes.Dock = DockStyle.Fill;
            lblNombreVENDEDORMes.TextAlign = ContentAlignment.MiddleCenter;
            lblNombreVENDEDORMes.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblNombreVENDEDORMes.ForeColor = Color.White;
            lblNombreVENDEDORMes.AutoEllipsis = true;

            lblOrdenesVENDEDORMes.AutoSize = false;
            lblOrdenesVENDEDORMes.Dock = DockStyle.Fill;
            lblOrdenesVENDEDORMes.TextAlign = ContentAlignment.MiddleCenter;
            lblOrdenesVENDEDORMes.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblOrdenesVENDEDORMes.ForeColor = Color.White;

            lblIngresosVENDEDORMes.AutoSize = false;
            lblIngresosVENDEDORMes.Dock = DockStyle.Fill;
            lblIngresosVENDEDORMes.TextAlign = ContentAlignment.MiddleCenter;
            lblIngresosVENDEDORMes.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblIngresosVENDEDORMes.ForeColor = Color.White;
        }

        private void ConfigurarColumnasTabla()
        {
            dataGridViewVendedores.AutoGenerateColumns = false;

            CDNI_vendedor.DataPropertyName = "DNI";
            CNombre.DataPropertyName = "Vendedor";
            COrdenes.DataPropertyName = "Ordenes";
            CIngresos.DataPropertyName = "Ingresos";

            CDNI_vendedor.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            CNombre.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            COrdenes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CIngresos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            COrdenes.DefaultCellStyle.Format = "N0";
            CIngresos.DefaultCellStyle.Format = "C2";
            CIngresos.DefaultCellStyle.FormatProvider = cultura;

            dataGridViewVendedores.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CDNI_vendedor.FillWeight = 20;
            CNombre.FillWeight = 42;
            COrdenes.FillWeight = 18;
            CIngresos.FillWeight = 20;
        }

        private void CargarMetricasVendedores()
        {
            try
            {
                var kpi = _lReporte.ObtenerKpisGerenteVendedores();

                lblVendedores.Text = kpi.VendedoresActivos.ToString("N0", cultura);
                lblVentasXVendedor.Text = FormatearMonedaCompacta(kpi.IngresoPromedio);
                lblNOrdenesXVendedor.Text = kpi.OrdenesPromedio.ToString("0.#", cultura);

                lblNombreVENDEDORMes.Text = kpi.VendedorMesNombre;
                lblOrdenesVENDEDORMes.Text = kpi.VendedorMesOrdenes.ToString("N0", cultura);
                lblIngresosVENDEDORMes.Text = FormatearMonedaCompacta(kpi.VendedorMesIngresos);

                _toolTip.SetToolTip(lblVentasXVendedor, $"Ingreso promedio: $ {kpi.IngresoPromedio.ToString("N2", cultura)}");
                _toolTip.SetToolTip(lblNombreVENDEDORMes, $"Vendedor del mes: {kpi.VendedorMesNombre}");
                _toolTip.SetToolTip(lblIngresosVENDEDORMes, $"Ingresos del mes: $ {kpi.VendedorMesIngresos.ToString("N2", cultura)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar métricas de vendedores: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarTablaVendedores()
        {
            try
            {
                DataTable dt = _lReporte.ObtenerTablaRendimientoVendedores();
                dataGridViewVendedores.DataSource = dt;
                dataGridViewVendedores.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar tabla de vendedores: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGraficoDonutVendedores()
        {
            _datosDonut.Clear();
            try
            {
                DataTable dt = _lReporte.ObtenerVentasPorVendedorDonut();
                if (dt.Rows.Count == 0) return;

                decimal totalVentas = 0;
                var lista = new List<(string Vendedor, decimal Total)>();

                foreach (DataRow row in dt.Rows)
                {
                    decimal tot = Convert.ToDecimal(row["TotalRecaudado"]);
                    lista.Add((row["Vendedor"].ToString()!, tot));
                    totalVentas += tot;
                }

                if (totalVentas <= 0) return;

                Color[] paleta = new[]
                {
                    Color.FromArgb(109, 124, 163),
                    Color.FromArgb(136, 152, 194),
                    Color.FromArgb(85, 98, 132),
                    Color.FromArgb(162, 175, 212),
                    Color.FromArgb(63, 74, 102),
                    Color.FromArgb(48, 56, 78)
                };

                if (lista.Count <= 6)
                {
                    for (int i = 0; i < lista.Count; i++)
                    {
                        float pct = (float)(lista[i].Total / totalVentas * 100m);
                        _datosDonut.Add((lista[i].Vendedor, lista[i].Total, pct, paleta[i % paleta.Length]));
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float pct = (float)(lista[i].Total / totalVentas * 100m);
                        _datosDonut.Add((lista[i].Vendedor, lista[i].Total, pct, paleta[i]));
                    }

                    decimal sumaOtros = lista.Skip(5).Sum(x => x.Total);
                    float pctOtros = (float)(sumaOtros / totalVentas * 100m);
                    _datosDonut.Add(("Otros", sumaOtros, pctOtros, paleta[5]));
                }

                panelGraficoDonutDefault.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar gráfico de ventas por vendedor: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PanelGraficoDonutDefault_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = panelGraficoDonutDefault.ClientSize.Width;
            int h = panelGraficoDonutDefault.ClientSize.Height;

            if (_datosDonut.Count == 0)
            {
                using var fontVacio = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                g.DrawString("Sin ventas de vendedores registradas.", fontVacio, Brushes.LightGray, 30, h / 2 - 10);
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
            using (var brushFondo = new SolidBrush(panelGraficoDonutDefault.BackColor))
            {
                g.FillEllipse(brushFondo, rectHueco);
            }

            decimal totalRecaudado = _datosDonut.Sum(x => x.Total);
            using (var fontTotalNum = new Font("Segoe UI", 11F, FontStyle.Bold))
            using (var fontTotalSub = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            {
                string txtNum = FormatearMonedaCompacta(totalRecaudado);
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

                string nombre = item.Vendedor.Length > 13 ? item.Vendedor.Substring(0, 11) + "..." : item.Vendedor;
                g.DrawString(nombre, fontLeyenda, Brushes.White, posX + 18, posY + 2);
                g.DrawString($"{item.Porcentaje:0.#}%", fontPct, Brushes.LightGray, posX + 160, posY + 2);

                posY += altoItem;
            }
        }

        private void CargarGraficoBarrasVendedores()
        {
            panelContenedorBarras.Controls.Clear();

            try
            {
                DataTable dt = _lReporte.ObtenerTop6VendedoresPorOrdenes();

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
                    string vendedor = row["Vendedor"].ToString()!;
                    int cantidad = Convert.ToInt32(row["CantidadOrdenes"]);

                    var lblVend = new Label
                    {
                        Text = vendedor,
                        Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                        ForeColor = Color.FromArgb(220, 225, 235),
                        Location = new Point(0, posY + 5),
                        Size = new Size(115, 24),
                        TextAlign = ContentAlignment.MiddleRight,
                        AutoEllipsis = true
                    };
                    panelContenedorBarras.Controls.Add(lblVend);

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
                MessageBox.Show("Error al cargar ranking de vendedores: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (dataGridViewVendedores.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos de vendedores para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv|Archivo de Texto (*.txt)|*.txt",
                Title = "Guardar rendimiento de vendedores",
                FileName = $"Rendimiento_Vendedores_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string separador = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";

                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();

                foreach (DataGridViewColumn col in dataGridViewVendedores.Columns)
                {
                    cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(separador, cabeceras));

                foreach (DataGridViewRow row in dataGridViewVendedores.Rows)
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

                MessageBox.Show("Rendimiento de vendedores exportado con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
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