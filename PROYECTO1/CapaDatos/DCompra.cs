#pragma warning disable CS0618

using System;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DCompra
    {
        public DataTable ListarProveedoresActivos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT * FROM dbo.proveedor WHERE activo = 1;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarProductosParaCompra()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                p.id_producto, 
                p.id_proveedor, 
                ISNULL(p.nombre, '') AS nombre, 
                ISNULL(c.nombre, 'Sin Categoría') AS categoria, 
                ISNULL(p.talla, 'Único') AS talla, 
                ISNULL(p.stock, 0) AS stock, 
                ISNULL(p.precio_costo, 0) AS precio_costo 
            FROM dbo.producto p
            LEFT JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
            WHERE p.activo = 1;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public bool RegistrarTransaccionCompra(ECompra compra, out int idCompraGenerada, out string error)
        {
            idCompraGenerada = 0;
            error = string.Empty;

            using var cn = BaseDatos.GetConnection();
            cn.Open();
            using var tx = cn.BeginTransaction();

            try
            {
                const string sqlCompra = @"
                INSERT INTO dbo.compra (id_usuario, id_proveedor, id_metodo_pago, fecha, total)
                VALUES (@usuario, @proveedor, @metodo_pago, GETDATE(), @total);
                SELECT SCOPE_IDENTITY();";

                using (var cmdC = new SqlCommand(sqlCompra, cn, tx))
                {
                    cmdC.Parameters.AddWithValue("@usuario", compra.IdUsuario);
                    cmdC.Parameters.AddWithValue("@proveedor", compra.IdProveedor);
                    cmdC.Parameters.AddWithValue("@metodo_pago", compra.IdMetodoPago);
                    cmdC.Parameters.AddWithValue("@total", compra.Total);

                    idCompraGenerada = Convert.ToInt32(cmdC.ExecuteScalar());
                }

                const string sqlDetalle = @"
                INSERT INTO dbo.detalle_compra (id_compra, id_producto, cantidad, precio_costo)
                VALUES (@compra, @producto, @cantidad, @precio_costo);

                UPDATE dbo.producto
                SET stock = stock + @cantidad,
                    precio_costo = @precio_costo
                WHERE id_producto = @producto;";

                foreach (var item in compra.Detalles)
                {
                    using var cmdD = new SqlCommand(sqlDetalle, cn, tx);
                    cmdD.Parameters.AddWithValue("@compra", idCompraGenerada);
                    cmdD.Parameters.AddWithValue("@producto", item.IdProducto);
                    cmdD.Parameters.AddWithValue("@cantidad", item.Cantidad);
                    cmdD.Parameters.AddWithValue("@precio_costo", item.PrecioCosto);
                    cmdD.ExecuteNonQuery();
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                idCompraGenerada = 0;
                error = ex.Message;
                return false;
            }
        }
    }
}