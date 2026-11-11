using System.Data;
using System;
using System.Linq;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LCompra
    {
        private readonly DCompra _dCompra = new();
        private readonly DVenta _dVenta = new();

        public DataTable ObtenerProveedoresParaCompra() => _dCompra.ListarProveedoresActivos();
        public DataTable ObtenerProductosParaCompra() => _dCompra.ListarProductosParaCompra();

        public DataTable ObtenerMetodosPago()
        {
            DataTable dt = _dVenta.ListarMetodosPagoActivos();
            DataView dv = dt.DefaultView;

            // Filtro explícito: solo permite Efectivo y Transferencia para compras a proveedores
            dv.RowFilter = "nombre IN ('Efectivo', 'Transferencia')";

            return dv.ToTable();
        }

        public bool RegistrarCompra(ECompra compra, out int idCompra, out string mensaje)
        {
            idCompra = 0;
            mensaje = string.Empty;

            if (compra.IdProveedor <= 0)
            {
                mensaje = "Debe seleccionar un proveedor válido.";
                return false;
            }

            if (compra.IdMetodoPago <= 0)
            {
                mensaje = "Debe seleccionar un método de pago válido.";
                return false;
            }

            if (compra.Detalles == null || compra.Detalles.Count == 0)
            {
                mensaje = "La orden de compra debe contener al menos un producto.";
                return false;
            }

            if (compra.IdUsuario <= 0)
            {
                mensaje = "No se identificó al usuario que registra la compra.";
                return false;
            }

            if (compra.Detalles.Any(d => d.IdProducto <= 0 || d.Cantidad <= 0 ||
                                         d.PrecioCosto <= 0 || d.PrecioVenta <= 0 ||
                                         decimal.Round(d.PrecioCosto, 2) != d.PrecioCosto ||
                                         decimal.Round(d.PrecioVenta, 2) != d.PrecioVenta))
            {
                mensaje = "Revisá los productos, las cantidades y los precios (máximo dos decimales).";
                return false;
            }

            // Validación estricta de negocio: Precio de Venta > Precio de Costo
            if (compra.Detalles.Any(d => d.PrecioVenta <= d.PrecioCosto))
            {
                mensaje = "El precio de venta debe ser estrictamente mayor al precio de costo para todos los productos.";
                return false;
            }

            if (compra.Detalles.GroupBy(d => d.IdProducto).Any(g => g.Count() > 1))
            {
                mensaje = "El pedido contiene talles duplicados. Unificá las cantidades.";
                return false;
            }

            try
            {
                decimal totalCalculado = compra.Detalles.Sum(d => checked(d.Cantidad * d.PrecioCosto));
                if (compra.Total != totalCalculado)
                {
                    mensaje = "El total no coincide con el detalle de la compra.";
                    return false;
                }
            }
            catch (OverflowException)
            {
                mensaje = "El importe total supera el límite admitido.";
                return false;
            }

            return _dCompra.RegistrarTransaccionCompra(compra, out idCompra, out mensaje);
        }
    }
}