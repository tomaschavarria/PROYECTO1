using System.Data;
using PROYECTO1.CapaDatos;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaLogica
{
    public class LCliente
    {
        private readonly DCliente _dCliente = new();

        public DataTable Listar()
        {
            return _dCliente.ListarClientes();
        }

        public ECliente? ObtenerPorDni(int dni)
        {
            if (dni < 1000000 || dni > 99999999)
                return null;

            return _dCliente.ObtenerPorDni(dni);
        }
        public bool Registrar(ECliente cli, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(cli, out mensajeError))
                return false;

            if (_dCliente.ExisteDni(cli.Dni))
            {
                mensajeError = $"Ya existe un cliente registrado con el DNI {cli.Dni}.";
                return false;
            }

            return _dCliente.Insertar(cli);
        }

        public bool Modificar(ECliente cli, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (!ValidarCampos(cli, out mensajeError))
                return false;

            if (_dCliente.ExisteDni(cli.Dni, cli.IdCliente))
            {
                mensajeError = $"El DNI {cli.Dni} ya pertenece a otro cliente registrado.";
                return false;
            }

            return _dCliente.Modificar(cli);
        }

        public bool CambiarEstadoPorDni(int dni, bool nuevoEstado)
        {
            return _dCliente.CambiarEstadoPorDni(dni, nuevoEstado);
        }

        private bool ValidarCampos(ECliente cli, out string mensajeError)
        {
            mensajeError = string.Empty;

            if (string.IsNullOrWhiteSpace(cli.Nombre) || string.IsNullOrWhiteSpace(cli.Apellido))
            {
                mensajeError = "El nombre y apellido son obligatorios.";
                return false;
            }

            if (cli.Dni < 1000000 || cli.Dni > 99999999)
            {
                mensajeError = "El DNI ingresado no es válido.";
                return false;
            }

            return true;
        }
    }
}