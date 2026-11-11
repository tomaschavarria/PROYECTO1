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

        public EReporteKpiHistorico ObtenerKpisHistoricos(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerKpisHistoricos(idUsuario, desde, hasta, tipoCliente, metodoPago);
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

        public List<(DateTime Fecha, decimal Total)> ObtenerGraficoHistorico(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var resultado = new List<(DateTime Fecha, decimal Total)>();
            var datosBD = _dReporte.ObtenerVentasMensualesAgrupadas(idUsuario, desde, hasta, tipoCliente, metodoPago);

            if (datosBD.Count > 0)
            {
                DateTime mesInicio = datosBD.Keys.Min();
                DateTime mesFin;

                if (hasta.HasValue)
                {
                    mesFin = new DateTime(hasta.Value.Year, hasta.Value.Month, 1);
                }
                else
                {
                    mesFin = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                }

                DateTime iter = mesInicio;

                while (iter <= mesFin)
                {
                    decimal totalMes = datosBD.ContainsKey(iter) ? datosBD[iter] : 0m;
                    resultado.Add((iter, totalMes));
                    iter = iter.AddMonths(1);
                }
            }

            return resultado;
        }

        public DataTable ObtenerTopCategorias(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerTopCategorias(idUsuario, desde, hasta, tipoCliente, metodoPago);
        }

        public DataTable ObtenerHistorialVentas(int idUsuario, DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ListarHistorialVentas(idUsuario, desde, hasta, tipoCliente, metodoPago);
        }

        private const int AnchoComprobante = 40;

        private static string LineaImporte(string etiqueta, string importe)
        {
            string izquierda = " " + etiqueta;
            int espacios = Math.Max(1, AnchoComprobante - izquierda.Length - importe.Length);
            return izquierda + new string(' ', espacios) + importe;
        }

        private string Dinero(decimal valor) => "$ " + valor.ToString("N2", cultura);

        private static void AgregarNombreProducto(StringBuilder sb, string nombre)
        {
            nombre = (nombre ?? string.Empty).Trim();
            if (nombre.Length == 0) nombre = "Sin descripción";
            const int max = 37;
            while (nombre.Length > max)
            {
                int corte = nombre.LastIndexOf(' ', max);
                if (corte <= 0) corte = max;
                sb.AppendLine(" " + nombre.Substring(0, corte).Trim());
                nombre = nombre.Substring(corte).TrimStart();
            }
            sb.AppendLine(" " + nombre);
        }

        public string GenerarTextoComprobante(int idVenta)
        {
            DataTable cabecera = _dReporte.ObtenerCabeceraComprobante(idVenta);
            DataTable detalle = _dReporte.ObtenerDetalleComprobante(idVenta);
            if (cabecera.Rows.Count == 0) return string.Empty;

            DataRow r = cabecera.Rows[0];
            DateTime fecha = Convert.ToDateTime(r["fecha"]);
            string cliente = Convert.ToString(r["Cliente"]) ?? "Consumidor Final";
            string dni = Convert.ToString(r["DNI"]) ?? "";
            if (string.IsNullOrWhiteSpace(dni) || dni == "-") dni = "00000000";
            string vendedor = Convert.ToString(r["Vendedor"]) ?? "";
            string metodo = Convert.ToString(r["metodo_pago"]) ?? "";
            int cuotas = Convert.ToInt32(r["cuotas"]);
            decimal ajuste = Convert.ToDecimal(r["recargo"]);
            decimal total = Convert.ToDecimal(r["total"]);
            string separador = new string('-', AnchoComprobante);
            string borde = new string('=', AnchoComprobante);

            var sb = new StringBuilder();
            sb.AppendLine(borde);
            sb.AppendLine("             TIENDA DE ROPA");
            sb.AppendLine("          COMPROBANTE DE VENTA");
            sb.AppendLine(borde);
            sb.AppendLine($" Venta N°: #{idVenta:D6}");
            sb.AppendLine($" Fecha:    {fecha:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($" Vendedor: {vendedor}");
            sb.AppendLine(separador);
            sb.AppendLine($" Cliente:  {cliente}");
            sb.AppendLine($" DNI:      {dni}");
            sb.AppendLine(separador);
            sb.AppendLine("             DETALLE DE VENTA");
            sb.AppendLine(separador);

            decimal subtotalGeneral = 0m;
            foreach (DataRow item in detalle.Rows)
            {
                AgregarNombreProducto(sb, Convert.ToString(item["nombre"]) ?? "");
                string talle = Convert.ToString(item["talla"]) ?? "Único";
                int cantidad = Convert.ToInt32(item["cantidad"]);
                decimal unitario = Convert.ToDecimal(item["precio_unitario"]);
                decimal subtotal = Convert.ToDecimal(item["subtotal"]);
                subtotalGeneral += subtotal;
                sb.AppendLine($" Talle: {talle}    Cantidad: {cantidad}");
                sb.AppendLine(LineaImporte("Precio unitario:", Dinero(unitario)));
                sb.AppendLine(LineaImporte("Subtotal artículo:", Dinero(subtotal)));
                sb.AppendLine(separador);
            }

            sb.AppendLine($" Método de pago: {metodo}" + (cuotas > 1 ? $" ({cuotas} cuotas)" : string.Empty));
            sb.AppendLine(separador);
            sb.AppendLine(LineaImporte("Subtotal:", Dinero(subtotalGeneral)));
            if (ajuste < 0)
            {
                decimal porcentaje = subtotalGeneral > 0m ? -ajuste / subtotalGeneral * 100m : 0m;
                sb.AppendLine(LineaImporte($"Descuento ({porcentaje:0.#}%):", "-" + Dinero(-ajuste)));
            }
            else if (ajuste > 0)
            {
                sb.AppendLine(LineaImporte("Recargo:", "+" + Dinero(ajuste)));
            }
            sb.AppendLine(LineaImporte("TOTAL A PAGAR:", Dinero(total)));
            sb.AppendLine(borde);
            sb.AppendLine("        ¡GRACIAS POR SU COMPRA!");
            sb.AppendLine(borde);
            return sb.ToString();
        }

        public string GenerarTextoComprobanteCompra(int idCompra)
        {
            DataTable cabecera = _dReporte.ObtenerCabeceraComprobanteCompra(idCompra);
            DataTable detalle = _dReporte.ObtenerDetalleComprobanteCompra(idCompra);
            if (cabecera.Rows.Count == 0) return string.Empty;

            DataRow r = cabecera.Rows[0];
            DateTime fecha = Convert.ToDateTime(r["fecha"]);
            string proveedor = Convert.ToString(r["Proveedor"]) ?? "";
            string cuit = Convert.ToString(r["CUIT"]) ?? "";
            string comprador = Convert.ToString(r["Comprador"]) ?? "";
            string metodo = Convert.ToString(r["metodo_pago"]) ?? "";
            decimal total = Convert.ToDecimal(r["total"]);
            string separador = new string('-', AnchoComprobante);
            string borde = new string('=', AnchoComprobante);
            var sb = new StringBuilder();
            sb.AppendLine(borde);
            sb.AppendLine("             TIENDA DE ROPA");
            sb.AppendLine("          COMPROBANTE DE COMPRA");
            sb.AppendLine(borde);
            sb.AppendLine($" Compra N°: #{idCompra:D6}");
            sb.AppendLine($" Fecha:     {fecha:dd/MM/yyyy HH:mm:ss}");
            sb.AppendLine($" Comprador: {comprador}");
            sb.AppendLine(separador);
            sb.AppendLine($" Proveedor: {proveedor}");
            sb.AppendLine($" CUIT:      {cuit}");
            sb.AppendLine($" Pago:      {metodo}");
            sb.AppendLine(separador);
            sb.AppendLine("            DETALLE DE COMPRA");
            sb.AppendLine(separador);

            decimal subtotalGeneral = 0m;
            foreach (DataRow item in detalle.Rows)
            {
                AgregarNombreProducto(sb, Convert.ToString(item["nombre"]) ?? "");
                string talle = Convert.ToString(item["talla"]) ?? "Único";
                int cantidad = Convert.ToInt32(item["cantidad"]);
                decimal unitario = Convert.ToDecimal(item["precio_costo"]);
                decimal subtotal = Convert.ToDecimal(item["subtotal"]);
                subtotalGeneral += subtotal;
                sb.AppendLine($" Talle: {talle}    Cantidad: {cantidad}");
                sb.AppendLine(LineaImporte("Costo unitario:", Dinero(unitario)));
                sb.AppendLine(LineaImporte("Subtotal artículo:", Dinero(subtotal)));
                sb.AppendLine(separador);
            }
            sb.AppendLine(LineaImporte("Subtotal:", Dinero(subtotalGeneral)));
            sb.AppendLine(LineaImporte("TOTAL COMPRA:", Dinero(total)));
            sb.AppendLine(borde);
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

        // ---------------- METODOS GERENTE MODIFICADOS PARA SOPORTAR FILTROS -------------
        public EReporteKpiHistoricoGerente ObtenerKpisHistoricosGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerKpisHistoricosGerente(desde, hasta, tipoCliente, metodoPago);
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

        public List<(DateTime Fecha, decimal Total)> ObtenerGraficoHistoricoGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var resultado = new List<(DateTime Fecha, decimal Total)>();
            var datosBD = _dReporte.ObtenerVentasMensualesAgrupadasGerente(desde, hasta, tipoCliente, metodoPago);

            if (datosBD.Count > 0)
            {
                DateTime mesInicio = datosBD.Keys.Min();
                DateTime mesFin = hasta.HasValue ? new DateTime(hasta.Value.Year, hasta.Value.Month, 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                DateTime iter = mesInicio;

                while (iter <= mesFin)
                {
                    decimal totalMes = datosBD.ContainsKey(iter) ? datosBD[iter] : 0m;
                    resultado.Add((iter, totalMes));
                    iter = iter.AddMonths(1);
                }
            }

            return resultado;
        }

        public DataTable ObtenerTopCategoriasGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerTopCategoriasGerente(desde, hasta, tipoCliente, metodoPago);
        }

        public DataTable ObtenerTop10ProductosGerente(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerTop10ProductosGerente(desde, hasta, tipoCliente, metodoPago);
        }

        public EReporteKpiGerenteClientes ObtenerKpisGerenteClientes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            DateTime ahora = DateTime.Now;
            DateTime inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            DateTime finMes = ahora;

            return _dReporte.ObtenerKpisGerenteClientes(inicioMes, finMes, desde, hasta, tipoCliente, metodoPago);
        }

        public List<(DateTime Fecha, int Cantidad)> ObtenerGraficoNuevosClientes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            var resultado = new List<(DateTime Fecha, int Cantidad)>();
            var datosBD = _dReporte.ObtenerNuevosClientesMensuales(desde, hasta, tipoCliente, metodoPago);

            if (datosBD.Count > 0)
            {
                DateTime mesInicio = datosBD.Keys.Min();
                DateTime mesFin = hasta.HasValue ? new DateTime(hasta.Value.Year, hasta.Value.Month, 1) : new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

                DateTime iter = mesInicio;
                while (iter <= mesFin)
                {
                    int cant = datosBD.ContainsKey(iter) ? datosBD[iter] : 0;
                    resultado.Add((iter, cant));
                    iter = iter.AddMonths(1);
                }
            }

            return resultado;
        }

        public DataTable ObtenerMetodosPagoPorOrdenes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerMetodosPagoPorOrdenes(desde, hasta, tipoCliente, metodoPago);
        }

        public DataTable ObtenerTop10Clientes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerTop10Clientes(desde, hasta, tipoCliente, metodoPago);
        }

        public EReporteKpiGerenteVendedores ObtenerKpisGerenteVendedores(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            DateTime ahora = DateTime.Now;
            DateTime inicioMes = new DateTime(ahora.Year, ahora.Month, 1);
            DateTime finMes = ahora;

            return _dReporte.ObtenerKpisGerenteVendedores(inicioMes, finMes, desde, hasta, tipoCliente, metodoPago);
        }

        public DataTable ObtenerVentasPorVendedorDonut(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerVentasPorVendedorDonut(desde, hasta, tipoCliente, metodoPago);
        }

        public DataTable ObtenerTop6VendedoresPorOrdenes(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerTop6VendedoresPorOrdenes(desde, hasta, tipoCliente, metodoPago);
        }

        public DataTable ObtenerTablaRendimientoVendedores(DateTime? desde = null, DateTime? hasta = null, string tipoCliente = "Todos", string metodoPago = "Todos")
        {
            return _dReporte.ObtenerTablaRendimientoVendedores(desde, hasta, tipoCliente, metodoPago);
        }
    }
}