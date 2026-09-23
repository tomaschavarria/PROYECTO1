using System;
using System.Windows.Forms;
//Dan permiso a este formulario  para poder instanciar formularios como InicioSesion y Bienvenida
using PROYECTO1.CapaPresentacion;

namespace PROYECTO1
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();

            using (var login = new InicioSesion()) //Crea en memoria el formulario de login (new).  Se instancia para pedir las credenciales al operador.

            {
                var result = login.ShowDialog();  //Abre la ventana de login en modo modal. Detiene la ejecución en esa línea exacta: el código no pasa a la siguiente línea hasta que el usuario cierre el login o presione un botón


                //	○ result != DialogResult.OK: Evalúa si el usuario cerró la ventana con la cruz (X) o presionó cancelar. 
                //  ○ SesionActual.Usuario == null: Evalúa si no se cargó un usuario inválido
                //  return;: Finaliza inmediatamente el método Main(), evitando que se abra el sistema si no hay autenticación válida. 
                if (result != DialogResult.OK || SesionActual.Usuario == null)
                    return;

                Application.Run(new Bienvenida()); // Se instancia el formulario de bienvenidda una vez validada la sesión

            }
        }
    }
}