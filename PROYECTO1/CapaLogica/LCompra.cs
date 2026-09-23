using System.Data;
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

            return _dCompra.RegistrarTransaccionCompra(compra, out idCompra, out mensaje);
        }
    }
}
