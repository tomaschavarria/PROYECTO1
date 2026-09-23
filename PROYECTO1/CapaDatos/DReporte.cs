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
        public EReporteKpiHistorico ObtenerKpisHistoricos(int idUsuario)
        {
            var kpi = new EReporteKpiHistorico();
            const string sql = @"
            SELECT 
                ISNULL(SUM(total), 0) AS TotalFacturado,
                COUNT(id_venta) AS CantidadVentas,
                COUNT(DISTINCT id_cliente) AS ClientesUnicos,
                CASE 
                    WHEN COUNT(DISTINCT id_cliente) = 0 THEN 0
                    ELSE ISNULL(SUM(total), 0) / COUNT(DISTINCT id_cliente)
                END AS PromedioPorCliente
            FROM dbo.venta
            WHERE id_usuario = @idUsuario;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpi.TotalFacturado = Convert.ToDecimal(dr["TotalFacturado"]);
                kpi.CantidadVentas = Convert.ToInt32(dr["CantidadVentas"]);
                kpi.ClientesUnicos = Convert.ToInt32(dr["ClientesUnicos"]);
                kpi.PromedioPorCliente = Convert.ToDecimal(dr["PromedioPorCliente"]);
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
                COUNT(DISTINCT CASE WHEN fecha >= @inicioActual AND fecha <= @finActual THEN id_cliente END) AS ClientesActual,
                COUNT(DISTINCT CASE WHEN fecha >= @inicioAnterior AND fecha <= @finAnterior THEN id_cliente END) AS ClientesAnterior
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
                kpi.ClientesActual = Convert.ToInt32(dr["ClientesActual"]);
                kpi.ClientesAnterior = Convert.ToInt32(dr["ClientesAnterior"]);
            }

            return kpi;
        }

        public Dictionary<DateTime, decimal> ObtenerVentasMensualesAgrupadas(int idUsuario)
        {
            var datos = new Dictionary<DateTime, decimal>();
            const string sql = @"
            SELECT 
                DATEFROMPARTS(YEAR(fecha), MONTH(fecha), 1) AS Periodo,
                ISNULL(SUM(total), 0) AS TotalMes
            FROM dbo.venta
            WHERE id_usuario = @idUsuario
            GROUP BY DATEFROMPARTS(YEAR(fecha), MONTH(fecha), 1)
            ORDER BY Periodo ASC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
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

        public DataTable ObtenerTopCategorias(int idUsuario)
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT TOP 6
                ISNULL(c.nombre, 'Sin Categoría') AS Categoria,
                COUNT(DISTINCT v.id_venta) AS CantidadOrdenes
            FROM dbo.venta v
            INNER JOIN dbo.detalle_venta dv ON v.id_venta = dv.id_venta
            INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
            INNER JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
            WHERE v.id_usuario = @idUsuario
            GROUP BY c.nombre
            ORDER BY CantidadOrdenes DESC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public DataTable ListarHistorialVentas(int idUsuario)
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                v.id_venta AS NroVenta,
                CONVERT(VARCHAR(16), v.fecha, 120) AS Fecha,
                ISNULL(c.nombre + ' ' + c.apellido, 'Consumidor Final') AS Cliente,
                ISNULL(CAST(c.DNI_cliente AS VARCHAR), '-') AS DNI,
                CASE 
                    WHEN mp.nombre COLLATE Latin1_General_CI_AI = 'Credito' AND v.cuotas > 1 THEN CONCAT(mp.nombre, ' (', v.cuotas, ' ctas)')
                    ELSE mp.nombre
                END AS MetodoPago,
                v.total AS Total
            FROM dbo.venta v
            LEFT JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            WHERE v.id_usuario = @idUsuario
            ORDER BY v.fecha DESC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@idUsuario", idUsuario);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public DataTable ObtenerCabeceraComprobante(int idVenta)
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                v.fecha, mp.nombre AS metodo_pago, v.cuotas, v.recargo, v.total,
                ISNULL(c.nombre + ' ' + c.apellido, 'Consumidor Final') AS Cliente,
                ISNULL(CAST(c.DNI_cliente AS VARCHAR), '-') AS DNI,
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
                v.total AS Total
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

        public EReporteKpiHistoricoGerente ObtenerKpisHistoricosGerente()
        {
            var kpi = new EReporteKpiHistoricoGerente();
            const string sql = @"
            SELECT 
                (SELECT ISNULL(SUM(total), 0) FROM dbo.venta) AS Ingresos,
                (SELECT COUNT(id_venta) FROM dbo.venta) AS Ordenes,
                (SELECT ISNULL(SUM(dv.cantidad * (ISNULL(dv.precio_unitario, 0) - ISNULL(p.precio_costo, 0))), 0)
                 FROM dbo.detalle_venta dv
                 INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto) AS Ganancias;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                kpi.Ingresos = Convert.ToDecimal(dr["Ingresos"]);
                kpi.Ordenes = Convert.ToInt32(dr["Ordenes"]);
                kpi.Ganancias = Convert.ToDecimal(dr["Ganancias"]);
                kpi.TicketPromedio = kpi.Ordenes > 0 ? kpi.Ingresos / kpi.Ordenes : 0m;
            }

            return kpi;
        }

        public EReporteKpiMesGerente ObtenerKpisMesGerente(DateTime inicioActual, DateTime finActual, DateTime inicioAnterior, DateTime finAnterior)
        {
            var kpi = new EReporteKpiMesGerente();
            const string sql = @"
            SELECT
                (SELECT ISNULL(SUM(total), 0) FROM dbo.venta WHERE fecha >= @inicioActual AND fecha <= @finActual) AS VentasActual,
                (SELECT ISNULL(SUM(total), 0) FROM dbo.venta WHERE fecha >= @inicioAnterior AND fecha <= @finAnterior) AS VentasAnterior,
                (SELECT COUNT(id_venta) FROM dbo.venta WHERE fecha >= @inicioActual AND fecha <= @finActual) AS OrdenesActual,
                (SELECT COUNT(id_venta) FROM dbo.venta WHERE fecha >= @inicioAnterior AND fecha <= @finAnterior) AS OrdenesAnterior,
                (SELECT ISNULL(SUM(dv.cantidad * (ISNULL(dv.precio_unitario, 0) - ISNULL(p.precio_costo, 0))), 0)
                 FROM dbo.detalle_venta dv
                 INNER JOIN dbo.venta v ON dv.id_venta = v.id_venta
                 INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
                 WHERE v.fecha >= @inicioActual AND v.fecha <= @finActual) AS GananciasActual,
                (SELECT ISNULL(SUM(dv.cantidad * (ISNULL(dv.precio_unitario, 0) - ISNULL(p.precio_costo, 0))), 0)
                 FROM dbo.detalle_venta dv
                 INNER JOIN dbo.venta v ON dv.id_venta = v.id_venta
                 INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
                 WHERE v.fecha >= @inicioAnterior AND v.fecha <= @finAnterior) AS GananciasAnterior,
                (SELECT ISNULL(SUM(dv.cantidad), 0)
                 FROM dbo.detalle_venta dv
                 INNER JOIN dbo.venta v ON dv.id_venta = v.id_venta
                 WHERE v.fecha >= @inicioActual AND v.fecha <= @finActual) AS ProdVendidosActual,
                (SELECT ISNULL(SUM(dv.cantidad), 0)
                 FROM dbo.detalle_venta dv
                 INNER JOIN dbo.venta v ON dv.id_venta = v.id_venta
                 WHERE v.fecha >= @inicioAnterior AND v.fecha <= @finAnterior) AS ProdVendidosAnterior;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
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
                kpi.GananciasActual = Convert.ToDecimal(dr["GananciasActual"]);
                kpi.GananciasAnterior = Convert.ToDecimal(dr["GananciasAnterior"]);
                kpi.ProdVendidosActual = Convert.ToInt32(dr["ProdVendidosActual"]);
                kpi.ProdVendidosAnterior = Convert.ToInt32(dr["ProdVendidosAnterior"]);
            }

            return kpi;
        }

        public Dictionary<DateTime, decimal> ObtenerVentasMensualesAgrupadasGerente()
        {
            var datos = new Dictionary<DateTime, decimal>();
            const string sql = @"
            SELECT 
                DATEFROMPARTS(YEAR(fecha), MONTH(fecha), 1) AS Periodo,
                ISNULL(SUM(total), 0) AS TotalMes
            FROM dbo.venta
            GROUP BY DATEFROMPARTS(YEAR(fecha), MONTH(fecha), 1)
            ORDER BY Periodo ASC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
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

        public DataTable ObtenerTopCategoriasGerente()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT TOP 6
                ISNULL(c.nombre, 'Sin Categoría') AS Categoria,
                COUNT(DISTINCT v.id_venta) AS CantidadOrdenes
            FROM dbo.venta v
            INNER JOIN dbo.detalle_venta dv ON v.id_venta = dv.id_venta
            INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
            INNER JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
            GROUP BY c.nombre
            ORDER BY CantidadOrdenes DESC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public DataTable ObtenerTop10ProductosGerente()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT TOP 10
                p.id_producto AS IdProducto,
                p.nombre AS Nombre,
                COUNT(DISTINCT v.id_venta) AS Ordenes,
                ISNULL(SUM(dv.cantidad * dv.precio_unitario), 0) AS Ingresos
            FROM dbo.detalle_venta dv
            INNER JOIN dbo.producto p ON dv.id_producto = p.id_producto
            INNER JOIN dbo.venta v ON dv.id_venta = v.id_venta
            WHERE p.activo = 1
            GROUP BY p.id_producto, p.nombre
            ORDER BY Ingresos DESC, Ordenes DESC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            using var da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            return dt;
        }

        public EReporteKpiGerenteClientes ObtenerKpisGerenteClientes(DateTime inicioMes, DateTime finMes)
        {
            var kpi = new EReporteKpiGerenteClientes();
            using var cn = BaseDatos.GetConnection();

            const string sqlKpis = @"
            SELECT 
                (SELECT COUNT(*) FROM dbo.cliente WHERE activo = 1) AS ClientesActivos,
                (SELECT CASE WHEN COUNT(DISTINCT v.id_cliente) = 0 THEN 0 
                             ELSE ISNULL(SUM(v.total), 0) / COUNT(DISTINCT v.id_cliente) END 
                 FROM dbo.venta v WHERE v.id_cliente IS NOT NULL) AS VentasPorCliente,
                (SELECT COUNT(*) 
                 FROM (SELECT id_cliente FROM dbo.venta WHERE id_cliente IS NOT NULL GROUP BY id_cliente HAVING COUNT(id_venta) > 1) AS R) AS ClientesRecurrentes;";

            using (var cmd = new SqlCommand(sqlKpis, cn))
            {
                cn.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    kpi.ClientesActivos = Convert.ToInt32(dr["ClientesActivos"]);
                    kpi.VentasPorCliente = Convert.ToDecimal(dr["VentasPorCliente"]);
                    kpi.ClientesRecurrentes = Convert.ToInt32(dr["ClientesRecurrentes"]);
                }
            }

            const string sqlClienteMes = @"
            SELECT TOP 1
                ISNULL(c.nombre + ' ' + c.apellido, 'Sin Cliente') AS Nombre,
                ISNULL(SUM(v.total), 0) AS Ingresos,
                COUNT(v.id_venta) AS Ordenes
            FROM dbo.venta v
            INNER JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
            WHERE v.fecha >= @inicioMes AND v.fecha <= @finMes
            GROUP BY c.id_cliente, c.nombre, c.apellido
            ORDER BY Ingresos DESC, Ordenes DESC;";

            using (var cmdMes = new SqlCommand(sqlClienteMes, cn))
            {
                cmdMes.Parameters.AddWithValue("@inicioMes", inicioMes);
                cmdMes.Parameters.AddWithValue("@finMes", finMes);

                using var drMes = cmdMes.ExecuteReader();
                if (drMes.Read())
                {
                    kpi.ClienteMesNombre = drMes["Nombre"].ToString()!;
                    kpi.ClienteMesIngresos = Convert.ToDecimal(drMes["Ingresos"]);
                    kpi.ClienteMesOrdenes = Convert.ToInt32(drMes["Ordenes"]);
                }
            }

            return kpi;
        }

        public Dictionary<DateTime, int> ObtenerNuevosClientesMensuales()
        {
            var datos = new Dictionary<DateTime, int>();
            const string sql = @"
            SELECT 
                DATEFROMPARTS(YEAR(PrimeraCompra), MONTH(PrimeraCompra), 1) AS Periodo,
                COUNT(*) AS Cantidad
            FROM (
                SELECT id_cliente, MIN(fecha) AS PrimeraCompra
                FROM dbo.venta
                WHERE id_cliente IS NOT NULL
                GROUP BY id_cliente
            ) AS PC
            GROUP BY DATEFROMPARTS(YEAR(PrimeraCompra), MONTH(PrimeraCompra), 1)
            ORDER BY Periodo ASC;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cn.Open();

            using var dr = cmd.ExecuteReader();
            while (dr.Read())
            {
                DateTime periodo = Convert.ToDateTime(dr["Periodo"]);
                int cant = Convert.ToInt32(dr["Cantidad"]);
                datos[periodo] = cant;
            }

            return datos;
        }

        public DataTable ObtenerMetodosPagoPorOrdenes()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                ISNULL(mp.nombre, 'Sin Método') AS MetodoPago,
                COUNT(v.id_venta) AS Cantidad
            FROM dbo.venta v
            INNER JOIN dbo.metodo_pago mp ON v.id_metodo_pago = mp.id_metodo_pago
            GROUP BY mp.nombre
            ORDER BY Cantidad DESC;";

            using var cn = BaseDatos.GetConnection();
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);

            return dt;
        }

        public DataTable ObtenerTop10Clientes()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT TOP 10
                c.DNI_cliente AS DNI,
                ISNULL(c.nombre + ' ' + c.apellido, 'Consumidor Final') AS Cliente,
                COUNT(v.id_venta) AS Ordenes,
                ISNULL(SUM(v.total), 0) AS Ingresos
            FROM dbo.venta v
            INNER JOIN dbo.cliente c ON v.id_cliente = c.id_cliente
            WHERE c.activo = 1
            GROUP BY c.DNI_cliente, c.nombre, c.apellido
            ORDER BY Ingresos DESC, Ordenes DESC;";

            using var cn = BaseDatos.GetConnection();
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);

            return dt;
        }

        public EReporteKpiGerenteVendedores ObtenerKpisGerenteVendedores(DateTime inicioMes, DateTime finMes)
        {
            var kpi = new EReporteKpiGerenteVendedores();
            using var cn = BaseDatos.GetConnection();

            const string sqlKpis = @"
            SELECT 
                (SELECT COUNT(*) 
                 FROM dbo.usuario u 
                 WHERE u.id_rol = 3 AND u.estado = 1) AS VendedoresActivos,
                (SELECT ISNULL(SUM(v.total), 0) 
                 FROM dbo.venta v 
                 INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario 
                 WHERE u.id_rol = 3) AS TotalIngresos,
                (SELECT COUNT(v.id_venta) 
                 FROM dbo.venta v 
                 INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario 
                 WHERE u.id_rol = 3) AS TotalOrdenes;";

            using (var cmd = new SqlCommand(sqlKpis, cn))
            {
                cn.Open();
                using var dr = cmd.ExecuteReader();
                if (dr.Read())
                {
                    kpi.VendedoresActivos = Convert.ToInt32(dr["VendedoresActivos"]);
                    decimal totalIngresos = Convert.ToDecimal(dr["TotalIngresos"]);
                    int totalOrdenes = Convert.ToInt32(dr["TotalOrdenes"]);

                    if (kpi.VendedoresActivos > 0)
                    {
                        kpi.IngresoPromedio = totalIngresos / kpi.VendedoresActivos;
                        kpi.OrdenesPromedio = (decimal)totalOrdenes / kpi.VendedoresActivos;
                    }
                }
            }

            const string sqlVendedorMes = @"
            SELECT TOP 1
                ISNULL(u.nombre + ' ' + u.apellido, 'Sin Vendedor') AS Vendedor,
                COUNT(v.id_venta) AS Ordenes,
                ISNULL(SUM(v.total), 0) AS Ingresos
            FROM dbo.venta v
            INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
            WHERE v.fecha >= @inicioMes AND v.fecha <= @finMes AND u.id_rol = 3
            GROUP BY u.id_usuario, u.nombre, u.apellido
            ORDER BY Ingresos DESC, Ordenes DESC;";

            using (var cmdMes = new SqlCommand(sqlVendedorMes, cn))
            {
                cmdMes.Parameters.AddWithValue("@inicioMes", inicioMes);
                cmdMes.Parameters.AddWithValue("@finMes", finMes);

                using var drMes = cmdMes.ExecuteReader();
                if (drMes.Read())
                {
                    kpi.VendedorMesNombre = drMes["Vendedor"].ToString()!;
                    kpi.VendedorMesOrdenes = Convert.ToInt32(drMes["Ordenes"]);
                    kpi.VendedorMesIngresos = Convert.ToDecimal(drMes["Ingresos"]);
                }
            }

            return kpi;
        }

        public DataTable ObtenerVentasPorVendedorDonut()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                ISNULL(u.nombre + ' ' + u.apellido, 'Sin Asignar') AS Vendedor,
                ISNULL(SUM(v.total), 0) AS TotalRecaudado
            FROM dbo.venta v
            INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
            WHERE u.id_rol = 3
            GROUP BY u.id_usuario, u.nombre, u.apellido
            ORDER BY TotalRecaudado DESC;";

            using var cn = BaseDatos.GetConnection();
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);

            return dt;
        }

        public DataTable ObtenerTop6VendedoresPorOrdenes()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT TOP 6
                ISNULL(u.nombre + ' ' + u.apellido, 'Sin Asignar') AS Vendedor,
                COUNT(v.id_venta) AS CantidadOrdenes
            FROM dbo.venta v
            INNER JOIN dbo.usuario u ON v.id_usuario = u.id_usuario
            WHERE u.id_rol = 3
            GROUP BY u.id_usuario, u.nombre, u.apellido
            ORDER BY CantidadOrdenes DESC;";

            using var cn = BaseDatos.GetConnection();
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);

            return dt;
        }

        public DataTable ObtenerTablaRendimientoVendedores()
        {
            var dt = new DataTable();
            const string sql = @"
            SELECT 
                CAST(u.DNI_usuario AS VARCHAR(20)) AS DNI,
                ISNULL(u.nombre + ' ' + u.apellido, 'Sin Asignar') AS Vendedor,
                COUNT(v.id_venta) AS Ordenes,
                ISNULL(SUM(v.total), 0) AS Ingresos
            FROM dbo.usuario u
            INNER JOIN dbo.rol r ON u.id_rol = r.id_rol
            LEFT JOIN dbo.venta v ON u.id_usuario = v.id_usuario
            WHERE (r.nombre LIKE '%vendedor%' OR u.id_rol = 3)
              AND u.estado = 1
            GROUP BY u.id_usuario, u.DNI_usuario, u.nombre, u.apellido
            ORDER BY Ingresos DESC, Ordenes DESC;";

            using var cn = BaseDatos.GetConnection();
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);

            return dt;
        }
    }
}