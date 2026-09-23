#pragma warning disable CS0618

using System;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DProveedor
    {
        public DataTable ListarProveedores()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
        SELECT 
            id_proveedor,
            razon_social AS [Razón Social],
            cuit AS CUIT,
            ISNULL(telefono, '') AS [Teléfono],
            ISNULL(correo, '') AS Correo,
            ISNULL(direccion, '') AS [Dirección],
            activo AS Actividad
        FROM dbo.proveedor
        ORDER BY id_proveedor DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public bool ExisteCuit(string cuit, int idExcluir = 0)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT COUNT(1) 
            FROM dbo.proveedor 
            WHERE REPLACE(REPLACE(cuit, '-', ''), ' ', '') = @cuit AND (@idExcluir = 0 OR id_proveedor <> @idExcluir);";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cuit", cuit);
            cmd.Parameters.AddWithValue("@idExcluir", idExcluir);
            cn.Open();

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool Insertar(EProveedor prov)
        {
            const string sql = @"
            INSERT INTO dbo.proveedor (cuit, razon_social, telefono, correo, direccion, activo)
            VALUES (@cuit, @razon, @tel, @cor, @dir, @act);";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cuit", prov.Cuit);
            cmd.Parameters.AddWithValue("@razon", prov.RazonSocial);
            cmd.Parameters.AddWithValue("@tel", (object?)prov.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cor", (object?)prov.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dir", (object?)prov.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@act", prov.Activo ? 1 : 0);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Modificar(EProveedor prov)
        {
            // El estado (activo) NO se actualiza acá: solo cambia con CambiarEstado desde la grilla.
            // Así, editar un proveedor desactivado no lo reactiva sin querer.
            const string sql = @"
            UPDATE dbo.proveedor
            SET cuit = @cuit,
                razon_social = @razon,
                telefono = @tel,
                correo = @cor,
                direccion = @dir
            WHERE id_proveedor = @id;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cuit", prov.Cuit);
            cmd.Parameters.AddWithValue("@razon", prov.RazonSocial);
            cmd.Parameters.AddWithValue("@tel", (object?)prov.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cor", (object?)prov.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dir", (object?)prov.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@id", prov.IdProveedor);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool CambiarEstado(int idProveedor, bool nuevoEstado)
        {
            const string sql = "UPDATE dbo.proveedor SET activo = @act WHERE id_proveedor = @id;";
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@act", nuevoEstado ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", idProveedor);
            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public EProveedor? ObtenerPorId(int id)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT * FROM dbo.proveedor WHERE id_proveedor = @id;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new EProveedor
                {
                    IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
                    Cuit = dr["cuit"].ToString() ?? string.Empty,
                    RazonSocial = dr["razon_social"].ToString() ?? string.Empty,
                    Telefono = dr["telefono"] != DBNull.Value ? dr["telefono"].ToString() : null,
                    Correo = dr["correo"] != DBNull.Value ? dr["correo"].ToString() : null,
                    Direccion = dr["direccion"] != DBNull.Value ? dr["direccion"].ToString() : null,
                    Activo = Convert.ToBoolean(dr["activo"])
                };
            }
            return null;
        }
    }
}