using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PROYECTO1.CapaLogica;

namespace PROYECTO1.CapaPresentacion
{
    public partial class BackUp : Form
    {
        private readonly LBackup _lBackup = new();

        public BackUp()
        {
            InitializeComponent();

            this.Load += BackUp_Load;
            buttonRuta.Click += ButtonRuta_Click;
            buttonBackUp.Click += ButtonBackUp_Click;

            buttonBackUp.Enabled = false;
            textBoxRuta.TextChanged += (_, __) => HabilitarBackupSiCorresponde();
        }

        private void BackUp_Load(object sender, EventArgs e)
        {
            CargarBases();
        }

        private void CargarBases()
        {
            try
            {
                // Limpia cualquier item previo
                comboBoxBaseDatos.Items.Clear();

                // Obtenemos la lista real de la base de datos para verificar que "tiendaDB" existe
                List<string> lista = _lBackup.ObtenerBasesDeDatos();

                if (lista.Contains("tiendaDB"))
                {
                    comboBoxBaseDatos.Items.Add("tiendaDB");
                }
                else
                {
                    // Fallback por si la base no se llama exactamente así en tu entorno
                    comboBoxBaseDatos.Items.Add("Base de datos no encontrada");
                }

                // Selecciona el primer item obligatoriamente
                comboBoxBaseDatos.SelectedIndex = 0;

                // Deshabilita el ComboBox para que el usuario no pueda abrir la lista desplegable ni cambiarlo
                comboBoxBaseDatos.Enabled = false;

                HabilitarBackupSiCorresponde();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron listar las bases de datos:\n" + ex.Message,
                                "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ButtonRuta_Click(object sender, EventArgs e)
        {
            string db = comboBoxBaseDatos.SelectedItem as string ?? "tiendaDB";

            using var sfd = new SaveFileDialog
            {
                Title = "Guardar copia de seguridad",
                Filter = "Backup SQL (*.bak)|*.bak",
                FileName = $"{db}_{DateTime.Now:yyyyMMdd_HHmmss}.bak",
                OverwritePrompt = true
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                textBoxRuta.Text = sfd.FileName;
            }
        }

        private void HabilitarBackupSiCorresponde()
        {
            buttonBackUp.Enabled =
                !string.IsNullOrWhiteSpace(textBoxRuta.Text) &&
                comboBoxBaseDatos.SelectedItem != null &&
                comboBoxBaseDatos.SelectedItem.ToString() != "Base de datos no encontrada";
        }

        private void ButtonBackUp_Click(object sender, EventArgs e)
        {
            string db = comboBoxBaseDatos.SelectedItem?.ToString() ?? string.Empty;
            string ruta = textBoxRuta.Text.Trim();

            if (_lBackup.GenerarCopiaSeguridad(db, ruta, out string error))
            {
                MessageBox.Show($"¡Copia de seguridad realizada con éxito!\n\nArchivo:\n{ruta}",
                                "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show(error, "Atención", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}