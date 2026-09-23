using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace PROYECTO1.CapaPresentacion
{
    public partial class FormTicket : Form
    {
        private readonly string _contenidoTicket;

        public FormTicket(string contenidoTicket)
        {
            InitializeComponent();
            _contenidoTicket = contenidoTicket;
            richTextBoxTicket.Text = contenidoTicket;

            buttonCerrar.Click += (_, __) => this.Close();

            ModalHelper.ConfigurarTransparencia(this);
        }

        private void ButtonImprimir_Click(object sender, EventArgs e)
        {
            
            using var printDoc = new PrintDocument();
            printDoc.PrintPage += (s, ev) =>
            {
                using var fuente = new Font("Consolas", 8.5F, FontStyle.Regular);
                ev.Graphics.DrawString(_contenidoTicket, fuente, Brushes.Black, new PointF(10, 10));
            };

            using var printDialog = new PrintDialog();
            printDialog.Document = printDoc;

            if (printDialog.ShowDialog() == DialogResult.OK)
            {
                printDoc.Print();
            }
            
        }
    }
}