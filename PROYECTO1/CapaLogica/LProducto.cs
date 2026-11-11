using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public List<string> ObtenerTallesRegistrados(string nombre, int idProveedor) =>
            _dProducto.ObtenerTallesRegistrados(nombre, idProveedor);

        public bool RegistrarVariantes(IReadOnlyList<EProducto> variantes, out string mensajeError)
        {
            mensajeError = string.Empty;
            if (variantes == null || variantes.Count == 0)
            {
                mensajeError = "Debe seleccionar al menos un talle.";
                return false;
            }

            var primero = variantes[0];
            if (variantes.Any(p =>
                !string.Equals(p.Nombre.Trim(), primero.Nombre.Trim(), StringComparison.OrdinalIgnoreCase) ||
                p.IdProveedor != primero.IdProveedor || p.IdCategoria != primero.IdCategoria ||
                p.PrecioCosto != primero.PrecioCosto || p.PrecioVenta != primero.PrecioVenta ||
                p.Stock != 0 || p.IdProducto != 0))
            {
                mensajeError = "Los talles deben pertenecer al mismo producto y comenzar con stock cero.";
                return false;
            }

            if (variantes.Select(p => p.Talle?.Trim() ?? string.Empty)
                         .Distinct(StringComparer.OrdinalIgnoreCase).Count() != variantes.Count)
            {
                mensajeError = "La selección contiene talles repetidos.";
                return false;
            }

            foreach (var producto in variantes)
                if (!ValidarCampos(producto, out mensajeError, comprobarExistencia: false))
                    return false;

            return _dProducto.InsertarVariantes(variantes, out mensajeError);
        }

        public bool Registrar(EProducto prod, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(prod, out mensajeError))
                return false;

            return _dProducto.Insertar(prod);
        }

        public bool Modificar(EProducto prod, out string mensajeError) =>
            ModificarVariantes(prod, aplicarAOtrosTalles: false, out mensajeError);

        public List<string> ObtenerOtrosTalles(int idProducto) =>
            _dProducto.ObtenerOtrosTalles(idProducto);

        public bool ModificarVariantes(EProducto prod, bool aplicarAOtrosTalles, out string mensajeError)
        {
            mensajeError = string.Empty;
            if (!ValidarCampos(prod, out mensajeError, comprobarExistencia: false))
                return false;
            return _dProducto.ModificarVariantes(prod, aplicarAOtrosTalles, out mensajeError);
        }

        public bool RegistrarTallesParaProducto(int idProducto, IReadOnlyList<string> talles,
            out string mensajeError)
        {
            mensajeError = string.Empty;
            if (idProducto <= 0 || talles == null || talles.Count == 0 ||
                talles.Any(t => string.IsNullOrWhiteSpace(t)) ||
                talles.Select(t => t.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != talles.Count)
            {
                mensajeError = "Seleccioná talles válidos y sin repetir.";
                return false;
            }
            return _dProducto.InsertarTallesParaProducto(idProducto, talles, out mensajeError);
        }

        public bool CambiarEstado(int idProducto, bool nuevoEstado)
        {
            return _dProducto.CambiarEstado(idProducto, nuevoEstado);
        }

        private bool ValidarCampos(EProducto prod, out string mensajeError, bool comprobarExistencia = true)
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

            if (string.IsNullOrWhiteSpace(prod.Talle))
            {
                mensajeError = "Debe seleccionar un talle válido.";
                return false;
            }

            if (prod.Stock < 0)
            {
                mensajeError = "El stock no puede ser un número negativo.";
                return false;
            }

            if (prod.PrecioCosto <= 0)
            {
                mensajeError = "El precio de costo debe ser mayor a cero.";
                return false;
            }

            if (prod.PrecioVenta <= 0)
            {
                mensajeError = "El precio de venta debe ser mayor a cero.";
                return false;
            }

            if (prod.PrecioVenta <= prod.PrecioCosto)
            {
                mensajeError = "El precio de venta debe ser mayor al precio de costo.";
                return false;
            }

            if (comprobarExistencia && _dProducto.ExisteProducto(prod.Nombre, prod.Talle, prod.IdProveedor, prod.IdProducto))
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