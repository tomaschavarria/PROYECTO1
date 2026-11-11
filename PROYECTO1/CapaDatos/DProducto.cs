#pragma warning disable CS0618

using System;
using System.Data;
using System.Collections.Generic;
using System.Data.SqlClient;
using PROYECTO1.CapaEntidades;

namespace PROYECTO1.CapaDatos
{
    public class DProducto
    {
        public DataTable ListarProductos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
        SELECT 
            p.id_producto,
            p.nombre AS Nombre,
            ISNULL(p.descripcion, '') AS [Descripción],
            ISNULL(c.nombre, 'Sin Categoría') AS Categoria,
            ISNULL(p.stock, 0) AS Stock,
            ISNULL(p.precio_venta, 0) AS PrecioVenta,
            ISNULL(p.precio_costo, 0) AS PrecioCosto,
            ISNULL(p.talla, 'Único') AS Talle,
            ISNULL(pr.razon_social, 'Sin proveedor') AS Proveedor,
            p.activo AS Activo
        FROM dbo.producto p
        LEFT JOIN dbo.categoria c ON p.id_categoria = c.id_categoria
        LEFT JOIN dbo.proveedor pr ON p.id_proveedor = pr.id_proveedor
        ORDER BY p.id_producto DESC;";

            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarCategoriasActivas()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_categoria, nombre FROM dbo.categoria WHERE activo = 1 ORDER BY nombre;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        public DataTable ListarProveedoresActivos()
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            // Se agrega 'cuit' a la consulta para que el buscador pueda leerlo
            const string sql = "SELECT id_proveedor, cuit, razon_social FROM dbo.proveedor WHERE activo = 1 ORDER BY razon_social;";
            using var da = new SqlDataAdapter(sql, cn);
            da.Fill(dt);
            return dt;
        }

        // Trae un proveedor por id sin importar si está activo (para editar productos cuyo proveedor fue desactivado)
        public DataTable ObtenerProveedorPorId(int id)
        {
            var dt = new DataTable();
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT id_proveedor, cuit, razon_social FROM dbo.proveedor WHERE id_proveedor = @id;";
            using var da = new SqlDataAdapter(sql, cn);
            da.SelectCommand.Parameters.AddWithValue("@id", id);
            da.Fill(dt);
            return dt;
        }

