using System.Data;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LVenta
    {
        private readonly DVenta _dVenta = new();

        public DataTable ObtenerClientesParaVenta()
        {
            return _dVenta.ListarClientesActivos();
        }

        public DataTable ObtenerProductosParaVenta()
        {
            return _dVenta.ListarProductosActivos();
        }

        public DataTable ObtenerMetodosPago()
        {
            return _dVenta.ListarMetodosPagoActivos();
        }

        public bool RegistrarVenta(EVenta venta, out int idVenta, out string mensaje)
        {
            idVenta = 0;
            mensaje = string.Empty;

            if (venta.Detalles == null || venta.Detalles.Count == 0)
            {
                mensaje = "El detalle de la venta no contiene ningún producto.";
                return false;
            }

            if (venta.IdMetodoPago <= 0)
            {
                mensaje = "Debe seleccionar un método de pago válido.";
                return false;
            }

            return _dVenta.RegistrarTransaccionVenta(venta, out idVenta, out mensaje);
        }
    }
}