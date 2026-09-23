using System.Xml.Linq;
using static Guna.UI2.WinForms.Suite.Descriptions;
using static System.Net.Mime.MediaTypeNames;

namespace PROYECTO1.CapaPresentacion
{
    partial class FormTicket
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges1 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges2 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges3 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges4 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges5 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges6 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges7 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            Guna.UI2.WinForms.Suite.CustomizableEdges customizableEdges8 = new Guna.UI2.WinForms.Suite.CustomizableEdges();
            richTextBoxTicket = new RichTextBox();
            buttonImprimir = new Guna.UI2.WinForms.Guna2Button();
            buttonCerrar = new Guna.UI2.WinForms.Guna2Button();
            btnTituloTicket = new Guna.UI2.WinForms.Guna2Button();
            guna2Panel4 = new Guna.UI2.WinForms.Guna2Panel();
            guna2Panel4.SuspendLayout();
            SuspendLayout();
            // 
            // richTextBoxTicket
            // 
            richTextBoxTicket.BackColor = Color.White;
            richTextBoxTicket.BorderStyle = BorderStyle.None;
            richTextBoxTicket.Font = new System.Drawing.Font("Consolas", 9.5F);
            richTextBoxTicket.ForeColor = Color.Black;
            richTextBoxTicket.Location = new Point(25, 70);
            richTextBoxTicket.Name = "richTextBoxTicket";
            richTextBoxTicket.ReadOnly = true;
            richTextBoxTicket.Size = new Size(430, 480);
            richTextBoxTicket.TabIndex = 0;
            richTextBoxTicket.Text = "";
            // 
            // buttonImprimir
            // 
            buttonImprimir.AutoRoundedCorners = true;
            buttonImprimir.BackColor = Color.FromArgb(43, 50, 68);
            buttonImprimir.CustomizableEdges = customizableEdges1;
            buttonImprimir.DisabledState.BorderColor = Color.DarkGray;
            buttonImprimir.DisabledState.CustomBorderColor = Color.DarkGray;
            buttonImprimir.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            buttonImprimir.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            buttonImprimir.FillColor = SystemColors.ActiveCaption;
            buttonImprimir.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
            buttonImprimir.ForeColor = SystemColors.Highlight;
            buttonImprimir.Location = new Point(48, 526);
            buttonImprimir.Name = "buttonImprimir";
            buttonImprimir.ShadowDecoration.CustomizableEdges = customizableEdges2;
            buttonImprimir.Size = new Size(160, 45);
            buttonImprimir.TabIndex = 1;
            buttonImprimir.Text = "IMPRIMIR";
            buttonImprimir.Click += ButtonImprimir_Click;
            // 
            // buttonCerrar
            // 
            buttonCerrar.AutoRoundedCorners = true;
            buttonCerrar.BackColor = Color.FromArgb(43, 50, 68);
            buttonCerrar.CustomizableEdges = customizableEdges3;
            buttonCerrar.DisabledState.BorderColor = Color.DarkGray;
            buttonCerrar.DisabledState.CustomBorderColor = Color.DarkGray;
            buttonCerrar.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            buttonCerrar.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            buttonCerrar.FillColor = Color.FromArgb(255, 192, 192);
            buttonCerrar.Font = new System.Drawing.Font("Segoe UI", 9F, FontStyle.Bold);
            buttonCerrar.ForeColor = Color.FromArgb(230, 57, 70);
            buttonCerrar.Location = new Point(260, 565);
            buttonCerrar.Name = "buttonCerrar";
            buttonCerrar.ShadowDecoration.CustomizableEdges = customizableEdges4;
            buttonCerrar.Size = new Size(160, 45);
            buttonCerrar.TabIndex = 2;
            buttonCerrar.Text = "CERRAR";
            // 
            // btnTituloTicket
            // 
            btnTituloTicket.AutoRoundedCorners = true;
            btnTituloTicket.BackColor = Color.Transparent;
            btnTituloTicket.BorderColor = Color.FromArgb(43, 50, 68);
            btnTituloTicket.BorderThickness = 1;
            customizableEdges5.BottomLeft = false;
            customizableEdges5.BottomRight = false;
            btnTituloTicket.CustomizableEdges = customizableEdges5;
            btnTituloTicket.DisabledState.BorderColor = Color.DarkGray;
            btnTituloTicket.DisabledState.CustomBorderColor = Color.DarkGray;
            btnTituloTicket.DisabledState.FillColor = Color.FromArgb(169, 169, 169);
            btnTituloTicket.DisabledState.ForeColor = Color.FromArgb(141, 141, 141);
            btnTituloTicket.FillColor = Color.FromArgb(43, 50, 68);
            btnTituloTicket.Font = new System.Drawing.Font("Arial Black", 12F, FontStyle.Bold);
            btnTituloTicket.ForeColor = Color.White;
            btnTituloTicket.Location = new Point(119, 3);
            btnTituloTicket.Name = "btnTituloTicket";
            btnTituloTicket.ShadowDecoration.CustomizableEdges = customizableEdges6;
            btnTituloTicket.Size = new Size(240, 40);
            btnTituloTicket.TabIndex = 3;
            btnTituloTicket.Text = "COMPROBANTE";
            // 
            // guna2Panel4
            // 
            guna2Panel4.BorderColor = Color.FromArgb(43, 50, 68);
            guna2Panel4.BorderRadius = 25;
            guna2Panel4.Controls.Add(buttonImprimir);
            guna2Panel4.CustomBorderColor = Color.FromArgb(43, 50, 68);
            guna2Panel4.CustomizableEdges = customizableEdges7;
            guna2Panel4.FillColor = Color.FromArgb(43, 50, 68);
            guna2Panel4.Location = new Point(12, 39);
            guna2Panel4.Name = "guna2Panel4";
            guna2Panel4.ShadowDecoration.CustomizableEdges = customizableEdges8;
            guna2Panel4.Size = new Size(456, 585);
            guna2Panel4.TabIndex = 82;
            // 
            // FormTicket
            // 
            AutoScaleDimensions = new SizeF(9F, 21F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(25, 29, 36);
            ClientSize = new Size(480, 630);
            Controls.Add(buttonCerrar);
            Controls.Add(richTextBoxTicket);
            Controls.Add(guna2Panel4);
            Controls.Add(btnTituloTicket);
            FormBorderStyle = FormBorderStyle.None;
            Name = "FormTicket";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Ticket de Venta";
            guna2Panel4.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.RichTextBox richTextBoxTicket;
        private Guna.UI2.WinForms.Guna2Button buttonImprimir;
        private Guna.UI2.WinForms.Guna2Button buttonCerrar;
        private Guna.UI2.WinForms.Guna2Button btnTituloTicket;
        private Guna.UI2.WinForms.Guna2Panel guna2Panel4;
    }
}