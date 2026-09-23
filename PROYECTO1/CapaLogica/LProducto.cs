using System.Data;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LProducto
    {
        private readonly DProducto _dProducto = new();

        public DataTable Listar() => _dProducto.ListarProductos();
        public DataTable ObtenerCategorias() => _dProducto.ListarCategoriasActivas();
        public DataTable ObtenerProveedores() => _dProducto.ListarProveedoresActivos();
        public DataTable ObtenerProveedorPorId(int id) => _dProducto.ObtenerProveedorPorId(id);
        public EProducto? ObtenerPorId(int id) => _dProducto.ObtenerPorId(id);

        public bool Registrar(EProducto prod, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(prod, out mensajeError))
                return false;

            return _dProducto.Insertar(prod);
        }

        public bool Modificar(EProducto prod, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(prod, out mensajeError))
                return false;

            return _dProducto.Modificar(prod);
        }

        public bool CambiarEstado(int idProducto, bool nuevoEstado)
        {
            return _dProducto.CambiarEstado(idProducto, nuevoEstado);
        }

        private bool ValidarCampos(EProducto prod, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(prod.Nombre))
            {
                mensajeError = "El nombre del producto no puede estar vacío.";
                return false;
            }

            if (prod.IdCategoria <= 0)
            {
                mensajeError = "Debe seleccionar una categoría válida.";
                return false;
            }

            if (prod.IdProveedor <= 0)
            {
                mensajeError = "Debe seleccionar una marca/proveedor válido.";
                return false;
            }

            if (prod.Stock < 0)
            {
                mensajeError = "El stock no puede ser un número negativo.";
                return false;
            }

            // Validación estricta: Costo > 0
            if (prod.PrecioCosto <= 0)
            {
                mensajeError = "El precio de costo debe ser mayor a cero.";
                return false;
            }

            // Validación estricta: Venta > 0
            if (prod.PrecioVenta <= 0)
            {
                mensajeError = "El precio de venta debe ser mayor a cero.";
                return false;
            }

            // Validación de negocio: Precio de Venta > Precio de Costo
            if (prod.PrecioVenta <= prod.PrecioCosto)
            {
                mensajeError = "El precio de venta debe ser mayor al precio de costo.";
                return false;
            }

            if (_dProducto.ExisteProducto(prod.Nombre, prod.Talle, prod.IdProveedor, prod.IdProducto))
            {
                mensajeError = "Ya existe un producto con ese nombre, talle y proveedor.";
                return false;
            }

            return true;
        }

        public bool RegistrarCategoria(string nombre, out int idGenerado, out string error)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                idGenerado = 0;
                error = "El nombre de la categoría no puede estar vacío.";
                return false;
            }

            return _dProducto.RegistrarCategoria(nombre, out idGenerado, out error);
        }
    }
}