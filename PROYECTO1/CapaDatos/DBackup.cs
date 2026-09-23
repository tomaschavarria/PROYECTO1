#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PROYECTO1.CapaDatos
{
    public class DBackup
    {
        // Cadena de conexión directa a la base master para tareas de administración
        private const string CadenaMaster =
            @"Server=localhost;Database=master;Integrated Security=True;TrustServerCertificate=True;";

        // Obtiene la lista de bases de datos del servidor, excluyendo las del sistema (master, tempdb, model, msdb)
        public List<string> ListarBasesDeDatos()
        {
            var lista = new List<string>();
            const string sql = @"SELECT name FROM sys.databases WHERE database_id > 4 ORDER BY name;";

            using var cn = new SqlConnection(CadenaMaster);
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                lista.Add(dr.GetString(0));
            }

            return lista;
        }

        // Ejecuta la copia de seguridad física mediante Transact-SQL
        public bool EjecutarBackup(string baseDatos, string rutaArchivo, out string mensajeError)
        {
            mensajeError = string.Empty;

            try
            {
                using var cn = new SqlConnection(CadenaMaster);
                cn.Open();

                // Comando parametrizado para la ruta del archivo y delimitado por corchetes para el nombre de la BD
                string sql = $@"
                BACKUP DATABASE [{baseDatos.Replace("]", "]]")}]
                TO DISK = @ruta
                WITH INIT, COPY_ONLY, STATS = 10;";

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@ruta", rutaArchivo);
                cmd.CommandTimeout = 0; // Sin límite de tiempo para no cortar bases pesadas

                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqlException ex)
            {
                mensajeError = "Error de SQL Server: " + ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                mensajeError = "Error inesperado: " + ex.Message;
                return false;
            }
        }
    }
}