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
using PROYECTO1.CapaEntidades;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class ReporteVendedor : Form
    {
        private readonly LReporte _lReporte = new();
        private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");
        private readonly List<(DateTime Fecha, decimal Total)> datosHistoricos = new();
        private readonly ToolTip _toolTip = new();
        private readonly Font _fontEmoji = new("Segoe UI Emoji", 11F, FontStyle.Regular);

        // BindingSource para habilitar búsqueda y filtrado en memoria
        private readonly BindingSource _bsVentas = new();
        private DataTable _dtVentas = null!;

        private Label lblVenMesComp = null!;
        private Label lblOrdenesMesComp = null!;
        private Label lblClientesMesComp = null!;

        private int IdUsuarioActual => SesionActual.Usuario?.Id ?? 1;

        public ReporteVendedor()
        {
            InitializeComponent();
            this.Load += Reportes_Load;
            panelGraficoLineasDefault.Paint += PanelGraficoLineasDefault_Paint;

            // Eventos del buscador y exportador
            textBoxBuscador.TextChanged += (_, __) => AplicarFiltroVentas();
            comboBoxColumna.SelectedIndexChanged += (_, __) => AplicarFiltroVentas();
            btnExportar.Click += BtnExportar_Click;
        }

        private void Reportes_Load(object? sender, EventArgs e)
        {
            ConfigurarEstiloLabels();
            ConfigurarColumnasTabla();
            ConfigurarBuscador();
            CargarMetricasVendedor();
            CargarMetricasMesVendedor();
            CargarHistorialVentas();
            CargarGraficoCategorias();
            CargarGraficoHistorico();
        }

        private void ConfigurarEstiloLabels()
        {
            // Configura cada tarjeta de KPI superior para que la imagen y el texto convivan sin taparse
            void ConfigurarTarjetaKPI(Guna2Panel panel, Label lblImg, Label lblTexto, float tamanoFuente)
            {
                // 1. Imagen a la izquierda, centrada verticalmente
                lblImg.AutoSize = false;
                lblImg.Dock = DockStyle.None;
                lblImg.Text = string.Empty; // Limpia espacios residuales
                lblImg.Size = new Size(60, 60);
                lblImg.Location = new Point(18, (panel.Height - 60) / 2);
                lblImg.ImageAlign = ContentAlignment.MiddleCenter;
                lblImg.TextAlign = ContentAlignment.MiddleCenter;
                lblImg.BackColor = Color.Transparent;
                lblImg.BringToFront();

                // 2. Número a la derecha, ocupando el resto del panel sin invadir el icono
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

            ConfigurarTarjetaKPI(panelVentas, imagenVentas, lblVentas, 18F);
            ConfigurarTarjetaKPI(panelNOrdenes, imagenNOrdenes, lblNOrdenes, 26F);
            ConfigurarTarjetaKPI(panelClientes, imagenClientes, lblClientes, 26F);
            ConfigurarTarjetaKPI(panelVentasCliente, imagenVxCliente, lblVentasCliente, 18F);

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
            ConfigurarTarjetaMes(pnlOrdenesMes, lblOrdenesMes, out lblOrdenesMesComp);
            ConfigurarTarjetaMes(pnlClientesMes, lblClientesMes, out lblClientesMesComp);
        }

        private void ConfigurarColumnasTabla()
        {
            dataGridViewVentas.AutoGenerateColumns = false;

            CNVenta.DataPropertyName = "NroVenta";
            CFecha.DataPropertyName = "Fecha";
            CCliente.DataPropertyName = "Cliente";
            CDNI.DataPropertyName = "DNI";
            CMetodo.DataPropertyName = "MetodoPago";
            CTotal.DataPropertyName = "Total";

            CFecha.DefaultCellStyle.Format = "dd/MM/yyyy HH:mm";
            CTotal.DefaultCellStyle.Format = "C2";
            CTotal.DefaultCellStyle.FormatProvider = cultura;

            Color colorFondoFila = Color.FromArgb(43, 50, 68);
            Color colorSeleccionFila = Color.FromArgb(60, 70, 95);

            dataGridViewVentas.DefaultCellStyle.BackColor = colorFondoFila;
            dataGridViewVentas.DefaultCellStyle.ForeColor = Color.White;
            dataGridViewVentas.DefaultCellStyle.SelectionBackColor = colorSeleccionFila;
            dataGridViewVentas.DefaultCellStyle.SelectionForeColor = Color.White;

            dataGridViewVentas.AlternatingRowsDefaultCellStyle.BackColor = colorFondoFila;
            dataGridViewVentas.AlternatingRowsDefaultCellStyle.ForeColor = Color.White;
            dataGridViewVentas.AlternatingRowsDefaultCellStyle.SelectionBackColor = colorSeleccionFila;
            dataGridViewVentas.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;

            dataGridViewVentas.RowsDefaultCellStyle.BackColor = colorFondoFila;
            dataGridViewVentas.RowsDefaultCellStyle.ForeColor = Color.White;

            CFactura.FlatStyle = FlatStyle.Flat;
            CFactura.Text = "🧾";
            CFactura.UseColumnTextForButtonValue = true;

            dataGridViewVentas.ShowCellToolTips = true;
            dataGridViewVentas.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == CFactura.Index)
                    e.ToolTipText = "Ver Comprobante";
            };

            // Responsividad automática sin cálculo de pixeles
            dataGridViewVentas.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            CNVenta.MinimumWidth = 80;
            CFecha.MinimumWidth = 110;
            CCliente.MinimumWidth = 140;
            CDNI.MinimumWidth = 90;
            CMetodo.MinimumWidth = 110;
            CTotal.MinimumWidth = 90;
            CFactura.MinimumWidth = 60;

            CNVenta.FillWeight = 11;
            CFecha.FillWeight = 16;
            CCliente.FillWeight = 26;
            CDNI.FillWeight = 13;
            CMetodo.FillWeight = 16;
            CTotal.FillWeight = 12;
            CFactura.FillWeight = 6;

            dataGridViewVentas.CellPainting += DataGridViewVentas_CellPainting;
            dataGridViewVentas.CellContentClick += DataGridViewVentas_CellContentClick;
        }

        private void ConfigurarBuscador()
        {
            var columnas = new List<KeyValuePair<string, string>>
            {
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


        // Escapa los caracteres especiales del LIKE de DataView para que escribir [ ] * % ' no provoque una excepción
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

        private void AplicarFiltroVentas()
        {
            if (_dtVentas == null) return;

            string texto = EscaparLike((textBoxBuscador.Text ?? string.Empty).Trim());
            if (string.IsNullOrEmpty(texto))
            {
                _bsVentas.Filter = string.Empty;
                return;
            }

            string columna = comboBoxColumna.SelectedValue?.ToString() ?? "Cliente";

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

        private void CargarHistorialVentas()
        {
            try
            {
                _dtVentas = _lReporte.ObtenerHistorialVentas(IdUsuarioActual);
                _bsVentas.DataSource = _dtVentas;
                dataGridViewVentas.DataSource = _bsVentas;
                AplicarFiltroVentas();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar el historial: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (dataGridViewVentas.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos de ventas para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv|Archivo de Texto (*.txt)|*.txt",
                Title = "Guardar historial de ventas",
                FileName = $"Ventas_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string separador = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";

                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();

                foreach (DataGridViewColumn col in dataGridViewVentas.Columns)
                {
                    if (col != CFactura)
                        cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(separador, cabeceras));

                foreach (DataGridViewRow row in dataGridViewVentas.Rows)
                {
                    if (row.IsNewRow) continue;

                    var celdas = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (cell.OwningColumn == CFactura) continue;

                        string valor = cell.FormattedValue?.ToString()?.Replace("\"", "\"\"") ?? "";

                        if (separador == ",")
                            celdas.Add($"\"{valor}\"");
                        else
                            celdas.Add(valor);
                    }
                    sw.WriteLine(string.Join(separador, celdas));
                }

                MessageBox.Show("¡Historial de ventas exportado con éxito!", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
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

        private void CargarMetricasVendedor()
        {
            try
            {
                EReporteKpiHistorico kpi = _lReporte.ObtenerKpisHistoricos(IdUsuarioActual);

                lblVentas.Text = FormatearMonedaCompacta(kpi.TotalFacturado);
                lblNOrdenes.Text = kpi.CantidadVentas.ToString("N0", cultura);
                lblClientes.Text = kpi.ClientesUnicos.ToString("N0", cultura);
                lblVentasCliente.Text = FormatearMonedaCompacta(kpi.PromedioPorCliente);

                _toolTip.SetToolTip(lblVentas, $"Total exacto: $ {kpi.TotalFacturado.ToString("N2", cultura)}");
                _toolTip.SetToolTip(lblVentasCliente, $"Promedio exacto: $ {kpi.PromedioPorCliente.ToString("N2", cultura)}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al cargar KPIs: " + ex.Message, "Reportes", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarMetricasMesVendedor()
        {
            DateTime ahora = DateTime.Now;

            try
            {
                EReporteKpiMes kpi = _lReporte.ObtenerKpisMes(IdUsuarioActual);

                lblVenMes.Text = FormatearMonedaCompacta(kpi.VentasActual);
                lblOrdenesMes.Text = kpi.OrdenesActual.ToString("N0", cultura);
                lblClientesMes.Text = kpi.ClientesActual.ToString("N0", cultura);

                AsignarComparativa(lblVenMesComp, kpi.VentasActual, kpi.VentasAnterior, esMoneda: true);
                AsignarComparativa(lblOrdenesMesComp, kpi.OrdenesActual, kpi.OrdenesAnterior, esMoneda: false);
                AsignarComparativa(lblClientesMesComp, kpi.ClientesActual, kpi.ClientesAnterior, esMoneda: false);

                _toolTip.SetToolTip(lblVenMes, $"Este mes (al día {ahora.Day}): $ {kpi.VentasActual:N2}\nMes anterior (mismo día): $ {kpi.VentasAnterior:N2}");
                _toolTip.SetToolTip(lblOrdenesMes, $"Este mes: {kpi.OrdenesActual:N0}\nMes anterior: {kpi.OrdenesAnterior:N0}");
                _toolTip.SetToolTip(lblClientesMes, $"Este mes: {kpi.ClientesActual:N0}\nMes anterior: {kpi.ClientesAnterior:N0}");
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

        private void CargarGraficoHistorico()
        {
            datosHistoricos.Clear();

            try
            {
                datosHistoricos.AddRange(_lReporte.ObtenerGraficoHistorico(IdUsuarioActual));
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
                DataTable dt = _lReporte.ObtenerTopCategorias(IdUsuarioActual);

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