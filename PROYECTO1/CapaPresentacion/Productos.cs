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
    public partial class Productos : Form
    {
        private readonly LProducto _lProducto = new();
        private readonly BindingSource _bsProductos = new();
        private DataTable _dtProductos;
        private bool? _soloActivos = null;

        public Productos()
        {
            InitializeComponent();

            Load += Productos_Load;
            buttonActivos.Click += (_, __) => ToggleFiltroEstado(true);
            buttonInactivos.Click += (_, __) => ToggleFiltroEstado(false);
            textBoxBuscador.TextChanged += (_, __) => AplicarFiltrosCombinados();
            comboBoxColumna.SelectedIndexChanged += (_, __) => AplicarFiltrosCombinados();
            dataGridView1.CellContentClick += DataGridView1_CellContentClick;
            btnExportar.Click += BtnExportar_Click;
        }

        private void Productos_Load(object sender, EventArgs e)
        {
            ConfigurarGrilla();
            ConfigurarBuscador();
            CargarTablaDesdeBD();

            if (SesionActual.Usuario?.IdRol == 2 || SesionActual.Usuario?.IdRol == 3)
            {
                buttonNuevoProducto.Visible = false;
                buttonModificar.Visible = false;
                dataGridView1.ReadOnly = true;
            }
        }

        private void ConfigurarGrilla()
        {
            dataGridView1.AutoGenerateColumns = false;

            if (dataGridView1.Columns["Estado"] is not DataGridViewCheckBoxColumn)
            {
                int idx = dataGridView1.Columns["Estado"].Index;
                dataGridView1.Columns.RemoveAt(idx);
                var colChk = new DataGridViewCheckBoxColumn
                {
                    Name = "Estado",
                    HeaderText = "Estado",
                    DataPropertyName = "Activo"
                };
                dataGridView1.Columns.Insert(idx, colChk);
            }

            id_Producto.DataPropertyName = "id_producto";
            Nombre.DataPropertyName = "Nombre";
            Descripción.DataPropertyName = "Descripción";
            Categoria.DataPropertyName = "Categoria";
            Stock.DataPropertyName = "Stock";
            PrecioVenta.DataPropertyName = "PrecioVenta";
            PrecioCosto.DataPropertyName = "PrecioCosto";
            Talle.DataPropertyName = "Talle";
            dataGridView1.Columns["Estado"].DataPropertyName = "Activo";

            // Columna Proveedor (se agrega por código, junto a Categoría)
            if (!dataGridView1.Columns.Contains("Proveedor"))
            {
                dataGridView1.Columns.Insert(Categoria.Index + 1, new DataGridViewTextBoxColumn
                {
                    Name = "Proveedor",
                    HeaderText = "Proveedor",
                    DataPropertyName = "Proveedor"
                });
            }

            PrecioVenta.DefaultCellStyle.Format = "N2";
            PrecioCosto.DefaultCellStyle.Format = "N2";
            Stock.DefaultCellStyle.Format = "N0";

            dataGridView1.ReadOnly = false;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.ReadOnly = (col.Name != "Estado");
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            id_Producto.MinimumWidth = 60;
            Stock.MinimumWidth = 65;
            Talle.MinimumWidth = 65;
            dataGridView1.Columns["Estado"].MinimumWidth = 65;
            PrecioVenta.MinimumWidth = 100;
            PrecioCosto.MinimumWidth = 100;
            Categoria.MinimumWidth = 120;
            dataGridView1.Columns["Proveedor"].MinimumWidth = 120;
            Nombre.MinimumWidth = 140;
            Descripción.MinimumWidth = 180;

            id_Producto.FillWeight = 7;
            Stock.FillWeight = 7;
            Talle.FillWeight = 7;
            dataGridView1.Columns["Estado"].FillWeight = 7;
            PrecioVenta.FillWeight = 11;
            PrecioCosto.FillWeight = 11;
            Categoria.FillWeight = 14;
            dataGridView1.Columns["Proveedor"].FillWeight = 14;
            Nombre.FillWeight = 16;
            Descripción.FillWeight = 20;
        }

        private void CargarTablaDesdeBD()
        {
            _dtProductos = _lProducto.Listar();
            _bsProductos.DataSource = _dtProductos;
            dataGridView1.DataSource = _bsProductos;
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
            if (_dtProductos == null) return;

            var partes = new List<string>();
            string texto = EscaparLike((textBoxBuscador.Text ?? string.Empty).Trim());

            if (!string.IsNullOrEmpty(texto))
            {
                string col = comboBoxColumna.SelectedValue?.ToString() ?? "Nombre";
                if (col == "id_producto")
                    partes.Add($"CONVERT(id_producto, 'System.String') LIKE '%{texto}%'");
                else
                    partes.Add($"[{col}] LIKE '%{texto}%'");
            }

            if (_soloActivos.HasValue)
            {
                // Detección dinámica de columna para evitar EvaluateException
                string colEstado = _dtProductos.Columns.Contains("Activo") ? "Activo" :
                                   _dtProductos.Columns.Contains("activo") ? "activo" :
                                   _dtProductos.Columns.Contains("Actividad") ? "Actividad" :
                                   _dtProductos.Columns.Contains("Estado") ? "Estado" : string.Empty;

                if (!string.IsNullOrEmpty(colEstado))
                {
                    partes.Add(_soloActivos.Value ? $"[{colEstado}] = true" : $"[{colEstado}] = false");
                }
            }

            _bsProductos.Filter = partes.Count > 0 ? string.Join(" AND ", partes) : string.Empty;
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

        private void ConfigurarBuscador()
        {
            var columnas = new List<KeyValuePair<string, string>>
            {
                new("Nombre",       "Nombre"),
                new("ID",           "id_producto"),
                new("Categoría",    "Categoria"),
                new("Descripción",  "Descripción"),
                new("Talle",        "Talle"),
                new("Proveedor",    "Proveedor")
            };

            comboBoxColumna.DataSource = columnas;
            comboBoxColumna.DisplayMember = "Key";
            comboBoxColumna.ValueMember = "Value";
            comboBoxColumna.SelectedIndex = 0;
        }

        private void DataGridView1_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Coincidencia por nombre de columna o por propiedad enlazada
            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col.Name == "Estado" || col.DataPropertyName == "Activo")
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

                int id = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["id_Producto"].Value);
                bool nuevoEstado = Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);

                _lProducto.CambiarEstado(id, nuevoEstado);
            }
        }

        private void ButtonNuevoProducto_Click(object sender, EventArgs e)
        {
            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formReg = new FormRegistrarProducto())
            {
                formReg.StartPosition = FormStartPosition.CenterScreen;
                if (formReg.ShowDialog(this) == DialogResult.OK)
                {
                    CargarTablaDesdeBD();
                }
            }
            overlay.Close();
        }

        private void ButtonModificar_Click(object sender, EventArgs e)
        {
            // 1. Valida que exista una fila seleccionada válida
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
            {
                MessageBox.Show("Por favor, seleccione un producto de la lista para modificar.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 2. Lectura y conversión segura del ID
            var valorId = dataGridView1.CurrentRow.Cells["id_Producto"].Value;
            if (valorId == null || valorId == DBNull.Value || !int.TryParse(valorId.ToString(), out int id))
            {
                MessageBox.Show("No se pudo obtener el identificador del producto seleccionado.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formReg = new FormRegistrarProducto(id))
            {
                formReg.StartPosition = FormStartPosition.CenterScreen;
                if (formReg.ShowDialog(this) == DialogResult.OK)
                {
                    CargarTablaDesdeBD();
                }
            }
            overlay.Close();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && buttonModificar.Visible) ButtonModificar_Click(sender, e);
        }

        private void BtnExportar_Click(object? sender, EventArgs e)
        {

            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Productos_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();
                foreach (DataGridViewColumn col in dataGridView1.Columns) cabeceras.Add(col.HeaderText);
                sw.WriteLine(string.Join(",", cabeceras));

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;
                    var celdas = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        string v = cell.Value?.ToString()?.Replace("\"", "\"\"") ?? "";
                        celdas.Add($"\"{v}\"");
                    }
                    sw.WriteLine(string.Join(",", celdas));
                }
                MessageBox.Show("Tabla exportada con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}