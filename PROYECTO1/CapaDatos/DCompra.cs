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
                ISNULL(p.precio_costo, 0) AS precio_costo,
                ISNULL(p.precio_venta, 0) AS precio_venta
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

                // Ambas operaciones se confirman juntas. Se conserva el costo histórico
                // en detalle_compra, y en producto quedan los precios vigentes del talle.
                const string sqlDetalle = @"
                INSERT INTO dbo.detalle_compra (id_compra, id_producto, cantidad, precio_costo)
                VALUES (@compra, @producto, @cantidad, @precio_costo);";
                const string sqlProducto = @"
                UPDATE dbo.producto
                   SET stock = stock + @cantidad,
                       precio_costo = @precio_costo,
                       precio_venta = @precio_venta
                 WHERE id_producto = @producto
                   AND id_proveedor = @proveedor AND activo = 1;";

                foreach (var item in compra.Detalles)
                {
                    using (var cmdD = new SqlCommand(sqlDetalle, cn, tx))
                    {
                        cmdD.Parameters.AddWithValue("@compra", idCompraGenerada);
                        cmdD.Parameters.AddWithValue("@producto", item.IdProducto);
                        cmdD.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        var paramCostoDetalle = cmdD.Parameters.Add("@precio_costo", SqlDbType.Decimal);
                        paramCostoDetalle.Precision = 18;
                        paramCostoDetalle.Scale = 2;
                        paramCostoDetalle.Value = item.PrecioCosto;
                        if (cmdD.ExecuteNonQuery() < 1)
                            throw new InvalidOperationException("No se pudo guardar el detalle de compra.");
                    }

                    using (var cmdP = new SqlCommand(sqlProducto, cn, tx))
                    {
                        cmdP.Parameters.AddWithValue("@producto", item.IdProducto);
                        cmdP.Parameters.AddWithValue("@proveedor", compra.IdProveedor);
                        cmdP.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        var pc = cmdP.Parameters.Add("@precio_costo", SqlDbType.Decimal);
                        pc.Precision = 18;
                        pc.Scale = 2;
                        pc.Value = item.PrecioCosto;
                        var pv = cmdP.Parameters.Add("@precio_venta", SqlDbType.Decimal);
                        pv.Precision = 18;
                        pv.Scale = 2;
                        pv.Value = item.PrecioVenta;
                        if (cmdP.ExecuteNonQuery() < 1)
                            throw new InvalidOperationException(
                                $"El producto #{item.IdProducto} ya no está activo o no corresponde al proveedor. No se registró la compra.");
                    }
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