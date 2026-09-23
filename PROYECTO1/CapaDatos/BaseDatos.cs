using System.Data.SqlClient;

namespace PROYECTO1
{
    internal class BaseDatos
    {
        private const string CS = @"Server=localhost;Database=tiendaDB;Integrated Security=True;TrustServerCertificate=True;";

        public static SqlConnection GetConnection() => new SqlConnection(CS);
    }
}