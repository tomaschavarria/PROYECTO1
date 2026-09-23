using System;
using System.Collections.Generic;
using System.IO;
using PROYECTO1.CapaDatos;

namespace PROYECTO1.CapaLogica
{
    public class LBackup
    {
        private readonly DBackup _dBackup = new();

        // Solicita la lista de bases de datos disponibles a la Capa de Datos
        public List<string> ObtenerBasesDeDatos()
        {
            return _dBackup.ListarBasesDeDatos();
        }

        // Valida las rutas y solicita a la Capa de Datos generar el archivo .bak
        public bool GenerarCopiaSeguridad(string baseDatos, string rutaDestino, out string mensaje)
        {
            mensaje = string.Empty;

            if (string.IsNullOrWhiteSpace(rutaDestino))
            {
                mensaje = "Debe especificar una ruta de guardado.";
                return false;
            }

            // Si el archivo no termina en .bak, se lo agregamos automáticamente
            if (!rutaDestino.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
            {
                rutaDestino += ".bak";
            }

            return _dBackup.EjecutarBackup(baseDatos, rutaDestino, out mensaje);
        }
    }
}