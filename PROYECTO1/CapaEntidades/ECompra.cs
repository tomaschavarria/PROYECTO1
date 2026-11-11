using System;
using System.Collections.Generic;

namespace PROYECTO1.CapaEntidades
{
    public class ECompra
    {
        public int IdCompra { get; set; }
        public int IdUsuario { get; set; }
        public int IdProveedor { get; set; }
        public int IdMetodoPago { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public decimal Total { get; set; }
        public List<EDetalleCompra> Detalles { get; set; } = new();
    }

    public class EDetalleCompra
    {
        public int IdDetalleCompra { get; set; }
        public int IdCompra { get; set; }
        public int IdProducto { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioCosto { get; set; }
        // Precio de venta que se aplicará al registro de producto tras confirmar la compra.
        // El historial del detalle_compra mantiene únicamente el costo pactado.
        public decimal PrecioVenta { get; set; }
        public decimal Subtotal => Cantidad * PrecioCosto;
    }
}