#pragma warning disable CS0618 

using System;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DCliente
    {
        public DataTable ListarClientes()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();

            const string sql = @"
            SELECT 
                id_cliente,
                nombre AS Nombre,
                apellido AS Apellido,
                DNI_cliente AS DNI,
                telefono AS Teléfono,
                direccion AS Dirección,
                correo AS Correo,
                nacimiento AS Nacimiento,
                activo AS Actividad
            FROM dbo.cliente
            ORDER BY id_cliente DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);

            return dt;
        }

        public bool ExisteDni(int dni, int idClienteExcluir = 0)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT COUNT(1) 
            FROM dbo.cliente 
            WHERE DNI_cliente = @dni 
              AND (@idExcluir = 0 OR id_cliente <> @idExcluir);";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@dni", dni);
            cmd.Parameters.AddWithValue("@idExcluir", idClienteExcluir);
            cn.Open();

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool Insertar(ECliente cli)
        {
            const string sql = @"
            INSERT INTO dbo.cliente 
                (DNI_cliente, nombre, apellido, telefono, direccion, correo, nacimiento, activo)
            VALUES 
                (@dni, @nom, @ape, @tel, @dir, @cor, @nac, @act);";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@dni", cli.Dni);
            cmd.Parameters.AddWithValue("@nom", cli.Nombre);
            cmd.Parameters.AddWithValue("@ape", cli.Apellido);
            cmd.Parameters.AddWithValue("@tel", (object?)cli.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dir", (object?)cli.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cor", (object?)cli.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nac", (object?)cli.Nacimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@act", cli.Activo ? 1 : 0);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Modificar(ECliente cli)
        {
            // El estado (activo) NO se actualiza acá: solo cambia con CambiarEstadoPorDni desde la grilla.
            // Así, editar un cliente desactivado no lo reactiva sin querer.
            const string sql = @"
            UPDATE dbo.cliente
            SET 
                DNI_cliente = @dni,
                nombre = @nom,
                apellido = @ape,
                telefono = @tel,
                direccion = @dir,
                correo = @cor,
                nacimiento = @nac
            WHERE id_cliente = @id;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", cli.IdCliente);
            cmd.Parameters.AddWithValue("@dni", cli.Dni);
            cmd.Parameters.AddWithValue("@nom", cli.Nombre);
            cmd.Parameters.AddWithValue("@ape", cli.Apellido);
            cmd.Parameters.AddWithValue("@tel", (object?)cli.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@dir", (object?)cli.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cor", (object?)cli.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nac", (object?)cli.Nacimiento ?? DBNull.Value);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public ECliente? ObtenerPorDni(int dni)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT * FROM dbo.cliente WHERE DNI_cliente = @dni;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@dni", dni);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new ECliente
                {
                    IdCliente = Convert.ToInt32(dr["id_cliente"]),
                    Dni = Convert.ToInt32(dr["DNI_cliente"]),
                    Nombre = dr["nombre"].ToString() ?? string.Empty,
                    Apellido = dr["apellido"].ToString() ?? string.Empty,
                    Telefono = dr["telefono"] != DBNull.Value ? dr["telefono"].ToString() : null,
                    Direccion = dr["direccion"] != DBNull.Value ? dr["direccion"].ToString() : null,
                    Correo = dr["correo"] != DBNull.Value ? dr["correo"].ToString() : null,
                    Nacimiento = dr["nacimiento"] != DBNull.Value ? Convert.ToDateTime(dr["nacimiento"]) : null,
                    Activo = Convert.ToBoolean(dr["activo"])
                };
            }
            return null;
        }

        public bool CambiarEstadoPorDni(int dni, bool nuevoEstado)
        {
            const string sql = "UPDATE dbo.cliente SET activo = @act WHERE DNI_cliente = @dni;";
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@dni", dni);
            cmd.Parameters.AddWithValue("@act", nuevoEstado ? 1 : 0);
            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}