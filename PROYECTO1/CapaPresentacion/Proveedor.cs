using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using PROYECTO1;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class Proveedor : Form
    {
        private readonly LProveedor _lProveedor = new();
        private readonly BindingSource _bsProveedor = new();
        private DataTable _dtProveedor;
        private bool? _soloActivos = null;

        public Proveedor()
        {
            InitializeComponent();

            Load += Proveedor_Load;
            buttonActivos.Click += (_, __) => ToggleFiltroEstado(true);
            buttonInactivos.Click += (_, __) => ToggleFiltroEstado(false);
            textBoxBuscador.TextChanged += (_, __) => AplicarFiltrosCombinados();
            comboBoxColumna.SelectedIndexChanged += (_, __) => AplicarFiltrosCombinados();
            buttonNuevoProveedor.Click += ButtonNuevoProveedor_Click;
            buttonModificar.Click += ButtonModificar_Click;
            btnExportar.Click += BtnExportar_Click;
            dataGridViewProveedor.CellDoubleClick += DataGridViewProveedor_CellDoubleClick;
            dataGridViewProveedor.CellContentClick += DataGridViewProveedor_CellContentClick;
        }

        private void Proveedor_Load(object? sender, EventArgs e)
        {
            ConfigurarGrilla();
            ConfigurarBuscador();
            CargarTablaDesdeBD();

            if (SesionActual.Usuario?.IdRol == 2)
            {
                buttonNuevoProveedor.Visible = false;
                buttonModificar.Visible = false;
                dataGridViewProveedor.ReadOnly = true;
            }
        }

        private void ConfigurarGrilla()
        {
            dataGridViewProveedor.AutoGenerateColumns = false;

            if (dataGridViewProveedor.Columns["Estado"] is not DataGridViewCheckBoxColumn)
            {
                int idx = dataGridViewProveedor.Columns["Estado"].Index;
                dataGridViewProveedor.Columns.RemoveAt(idx);
                var colChk = new DataGridViewCheckBoxColumn
                {
                    Name = "Estado",
                    HeaderText = "Estado",
                    DataPropertyName = "Actividad"
                };
                dataGridViewProveedor.Columns.Insert(idx, colChk);
            }

            // 1. Enlace imprescindible para que el sistema reconozca qué proveedor editar
            id_Proveedor.DataPropertyName = "id_proveedor";

            // 2. Oculta la columna de la vista del usuario sin romper la lógica interna
            id_Proveedor.Visible = false;

            RazonSocial.DataPropertyName = "Razón Social";
            CUIT.DataPropertyName = "CUIT";
            Telefono.DataPropertyName = "Teléfono";
            Correo.DataPropertyName = "Correo";
            Direccion.DataPropertyName = "Dirección";
            dataGridViewProveedor.Columns["Estado"].DataPropertyName = "Actividad";

            dataGridViewProveedor.ReadOnly = false;
            foreach (DataGridViewColumn col in dataGridViewProveedor.Columns)
            {
                col.ReadOnly = (col.Name != "Estado");
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            dataGridViewProveedor.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            dataGridViewProveedor.Columns["Estado"].MinimumWidth = 65;
            Telefono.MinimumWidth = 110;
            CUIT.MinimumWidth = 120;
            Correo.MinimumWidth = 150;
            RazonSocial.MinimumWidth = 160;
            Direccion.MinimumWidth = 160;

            dataGridViewProveedor.Columns["Estado"].FillWeight = 8;
            Telefono.FillWeight = 14;
            CUIT.FillWeight = 16;
            Correo.FillWeight = 16;
            RazonSocial.FillWeight = 23;
            Direccion.FillWeight = 23;
        }

        private void CargarTablaDesdeBD()
        {
            _dtProveedor = _lProveedor.Listar();
            _bsProveedor.DataSource = _dtProveedor;
            dataGridViewProveedor.DataSource = _bsProveedor;
            AplicarFiltrosCombinados();
        }

        private void ToggleFiltroEstado(bool activos)
        {
            if (_soloActivos.HasValue && _soloActivos.Value == activos)
                _soloActivos = null;
            else
                _soloActivos = activos;

            ActualizarEstilosBotonesFiltro();
            AplicarFiltrosCombinados();
        }

        private void ActualizarEstilosBotonesFiltro()
        {
            Color normal = Color.FromArgb(109, 124, 163);
            Color activo = Color.FromArgb(43, 50, 68);

            buttonActivos.FillColor = (_soloActivos == true) ? activo : normal;
            buttonInactivos.FillColor = (_soloActivos == false) ? activo : normal;
        }

        private void AplicarFiltrosCombinados()
        {
            if (_dtProveedor == null) return;

            var partes = new List<string>();
            string texto = EscaparLike((textBoxBuscador.Text ?? string.Empty).Trim());

            if (!string.IsNullOrEmpty(texto))
            {
                string col = comboBoxColumna.SelectedValue?.ToString() ?? "Razón Social";
                if (col == "id_proveedor")
                    partes.Add($"CONVERT(id_proveedor, 'System.String') LIKE '%{texto}%'");
                else
                    partes.Add($"[{col}] LIKE '%{texto}%'");
            }

            if (_soloActivos.HasValue)
            {
                string colEstado = _dtProveedor.Columns.Contains("Actividad") ? "Actividad" :
                                   _dtProveedor.Columns.Contains("Activo") ? "Activo" :
                                   _dtProveedor.Columns.Contains("activo") ? "activo" :
                                   _dtProveedor.Columns.Contains("Estado") ? "Estado" : string.Empty;

                if (!string.IsNullOrEmpty(colEstado))
                {
                    partes.Add(_soloActivos.Value ? $"[{colEstado}] = true" : $"[{colEstado}] = false");
                }
            }

            _bsProveedor.Filter = partes.Count > 0 ? string.Join(" AND ", partes) : string.Empty;
        }

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

        private void ConfigurarBuscador()
        {
            var columnas = new List<KeyValuePair<string, string>>
            {
                new("Razón Social", "Razón Social"),
                new("CUIT",         "CUIT"),
                new("Teléfono",     "Teléfono"),
                new("Correo",       "Correo")
            };

            comboBoxColumna.DataSource = columnas;
            comboBoxColumna.DisplayMember = "Key";
            comboBoxColumna.ValueMember = "Value";
            comboBoxColumna.SelectedIndex = 0;
        }

        private void DataGridViewProveedor_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var col = dataGridViewProveedor.Columns[e.ColumnIndex];
            if (col.Name == "Estado" || col.DataPropertyName == "Actividad")
            {
                dataGridViewProveedor.CommitEdit(DataGridViewDataErrorContexts.Commit);

                var valorId = dataGridViewProveedor.Rows[e.RowIndex].Cells["id_Proveedor"].Value;
                if (valorId != null && int.TryParse(valorId.ToString(), out int id))
                {
                    bool nuevoEstado = Convert.ToBoolean(dataGridViewProveedor.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    _lProveedor.CambiarEstado(id, nuevoEstado);
                }
            }
        }

        private void ButtonNuevoProveedor_Click(object? sender, EventArgs e)
        {
            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formReg = new FormRegistrarProveedor())
            {
                formReg.StartPosition = FormStartPosition.CenterScreen;
                if (formReg.ShowDialog(this) == DialogResult.OK)
                {
                    CargarTablaDesdeBD();
                }
            }
            overlay.Close();
        }

        private void ButtonModificar_Click(object? sender, EventArgs e)
        {
            if (dataGridViewProveedor.CurrentRow == null || dataGridViewProveedor.CurrentRow.Index < 0)
            {
                MessageBox.Show("Por favor, seleccione un proveedor de la lista para modificar.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var valorId = dataGridViewProveedor.CurrentRow.Cells["id_Proveedor"].Value;
            if (valorId == null || valorId == DBNull.Value || !int.TryParse(valorId.ToString(), out int id))
            {
                MessageBox.Show("No se pudo obtener el identificador del proveedor seleccionado.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formReg = new FormRegistrarProveedor(id))
            {
                formReg.StartPosition = FormStartPosition.CenterScreen;
                if (formReg.ShowDialog(this) == DialogResult.OK)
                {
                    CargarTablaDesdeBD();
                }
            }

            overlay.Close();
        }

        private void DataGridViewProveedor_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && buttonModificar.Visible) ButtonModificar_Click(sender, e);
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {
            if (dataGridViewProveedor.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Proveedores_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();
                foreach (DataGridViewColumn col in dataGridViewProveedor.Columns)
                {
                    if (col.Visible) cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(",", cabeceras));

                foreach (DataGridViewRow row in dataGridViewProveedor.Rows)
                {
                    if (row.IsNewRow) continue;
                    var celdas = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (dataGridViewProveedor.Columns[cell.ColumnIndex].Visible)
                        {
                            string v = cell.Value?.ToString()?.Replace("\"", "\"\"") ?? "";
                            celdas.Add($"\"{v}\"");
                        }
                    }
                    sw.WriteLine(string.Join(",", celdas));
                }
                MessageBox.Show("Tabla exportada con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}