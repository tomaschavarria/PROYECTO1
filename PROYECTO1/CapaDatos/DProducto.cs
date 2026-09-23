#pragma warning disable CS0618

using System;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DProducto
    {
        public DataTable ListarProductos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
        SELECT 
            p.id_producto,
            p.nombre AS Nombre,
            ISNULL(p.descripcion, '') AS [Descripción],
            ISNULL(c.nombre, 'Sin Categoría') AS Categoria,
            ISNULL(p.stock, 0) AS Stock,
            ISNULL(p.precio_venta, 0) AS PrecioVenta,
            ISNULL(p.precio_costo, 0) AS PrecioCosto,
            ISNULL(p.talla, 'Único') AS Talle,
            ISNULL(pr.razon_social, 'Sin proveedor') AS Proveedor,
            p.activo AS Activo
        FROM dbo.producto p
        LEFT JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
        LEFT JOIN dbo.proveedor pr ON p.id_proveedor = pr.id_proveedor
        ORDER BY p.id_producto DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarCategoriasActivas()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_categoria, nombre FROM dbo.categoria WHERE activo = 1 ORDER BY nombre;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarProveedoresActivos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            // Se agrega 'cuit' a la consulta para que el buscador pueda leerlo
            const string sql = "SELECT id_proveedor, cuit, razon_social FROM dbo.proveedor WHERE activo = 1 ORDER BY razon_social;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        // Trae un proveedor por id sin importar si está activo (para editar productos cuyo proveedor fue desactivado)
        public DataTable ObtenerProveedorPorId(int id)
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_proveedor, cuit, razon_social FROM dbo.proveedor WHERE id_proveedor = @id;";
            using var da = new SqlDataAdapter(sql, cn);
            da.SelectCommand.Parameters.AddWithValue("@id", id);
            da.Fill(dt);
            return dt;
        }

        // Considera "el mismo producto" cuando coinciden nombre + talle + proveedor
        // (dos talles del mismo nombre son productos distintos, y el mismo nombre en otro proveedor también)
        public bool ExisteProducto(string nombre, string? talle, int idProveedor, int idExcluir = 0)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT COUNT(1)
            FROM dbo.producto
            WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
              AND LOWER(LTRIM(RTRIM(ISNULL(talla, 'Único')))) = LOWER(LTRIM(RTRIM(ISNULL(@talla, 'Único'))))
              AND id_proveedor = @proveedor
              AND (@idExcluir = 0 OR id_producto <> @idExcluir);";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@nombre", nombre ?? string.Empty);
            cmd.Parameters.AddWithValue("@talla", (object?)talle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@proveedor", idProveedor);
            cmd.Parameters.AddWithValue("@idExcluir", idExcluir);
            cn.Open();

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        public EProducto? ObtenerPorId(int id)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT * FROM dbo.producto WHERE id_producto = @id;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new EProducto
                {
                    IdProducto = Convert.ToInt32(dr["id_producto"]),
                    IdCategoria = Convert.ToInt32(dr["id_categoria"]),
                    IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
                    Nombre = dr["nombre"].ToString() ?? string.Empty,
                    Descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : null,
                    Talle = dr["talla"] != DBNull.Value ? dr["talla"].ToString() : null,
                    Stock = Convert.ToInt32(dr["stock"]),
                    PrecioCosto = Convert.ToDecimal(dr["precio_costo"]),
                    PrecioVenta = Convert.ToDecimal(dr["precio_venta"]),
                    Activo = Convert.ToBoolean(dr["activo"])
                };
            }
            return null;
        }

        public bool Insertar(EProducto prod)
        {
            const string sql = @"
            INSERT INTO dbo.producto 
                (id_categoria, id_proveedor, nombre, descripcion, talla, stock, precio_costo, precio_venta, activo)
            VALUES 
                (@cat, @prov, @nom, @desc, @tal, @stock, @pc, @pv, @act);";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cat", prod.IdCategoria);
            cmd.Parameters.AddWithValue("@prov", prod.IdProveedor);
            cmd.Parameters.AddWithValue("@nom", prod.Nombre);
            cmd.Parameters.AddWithValue("@desc", (object?)prod.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tal", (object?)prod.Talle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@stock", prod.Stock);
            cmd.Parameters.AddWithValue("@pc", prod.PrecioCosto);
            cmd.Parameters.AddWithValue("@pv", prod.PrecioVenta);
            cmd.Parameters.AddWithValue("@act", prod.Activo ? 1 : 0);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Modificar(EProducto prod)
        {
            // Stock y estado NO se actualizan acá: el stock solo cambia por compras/ventas y el estado por CambiarEstado.
            // Así no se pisan cambios hechos mientras el formulario de edición estaba abierto.
            const string sql = @"
            UPDATE dbo.producto
            SET id_categoria = @cat,
                id_proveedor = @prov,
                nombre = @nom,
                descripcion = @desc,
                talla = @tal,
                precio_costo = @pc,
                precio_venta = @pv
            WHERE id_producto = @id;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cat", prod.IdCategoria);
            cmd.Parameters.AddWithValue("@prov", prod.IdProveedor);
            cmd.Parameters.AddWithValue("@nom", prod.Nombre);
            cmd.Parameters.AddWithValue("@desc", (object?)prod.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tal", (object?)prod.Talle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pc", prod.PrecioCosto);
            cmd.Parameters.AddWithValue("@pv", prod.PrecioVenta);
            cmd.Parameters.AddWithValue("@id", prod.IdProducto);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool CambiarEstado(int idProducto, bool nuevoEstado)
        {
            const string sql = "UPDATE dbo.producto SET activo = @act WHERE id_producto = @id;";
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@act", nuevoEstado ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", idProducto);
            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool RegistrarCategoria(string nombre, out int idGenerado, out string error)
        {
            idGenerado = 0;
            error = string.Empty;

            try
            {
                using var cn = BaseDatos.GetConnection();
                const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.categoria WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre))))
            BEGIN
                INSERT INTO dbo.categoria (nombre) VALUES (@nombre);
                SELECT SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                SELECT id_categoria FROM dbo.categoria WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)));
            END";

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@nombre", nombre.Trim());
                cn.Open();

                object res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    idGenerado = Convert.ToInt32(res);
                    return true;
                }

                error = "No se pudo registrar la categoría.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}