using System.Data;
using PROYECTO1;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LUsuario
    {

        // Instancia de la capa de datos para interactuar con la base de datos
        private readonly DUsuario _dUsuario = new();

        public DataTable Listar()
        {
            return _dUsuario.ListarUsuarios();
        }

        public DataTable ObtenerRoles()
        {
            return _dUsuario.ListarRoles();
        }

        public EUsuario? ObtenerPorId(int idUsuario)
        {
            return _dUsuario.ObtenerPorId(idUsuario);
        }

        public bool Registrar(EUsuario usuario, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(usuario, out mensajeError))
                return false;

            if (string.IsNullOrWhiteSpace(usuario.Contraseña))
            {
                mensajeError = "Debe asignar una contraseña al usuario.";
                return false;
            }

            if (_dUsuario.ExisteUsuario(usuario.NombreUsuario, usuario.Dni))
            {
                mensajeError = "Ya existe un usuario con ese nombre de usuario o DNI.";
                return false;
            }

            // Hasheo SHA-256 irreversible
            usuario.Contraseña = SeguridadHelper.HashSHA256(usuario.Contraseña);

            return _dUsuario.Insertar(usuario);
        }

        public bool Modificar(EUsuario usuario, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(usuario, out mensajeError))
                return false;

            if (_dUsuario.ExisteUsuario(usuario.NombreUsuario, usuario.Dni, usuario.IdUsuario))
            {
                mensajeError = "Ya existe otro usuario con ese nombre de usuario o DNI.";
                return false;
            }

            // Si se escribió una contraseña nueva en texto plano, se hashea; si quedó el hash de 64 caracteres, se conserva
            if (!string.IsNullOrWhiteSpace(usuario.Contraseña) && usuario.Contraseña.Length != 64)
            {
                usuario.Contraseña = SeguridadHelper.HashSHA256(usuario.Contraseña);
            }

            return _dUsuario.Modificar(usuario);
        }

        public bool CambiarEstado(int idUsuario, bool nuevoEstado)
        {
            return _dUsuario.CambiarEstado(idUsuario, nuevoEstado);
        }


        public UsuarioSesion? IniciarSesion(string usuario, string clave, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(clave))
            {
                mensajeError = "Ingresá usuario y contraseña.";
                return null;
            }

            // Se calcula el hash de lo que tipeó el usuario para compararlo contra la BD
            string claveHasheada = SeguridadHelper.HashSHA256(clave);

            var usuarioValido = _dUsuario.Autenticar(usuario, claveHasheada);
            if (usuarioValido == null)
            {
                mensajeError = "Usuario o contraseña incorrectos.";
                return null;
            }

            if (!usuarioValido.Activo)
            {
                mensajeError = "El usuario se encuentra inhabilitado para operar en el sistema.";
                return null;
            }

            return usuarioValido;
        }

        private bool ValidarCampos(EUsuario usuario, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(usuario.Nombre) || string.IsNullOrWhiteSpace(usuario.Apellido))
            {
                mensajeError = "El nombre y apellido son obligatorios.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(usuario.NombreUsuario))
            {
                mensajeError = "El nombre de usuario es obligatorio.";
                return false;
            }

            if (usuario.Dni < 1000000 || usuario.Dni > 99999999)
            {
                mensajeError = "El DNI ingresado no es válido.";
                return false;
            }

            if (usuario.IdRol <= 0)
            {
                mensajeError = "Debe asignar un rol válido al usuario.";
                return false;
            }

            return true;
        }
    }
}