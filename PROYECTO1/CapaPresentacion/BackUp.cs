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
            comboBoxBaseDatos.SelectedIndexChanged += (_, __) => HabilitarBackupSiCorresponde();
            textBoxRuta.TextChanged += (_, __) => HabilitarBackupSiCorresponde();
        }

        private void BackUp_Load(object sender, EventArgs e)
        {
            CargarBases();
        }

        // Carga el desplegable con las bases encontradas en el motor SQL
        private void CargarBases()
        {
            try
            {
                List<string> lista = _lBackup.ObtenerBasesDeDatos();
                comboBoxBaseDatos.DataSource = lista;

                if (lista.Count > 0)
                    comboBoxBaseDatos.SelectedIndex = 0;

                HabilitarBackupSiCorresponde();
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudieron listar las bases de datos:\n" + ex.Message,
                                "Error de conexión", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Abre el cuadro de diálogo para elegir la carpeta y nombre del .bak
        private void ButtonRuta_Click(object sender, EventArgs e)
        {
            string db = comboBoxBaseDatos.SelectedItem as string ?? "backup";

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

        // Habilita el botón de backup únicamente si hay base seleccionada y ruta elegida
        private void HabilitarBackupSiCorresponde()
        {
            buttonBackUp.Enabled =
                !string.IsNullOrWhiteSpace(textBoxRuta.Text) &&
                comboBoxBaseDatos.SelectedItem != null;
        }

        // Llama a la lógica para ejecutar la copia de seguridad
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