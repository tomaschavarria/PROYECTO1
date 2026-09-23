namespace PROYECTO1.CapaEntidades
{
    public class EProveedor
    {
        public int IdProveedor { get; set; }
        public string Cuit { get; set; } = string.Empty;
        public string RazonSocial { get; set; } = string.Empty;
        public string? Telefono { get; set; }
        public string? Correo { get; set; }
        public string? Direccion { get; set; }
        public bool Activo { get; set; } = true;
    }
}