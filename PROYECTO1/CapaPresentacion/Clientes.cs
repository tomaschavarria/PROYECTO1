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
    public partial class Clientes : Form
    {
        // Instancia de la Capa Lógica para clientes
        private readonly LCliente _lCliente = new();

        // Puente de enlace para la grilla
        private readonly BindingSource _bsClientes = new BindingSource();
        private DataTable _dtClientes;

        // Estado del filtro de actividad: true = activos, false = inactivos, null = todos
        private bool? _soloActivos = null;

        public Clientes()
        {
            InitializeComponent();

            buttonActivos.Click += (_, __) => ToggleFiltroEstado(true);
            buttonInactivos.Click += (_, __) => ToggleFiltroEstado(false);

            textBoxBuscador.TextChanged += (_, __) => AplicarFiltrosCombinados();
            comboBoxColumna.SelectedIndexChanged += (_, __) => AplicarFiltrosCombinados();
            btnExportar.Click += btnExportar_Click;

            // Asegura que al tildar/destildar el checkbox se confirme el cambio inmediatamente
            dataGridView1.CurrentCellDirtyStateChanged += DataGridView1_CurrentCellDirtyStateChanged;
        }

        private void DataGridView1_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
        {
            if (dataGridView1.IsCurrentCellDirty && dataGridView1.CurrentCell is DataGridViewCheckBoxCell)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void Cliente_Load(object sender, EventArgs e)
        {
            ConfigurarGrilla();
            ConfigurarBuscador();
            CargarDesdeBD();

            // Restricción de permisos para el Gerente y el Vendedor (solo lectura)
            if (SesionActual.Usuario?.IdRol == 2 || SesionActual.Usuario?.IdRol == 3)
            {
                dataGridView1.ReadOnly = true;
            }

            if (SesionActual.Usuario?.IdRol == 2)
            {
                buttonNuevoCliente.Visible = false;
                buttonModificar.Visible = false;
            }

            
        }

        // Enlaza las columnas visuales con los nombres devueltos por la base de datos
        private void ConfigurarGrilla()
        {
            dataGridView1.AutoGenerateColumns = false;

            Nombre.DataPropertyName = "Nombre";
            Apellido.DataPropertyName = "Apellido";
            DNI.DataPropertyName = "DNI";
            Teléfono.DataPropertyName = "Teléfono";
            Dirección.DataPropertyName = "Dirección";
            Correo.DataPropertyName = "Correo";
            Nacimiento.DataPropertyName = "Nacimiento";
            Estado.DataPropertyName = "Actividad";
        }

        // Vincula la tabla con el BindingSource y la grilla
        private void ConfigurarTablaYBinding()
        {
            _bsClientes.DataSource = _dtClientes;
            dataGridView1.DataSource = _bsClientes;
        }

        // Trae la lista de clientes desde la Capa Lógica
        private void CargarDesdeBD()
        {
            _dtClientes = _lCliente.Listar();
            ConfigurarTablaYBinding();
            AplicarFiltrosCombinados();
        }

        // Alterna el filtro de Activos / Inactivos
        private void ToggleFiltroEstado(bool activos)
        {
            if (_soloActivos.HasValue && _soloActivos.Value == activos)
                _soloActivos = null;
            else
                _soloActivos = activos;

            ActualizarEstilosBotonesFiltro();
            AplicarFiltrosCombinados();
        }

        // Resalta visualmente el botón de filtro seleccionado
        private void ActualizarEstilosBotonesFiltro()
        {
            Color colorNormal = Color.FromArgb(109, 124, 163);
            Color colorSeleccionado = Color.FromArgb(43, 50, 68);

            if (_soloActivos == true)
            {
                buttonActivos.FillColor = colorSeleccionado;
                buttonInactivos.FillColor = colorNormal;
            }
            else if (_soloActivos == false)
            {
                buttonActivos.FillColor = colorNormal;
                buttonInactivos.FillColor = colorSeleccionado;
            }
            else
            {
                buttonActivos.FillColor = colorNormal;
                buttonInactivos.FillColor = colorNormal;
            }
        }

        // Une el buscador de texto con el filtro de Activos/Inactivos
        private void AplicarFiltrosCombinados()
        {
            if (_dtClientes == null) return;

            var partes = new List<string>();
            string filtroTexto = ConstruirFiltroTexto();

            if (!string.IsNullOrEmpty(filtroTexto))
                partes.Add(filtroTexto);

            if (_soloActivos.HasValue)
            {
                partes.Add(_soloActivos.Value ? "Actividad = true" : "Actividad = false");
            }

            _bsClientes.Filter = partes.Count > 0 ? string.Join(" AND ", partes) : string.Empty;
        }

        // Arma la condición de búsqueda para la columna elegida
        private string ConstruirFiltroTexto()
        {
            string texto = EscaparLike((textBoxBuscador.Text ?? string.Empty).Trim());
            if (string.IsNullOrEmpty(texto)) return string.Empty;

            string columna = comboBoxColumna.SelectedValue.ToString()!;

            if (columna == "DNI")
            {
                return $"CONVERT(DNI, 'System.String') LIKE '%{texto}%'";
            }

            return $"{columna} LIKE '%{texto}%'";
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
                new("DNI",       "DNI"),
                new("Apellido",  "Apellido"),
                new("Nombre",    "Nombre"),
                new("Teléfono",  "Teléfono"),
                new("Correo",    "Correo")
            };

            comboBoxColumna.DataSource = columnas;
            comboBoxColumna.DisplayMember = "Key";
            comboBoxColumna.ValueMember = "Value";
            comboBoxColumna.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxColumna.SelectedIndex = 0;
        }

        // Se ejecuta al hacer clic en el checkbox de Estado para dar de alta/baja lógica en la BD
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            // Se valida con el nombre real de la columna ("Estado")
            if (dataGridView1.Columns[e.ColumnIndex].Name == "Estado" || e.ColumnIndex == Estado.Index)
            {
                dataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit);

                int dni = Convert.ToInt32(dataGridView1.Rows[e.RowIndex].Cells["DNI"].Value);
                bool nuevoEstado = Convert.ToBoolean(dataGridView1.Rows[e.RowIndex].Cells["Estado"].Value);

                _lCliente.CambiarEstadoPorDni(dni, nuevoEstado);
            }
        }

        private void dataGridView1_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && buttonModificar.Visible) buttonModificar_Click(sender, e);
        }

        private void buttonModificar_Click(object sender, EventArgs e)
        {
            if (dataGridView1.CurrentRow == null || dataGridView1.CurrentRow.Index < 0)
            {
                MessageBox.Show("Por favor, seleccione un cliente de la lista para modificar.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string dniActual = Convert.ToString(dataGridView1.CurrentRow.Cells["DNI"].Value) ?? string.Empty;

            if (string.IsNullOrWhiteSpace(dniActual))
            {
                MessageBox.Show("No se pudo obtener el DNI del cliente seleccionado.",
                                "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form overlay = ModalHelper.CrearOverlay(this))
            {
                overlay.Show();
                overlay.BringToFront();

                using (var formRegistro = new FormRegistrarCliente(dniActual))
                {
                    formRegistro.StartPosition = FormStartPosition.CenterScreen;

                    if (formRegistro.ShowDialog(this) == DialogResult.OK)
                    {
                        CargarDesdeBD();
                    }
                }

                overlay.Close();
            }
        }

        private void buttonNuevoCliente_Click(object sender, EventArgs e)
        {
            using (Form overlay = ModalHelper.CrearOverlay(this))
            {
                overlay.Show();
                overlay.BringToFront();

                using (var formRegistro = new FormRegistrarCliente())
                {
                    formRegistro.StartPosition = FormStartPosition.CenterScreen;

                    if (formRegistro.ShowDialog(this) == DialogResult.OK)
                    {
                        CargarDesdeBD();
                    }
                }

                overlay.Close();
            }
        }

        private void btnExportar_Click(object sender, EventArgs e)
        {
            if (dataGridView1.Rows.Count == 0)
            {
                MessageBox.Show("No hay datos para exportar.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "Archivo CSV (*.csv)|*.csv|Archivo de Texto (*.txt)|*.txt";
                sfd.Title = "Guardar tabla de clientes";
                sfd.FileName = "Clientes_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    string separador = sfd.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase) ? "," : "\t";

                    using (StreamWriter sw = new StreamWriter(sfd.FileName, false, System.Text.Encoding.UTF8))
                    {
                        List<string> cabeceras = new List<string>();
                        foreach (DataGridViewColumn col in dataGridView1.Columns)
                        {
                            cabeceras.Add(col.HeaderText);
                        }
                        sw.WriteLine(string.Join(separador, cabeceras));

                        foreach (DataGridViewRow row in dataGridView1.Rows)
                        {
                            if (row.IsNewRow) continue;

                            List<string> celdas = new List<string>();
                            foreach (DataGridViewCell cell in row.Cells)
                            {
                                string valor = cell.Value != null ? cell.Value.ToString().Replace("\"", "\"\"") : "";

                                if (separador == ",")
                                    celdas.Add($"\"{valor}\"");
                                else
                                    celdas.Add(valor);
                            }
                            sw.WriteLine(string.Join(separador, celdas));
                        }
                    }

                    MessageBox.Show("¡Tabla exportada con éxito!", "Exportación completada", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
    }
}