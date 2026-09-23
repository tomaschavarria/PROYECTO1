using System;

namespace PROYECTO1.CapaEntidades
{
    public class ECliente
    {
        public int IdCliente { get; set; }
        public int Dni { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public string? Correo { get; set; }
        public DateTime? Nacimiento { get; set; }
        public bool Activo { get; set; } = true;
    }
}