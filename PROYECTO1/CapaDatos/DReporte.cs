#pragma warning disable CS0618

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DReporte
    {
        // =======================================================================================
        // HELPER PARA FILTROS DINÁMICOS DEL VENDEDOR Y GERENTE
        // =======================================================================================
        private void AgregarFiltrosVenta(SqlCommand cmd, ref string sql, DateTime? desde, DateTime? hasta, string tipoCliente, string metodoPago)
        {
            if (desde.HasValue)
            {
                sql += " AND v.fecha >= @desde";
                cmd.Parameters.AddWithValue("@desde", desde.Value.Date);
            }
            if (hasta.HasValue)
            {
                sql += " AND v.fecha < @hasta";
                cmd.Parameters.AddWithValue("@hasta", hasta.Value.Date.AddDays(1));
            }
            if (!string.IsNullOrEmpty(tipoCliente) && tipoCliente != "Todos")
            {
                if (tipoCliente == "Consumidor Final")
                    sql += " AND v.id_cliente IS NULL";
                else if (tipoCliente == "Registrados")
                    sql += " AND v.id_cliente IS NOT NULL";
            }
            if (!string.IsNullOrEmpty(metodoPago) && metodoPago != "Todos")
            {
                sql += " AND mp.nombre = @metodoPago";
                cmd.Parameters.AddWithValue("@metodoPago", metodoPago);
            }
        }

        private void AgregarFiltrosGerente(SqlCommand cmd, ref string sql, DateTime? desde, DateTime? hasta, string tipoCliente, string metodoPago, string aliasVenta = "v")
        {
            if (desde.HasValue)
            {
                sql += $" AND {aliasVenta}.fecha >= @desde";
                if (!cmd.Parameters.Contains("@desde")) cmd.Parameters.AddWithValue("@desde", desde.Value.Date);
            }
            if (hasta.HasValue)
            {
                sql += $" AND {aliasVenta}.fecha < @hasta";
                if (!cmd.Parameters.Contains("@hasta")) cmd.Parameters.AddWithValue("@hasta", hasta.Value.Date.AddDays(1));
            }
            if (tipoCliente == "Consumidor Final")
            {
                sql += $" AND {aliasVenta}.id_cliente IS NULL";
            }
            else if (tipoCliente == "Registrados")
            {
                sql += $" AND {aliasVenta}.id_cliente IS NOT NULL";
            }
            if (metodoPago != "Todos")
            {
                sql += $" AND EXISTS (SELECT 1 FROM dbo.metodo_pago mp WHERE mp.id_metodo_pago = {aliasVenta}.id_metodo_pago AND mp.nombre = @metodoPago)";
                if (!cmd.Parameters.Contains("@metodoPago")) cmd.Parameters.AddWithValue("@metodoPago", metodoPago);
            }
        }

        // =======================================================================================
        // REPORTES DEL VENDEDOR
        // =======================================================================================

        public EReporteKpiHistorico ObtenerKpisHistoricos(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var kpi = new EReporteKpiHistorico();

            // CORRECCIÓN: Se reemplaza el Ticket Promedio por el conteo de clientes únicos (Clientes Atendidos).
            // Para Consumidor Final (id_cliente NULL) se asume como 1 cliente global para no sobredimensionar.
            string sql = @"
            SELECT 
                ISNULL(SUM(v.total), 0) AS TotalFacturado,
                COUNT(v.id_venta) AS CantidadVentas,
                COUNT(CASE WHEN v.id_cliente IS NULL THEN 1 END) AS VentasConsumidorFinal,
                COUNT(DISTINCT v.id_cliente) + CASE WHEN MAX(CASE WHEN v.id_cliente IS NULL THEN 1 ELSE 0 END) = 1 THEN 1 ELSE 0 END AS ClientesAtendidos
            FROM dbo.venta v
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            WHERE v.id_usuario = @idUsuario";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

            AgregarFiltrosVenta(cmd, ref sql, desde, hasta, tipoCliente, metodoPago);
            cmd.CommandText = sql + ";";

            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpi.TotalFacturado = Convert.ToDecimal(dr["TotalFacturado"]);
                kpi.CantidadVentas = Convert.ToInt32(dr["CantidadVentas"]);
                kpi.VentasConsumidorFinal = Convert.ToInt32(dr["VentasConsumidorFinal"]);
                kpi.ClientesAtendidos = Convert.ToInt32(dr["ClientesAtendidos"]);
            }

            return kpi;
        }

        public EReporteKpiMes ObtenerKpisMes(int idUsuario, DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior)
        {
            var kpi = new EReporteKpiMes();
            const string sql = @"
            SELECT 
                ISNULL(SUM(CASE WHEN fecha >= @inicioActual AND fecha <= @finActual THEN total ELSE 0 END), 0) AS VentasActual,
                ISNULL(SUM(CASE WHEN fecha >= @inicioAnterior AND fecha <= @finAnterior THEN total ELSE 0 END), 0) AS VentasAnterior,
                COUNT(DISTINCT CASE WHEN fecha >= @inicioActual AND fecha <= @finActual THEN id_venta END) AS OrdenesActual,
                COUNT(DISTINCT CASE WHEN fecha >= @inicioAnterior AND fecha <= @finAnterior THEN id_venta END) AS OrdenesAnterior,
                COUNT(CASE WHEN fecha >= @inicioActual AND fecha <= @finActual AND id_cliente IS NULL THEN 1 END) AS ConsumidorFinalActual,
                COUNT(CASE WHEN fecha >= @inicioAnterior AND fecha <= @finAnterior AND id_cliente IS NULL THEN 1 END) AS ConsumidorFinalAnterior
            FROM dbo.venta
            WHERE id_usuario = @idUsuario
              AND ((fecha >= @inicioActual AND fecha <= @finActual) OR (fecha >= @inicioAnterior AND fecha <= @finAnterior));";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
            cmd.Parameters.AddWithValue("@inicioActual", inicioActual);
            cmd.Parameters.AddWithValue("@finActual", finActual);
            cmd.Parameters.AddWithValue("@inicioAnterior", inicioAnterior);
            cmd.Parameters.AddWithValue("@finAnterior", finAnterior);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpi.VentasActual = Convert.ToDecimal(dr["VentasActual"]);
                kpi.VentasAnterior = Convert.ToDecimal(dr["VentasAnterior"]);
                kpi.OrdenesActual = Convert.ToInt32(dr["OrdenesActual"]);
                kpi.OrdenesAnterior = Convert.ToInt32(dr["OrdenesAnterior"]);
                kpi.ConsumidorFinalActual = Convert.ToInt32(dr["ConsumidorFinalActual"]);
                kpi.ConsumidorFinalAnterior = Convert.ToInt32(dr["ConsumidorFinalAnterior"]);
            }

            return kpi;
        }

        public Dictionary<DateTime, decimal> ObtenerVentasMensualesAgrupadas(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var datos = new Dictionary<DateTime, decimal>();
            string sql = @"
            SELECT 
                DATEFROMPARTS(YEAR(v.fecha), MONTH(v.fecha), 1) AS Periodo,
                ISNULL(SUM(v.total), 0) AS TotalMes
            FROM dbo.venta v
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            WHERE v.id_usuario = @idUsuario";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

            AgregarFiltrosVenta(cmd, ref sql, desde, hasta, tipoCliente, metodoPago);

            sql += @" 
            GROUP BY DATEFROMPARTS(YEAR(v.fecha), MONTH(v.fecha), 1)
            ORDER BY Periodo ASC;";

            cmd.CommandText = sql;
            cn.Open();

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                DateTime periodo = Convert.ToDateTime(dr["Periodo"]);
                decimal total = Convert.ToDecimal(dr["TotalMes"]);
                datos[periodo] = total;
            }

            return datos;
        }

        public DataTable ObtenerTopCategorias(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            string sql = @"
            SELECT TOP 6
                ISNULL(c.nombre, 'Sin Categoría') AS Categoria,
                COUNT(DISTINCT v.id_venta) AS CantidadOrdenes
            FROM dbo.venta v
            INNER JOIN dbo.detalle_venta dv ON v.id_venta = dv.id_venta
            INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
            INNER JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            WHERE v.id_usuario = @idUsuario";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

            AgregarFiltrosVenta(cmd, ref sql, desde, hasta, tipoCliente, metodoPago);

            sql += @" 
            GROUP BY c.nombre
            ORDER BY CantidadOrdenes DESC;";

            cmd.CommandText = sql;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public DataTable ListarHistorialVentas(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            string sql = @"
            SELECT 
                v.id_venta AS NroVenta,
                CONVERT(VARCHAR(16), v.fecha, 120) AS Fecha,
                ISNULL(cli.nombre + ' ' + cli.apellido, 'Consumidor Final') AS Cliente,
                ISNULL(CAST(cli.DNI_cliente AS VARCHAR), '00000000') AS DNI,
                CASE 
                    WHEN mp.nombre COLLATE Latin1_General_CI_AI = 'Credito' AND v.cuotas > 1 THEN CONCAT(mp.nombre, ' (', v.cuotas, ' ctas)')
                    ELSE mp.nombre
                END AS MetodoPago,
                v.total AS Total,
                CASE WHEN v.id_cliente IS NULL THEN 1 ELSE 0 END AS EsConsumidorFinal
            FROM dbo.venta v
            LEFT JOIN dbo.cliente cli ON v.id_cliente = cli.id_cliente -- ¡AQUÍ ESTABA EL ERROR CORREGIDO!
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            WHERE v.id_usuario = @idUsuario";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand();
            cmd.Connection = cn;
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);

            AgregarFiltrosVenta(cmd, ref sql, desde, hasta, tipoCliente, metodoPago);

            sql += " ORDER BY v.fecha DESC;";
            cmd.CommandText = sql;

            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        // =======================================================================================
        // COMPROBANTES Y ADMIN
        // =======================================================================================

        public DataTable ObtenerCabeceraComprobante(int idVenta)
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                v.fecha, mp.nombre AS metodo_pago, v.cuotas, v.recargo, v.total,
                ISNULL(c.nombre + ' ' + c.apellido, 'Consumidor Final') AS Cliente,
                ISNULL(CAST(c.DNI_cliente AS VARCHAR), '00000000') AS DNI,
                u.nombre + ' ' + u.apellido AS Vendedor
            FROM dbo.venta v
            LEFT JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
            INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            WHERE v.id_venta = @idVenta;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idVenta", idVenta);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public DataTable ObtenerDetalleComprobante(int idVenta)
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                p.nombre,
                ISNULL(p.talla, 'Único') AS talla,
                dv.cantidad,
                dv.precio_unitario,
                (dv.cantidad * dv.precio_unitario) AS subtotal
            FROM dbo.detalle_venta dv
            INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
            WHERE dv.id_venta = @idVenta;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idVenta", idVenta);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public (int TotalUsers, int InactUsers, int TotalCli, int InactCli, int TotalProd, int InactProd, int TotalProv, int InactProv) ObtenerKpisAdmin()
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                (SELECT COUNT(*) FROM dbo.usuario WHERE estado = 1) AS TotalUsers,
                (SELECT COUNT(*) FROM dbo.usuario WHERE estado = 0) AS InactUsers,
                (SELECT COUNT(*) FROM dbo.cliente WHERE activo = 1) AS TotalCli,
                (SELECT COUNT(*) FROM dbo.cliente WHERE activo = 0) AS InactCli,
                (SELECT COUNT(*) FROM dbo.producto WHERE activo = 1) AS TotalProd,
                (SELECT COUNT(*) FROM dbo.producto WHERE activo = 0) AS InactProd,
                (SELECT COUNT(*) FROM dbo.proveedor WHERE activo = 1) AS TotalProv,
                (SELECT COUNT(*) FROM dbo.proveedor WHERE activo = 0) AS InactProv;";

            using var cmd = new SqlCommand(sql, cn);
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return (
                    Convert.ToInt32(dr["TotalUsers"]),
                    Convert.ToInt32(dr["InactUsers"]),
                    Convert.ToInt32(dr["TotalCli"]),
                    Convert.ToInt32(dr["InactCli"]),
                    Convert.ToInt32(dr["TotalProd"]),
                    Convert.ToInt32(dr["InactProd"]),
                    Convert.ToInt32(dr["TotalProv"]),
                    Convert.ToInt32(dr["InactProv"])
                );
            }
            return (0, 0, 0, 0, 0, 0, 0, 0);
        }

        public DataTable ObtenerProductosPorCategoria()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                ISNULL(c.nombre, 'Sin Categoría') AS Categoria, 
                COUNT(p.id_producto) AS Cantidad
            FROM dbo.producto p
            LEFT JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
            WHERE p.activo = 1
            GROUP BY c.nombre
            ORDER BY Cantidad DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTopProveedoresPorProductos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT TOP 6 
                ISNULL(pr.razon_social, 'Sin Proveedor') AS Proveedor, 
                COUNT(p.id_producto) AS Cantidad
            FROM dbo.producto p
            INNER JOIN dbo.proveedor pr ON p.id_proveedor = pr.id_proveedor
            WHERE p.activo = 1
            GROUP BY pr.razon_social
            ORDER BY Cantidad DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerProductosBajoStock(int limiteCritico = 5)
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT TOP 25 
                id_producto, 
                nombre AS Nombre,
                ISNULL(talla, 'Único') AS Talle,
                ISNULL(stock, 0) AS Stock
            FROM dbo.producto
            WHERE activo = 1 
              AND stock <= @limite
            ORDER BY stock ASC, nombre ASC;";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@limite", limiteCritico);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTodasLasVentas()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                v.id_venta AS NroVenta,
                CONVERT(VARCHAR(16), v.fecha, 120) AS Fecha,
                ISNULL(c.nombre + ' ' + c.apellido, 'Consumidor Final') AS Cliente,
                ISNULL(CAST(c.DNI_cliente AS VARCHAR), '-') AS DNI,
                ISNULL(u.nombre + ' ' + u.apellido, 'Sin Vendedor') AS Vendedor,
                CASE 
                    WHEN mp.nombre COLLATE Latin1_General_CI_AI = 'Credito' AND v.cuotas > 1 THEN CONCAT(mp.nombre, ' (', v.cuotas, ' ctas)')
                    ELSE mp.nombre
                END AS MetodoPago,
                v.total AS Total,
                CASE WHEN v.id_cliente IS NULL THEN 1 ELSE 0 END AS EsConsumidorFinal
            FROM dbo.venta v
            LEFT JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
            LEFT JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            ORDER BY v.fecha DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTodasLasCompras()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                c.id_compra AS NroCompra,
                CONVERT(VARCHAR(16), c.fecha, 120) AS Fecha,
                ISNULL(p.razon_social, 'Sin Proveedor') AS Proveedor,
                ISNULL(p.cuit, '-') AS CUIT,
                ISNULL(u.nombre + ' ' + u.apellido, 'Administrador') AS Comprador,
                ISNULL(mp.nombre, 'Sin Método') AS MetodoPago,
                c.total AS Total
            FROM dbo.compra c
            LEFT JOIN dbo.proveedor p ON c.id_proveedor = p.id_proveedor
            LEFT JOIN dbo.usuario u ON c.id_usuario = u.id_usuario
            LEFT JOIN dbo.metodo_pago mp ON c.id_metodo_pago = mp.id_metodo_pago
            ORDER BY c.fecha DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerCabeceraComprobanteCompra(int idCompra)
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                c.fecha, 
                ISNULL(mp.nombre, 'Efectivo') AS metodo_pago, 
                c.total,
                ISNULL(p.razon_social, 'Sin Proveedor') AS Proveedor,
                ISNULL(p.cuit, '-') AS CUIT,
                ISNULL(u.nombre + ' ' + u.apellido, 'Administrador') AS Comprador
            FROM dbo.compra c
            LEFT JOIN dbo.proveedor p ON c.id_proveedor = p.id_proveedor
            LEFT JOIN dbo.usuario u ON c.id_usuario = u.id_usuario
            LEFT JOIN dbo.metodo_pago mp ON c.id_metodo_pago = mp.id_metodo_pago
            WHERE c.id_compra = @idCompra;";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idCompra", idCompra);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerDetalleComprobanteCompra(int idCompra)
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT 
                prod.nombre,
                ISNULL(prod.talla, 'Único') AS talla,
                dc.cantidad,
                dc.precio_costo,
                (dc.cantidad * dc.precio_costo) AS subtotal
            FROM dbo.detalle_compra dc
            INNER JOIN dbo.producto prod ON dc.id_producto = prod.id_producto
            WHERE dc.id_compra = @idCompra;";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idCompra", idCompra);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        // =======================================================================================
        // REPORTES DEL GERENTE
        // =======================================================================================

        public EReporteKpiHistoricoGerente ObtenerKpisHistoricosGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var kpi = new EReporteKpiHistoricoGerente();
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand();

            string sqlFiltros = "";
            AgregarFiltrosGerente(cmd, ref sqlFiltros, desde, hasta, tipoCliente, metodoPago, "v");

            string sql = $@"
                SELECT 
                    (SELECT ISNULL(SUM(v.total), 0) FROM dbo.venta v WHERE 1=1 {sqlFiltros}) AS Ingresos,
                    (SELECT COUNT(v.id_venta) FROM dbo.venta v WHERE 1=1 {sqlFiltros}) AS Ordenes,
                    (SELECT ISNULL(SUM(dv.cantidad * (ISNULL(dv.precio_unitario, 0) - ISNULL(p.precio_costo, 0))), 0)
                     FROM dbo.detalle_venta dv
                     INNER JOIN dbo.venta v ON dv.id_venta = v.id_venta
                     INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
                     WHERE 1=1 {sqlFiltros}) AS Ganancias;";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpi.Ingresos = Convert.ToDecimal(dr["Ingresos"]);
                kpi.Ganancias = Convert.ToDecimal(dr["Ganancias"]);
                kpi.Ordenes = Convert.ToInt32(dr["Ordenes"]);
                kpi.TicketPromedio = kpi.Ordenes > 0 ? kpi.Ingresos / kpi.Ordenes : 0;
            }
            return kpi;
        }

        public EReporteKpiMesGerente ObtenerKpisMesGerente(DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior)
        {
            var kpi = new EReporteKpiMesGerente();
            using var cn = BaseDatos.GetConnection();
            cn.Open();

            string sqlBase = @"
                SELECT 
                    ISNULL(SUM(v.total), 0) AS Ventas,
                    COUNT(v.id_venta) AS Ordenes,
                    (SELECT ISNULL(SUM(dv.cantidad * (ISNULL(dv.precio_unitario, 0) - ISNULL(p.precio_costo, 0))), 0)
                     FROM dbo.detalle_venta dv
                     INNER JOIN dbo.venta v2 ON dv.id_venta = v2.id_venta
                     INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
                     WHERE v2.fecha >= @ini AND v2.fecha <= @fin) AS Ganancias,
                    ISNULL((SELECT SUM(dv.cantidad) FROM dbo.detalle_venta dv 
                            INNER JOIN dbo.venta v2 ON dv.id_venta = v2.id_venta 
                            WHERE v2.fecha >= @ini AND v2.fecha <= @fin), 0) AS ProductosVendidos
                FROM dbo.venta v
                WHERE v.fecha >= @ini AND v.fecha <= @fin";

            using (var cmdA = new SqlCommand(sqlBase, cn))
            {
                cmdA.Parameters.AddWithValue("@ini", inicioActual);
                cmdA.Parameters.AddWithValue("@fin", finActual);
                using var drA = cmdA.ExecuteReader();
                if (drA.Read())
                {
                    kpi.VentasActual = Convert.ToDecimal(drA["Ventas"]);
                    kpi.GananciasActual = Convert.ToDecimal(drA["Ganancias"]);
                    kpi.OrdenesActual = Convert.ToInt32(drA["Ordenes"]);
                    kpi.ProdVendidosActual = Convert.ToInt32(drA["ProductosVendidos"]);
                }
            }

            using (var cmdB = new SqlCommand(sqlBase, cn))
            {
                cmdB.Parameters.AddWithValue("@ini", inicioAnterior);
                cmdB.Parameters.AddWithValue("@fin", finAnterior);
                using var drB = cmdB.ExecuteReader();
                if (drB.Read())
                {
                    kpi.VentasAnterior = Convert.ToDecimal(drB["Ventas"]);
                    kpi.GananciasAnterior = Convert.ToDecimal(drB["Ganancias"]);
                    kpi.OrdenesAnterior = Convert.ToInt32(drB["Ordenes"]);
                    kpi.ProdVendidosAnterior = Convert.ToInt32(drB["ProductosVendidos"]);
                }
            }
            return kpi;
        }

        public Dictionary<DateTime, decimal> ObtenerVentasMensualesAgrupadasGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dict = new Dictionary<DateTime, decimal>();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT 
                    YEAR(v.fecha) AS Anio, 
                    MONTH(v.fecha) AS Mes, 
                    SUM(v.total) AS Total
                FROM dbo.venta v
                WHERE 1=1 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY YEAR(v.fecha), MONTH(v.fecha)";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            cn.Open();

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int anio = Convert.ToInt32(dr["Anio"]);
                int mes = Convert.ToInt32(dr["Mes"]);
                decimal total = Convert.ToDecimal(dr["Total"]);
                dict.Add(new DateTime(anio, mes, 1), total);
            }
            return dict;
        }

        public DataTable ObtenerTopCategoriasGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT TOP 6
                    c.nombre AS Categoria,
                    COUNT(v.id_venta) AS CantidadOrdenes
                FROM dbo.venta v
                INNER JOIN dbo.detalle_venta dv ON v.id_venta = dv.id_venta
                INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
                INNER JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
                WHERE 1=1 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY c.nombre ORDER BY CantidadOrdenes DESC";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTop10ProductosGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT TOP 10
                    p.id_producto AS IdProducto,
                    p.nombre AS Nombre,
                    SUM(dv.cantidad) AS Ordenes,
                    SUM(dv.subtotal) AS Ingresos
                FROM dbo.venta v
                INNER JOIN dbo.detalle_venta dv ON v.id_venta = dv.id_venta
                INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
                WHERE 1=1 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY p.id_producto, p.nombre ORDER BY Ordenes DESC";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public EReporteKpiGerenteClientes ObtenerKpisGerenteClientes(DateTime inicioMes, DateTime finMes, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var kpi = new EReporteKpiGerenteClientes();
            using var cn = BaseDatos.GetConnection();
            cn.Open();

            using var cmdGlobal = new SqlCommand();
            string sqlFiltrosV = "";
            AgregarFiltrosGerente(cmdGlobal, ref sqlFiltrosV, desde, hasta, tipoCliente, metodoPago, "v");
            string sqlFiltrosV2 = sqlFiltrosV.Replace("v.", "v2.");

            string sqlGlobal = $@"
                SELECT 
                    COUNT(DISTINCT v.id_cliente) AS ClientesActivos,
                    ISNULL(SUM(v.total), 0) AS TotalIngresos,
                    (SELECT COUNT(1) FROM (
                        SELECT id_cliente FROM dbo.venta v2 WHERE v2.id_cliente IS NOT NULL {sqlFiltrosV2} GROUP BY id_cliente HAVING COUNT(id_venta) > 1
                    ) AS Recurrentes) AS ClientesRecurrentes
                FROM dbo.venta v
                WHERE v.id_cliente IS NOT NULL {sqlFiltrosV}";

            cmdGlobal.CommandText = sqlGlobal;
            cmdGlobal.Connection = cn;

            using (var dr = cmdGlobal.ExecuteReader())
            {
                if (dr.Read())
                {
                    kpi.ClientesActivos = Convert.ToInt32(dr["ClientesActivos"]);
                    decimal totalIngresos = Convert.ToDecimal(dr["TotalIngresos"]);
                    kpi.ClientesRecurrentes = Convert.ToInt32(dr["ClientesRecurrentes"]);
                    kpi.VentasPorCliente = kpi.ClientesActivos > 0 ? totalIngresos / kpi.ClientesActivos : 0;
                }
            }

            string sqlMes = @"
                SELECT TOP 1
                    ISNULL(c.nombre, 'Consumidor') + ' ' + ISNULL(c.apellido, 'Final') AS ClienteMesNombre,
                    COUNT(v.id_venta) AS ClienteMesOrdenes,
                    SUM(v.total) AS ClienteMesIngresos
                FROM dbo.venta v
                LEFT JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
                WHERE v.fecha >= @ini AND v.fecha <= @fin AND v.id_cliente IS NOT NULL
                GROUP BY c.id_cliente, c.nombre, c.apellido
                ORDER BY ClienteMesIngresos DESC";

            using var cmdMes = new SqlCommand(sqlMes, cn);
            cmdMes.Parameters.AddWithValue("@ini", inicioMes);
            cmdMes.Parameters.AddWithValue("@fin", finMes);

            using (var dr = cmdMes.ExecuteReader())
            {
                if (dr.Read())
                {
                    kpi.ClienteMesNombre = dr["ClienteMesNombre"].ToString();
                    kpi.ClienteMesOrdenes = Convert.ToInt32(dr["ClienteMesOrdenes"]);
                    kpi.ClienteMesIngresos = Convert.ToDecimal(dr["ClienteMesIngresos"]);
                }
                else
                {
                    kpi.ClienteMesNombre = "N/A";
                }
            }

            return kpi;
        }

        public Dictionary<DateTime, int> ObtenerNuevosClientesMensuales(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dict = new Dictionary<DateTime, int>();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT YEAR(PrimerCompra) AS Anio, MONTH(PrimerCompra) AS Mes, COUNT(id_cliente) AS Cantidad
                FROM (
                    SELECT v.id_cliente, MIN(v.fecha) AS PrimerCompra
                    FROM dbo.venta v
                    WHERE v.id_cliente IS NOT NULL {0}
                    GROUP BY v.id_cliente
                ) AS Primeras
                GROUP BY YEAR(PrimerCompra), MONTH(PrimerCompra)";

            using var cmd = new SqlCommand();
            string sqlFiltros = "";
            AgregarFiltrosGerente(cmd, ref sqlFiltros, desde, hasta, tipoCliente, metodoPago, "v");
            cmd.CommandText = string.Format(sql, sqlFiltros);
            cmd.Connection = cn;
            cn.Open();

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                int anio = Convert.ToInt32(dr["Anio"]);
                int mes = Convert.ToInt32(dr["Mes"]);
                int cant = Convert.ToInt32(dr["Cantidad"]);
                dict.Add(new DateTime(anio, mes, 1), cant);
            }
            return dict;
        }

        public DataTable ObtenerMetodosPagoPorOrdenes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT 
                    mp.nombre AS MetodoPago,
                    COUNT(v.id_venta) AS Cantidad
                FROM dbo.venta v
                INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
                WHERE 1=1 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY mp.nombre ORDER BY Cantidad DESC";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTop10Clientes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT TOP 10
                    ISNULL(CAST(c.DNI_cliente AS VARCHAR), '00000000') AS DNI,
                    ISNULL(c.nombre, 'Consumidor') + ' ' + ISNULL(c.apellido, 'Final') AS Cliente,
                    COUNT(v.id_venta) AS Ordenes,
                    SUM(v.total) AS Ingresos
                FROM dbo.venta v
                LEFT JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
                WHERE 1=1 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY c.id_cliente, c.DNI_cliente, c.nombre, c.apellido ORDER BY Ingresos DESC";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public EReporteKpiGerenteVendedores ObtenerKpisGerenteVendedores(DateTime inicioMes, DateTime finMes, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var kpi = new EReporteKpiGerenteVendedores();
            using var cn = BaseDatos.GetConnection();
            cn.Open();

            using var cmdGlobal = new SqlCommand();
            string sqlFiltrosV = "";
            AgregarFiltrosGerente(cmdGlobal, ref sqlFiltrosV, desde, hasta, tipoCliente, metodoPago, "v");

            string sqlGlobal = $@"
                SELECT 
                    (SELECT COUNT(*) FROM dbo.usuario u WHERE u.id_rol = 3 AND u.estado = 1) AS VendedoresActivos,
                    (SELECT ISNULL(SUM(v.total), 0) FROM dbo.venta v INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario WHERE u.id_rol = 3 {sqlFiltrosV}) AS TotalIngresos,
                    (SELECT COUNT(v.id_venta) FROM dbo.venta v INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario WHERE u.id_rol = 3 {sqlFiltrosV}) AS TotalOrdenes;";

            cmdGlobal.CommandText = sqlGlobal;
            cmdGlobal.Connection = cn;

            using (var dr = cmdGlobal.ExecuteReader())
            {
                if (dr.Read())
                {
                    kpi.VendedoresActivos = Convert.ToInt32(dr["VendedoresActivos"]);
                    decimal totalIngresos = Convert.ToDecimal(dr["TotalIngresos"]);
                    int totalOrdenes = Convert.ToInt32(dr["TotalOrdenes"]);

                    kpi.IngresoPromedio = kpi.VendedoresActivos > 0 ? totalIngresos / kpi.VendedoresActivos : 0;
                    kpi.OrdenesPromedio = kpi.VendedoresActivos > 0 ? (decimal)totalOrdenes / kpi.VendedoresActivos : 0;
                    kpi.TotalOrdenes = totalOrdenes;
                }
            }

            string sqlMes = @"
                SELECT TOP 1
                    u.nombre + ' ' + u.apellido AS VendedorMesNombre,
                    COUNT(v.id_venta) AS VendedorMesOrdenes,
                    SUM(v.total) AS VendedorMesIngresos
                FROM dbo.venta v
                INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
                WHERE v.fecha >= @ini AND v.fecha <= @fin
                GROUP BY u.id_usuario, u.nombre, u.apellido
                ORDER BY VendedorMesIngresos DESC";

            using var cmdMes = new SqlCommand(sqlMes, cn);
            cmdMes.Parameters.AddWithValue("@ini", inicioMes);
            cmdMes.Parameters.AddWithValue("@fin", finMes);

            using (var dr = cmdMes.ExecuteReader())
            {
                if (dr.Read())
                {
                    kpi.VendedorMesNombre = dr["VendedorMesNombre"].ToString();
                    kpi.VendedorMesOrdenes = Convert.ToInt32(dr["VendedorMesOrdenes"]);
                    kpi.VendedorMesIngresos = Convert.ToDecimal(dr["VendedorMesIngresos"]);
                }
                else
                {
                    kpi.VendedorMesNombre = "N/A";
                }
            }

            return kpi;
        }

        public DataTable ObtenerVentasPorVendedorDonut(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            string sql = @"
                SELECT 
                    ISNULL(u.nombre + ' ' + u.apellido, 'Sin Asignar') AS Vendedor,
                    ISNULL(SUM(v.total), 0) AS TotalRecaudado
                FROM dbo.venta v
                INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
                WHERE u.id_rol = 3 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY u.id_usuario, u.nombre, u.apellido ORDER BY TotalRecaudado DESC";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTop6VendedoresPorOrdenes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();

            string sql = @"
                SELECT TOP 6
                    ISNULL(u.nombre + ' ' + u.apellido, 'Sin Asignar') AS Vendedor,
                    ISNULL(SUM(dv.cantidad), 0) AS CantidadUnidades
                FROM dbo.venta v
                INNER JOIN dbo.detalle_venta dv ON v.id_venta = dv.id_venta
                INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
                WHERE u.id_rol = 3 ";

            using var cmd = new SqlCommand();
            AgregarFiltrosGerente(cmd, ref sql, desde, hasta, tipoCliente, metodoPago, "v");
            sql += " GROUP BY u.id_usuario, u.nombre, u.apellido ORDER BY CantidadUnidades DESC";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }

        public DataTable ObtenerTablaRendimientoVendedores(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();

            string sql = @"
                SELECT 
                    CAST(u.DNI_usuario AS VARCHAR(20)) AS DNI,
                    ISNULL(u.nombre + ' ' + u.apellido, 'Sin Asignar') AS Vendedor,
                    COUNT(v.id_venta) AS Ordenes,
                    ISNULL(SUM(v.total), 0) AS Ingresos
                FROM dbo.usuario u
                INNER JOIN dbo.rol r ON u.id_rol = r.id_rol
                LEFT JOIN dbo.venta v ON u.id_usuario = v.id_usuario ";

            using var cmd = new SqlCommand();
            string sqlFiltros = "";
            AgregarFiltrosGerente(cmd, ref sqlFiltros, desde, hasta, tipoCliente, metodoPago, "v");

            sql += sqlFiltros;

            sql += @"
                WHERE (r.nombre LIKE '%vendedor%' OR u.id_rol = 3)
                  AND u.estado = 1
                GROUP BY u.id_usuario, u.DNI_usuario, u.nombre, u.apellido
                ORDER BY Ingresos DESC, Ordenes DESC;";

            cmd.CommandText = sql;
            cmd.Connection = cn;
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            return dt;
        }
    }
}