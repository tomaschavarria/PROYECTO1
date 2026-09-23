using System;
using System.Collections.Generic;

namespace PROYECTO1.CapaEntidades
{
    public class EVenta
    {
        public int IdVenta { get; set; }
        public int IdUsuario { get; set; }
        public int? IdCliente { get; set; }
        public int IdMetodoPago { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public int Cuotas { get; set; } = 1;
        public decimal Recargo { get; set; } = 0m;
        public List<EDetalleVenta> Detalles { get; set; } = new();
    }

    public class EDetalleVenta
    {
        public int IdDetalle { get; set; }
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}