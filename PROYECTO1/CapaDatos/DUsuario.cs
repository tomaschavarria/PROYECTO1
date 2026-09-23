#pragma warning disable CS0618

using System;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DUsuario
    {
        public DataTable ListarUsuarios()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                u.id_usuario,
                u.id_rol,
                r.nombre AS Rol,
                u.nombre,
                u.apellido,
                u.nombre_usuario,
                u.DNI_usuario,
                u.correo,
                u.telefono,
                u.direccion,
                u.[contraseña],
                u.fecha_de_nacimiento,
                u.foto,
                CAST(CASE WHEN u.estado = 1 THEN 1 ELSE 0 END AS bit) AS Activo
            FROM dbo.usuario u
            INNER JOIN dbo.rol r ON u.id_rol = r.id_rol
            ORDER BY u.id_usuario DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarRoles()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_rol, nombre FROM dbo.rol ORDER BY nombre;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public EUsuario? ObtenerPorId(int id)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT * FROM dbo.usuario WHERE id_usuario = @id;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new EUsuario
                {
                    IdUsuario = Convert.ToInt32(dr["id_usuario"]),
                    IdRol = Convert.ToInt32(dr["id_rol"]),
                    Nombre = dr["nombre"].ToString() ?? string.Empty,
                    Apellido = dr["apellido"].ToString() ?? string.Empty,
                    NombreUsuario = dr["nombre_usuario"].ToString() ?? string.Empty,
                    Contraseña = dr["contraseña"].ToString() ?? string.Empty,
                    Dni = Convert.ToInt32(dr["DNI_usuario"]),
                    Correo = dr["correo"] != DBNull.Value ? dr["correo"].ToString() : null,
                    Telefono = dr["telefono"] != DBNull.Value ? dr["telefono"].ToString() : null,
                    Direccion = dr["direccion"] != DBNull.Value ? dr["direccion"].ToString() : null,
                    Nacimiento = dr["fecha_de_nacimiento"] != DBNull.Value ? Convert.ToDateTime(dr["fecha_de_nacimiento"]) : null,
                    Estado = Convert.ToBoolean(dr["estado"]),
                    Foto = dr["foto"] != DBNull.Value ? dr["foto"].ToString() : null
                };
            }
            return null;
        }

        public bool ExisteUsuario(string nombreUsuario, int dni, int idExcluir = 0)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT COUNT(1) 
            FROM dbo.usuario 
            WHERE (nombre_usuario = @usuario OR DNI_usuario = @dni)
              AND (@id = 0 OR id_usuario <> @id);";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@usuario", nombreUsuario);
            cmd.Parameters.AddWithValue("@dni", dni);
            cmd.Parameters.AddWithValue("@id", idExcluir);

            cn.Open();
            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public bool Insertar(EUsuario user)
        {
            const string sql = @"
            INSERT INTO dbo.usuario
             (id_rol, nombre, apellido, nombre_usuario, [contraseña], DNI_usuario,
              correo, telefono, direccion, fecha_de_nacimiento, foto, estado)
            VALUES
             (@rol, @nombre, @apellido, @usuario, @clave, @dni,
              @correo, @telefono, @direccion, @fechaNacimiento, @foto, @estado);";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@rol", user.IdRol);
            cmd.Parameters.AddWithValue("@nombre", user.Nombre);
            cmd.Parameters.AddWithValue("@apellido", user.Apellido);
            cmd.Parameters.AddWithValue("@usuario", user.NombreUsuario);
            cmd.Parameters.AddWithValue("@clave", user.Contraseña);
            cmd.Parameters.AddWithValue("@dni", user.Dni);
            cmd.Parameters.AddWithValue("@correo", (object?)user.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", (object?)user.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@direccion", (object?)user.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fechaNacimiento", (object?)user.Nacimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@foto", string.IsNullOrEmpty(user.Foto) ? (object)DBNull.Value : user.Foto);
            cmd.Parameters.AddWithValue("@estado", user.Estado ? 1 : 0);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Modificar(EUsuario usuario)
        {
            const string sql = @"
            UPDATE dbo.usuario
               SET id_rol = @rol,
                   nombre = @nombre,
                   apellido = @apellido,
                   nombre_usuario = @usuario,
                   [contraseña] = @clave,
                   DNI_usuario = @dni,
                   correo = @correo,
                   telefono = @telefono,
                   direccion = @direccion,
                   fecha_de_nacimiento = @fechaNacimiento,
                   foto = @foto
             WHERE id_usuario = @id;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@rol", usuario.IdRol);
            cmd.Parameters.AddWithValue("@nombre", usuario.Nombre);
            cmd.Parameters.AddWithValue("@apellido", usuario.Apellido);
            cmd.Parameters.AddWithValue("@usuario", usuario.NombreUsuario);
            cmd.Parameters.AddWithValue("@clave", usuario.Contraseña);
            cmd.Parameters.AddWithValue("@dni", usuario.Dni);
            cmd.Parameters.AddWithValue("@correo", (object?)usuario.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@telefono", (object?)usuario.Telefono ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@direccion", (object?)usuario.Direccion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@fechaNacimiento", (object?)usuario.Nacimiento ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@foto", string.IsNullOrEmpty(usuario.Foto) ? (object)DBNull.Value : usuario.Foto);
            cmd.Parameters.AddWithValue("@id", usuario.IdUsuario);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool CambiarEstado(int idUsuario, bool nuevoEstado)
        {
            const string sql = "UPDATE dbo.usuario SET estado = @estado WHERE id_usuario = @id;";
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@estado", nuevoEstado ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", idUsuario);
            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public UsuarioSesion? Autenticar(string usuario, string claveHasheada)
        {
            const string sql = @"
            SELECT TOP 1 id_usuario, nombre_usuario, nombre, apellido, id_rol, foto, estado
            FROM dbo.usuario
            WHERE nombre_usuario = @usuario AND [contraseña] = @clave;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@usuario", usuario);
            cmd.Parameters.AddWithValue("@clave", claveHasheada);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new UsuarioSesion
                {
                    Id = dr.GetInt32(dr.GetOrdinal("id_usuario")),
                    NombreUsuario = dr.GetString(dr.GetOrdinal("nombre_usuario")),
                    Nombre = dr.GetString(dr.GetOrdinal("nombre")),
                    Apellido = dr.GetString(dr.GetOrdinal("apellido")),
                    IdRol = dr.GetInt32(dr.GetOrdinal("id_rol")),
                    Foto = dr["foto"] != DBNull.Value ? dr["foto"].ToString() : string.Empty,
                    Activo = Convert.ToBoolean(dr["estado"])
                };
            }
            return null;
        }
    }
}