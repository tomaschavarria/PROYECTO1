using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Text;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LReporte
    {
        private readonly DReporte _dReporte = new();
        private readonly CultureInfo cultura = CultureInfo.GetCultureInfo("es-AR");

        public EReporteKpiHistorico ObtenerKpisHistoricos(int idUsuario)
        {
            return _dReporte.ObtenerKpisHistoricos(idUsuario);
        }

        public EReporteKpiMes ObtenerKpisMes(int idUsuario)
        {
            DateTime ahora = DateTime.Now;

            DateTime inicioActual = new DateTime(ahora.Year, ahora.Month, 1);
            DateTime finActual = ahora;

            DateTime mesAnt = ahora.AddMonths(-1);
            int diasEnMesAnt = DateTime.DaysInMonth(mesAnt.Year, mesAnt.Month);
            int diaCorteAnt = Math.Min(ahora.Day, diasEnMesAnt);
            DateTime inicioAnterior = new DateTime(mesAnt.Year, mesAnt.Month, 1);
            DateTime finAnterior = new DateTime(mesAnt.Year, mesAnt.Month, diaCorteAnt, ahora.Hour, ahora.Minute, ahora.Second);

            return _dReporte.ObtenerKpisMes(idUsuario, inicioActual, finActual, inicioAnterior, finAnterior);
        }

        public List<(DateTime Fecha, decimal Total)> ObtenerGraficoHistorico(int idUsuario)
        {
            var resultado = new List<(DateTime Fecha, decimal Total)>();
            var datosBD = _dReporte.ObtenerVentasMensualesAgrupadas(idUsuario);

            if (datosBD.Count > 0)
            {
                DateTime mesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime iter = datosBD.Keys.Min();

                while (iter <= mesActual)
                {
                    decimal totalMes = datosBD.ContainsKey(iter) ? datosBD[iter] : 0m;
                    resultado.Add((iter, totalMes));
                    iter = iter.AddMonths(1);
                }
            }

            return resultado;
        }

        public DataTable ObtenerTopCategorias(int idUsuario)
        {
            return _dReporte.ObtenerTopCategorias(idUsuario);
        }

        public DataTable ObtenerHistorialVentas(int idUsuario)
        {
            return _dReporte.ListarHistorialVentas(idUsuario);
        }

        public string GenerarTextoComprobante(int idVenta)
        {
            DataTable dtCabecera = _dReporte.ObtenerCabeceraComprobante(idVenta);
            DataTable dtDetalle = _dReporte.ObtenerDetalleComprobante(idVenta);

            if (dtCabecera.Rows.Count == 0) return string.Empty;

            DataRow rCab = dtCabecera.Rows[0];
            DateTime fecha = Convert.ToDateTime(rCab["fecha"]);
            string cliente = rCab["Cliente"].ToString()!;
            string dni = rCab["DNI"].ToString()!;
            string vendedor = rCab["Vendedor"].ToString()!;
            string metodo = rCab["metodo_pago"].ToString()!;
            int cuotas = Convert.ToInt32(rCab["cuotas"]);
            decimal recargo = Convert.ToDecimal(rCab["recargo"]);
            decimal totalFinal = Convert.ToDecimal(rCab["total"]);

            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("             TIENDA DE ROPA               ");
            sb.AppendLine("          COMPROBANTE DE VENTA            ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Factura N°: #{idVenta:D6}");
            sb.AppendLine($"Fecha:      {fecha:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Vendedor:   {vendedor}");
            sb.AppendLine($"Cliente:    {cliente}");
            sb.AppendLine($"DNI:        {dni}");
            sb.AppendLine($"Pago:       {metodo}" + (cuotas > 1 ? $" ({cuotas} cuotas)" : ""));
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("Cant. Descripcion            P.Unit.  Subt");
            sb.AppendLine("------------------------------------------");

            decimal subtotalAcumulado = 0;
            foreach (DataRow rDet in dtDetalle.Rows)
            {
                string nombre = rDet["nombre"].ToString()!;
                string talle = rDet["talla"].ToString()!;
                int cant = Convert.ToInt32(rDet["cantidad"]);
                decimal unitario = Convert.ToDecimal(rDet["precio_unitario"]);
                decimal subtotal = Convert.ToDecimal(rDet["subtotal"]);
                subtotalAcumulado += subtotal;

                string itemDesc = $"{nombre} ({talle})";
                if (itemDesc.Length > 20) itemDesc = itemDesc.Substring(0, 17) + "...";

                sb.AppendLine($"{cant,-3} {itemDesc,-22} {unitario,7:N0} {subtotal,7:N0}");
            }

            sb.AppendLine("------------------------------------------");
            if (recargo > 0)
            {
                sb.AppendLine($"Subtotal:                 $ {subtotalAcumulado.ToString("N2", cultura)}");
                sb.AppendLine($"Recargo ({cuotas} ctas):        $ {recargo.ToString("N2", cultura)}");
                sb.AppendLine("------------------------------------------");
            }
            sb.AppendLine($"TOTAL FINAL:              $ {totalFinal.ToString("N2", cultura)}");
            sb.AppendLine("==========================================");
            sb.AppendLine("        ¡GRACIAS POR SU COMPRA!           ");
            sb.AppendLine("==========================================");

            return sb.ToString();
        }

        // ==========================================
        // COMPROBANTE DE COMPRA A PROVEEDOR
        // ==========================================
        public string GenerarTextoComprobanteCompra(int idCompra)
        {
            DataTable dtCabecera = _dReporte.ObtenerCabeceraComprobanteCompra(idCompra);
            DataTable dtDetalle = _dReporte.ObtenerDetalleComprobanteCompra(idCompra);

            if (dtCabecera.Rows.Count == 0) return string.Empty;

            DataRow rCab = dtCabecera.Rows[0];
            DateTime fecha = Convert.ToDateTime(rCab["fecha"]);
            string proveedor = rCab["Proveedor"].ToString()!;
            string cuit = rCab["CUIT"].ToString()!;
            string comprador = rCab["Comprador"].ToString()!;
            string metodo = rCab["metodo_pago"].ToString()!;
            decimal totalFinal = Convert.ToDecimal(rCab["total"]);

            var sb = new StringBuilder();
            sb.AppendLine("==========================================");
            sb.AppendLine("             TIENDA DE ROPA               ");
            sb.AppendLine("         COMPROBANTE DE COMPRA            ");
            sb.AppendLine("==========================================");
            sb.AppendLine($"Orden Compra: #{idCompra:D6}");
            sb.AppendLine($"Fecha:        {fecha:dd/MM/yyyy HH:mm}");
            sb.AppendLine($"Comprador:    {comprador}");
            sb.AppendLine($"Proveedor:    {proveedor}");
            sb.AppendLine($"CUIT:         {cuit}");
            sb.AppendLine($"Pago:         {metodo}");
            sb.AppendLine("------------------------------------------");
            sb.AppendLine("Cant. Descripcion            P.Costo  Subt");
            sb.AppendLine("------------------------------------------");

            foreach (DataRow rDet in dtDetalle.Rows)
            {
                string nombre = rDet["nombre"].ToString()!;
                string talle = rDet["talla"].ToString()!;
                int cant = Convert.ToInt32(rDet["cantidad"]);
                decimal costo = Convert.ToDecimal(rDet["precio_costo"]);
                decimal subtotal = Convert.ToDecimal(rDet["subtotal"]);

                string itemDesc = $"{nombre} ({talle})";
                if (itemDesc.Length > 20) itemDesc = itemDesc.Substring(0, 17) + "...";

                sb.AppendLine($"{cant,-3} {itemDesc,-22} {costo,7:N0} {subtotal,7:N0}");
            }

            sb.AppendLine("------------------------------------------");
            sb.AppendLine($"TOTAL FINAL:              $ {totalFinal.ToString("N2", cultura)}");

            return sb.ToString();
        }

        public (int TotalUsers, int InactUsers, int TotalCli, int InactCli, int TotalProd, int InactProd, int TotalProv, int InactProv) ObtenerKpisAdmin()
            => _dReporte.ObtenerKpisAdmin();

        public DataTable ObtenerProductosPorCategoria()
            => _dReporte.ObtenerProductosPorCategoria();

        public DataTable ObtenerTopProveedoresPorProductos()
            => _dReporte.ObtenerTopProveedoresPorProductos();

        public DataTable ObtenerProductosBajoStock()
            => _dReporte.ObtenerProductosBajoStock();

        public DataTable ObtenerTodasLasVentas()
            => _dReporte.ObtenerTodasLasVentas();

        public DataTable ObtenerTodasLasCompras()
            => _dReporte.ObtenerTodasLasCompras();

        public EReporteKpiHistoricoGerente ObtenerKpisHistoricosGerente()
        {
            return _dReporte.ObtenerKpisHistoricosGerente();
        }

        public EReporteKpiMesGerente ObtenerKpisMesGerente()
        {
            DateTime ahora = DateTime.Now;

            DateTime inicioActual = new DateTime(ahora.Year, ahora.Month, 1);
            DateTime finActual = ahora;

            DateTime mesAnt = ahora.AddMonths(-1);
            int diasEnMesAnt = DateTime.DaysInMonth(mesAnt.Year, mesAnt.Month);
            int diaCorteAnt = Math.Min(ahora.Day, diasEnMesAnt);
            DateTime inicioAnterior = new DateTime(mesAnt.Year, mesAnt.Month, 1);
            DateTime finAnterior = new DateTime(mesAnt.Year, mesAnt.Month, diaCorteAnt, ahora.Hour, ahora.Minute, ahora.Second);

            return _dReporte.ObtenerKpisMesGerente(inicioActual, finActual, inicioAnterior, finAnterior);
        }

        public List<(DateTime Fecha, decimal Total)> ObtenerGraficoHistoricoGerente()
        {
            var resultado = new List<(DateTime Fecha, decimal Total)>();
            var datosBD = _dReporte.ObtenerVentasMensualesAgrupadasGerente();

            if (datosBD.Count > 0)
            {
                DateTime mesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime iter = datosBD.Keys.Min();

                while (iter <= mesActual)
                {
                    decimal totalMes = datosBD.ContainsKey(iter) ? datosBD[iter] : 0m;
                    resultado.Add((iter, totalMes));
                    iter = iter.AddMonths(1);
                }
            }

            return resultado;
        }

        public DataTable ObtenerTopCategoriasGerente()
        {
            return _dReporte.ObtenerTopCategoriasGerente();
        }

        public DataTable ObtenerTop10ProductosGerente()
        {
            return _dReporte.ObtenerTop10ProductosGerente();
        }

        public EReporteKpiGerenteClientes ObtenerKpisGerenteClientes()
        {
            DateTime ahora = DateTime.Now;
            DateTime inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            DateTime finMes = ahora;

            return _dReporte.ObtenerKpisGerenteClientes(inicioMes, finMes);
        }

        public List<(DateTime Fecha, int Cantidad)> ObtenerGraficoNuevosClientes()
        {
            var resultado = new List<(DateTime Fecha, int Cantidad)>();
            var datosBD = _dReporte.ObtenerNuevosClientesMensuales();

            if (datosBD.Count > 0)
            {
                DateTime mesActual = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                DateTime iter = datosBD.Keys.Min();

                while (iter <= mesActual)
                {
                    int cant = datosBD.ContainsKey(iter) ? datosBD[iter] : 0;
                    resultado.Add((iter, cant));
                    iter = iter.AddMonths(1);
                }
            }

            return resultado;
        }

        public DataTable ObtenerMetodosPagoPorOrdenes()
        {
            return _dReporte.ObtenerMetodosPagoPorOrdenes();
        }

        public DataTable ObtenerTop10Clientes()
        {
            return _dReporte.ObtenerTop10Clientes();
        }

        public EReporteKpiGerenteVendedores ObtenerKpisGerenteVendedores()
        {
            DateTime ahora = DateTime.Now;
            DateTime inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            DateTime finMes = ahora;

            return _dReporte.ObtenerKpisGerenteVendedores(inicioMes, finMes);
        }

        public DataTable ObtenerVentasPorVendedorDonut()
        {
            return _dReporte.ObtenerVentasPorVendedorDonut();
        }

        public DataTable ObtenerTop6VendedoresPorOrdenes()
        {
            return _dReporte.ObtenerTop6VendedoresPorOrdenes();
        }

        public DataTable ObtenerTablaRendimientoVendedores()
        {
            return _dReporte.ObtenerTablaRendimientoVendedores();
        }
    }
}