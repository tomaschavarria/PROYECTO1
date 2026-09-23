#pragma warning disable CS0618

using System;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DVenta
    {
        public DataTable ListarClientesActivos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_cliente, DNI_cliente, nombre, apellido FROM dbo.cliente WHERE activo = 1;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarProductosActivos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                p.id_producto, 
                ISNULL(p.nombre, '') AS nombre, 
                ISNULL(p.descripcion, '') AS descripcion, 
                ISNULL(c.nombre, 'Sin Categoría') AS categoria, 
                ISNULL(p.talla, 'Único') AS talla, 
                ISNULL(p.stock, 0) AS stock, 
                ISNULL(p.precio_venta, 0) AS precio_venta 
            FROM dbo.producto p
            INNER JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
            WHERE p.activo = 1;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarMetodosPagoActivos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_metodo_pago, nombre FROM dbo.metodo_pago WHERE activo = 1 ORDER BY id_metodo_pago;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public bool RegistrarTransaccionVenta(EVenta venta, out int idVentaGenerada, out string error)
        {
            idVentaGenerada = 0;
            error = string.Empty;

            using var cn = BaseDatos.GetConnection();
            cn.Open();
            using var tx = cn.BeginTransaction();

            try
            {
                const string sqlVenta = @"
                INSERT INTO dbo.venta (id_usuario, id_cliente, id_metodo_pago, fecha, total, cuotas, recargo)
                VALUES (@usuario, @cliente, @metodo_pago, GETDATE(), @total, @cuotas, @recargo);
                SELECT SCOPE_IDENTITY();";

                using (var cmdV = new SqlCommand(sqlVenta, cn, tx))
                {
                    cmdV.Parameters.AddWithValue("@usuario", venta.IdUsuario);
                    cmdV.Parameters.AddWithValue("@cliente", (object?)venta.IdCliente ?? DBNull.Value);
                    cmdV.Parameters.AddWithValue("@metodo_pago", venta.IdMetodoPago);
                    cmdV.Parameters.AddWithValue("@total", venta.Total);
                    cmdV.Parameters.AddWithValue("@cuotas", venta.Cuotas);
                    cmdV.Parameters.AddWithValue("@recargo", venta.Recargo);

                    idVentaGenerada = Convert.ToInt32(cmdV.ExecuteScalar());
                }

                const string sqlDetalle = @"
                INSERT INTO dbo.detalle_venta (id_venta, id_producto, cantidad, precio_unitario)
                VALUES (@venta, @producto, @cantidad, @precio_unitario);

                UPDATE dbo.producto
                SET stock = stock - @cantidad
                WHERE id_producto = @producto AND stock >= @cantidad;";

                foreach (var item in venta.Detalles)
                {
                    using var cmdD = new SqlCommand(sqlDetalle, cn, tx);
                    cmdD.Parameters.AddWithValue("@venta", idVentaGenerada);
                    cmdD.Parameters.AddWithValue("@producto", item.IdProducto);
                    cmdD.Parameters.AddWithValue("@cantidad", item.Cantidad);
                    cmdD.Parameters.AddWithValue("@precio_unitario", item.PrecioUnitario);

                    // El INSERT siempre afecta 1 fila; si el UPDATE afectó 0, es que el stock ya no alcanzaba
                    // (por ejemplo, otra venta lo consumió mientras se armaba esta). Se corta y se revierte todo.
                    int filasAfectadas = cmdD.ExecuteNonQuery();
                    if (filasAfectadas < 2)
                    {
                        throw new InvalidOperationException(
                            $"No hay stock suficiente del producto ID {item.IdProducto} para completar la venta.");
                    }
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                tx.Rollback();
                idVentaGenerada = 0;
                error = ex.Message;
                return false;
            }
        }
    }
}