        // Considera "el mismo producto" cuando coinciden nombre + talle + proveedor
        // (dos talles del mismo nombre son productos distintos, y el mismo nombre en otro proveedor también)
        public bool ExisteProducto(string nombre, string? talle, int idProveedor, int idExcluir = 0)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
            SELECT COUNT(1)
            FROM dbo.producto
            WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
              AND LOWER(LTRIM(RTRIM(ISNULL(talla, 'Único')))) = LOWER(LTRIM(RTRIM(ISNULL(@talla, 'Único'))))
              AND id_proveedor = @proveedor
              AND (@idExcluir = 0 OR id_producto <> @idExcluir);";

            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@nombre", nombre ?? string.Empty);
            cmd.Parameters.AddWithValue("@talla", (object?)talle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@proveedor", idProveedor);
            cmd.Parameters.AddWithValue("@idExcluir", idExcluir);
            cn.Open();

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // Incluye variantes inactivas: no se debe crear otro registro con el mismo talle.
        public List<string> ObtenerTallesRegistrados(string nombre, int idProveedor)
        {
            var talles = new List<string>();
            using var cn = BaseDatos.GetConnection();
            const string sql = @"
                SELECT DISTINCT ISNULL(talla, 'Único')
                FROM dbo.producto
                WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                  AND id_proveedor = @proveedor;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@nombre", nombre);
            cmd.Parameters.AddWithValue("@proveedor", idProveedor);
            cn.Open();
            using var lector = cmd.ExecuteReader();
            while (lector.Read())
                talles.Add(lector.GetString(0).Trim());
            return talles;
        }

        // Registro atómico de variantes. Si una ya existe o falla una inserción,
        // se revierten TODAS las inserciones del lote.
        public bool InsertarVariantes(IReadOnlyList<EProducto> variantes, out string error)
        {
            error = string.Empty;
            if (variantes == null || variantes.Count == 0)
            {
                error = "Debe seleccionar al menos un talle.";
                return false;
            }

            using var cn = BaseDatos.GetConnection();
            cn.Open();
            using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                const string sqlExiste = @"
                    SELECT COUNT(1) FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                    WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                      AND LOWER(LTRIM(RTRIM(ISNULL(talla, 'Único')))) = LOWER(LTRIM(RTRIM(@talle)))
                      AND id_proveedor = @proveedor;";
                const string sqlCategoria = @"
                    SELECT COUNT(1) FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                    WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                      AND id_proveedor = @proveedor AND id_categoria <> @categoria;";
                using (var categoria = new SqlCommand(sqlCategoria, cn, tx))
                {
                    categoria.Parameters.AddWithValue("@nombre", variantes[0].Nombre);
                    categoria.Parameters.AddWithValue("@proveedor", variantes[0].IdProveedor);
                    categoria.Parameters.AddWithValue("@categoria", variantes[0].IdCategoria);
                    if (Convert.ToInt32(categoria.ExecuteScalar()) > 0)
                    {
                        error = "Este nombre y proveedor ya se usan en otra categoría. " +
                            "Elegí el producto existente desde la grilla para evitar inconsistencias.";
                        tx.Rollback();
                        return false;
                    }
                }

                const string sqlInsertar = @"
                    INSERT INTO dbo.producto
                        (id_categoria, id_proveedor, nombre, descripcion, talla, stock, precio_costo, precio_venta, activo)
                    VALUES (@cat, @prov, @nom, @desc, @tal, 0, @pc, @pv, 1);";

                foreach (var prod in variantes)
                {
                    using (var existe = new SqlCommand(sqlExiste, cn, tx))
                    {
                        existe.Parameters.AddWithValue("@nombre", prod.Nombre);
                        existe.Parameters.AddWithValue("@talle", prod.Talle ?? "Único");
                        existe.Parameters.AddWithValue("@proveedor", prod.IdProveedor);
                        if (Convert.ToInt32(existe.ExecuteScalar()) > 0)
                        {
                            error = $"Ya existe '{prod.Nombre}' en talle {prod.Talle} para ese proveedor (incluso si está inactivo).";
                            tx.Rollback();
                            return false;
                        }
                    }

                    using var insertar = new SqlCommand(sqlInsertar, cn, tx);
                    insertar.Parameters.AddWithValue("@cat", prod.IdCategoria);
                    insertar.Parameters.AddWithValue("@prov", prod.IdProveedor);
                    insertar.Parameters.AddWithValue("@nom", prod.Nombre);
                    insertar.Parameters.AddWithValue("@desc", (object?)prod.Descripcion ?? DBNull.Value);
                    insertar.Parameters.AddWithValue("@tal", (object?)prod.Talle ?? DBNull.Value);
                    insertar.Parameters.AddWithValue("@pc", prod.PrecioCosto);
                    insertar.Parameters.AddWithValue("@pv", prod.PrecioVenta);
                    if (insertar.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("No se pudo insertar una variante.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { /* Ya se deshizo la transacción. */ }
                error = "No se registraron los talles. " + ex.Message;
                return false;
            }
        }

        public EProducto? ObtenerPorId(int id)
        {
            using var cn = BaseDatos.GetConnection();
            const string sql = "SELECT * FROM dbo.producto WHERE id_producto = @id;";
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", id);
            cn.Open();
            using var dr = cmd.ExecuteReader();
            if (dr.Read())
            {
                return new EProducto
                {
                    IdProducto = Convert.ToInt32(dr["id_producto"]),
                    IdCategoria = Convert.ToInt32(dr["id_categoria"]),
                    IdProveedor = Convert.ToInt32(dr["id_proveedor"]),
                    Nombre = dr["nombre"].ToString() ?? string.Empty,
                    Descripcion = dr["descripcion"] != DBNull.Value ? dr["descripcion"].ToString() : null,
                    Talle = dr["talla"] != DBNull.Value ? dr["talla"].ToString() : null,
                    Stock = Convert.ToInt32(dr["stock"]),
                    PrecioCosto = Convert.ToDecimal(dr["precio_costo"]),
                    PrecioVenta = Convert.ToDecimal(dr["precio_venta"]),
                    Activo = Convert.ToBoolean(dr["activo"])
                };
            }
            return null;
        }

        public bool Insertar(EProducto prod)
        {
            const string sql = @"
            INSERT INTO dbo.producto 
                (id_categoria, id_proveedor, nombre, descripcion, talla, stock, precio_costo, precio_venta, activo)
            VALUES 
                (@cat, @prov, @nom, @desc, @tal, @stock, @pc, @pv, @act);";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cat", prod.IdCategoria);
            cmd.Parameters.AddWithValue("@prov", prod.IdProveedor);
            cmd.Parameters.AddWithValue("@nom", prod.Nombre);
            cmd.Parameters.AddWithValue("@desc", (object?)prod.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tal", (object?)prod.Talle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@stock", prod.Stock);
            cmd.Parameters.AddWithValue("@pc", prod.PrecioCosto);
            cmd.Parameters.AddWithValue("@pv", prod.PrecioVenta);
            cmd.Parameters.AddWithValue("@act", prod.Activo ? 1 : 0);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Modificar(EProducto prod)
        {
            // Stock y estado NO se actualizan acá: el stock solo cambia por compras/ventas y el estado por CambiarEstado.
            // Así no se pisan cambios hechos mientras el formulario de edición estaba abierto.
            const string sql = @"
            UPDATE dbo.producto
            SET id_categoria = @cat,
                id_proveedor = @prov,
                nombre = @nom,
                descripcion = @desc,
                talla = @tal,
                precio_costo = @pc,
                precio_venta = @pv
            WHERE id_producto = @id;";

            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@cat", prod.IdCategoria);
            cmd.Parameters.AddWithValue("@prov", prod.IdProveedor);
            cmd.Parameters.AddWithValue("@nom", prod.Nombre);
            cmd.Parameters.AddWithValue("@desc", (object?)prod.Descripcion ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@tal", (object?)prod.Talle ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@pc", prod.PrecioCosto);
            cmd.Parameters.AddWithValue("@pv", prod.PrecioVenta);
            cmd.Parameters.AddWithValue("@id", prod.IdProducto);

            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        // Variantes del mismo producto que la fila seleccionada. Se incluyen
        // las inactivas porque también están registradas en la tabla.
        public List<string> ObtenerOtrosTalles(int idProducto)
        {
            var talles = new List<string>();
            const string sql = @"
                SELECT ISNULL(otro.talla, 'Único') AS Talle
                FROM dbo.producto original
                INNER JOIN dbo.producto otro
                    ON LOWER(LTRIM(RTRIM(otro.nombre))) = LOWER(LTRIM(RTRIM(original.nombre)))
                   AND otro.id_proveedor = original.id_proveedor
                   AND otro.id_categoria = original.id_categoria
                WHERE original.id_producto = @id AND otro.id_producto <> original.id_producto
                ORDER BY Talle;";
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@id", idProducto);
            cn.Open();
            using var lector = cmd.ExecuteReader();
            while (lector.Read())
                talles.Add(lector.GetString(0).Trim());
            return talles;
        }

        // Toda la actualización usa una sola transacción. Nunca cambia el talle,
        // el stock ni el estado (activo) de los productos existentes.
        public bool ModificarVariantes(EProducto prod, bool aplicarAOtrosTalles, out string error)
        {
            error = string.Empty;
            using var cn = BaseDatos.GetConnection();
            cn.Open();
            using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                string nombreOriginal;
                int proveedorOriginal;
                int categoriaOriginal;
                string talleOriginal;
                using (var origen = new SqlCommand(@"
                    SELECT nombre, id_proveedor, id_categoria, ISNULL(talla, 'Único')
                    FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                    WHERE id_producto = @id;", cn, tx))
                {
                    origen.Parameters.AddWithValue("@id", prod.IdProducto);
                    using var lector = origen.ExecuteReader();
                    if (!lector.Read())
                        throw new InvalidOperationException("El producto seleccionado ya no existe.");
                    nombreOriginal = lector.GetString(0);
                    proveedorOriginal = lector.GetInt32(1);
                    categoriaOriginal = lector.GetInt32(2);
                    talleOriginal = lector.GetString(3).Trim();
                }

                if (!string.Equals(talleOriginal, prod.Talle?.Trim(), StringComparison.OrdinalIgnoreCase))
                {
                    error = "No se permite reemplazar el talle de un producto existente.";
                    tx.Rollback();
                    return false;
                }

                var ids = new List<int> { prod.IdProducto };
                var talles = new List<string> { talleOriginal };
                if (aplicarAOtrosTalles)
                {
                    using var grupo = new SqlCommand(@"
                        SELECT id_producto, ISNULL(talla, 'Único')
                        FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                        WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                          AND id_proveedor = @proveedor AND id_categoria = @categoria
                          AND id_producto <> @id;", cn, tx);
                    grupo.Parameters.AddWithValue("@nombre", nombreOriginal);
                    grupo.Parameters.AddWithValue("@proveedor", proveedorOriginal);
                    grupo.Parameters.AddWithValue("@categoria", categoriaOriginal);
                    grupo.Parameters.AddWithValue("@id", prod.IdProducto);
                    using var lector = grupo.ExecuteReader();
                    while (lector.Read())
                    {
                        ids.Add(lector.GetInt32(0));
                        talles.Add(lector.GetString(1).Trim());
                    }
                }

                // Verificar colisiones con cualquier producto fuera de las filas a editar.
                // La misma clave lógica se utiliza para impedir altas duplicadas.
                var clavesIds = new List<string>();
                for (int i = 0; i < ids.Count; i++) clavesIds.Add("@id" + i);
                string otrosIds = string.Join(", ", clavesIds);
                foreach (string talle in talles)
                {
                    using var colision = new SqlCommand($@"
                        SELECT COUNT(1) FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                        WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                          AND id_proveedor = @proveedor
                          AND LOWER(LTRIM(RTRIM(ISNULL(talla, 'Único')))) = LOWER(LTRIM(RTRIM(@talle)))
                          AND id_producto NOT IN ({otrosIds});", cn, tx);
                    colision.Parameters.AddWithValue("@nombre", prod.Nombre.Trim());
                    colision.Parameters.AddWithValue("@proveedor", prod.IdProveedor);
                    colision.Parameters.AddWithValue("@talle", talle);
                    for (int i = 0; i < ids.Count; i++)
                        colision.Parameters.AddWithValue("@id" + i, ids[i]);
                    if (Convert.ToInt32(colision.ExecuteScalar()) != 0)
                    {
                        error = $"Ya existe '{prod.Nombre}' en talle {talle} para ese proveedor.";
                        tx.Rollback();
                        return false;
                    }
                }

                using (var categoria = new SqlCommand($@"
                    SELECT COUNT(1) FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                    WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                      AND id_proveedor = @proveedor AND id_categoria <> @categoria
                      AND id_producto NOT IN ({otrosIds});", cn, tx))
                {
                    categoria.Parameters.AddWithValue("@nombre", prod.Nombre.Trim());
                    categoria.Parameters.AddWithValue("@proveedor", prod.IdProveedor);
                    categoria.Parameters.AddWithValue("@categoria", prod.IdCategoria);
                    for (int i = 0; i < ids.Count; i++)
                        categoria.Parameters.AddWithValue("@id" + i, ids[i]);
                    if (Convert.ToInt32(categoria.ExecuteScalar()) != 0)
                    {
                        error = "El mismo nombre y proveedor ya están registrados en otra categoría.";
                        tx.Rollback();
                        return false;
                    }
                }

                using (var actualizar = new SqlCommand($@"
                    UPDATE dbo.producto
                    SET id_categoria = @categoria, id_proveedor = @proveedor, nombre = @nombre,
                        descripcion = @descripcion, precio_costo = @pc, precio_venta = @pv
                    WHERE id_producto IN ({otrosIds});", cn, tx))
                {
                    actualizar.Parameters.AddWithValue("@categoria", prod.IdCategoria);
                    actualizar.Parameters.AddWithValue("@proveedor", prod.IdProveedor);
                    actualizar.Parameters.AddWithValue("@nombre", prod.Nombre.Trim());
                    actualizar.Parameters.AddWithValue("@descripcion", (object?)prod.Descripcion ?? DBNull.Value);
                    actualizar.Parameters.AddWithValue("@pc", prod.PrecioCosto);
                    actualizar.Parameters.AddWithValue("@pv", prod.PrecioVenta);
                    for (int i = 0; i < ids.Count; i++)
                        actualizar.Parameters.AddWithValue("@id" + i, ids[i]);
                    if (actualizar.ExecuteNonQuery() != ids.Count)
                        throw new InvalidOperationException("No se pudieron actualizar todas las variantes.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                error = "No se modificaron los productos. " + ex.Message;
                return false;
            }
        }

        // En modo Agregar Talle, usa exclusivamente los datos del producto real
        // (consultados y bloqueados en la misma transacción que la inserción).
        public bool InsertarTallesParaProducto(int idProducto, IReadOnlyList<string> talles, out string error)
        {
            error = string.Empty;
            using var cn = BaseDatos.GetConnection();
            cn.Open();
            using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
            try
            {
                int categoria;
                int proveedor;
                string nombre;
                string? descripcion;
                decimal costo;
                decimal venta;
                bool activo;

                using (var origen = new SqlCommand(@"
                    SELECT id_categoria, id_proveedor, nombre, descripcion,
                           precio_costo, precio_venta, activo
                    FROM dbo.producto WITH (UPDLOCK, HOLDLOCK) WHERE id_producto = @id;", cn, tx))
                {
                    origen.Parameters.AddWithValue("@id", idProducto);
                    using var lector = origen.ExecuteReader();
                    if (!lector.Read())
                        throw new InvalidOperationException("El producto seleccionado ya no existe.");
                    categoria = lector.GetInt32(0);
                    proveedor = lector.GetInt32(1);
                    nombre = lector.GetString(2);
                    descripcion = lector.IsDBNull(3) ? null : lector.GetString(3);
                    costo = Convert.ToDecimal(lector.GetValue(4));
                    venta = Convert.ToDecimal(lector.GetValue(5));
                    activo = Convert.ToBoolean(lector.GetValue(6));
                }
                if (!activo)
                {
                    error = "Reactivá el producto antes de agregarle talles.";
                    tx.Rollback();
                    return false;
                }

                using (var cat = new SqlCommand(@"
                    SELECT COUNT(1) FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                    WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                      AND id_proveedor = @proveedor AND id_categoria <> @categoria;", cn, tx))
                {
                    cat.Parameters.AddWithValue("@nombre", nombre);
                    cat.Parameters.AddWithValue("@proveedor", proveedor);
                    cat.Parameters.AddWithValue("@categoria", categoria);
                    if (Convert.ToInt32(cat.ExecuteScalar()) > 0)
                    {
                        error = "Hay variantes de este producto en otra categoría. Revisalas antes de agregar talles.";
                        tx.Rollback();
                        return false;
                    }
                }

                foreach (string talleBruto in talles)
                {
                    string talle = talleBruto.Trim();
                    using (var existe = new SqlCommand(@"
                        SELECT COUNT(1) FROM dbo.producto WITH (UPDLOCK, HOLDLOCK)
                        WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)))
                          AND id_proveedor = @proveedor
                          AND LOWER(LTRIM(RTRIM(ISNULL(talla, 'Único')))) = LOWER(LTRIM(RTRIM(@talle)));", cn, tx))
                    {
                        existe.Parameters.AddWithValue("@nombre", nombre);
                        existe.Parameters.AddWithValue("@proveedor", proveedor);
                        existe.Parameters.AddWithValue("@talle", talle);
                        if (Convert.ToInt32(existe.ExecuteScalar()) > 0)
                        {
                            error = $"El talle {talle} ya está registrado (incluso si está inactivo).";
                            tx.Rollback();
                            return false;
                        }
                    }

                    using var alta = new SqlCommand(@"
                        INSERT INTO dbo.producto
                            (id_categoria, id_proveedor, nombre, descripcion, talla, stock,
                             precio_costo, precio_venta, activo)
                        VALUES (@categoria, @proveedor, @nombre, @descripcion, @talle, 0,
                                @pc, @pv, 1);", cn, tx);
                    alta.Parameters.AddWithValue("@categoria", categoria);
                    alta.Parameters.AddWithValue("@proveedor", proveedor);
                    alta.Parameters.AddWithValue("@nombre", nombre);
                    alta.Parameters.AddWithValue("@descripcion", (object?)descripcion ?? DBNull.Value);
                    alta.Parameters.AddWithValue("@talle", talle);
                    alta.Parameters.AddWithValue("@pc", costo);
                    alta.Parameters.AddWithValue("@pv", venta);
                    if (alta.ExecuteNonQuery() != 1)
                        throw new InvalidOperationException("No se pudo registrar uno de los talles.");
                }

                tx.Commit();
                return true;
            }
            catch (Exception ex)
            {
                try { tx.Rollback(); } catch { }
                error = "No se agregaron los talles. " + ex.Message;
                return false;
            }
        }

        public bool CambiarEstado(int idProducto, bool nuevoEstado)
        {
            const string sql = "UPDATE dbo.producto SET activo = @act WHERE id_producto = @id;";
            using var cn = BaseDatos.GetConnection();
            using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.AddWithValue("@act", nuevoEstado ? 1 : 0);
            cmd.Parameters.AddWithValue("@id", idProducto);
            cn.Open();
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool RegistrarCategoria(string nombre, out int idGenerado, out string error)
        {
            idGenerado = 0;
            error = string.Empty;

            try
            {
                using var cn = BaseDatos.GetConnection();
                const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM dbo.categoria WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre))))
            BEGIN
                INSERT INTO dbo.categoria (nombre) VALUES (@nombre);
                SELECT SCOPE_IDENTITY();
            END
            ELSE
            BEGIN
                SELECT id_categoria FROM dbo.categoria WHERE LOWER(LTRIM(RTRIM(nombre))) = LOWER(LTRIM(RTRIM(@nombre)));
            END";

                using var cmd = new SqlCommand(sql, cn);
                cmd.Parameters.AddWithValue("@nombre", nombre.Trim());
                cn.Open();

                object res = cmd.ExecuteScalar();
                if (res != null && res != DBNull.Value)
                {
                    idGenerado = Convert.ToInt32(res);
                    return true;
                }

                error = "No se pudo registrar la categoría.";
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }
    }
}