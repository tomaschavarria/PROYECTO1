using System;

namespace PROYECTO1.CapaEntidades
{
    public class EUsuario
    {
        public int IdUsuario { get; set; }
        public int IdRol { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public string Contraseña { get; set; } = string.Empty; // Almacenará los 64 caracteres del hash
        public int Dni { get; set; }
        public string? Correo { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
        public DateTime? Nacimiento { get; set; }
        public bool Estado { get; set; } = true;
        public string? Foto { get; set; }
    }
}