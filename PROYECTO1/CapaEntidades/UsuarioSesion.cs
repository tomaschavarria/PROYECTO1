namespace PROYECTO1.CapaEntidades
{
    public class UsuarioSesion
    {
        public int Id { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public int IdRol { get; set; }
        public string Foto { get; set; } = string.Empty;
        public bool Activo { get; set; }
    }
}