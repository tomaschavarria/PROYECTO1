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
    public partial class Usuarios : Form
    {
        private readonly LUsuario _lUsuario = new();
        private readonly BindingSource _bsUsuarios = new();
        private DataTable _dtUsuarios;
        private bool? _soloActivos = null;

        public Usuarios()
        {
            InitializeComponent();

            dataGridView1.AutoGenerateColumns = false;

            buttonActivos.Click += (_, __) => ToggleFiltroEstado(true);
            buttonInactivos.Click += (_, __) => ToggleFiltroEstado(false);
            comboBoxColumna.SelectedIndexChanged += (_, __) => AplicarFiltrosCombinados();
            textBoxBuscador.TextChanged += (_, __) => AplicarFiltrosCombinados();
        }

        private void Usuarios_Load(object sender, EventArgs e)
        {
            ConfigurarGrilla();
            ConfigurarBuscador();
            CargarUsuariosEnGrilla();

            if (SesionActual.Usuario?.IdRol == 2) // Gerente: solo lectura
            {
                buttonNuevoUsuario.Visible = false;
                buttonModificar.Visible = false;
                dataGridView1.ReadOnly = true;
            }
        }

        private void ConfigurarGrilla()
        {
            dataGridView1.AutoGenerateColumns = false;

            // 1. Mantiene el ID en memoria para modificar/activar, pero lo oculta de la vista
            IdUsuario.DataPropertyName = "id_usuario";
            IdUsuario.Visible = false;

            // 2. Mapeo del Rol con su nombre real devuelto por la consulta SQL (r.nombre AS Rol)
            IdRol.DataPropertyName = "Rol";
            IdRol.HeaderText = "Rol";

            Nombre.DataPropertyName = "nombre";
            Apellido.DataPropertyName = "apellido";
            Usuario.DataPropertyName = "nombre_usuario";
            DNI.DataPropertyName = "DNI_usuario";
            Email.DataPropertyName = "correo";
            Telefono.DataPropertyName = "telefono";
            Dirección.DataPropertyName = "direccion";
            Fecha.DataPropertyName = "fecha_de_nacimiento";
            Estado.DataPropertyName = "estado";

            Fecha.DefaultCellStyle.Format = "dd/MM/yyyy";

            dataGridView1.ReadOnly = false;
            foreach (DataGridViewColumn col in dataGridView1.Columns)
            {
                col.ReadOnly = (col.Name != "Estado");
                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Anchos mínimos y pesos visuales equilibrados
            IdRol.MinimumWidth = 105;
            Estado.MinimumWidth = 65;
            DNI.MinimumWidth = 85;
            Telefono.MinimumWidth = 95;
            Fecha.MinimumWidth = 90;
            Usuario.MinimumWidth = 100;
            Nombre.MinimumWidth = 110;
            Apellido.MinimumWidth = 110;
            Email.MinimumWidth = 130;
            Dirección.MinimumWidth = 130;

            IdRol.FillWeight = 10;
            Estado.FillWeight = 6;
            DNI.FillWeight = 9;
            Telefono.FillWeight = 9;
            Fecha.FillWeight = 9;
            Usuario.FillWeight = 10;
            Nombre.FillWeight = 11;
            Apellido.FillWeight = 11;
            Email.FillWeight = 12;
            Dirección.FillWeight = 13;
        }

        private void CargarUsuariosEnGrilla()
        {
            _dtUsuarios = _lUsuario.Listar();

            if (_dtUsuarios != null)
            {
                string colEstado = _dtUsuarios.Columns.Contains("estado") ? "estado" :
                                   _dtUsuarios.Columns.Contains("Activo") ? "Activo" :
                                   _dtUsuarios.Columns.Contains("Estado") ? "Estado" : "Actividad";
                Estado.DataPropertyName = colEstado;
            }

            _bsUsuarios.DataSource = _dtUsuarios;
            dataGridView1.DataSource = _bsUsuarios;
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
            Color seleccionado = Color.FromArgb(43, 50, 68);

            buttonActivos.FillColor = (_soloActivos == true) ? seleccionado : normal;
            buttonInactivos.FillColor = (_soloActivos == false) ? seleccionado : normal;
        }

        private void AplicarFiltrosCombinados()
        {
            if (_dtUsuarios == null) return;

            var partes = new List<string>();
            string texto = EscaparLike((textBoxBuscador.Text ?? string.Empty).Trim());

            if (!string.IsNullOrEmpty(texto))
            {
                string col = comboBoxColumna.SelectedValue?.ToString() ?? "nombre";
                if (col == "DNI_usuario")
                    partes.Add($"CONVERT(DNI_usuario, 'System.String') LIKE '%{texto}%'");
                else
                    partes.Add($"[{col}] LIKE '%{texto}%'");
            }

            if (_soloActivos.HasValue)
            {
                string colEstado = _dtUsuarios.Columns.Contains("estado") ? "estado" :
                                   _dtUsuarios.Columns.Contains("Activo") ? "Activo" :
                                   _dtUsuarios.Columns.Contains("Estado") ? "Estado" :
                                   _dtUsuarios.Columns.Contains("Actividad") ? "Actividad" : string.Empty;

                if (!string.IsNullOrEmpty(colEstado))
                {
                    partes.Add(_soloActivos.Value ? $"[{colEstado}] = true" : $"[{colEstado}] = false");
                }
            }

            _bsUsuarios.Filter = partes.Count > 0 ? string.Join(" AND ", partes) : string.Empty;
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
                new("Rol",        "Rol"),
                new("DNI",        "DNI_usuario"),
                new("Nombre",     "nombre"),
                new("Apellido",   "apellido"),
                new("Usuario",    "nombre_usuario"),
                new("Teléfono",   "telefono"),
                new("Correo",     "correo")
            };

            comboBoxColumna.DataSource = columnas;
            comboBoxColumna.DisplayMember = "Key";
            comboBoxColumna.ValueMember = "Value";
            comboBoxColumna.SelectedIndex = 0;
        }

        private void dataGridView1_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var col = dataGridView1.Columns[e.ColumnIndex];
            if (col.Name == "Estado" || col == Estado)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

                var valorId = dataGridView1.Rows[e.RowIndex].Cells["IdUsuario"].Value;
                if (valorId != null && int.TryParse(valorId.ToString(), out int idUsuario))
                {
                    bool nuevoEstado = Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells[e.ColumnIndex].Value);
                    _lUsuario.CambiarEstado(idUsuario, nuevoEstado);
                }
            }
        }

        private void dataGridView1_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
        }

        private void buttonNuevoUsuario_Click(object sender, EventArgs e)
        {
            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formRegistroUsuario = new FormRegistrarUsuario())
            {
                formRegistroUsuario.StartPosition = FormStartPosition.CenterScreen;
                if (formRegistroUsuario.ShowDialog(this) == DialogResult.OK)
                {
                    CargarUsuariosEnGrilla();
                }
            }

            overlay.Close();
        }

        private void buttonModificar_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
            {
                MessageBox.Show("Por favor, seleccione un usuario de la lista para modificar.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var valorId = dataGridView1.CurrentRow.Cells["IdUsuario"].Value;
            if (valorId == null || valorId == DBNull.Value || !int.TryParse(valorId.ToString(), out int id))
            {
                MessageBox.Show("No se pudo obtener el identificador del usuario seleccionado.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using Form overlay = ModalHelper.CrearOverlay(this);
            overlay.Show();
            overlay.BringToFront();

            using (var formReg = new FormRegistrarUsuario(id))
            {
                formReg.StartPosition = FormStartPosition.CenterScreen;
                if (formReg.ShowDialog(this) == DialogResult.OK)
                {
                    CargarUsuariosEnGrilla();
                }
            }

            overlay.Close();
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && buttonModificar.Visible) buttonModificar_Click(sender, e);
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Archivo CSV (*.csv)|*.csv",
                FileName = $"Usuarios_{DateTime.Now:yyyyMMdd_HHmmss}.csv"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using var sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8);
                var cabeceras = new List<string>();
                foreach (DataGridViewColumn col in dataGridView1.Columns)
                {
                    if (col.Visible) cabeceras.Add(col.HeaderText);
                }
                sw.WriteLine(string.Join(",", cabeceras));

                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;

                    var celdas = new List<string>();
                    foreach (DataGridViewCell cell in row.Cells)
                    {
                        if (dataGridView1.Columns[cell.ColumnIndex].Visible)
                        {
                            string valor = cell.Value != null ? cell.Value.ToString()!.Replace("\"", "\"\"") : "";
                            celdas.Add($"\"{valor}\"");
                        }
                    }
                    sw.WriteLine(string.Join(",", celdas));
                }

                MessageBox.Show("Tabla exportada con éxito.", "Exportación", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}