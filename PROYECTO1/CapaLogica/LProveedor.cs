using System.Data;
using System.Linq;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LProveedor
    {
        private readonly DProveedor _dProveedor = new();

        public DataTable Listar() => _dProveedor.ListarProveedores();

        // Limpia espacios y deja en null los datos opcionales vacíos
        private static string? LimpiarOpcional(string? valor)
        {
            string? v = valor?.Trim();
            return string.IsNullOrEmpty(v) ? null : v;
        }

        private static bool CorreoValido(string correo)
        {
            try
            {
                var m = new System.Net.Mail.MailAddress(correo);
                return m.Address == correo;
            }
            catch (System.FormatException)
            {
                return false;
            }
        }

        // Normaliza y valida (largos según la tabla proveedor)
        private static bool ValidarCampos(EProveedor prov, out string mensajeError)
        {
            mensajeError = string.Empty;

            string cuitIngresado = (prov.Cuit ?? string.Empty).Trim();
            prov.RazonSocial = (prov.RazonSocial ?? string.Empty).Trim();
            prov.Telefono = LimpiarOpcional(prov.Telefono);
            prov.Correo = LimpiarOpcional(prov.Correo);
            prov.Direccion = LimpiarOpcional(prov.Direccion);

            if (cuitIngresado.Length == 0 || prov.RazonSocial.Length == 0)
            {
                mensajeError = "El CUIT y la Razón Social son obligatorios.";
                return false;
            }

            // El CUIT se guarda siempre solo con dígitos (sin guiones ni espacios) y debe tener 11
            string cuitDigitos = new string(cuitIngresado.Where(c => c >= '0' && c <= '9').ToArray());
            if (cuitDigitos.Length != 11)
            {
                mensajeError = "El CUIT debe tener 11 dígitos.";
                return false;
            }
            prov.Cuit = cuitDigitos;

            if (prov.RazonSocial.Length > 100)
            {
                mensajeError = "La Razón Social no puede superar los 100 caracteres.";
                return false;
            }

            if (prov.Telefono != null && prov.Telefono.Length > 50)
            {
                mensajeError = "El teléfono no puede superar los 50 caracteres.";
                return false;
            }

            if (prov.Correo != null && prov.Correo.Length > 100)
            {
                mensajeError = "El correo no puede superar los 100 caracteres.";
                return false;
            }

            if (prov.Correo != null && !CorreoValido(prov.Correo))
            {
                mensajeError = "El correo electrónico no tiene un formato válido.";
                return false;
            }

            if (prov.Direccion != null && prov.Direccion.Length > 150)
            {
                mensajeError = "La dirección no puede superar los 150 caracteres.";
                return false;
            }

            return true;
        }

        public bool Registrar(EProveedor prov, out string mensajeError)
        {
            if (!ValidarCampos(prov, out mensajeError))
                return false;

            if (_dProveedor.ExisteCuit(prov.Cuit))
            {
                mensajeError = "Ya existe un proveedor registrado con ese CUIT.";
                return false;
            }

            return _dProveedor.Insertar(prov);
        }

        public bool Modificar(EProveedor prov, out string mensajeError)
        {
            if (!ValidarCampos(prov, out mensajeError))
                return false;

            if (_dProveedor.ExisteCuit(prov.Cuit, prov.IdProveedor))
            {
                mensajeError = "Ese CUIT ya pertenece a otro proveedor registrado.";
                return false;
            }

            return _dProveedor.Modificar(prov);
        }

        public bool CambiarEstado(int idProveedor, bool nuevoEstado)
        {
            return _dProveedor.CambiarEstado(idProveedor, nuevoEstado);
        }


        public EProveedor? ObtenerPorId(int id) => _dProveedor.ObtenerPorId(id);
    }
}