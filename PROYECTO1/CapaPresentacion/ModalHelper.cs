using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Forms;

namespace PROYECTO1.CapaPresentacion
{
    public static class ModalHelper
    {
        // Configura un formulario para que su fondo sea transparente
        // pero sus controles permanezcan completamente visibles.
        public static void ConfigurarTransparencia(Form formulario)
        {
            formulario.BackColor = Color.Black;
            formulario.TransparencyKey = Color.Black;
        }

        // Crea la capa negra semitransparente que cubre toda la pantalla.
        public static Form CrearOverlay(Form formularioPadre)
        {
            Form overlay = new Form();

            overlay.FormBorderStyle = FormBorderStyle.None;
            overlay.StartPosition = FormStartPosition.Manual;
            overlay.ShowInTaskbar = false;

            overlay.BackColor = Color.Black;
            overlay.Opacity = 0.75;

            // IMPORTANTE:
            // ocupa toda la pantalla donde está el formulario padre.
            Screen pantalla = Screen.FromControl(formularioPadre);
            overlay.Bounds = pantalla.Bounds;

            overlay.Owner = formularioPadre;

            return overlay;
        }
    }
}