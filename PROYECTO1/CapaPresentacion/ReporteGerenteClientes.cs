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
    public partial class ReporteGerenteClientes : Form
    {
        private readonly LReporte _lReporte = new();
        private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
        private readonly List<(DateTime Fecha, int Cantidad)> datosHistoricos = new();
        private readonly List<(string Metodo, int Cantidad, float Porcentaje, Color ColorSegmento)> _datosDonut = new();
        private readonly ToolTip _toolTip = new();

        public ReporteGerenteClientes()
        {
            InitializeComponent();
            this.Load += ReporteGerenteClientes_Load;
            panelGraficoLineasDefault.Paint += PanelGraficoLineasDefault_Paint;
            panelContenedorDonut.Paint += PanelContenedorDonut_Paint;
            btnExportar.Click += BtnExportar_Click;
        }

        private void ReporteGerenteClientes_Load(object? sender, EventArgs e)
        {
            ConfigurarEstiloLabels();
            ConfigurarColumnasTabla();
            CargarMetricasClientes();
            CargarTop10Clientes();
            CargarGraficoNuevosClientes();
            CargarGraficoMetodosPago();
        }

        private void ConfigurarEstiloLabels()
        {
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

            ConfigurarTarjetaKPI(panelClientes, imagenClientes, lblClientes, 26F);
            ConfigurarTarjetaKPI(panelVentasCliente, imagenVxCliente, lblVentasCliente, 18F);
            ConfigurarTarjetaKPI(panelClientesRecurrentes, imagenClientesRecurrentes, lblClientesRecurrentes, 26F);

            // Ajuste de las 3 tarjetas del mes
            lblNombreClientesMes.AutoSize = false;
            lblNombreClientesMes.Dock = DockStyle.Fill;
            lblNombreClientesMes.TextAlign = ContentAlignment.MiddleCenter;
            lblNombreClientesMes.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblNombreClientesMes.ForeColor = Color.White;
            lblNombreClientesMes.AutoEllipsis = true;

            lblOrdenesClienteMes.AutoSize = false;
            lblOrdenesClienteMes.Dock = DockStyle.Fill;
            lblOrdenesClienteMes.TextAlign = ContentAlignment.MiddleCenter;
            lblOrdenesClienteMes.Font = new Font("Segoe UI", 22F, FontStyle.Bold);
            lblOrdenesClienteMes.ForeColor = Color.White;

            lblIngresosClientesMes.AutoSize = false;
            lblIngresosClientesMes.Dock = DockStyle.Fill;
            lblIngresosClientesMes.TextAlign = ContentAlignment.MiddleCenter;
            lblIngresosClientesMes.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
            lblIngresosClientesMes.ForeColor = Color.White;
        }

        private void ConfigurarColumnasTabla()
        {
            dataGridViewClientes.AutoGenerateColumns = false;

            CID_cliente.DataPropertyName = "DNI";
            CID_cliente.HeaderText = "DNI";
            CNombre.DataPropertyName = "Cliente";
            COrdenes.DataPropertyName = "Ordenes";
            CIngresos.DataPropertyName = "Ingresos";

            CID_cliente.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            COrdenes.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            CIngresos.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            COrdenes.DefaultCellStyle.Format = "N0";
            CIngresos.DefaultCellStyle.Format = "C2";
            CIngresos.DefaultCellStyle.FormatProvider = cultura;

            dataGridViewClientes.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CID_cliente.FillWeight = 18;
            CNombre.FillWeight = 42;
            COrdenes.FillWeight = 18;
            CIngresos.FillWeight = 22;
        }

        private void CargarMetricasClientes()
        {
            try
            {
                var kpi = _lReporte.ObtenerKpisGerenteClientes();

                lblClientes.Text = kpi.ClientesActivos.ToString("N0", cultura);
                lblVentasCliente.Text = FormatearMonedaCompacta(kpi.VentasPorCliente);
                lblClientesRecurrentes.Text = kpi.ClientesRecurrentes.ToString("N0", cultura);

                lblNombreClientesMes.Text = kpi.ClienteMesNombre;
                lblOrdenesClienteMes.Text = kpi.ClienteMesOrdenes.ToString("N0", cultura);
                lblIngresosClientesMes.Text = FormatearMonedaCompacta(kpi.ClienteMesIngresos);

                _toolTip.SetToolTip(lblVentasCliente, $"Promedio exacto: $ {kpi.VentasPorCliente.ToString("N2", cultura)}");
                _toolTip.SetToolTip(lblNombreClientesMes, $"Cliente del mes: {kpi.ClienteMesNombre}");
                _toolTip.SetToolTip(lblIngresosClientesMes, $"Ingresos del mes: $ {kpi.ClienteMesIngresos.ToString("N2", cultura)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar métricas de clientes: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarTop10Clientes()
        {
            try
            {
                DataTable dt = _lReporte.ObtenerTop10Clientes();
                dataGridViewClientes.DataSource = dt;
                dataGridViewClientes.ClearSelection();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar Top 10 Clientes: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarGraficoNuevosClientes()
        {
            datosHistoricos.Clear();
            try
            {
                datosHistoricos.AddRange(_lReporte.ObtenerGraficoNuevosClientes());
                panelGraficoLineasDefault.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar nuevos clientes por mes: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

            float padLeft = 45;
            float padRight = 25;
            float padTop = 20;
            float padBottom = 35;

            float plotW = w - padLeft - padRight;
            float plotH = h - padTop - padBottom;

            int maxVal = datosHistoricos.Max(d => d.Cantidad);
            if (maxVal <= 0) maxVal = 5;
            maxVal = (int)Math.Ceiling(maxVal * 1.2);

            int n = datosHistoricos.Count;
            int lineasY = 4;

            using (var fontAxis = new Font("Segoe UI", 8F, FontStyle.Regular))
            using (var brushAxis = new SolidBrush(Color.FromArgb(180, 190, 205)))
            {
                for (int i = 0; i <= lineasY; i++)
                {
                    float y = padTop + (plotH / lineasY) * i;
                    int valorNivel = maxVal - (maxVal / lineasY) * i;

                    string textoY = valorNivel.ToString("N0", cultura);
                    SizeF sz = g.MeasureString(textoY, fontAxis);
                    g.DrawString(textoY, fontAxis, brushAxis, padLeft - sz.Width - 8, y - sz.Height / 2);
                }
            }

            PointF[] puntos = new PointF[n];
            for (int i = 0; i < n; i++)
            {
                float px = padLeft + (plotW / (n - 1)) * i;
                float py = padTop + plotH - ((float)datosHistoricos[i].Cantidad / maxVal) * plotH;
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

        private void CargarGraficoMetodosPago()
        {
            _datosDonut.Clear();
            try
            {
                DataTable dt = _lReporte.ObtenerMetodosPagoPorOrdenes();
                if (dt.Rows.Count == 0) return;

                int totalOrdenes = 0;
                var listaTemporal = new List<(string Metodo, int Cant)>();

                foreach (DataRow row in dt.Rows)
                {
                    int cant = Convert.ToInt32(row["Cantidad"]);
                    listaTemporal.Add((row["MetodoPago"].ToString()!, cant));
                    totalOrdenes += cant;
                }

                if (totalOrdenes == 0) return;

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
                        float pct = (float)listaTemporal[i].Cant / totalOrdenes * 100f;
                        _datosDonut.Add((listaTemporal[i].Metodo, listaTemporal[i].Cant, pct, paleta[i % paleta.Length]));
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        float pct = (float)listaTemporal[i].Cant / totalOrdenes * 100f;
                        _datosDonut.Add((listaTemporal[i].Metodo, listaTemporal[i].Cant, pct, paleta[i]));
                    }

                    int sumaOtros = listaTemporal.Skip(5).Sum(x => x.Cant);
                    float pctOtros = (float)sumaOtros / totalOrdenes * 100f;
                    _datosDonut.Add(("Otros", sumaOtros, pctOtros, paleta[5]));
                }

                panelContenedorDonut.Invalidate();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar métodos de pago: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void PanelContenedorDonut_Paint(object? sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            int w = panelContenedorDonut.ClientSize.Width;
            int h = panelContenedorDonut.ClientSize.Height;

            if (_datosDonut.Count == 0)
            {
                using var fontVacio = new Font("Segoe UI", 9.5F, FontStyle.Italic);
                g.DrawString("Sin órdenes registradas.", fontVacio, Brushes.LightGray, 30, h / 2 - 10);
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
            using (var brushFondo = new SolidBrush(panelContenedorDonut.BackColor))
            {
                g.FillEllipse(brushFondo, rectHueco);
            }

            int totalItems = _datosDonut.Sum(x => x.Cantidad);
            using (var fontTotalNum = new Font("Segoe UI", 12F, FontStyle.Bold))
            using (var fontTotalSub = new Font("Segoe UI", 7.5F, FontStyle.Bold))
            {
                string txtNum = totalItems.ToString("N0", cultura);
                string txtSub = "ORDENES";

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

                string nombreMetodo = item.Metodo.Length > 13 ? item.Metodo.Substring(0, 11) + "..." : item.Metodo;
                g.DrawString(nombreMetodo, fontLeyenda, Brushes.White, posX + 18, posY + 2);
                g.DrawString($"{item.Porcentaje:0.#}%", fontPct, Brushes.LightGray, posX + 160, posY + 2);

                posY += altoItem;
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (dataGridViewClientes.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv|Archivo de Texto (*.txt)|*.txt",
                Title = "Guardar Top 10 Clientes",
                FileName = $"Top10Clientes_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string separador = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";

                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();

                foreach (DataGridViewColumn col in dataGridViewClientes.Columns)
                {
                    cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(separador, cabeceras));

                foreach (DataGridViewRow row in dataGridViewClientes.Rows)
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

                MessageBox.Show("Top 10 Clientes exportado con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
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