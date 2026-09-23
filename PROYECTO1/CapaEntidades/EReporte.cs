namespace PROYECTO1.CapaEntidades
{
    public class EReporteKpiHistorico
    {
        public decimal TotalFacturado { get; set; }
        public int CantidadVentas { get; set; }
        public int ClientesUnicos { get; set; }
        public decimal PromedioPorCliente { get; set; }
    }

    public class EReporteKpiMes
    {
        public decimal VentasActual { get; set; }
        public decimal VentasAnterior { get; set; }
        public int OrdenesActual { get; set; }
        public int OrdenesAnterior { get; set; }
        public int ClientesActual { get; set; }
        public int ClientesAnterior { get; set; }
    }

    public class EReporteKpiHistoricoGerente
    {
        public decimal Ingresos { get; set; }
        public decimal Ganancias { get; set; }
        public int Ordenes { get; set; }
        public decimal TicketPromedio { get; set; }
    }

    public class EReporteKpiMesGerente
    {
        public decimal VentasActual { get; set; }
        public decimal VentasAnterior { get; set; }
        public decimal GananciasActual { get; set; }
        public decimal GananciasAnterior { get; set; }
        public int OrdenesActual { get; set; }
        public int OrdenesAnterior { get; set; }
        public int ProdVendidosActual { get; set; }
        public int ProdVendidosAnterior { get; set; }
    }

    public class EReporteKpiGerenteClientes
    {
        public int ClientesActivos { get; set; }
        public decimal VentasPorCliente { get; set; }
        public int ClientesRecurrentes { get; set; }
        public string ClienteMesNombre { get; set; } = "Sin ventas";
        public decimal ClienteMesIngresos { get; set; }
        public int ClienteMesOrdenes { get; set; }
    }
    public class EReporteKpiGerenteVendedores
    {
        public int VendedoresActivos { get; set; }
        public decimal IngresoPromedio { get; set; }
        public decimal OrdenesPromedio { get; set; }
        public string VendedorMesNombre { get; set; } = "Sin ventas";
        public int VendedorMesOrdenes { get; set; }
        public decimal VendedorMesIngresos { get; set; }
    }
}