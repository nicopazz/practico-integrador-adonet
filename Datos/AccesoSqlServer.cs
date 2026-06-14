using System;
using Microsoft.Data.SqlClient;

namespace Practico.Starter.Datos
{
    public class AccesoSqlServer : IAccesoDatos
    {
        // Credenciales extraídas del PDF para SQL Server local
        private readonly string connMaster = "Server=localhost,1433;User Id=sa;Password=Curso.NET2026;Database=master;TrustServerCertificate=True;";
        private readonly string connPractico = "Server=localhost,1433;User Id=sa;Password=Curso.NET2026;Database=practico;TrustServerCertificate=True;";

        public void CrearEstructura()
        {
            // PASO 1: Conectarse a master para crear la base
            using (var conexion = new SqlConnection(connMaster))
            {
                conexion.Open();
                string sqlDb = @"
                    IF DB_ID('practico') IS NULL 
                    BEGIN
                        CREATE DATABASE practico;
                    END";
                using (var cmd = new SqlCommand(sqlDb, conexion))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Base 'practico' asegurada en el servidor.");
                }
            }

            // PASO 2: Conectarse a practico para crear las tablas
            using (var conexion = new SqlConnection(connPractico))
            {
                conexion.Open();

                // DDL: En SQL Server es buena práctica separar el DROP de las tablas si no existen
                string ddl = @"
                    IF OBJECT_ID('detalle_pedido', 'U') IS NOT NULL DROP TABLE detalle_pedido;
                    IF OBJECT_ID('pedidos', 'U') IS NOT NULL DROP TABLE pedidos;
                    IF OBJECT_ID('productos', 'U') IS NOT NULL DROP TABLE productos;
                    IF OBJECT_ID('clientes', 'U') IS NOT NULL DROP TABLE clientes;
                    IF OBJECT_ID('categorias', 'U') IS NOT NULL DROP TABLE categorias;

                    CREATE TABLE categorias (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL
                    );

                    CREATE TABLE clientes (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        email VARCHAR(100) NOT NULL
                    );

                    CREATE TABLE productos (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        precio DECIMAL(18,2) NOT NULL,
                        stock INT NOT NULL,
                        categoria_id INT FOREIGN KEY REFERENCES categorias(id)
                    );

                    CREATE TABLE pedidos (
                        id INT IDENTITY(1,1) PRIMARY KEY,
                        cliente_id INT FOREIGN KEY REFERENCES clientes(id),
                        fecha DATETIME NOT NULL
                    );

                    CREATE TABLE detalle_pedido (
                        pedido_id INT FOREIGN KEY REFERENCES pedidos(id),
                        producto_id INT FOREIGN KEY REFERENCES productos(id),
                        cantidad INT NOT NULL,
                        precio_unitario DECIMAL(18,2) NOT NULL,
                        PRIMARY KEY (pedido_id, producto_id)
                    );
                ";

                using (var cmd = new SqlCommand(ddl, conexion))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Estructura (5 tablas) creada.");
                }
            }
        }

        public void InsertarDatosPrueba()
        {
            using (var conexion = new SqlConnection(connPractico))
            {
                conexion.Open();
                
                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        // Helper: Ojo a la diferencia. Acá sumamos SELECT SCOPE_IDENTITY()
                        int Insertar(string sql, params SqlParameter[] parametros)
                        {
                            sql += "; SELECT SCOPE_IDENTITY();";
                            using (var cmd = new SqlCommand(sql, conexion, tx))
                            {
                                if (parametros != null) cmd.Parameters.AddRange(parametros);
                                // SCOPE_IDENTITY devuelve numeric, por lo que cast a decimal y luego a int es lo más seguro
                                return Convert.ToInt32(Convert.ToDecimal(cmd.ExecuteScalar()));
                            }
                        }

                        int catElectronica = Insertar("INSERT INTO categorias (nombre) VALUES (@n)", new SqlParameter("@n", "Electrónica"));
                        int catHogar = Insertar("INSERT INTO categorias (nombre) VALUES (@n)", new SqlParameter("@n", "Hogar"));
                        int catLibros = Insertar("INSERT INTO categorias (nombre) VALUES (@n)", new SqlParameter("@n", "Libros"));

                        int prod1 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new SqlParameter("@n", "Notebook 14\""), new SqlParameter("@p", 850000.00m), new SqlParameter("@s", 10), new SqlParameter("@c", catElectronica));
                        int prod2 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new SqlParameter("@n", "Mouse inalámbrico"), new SqlParameter("@p", 12000.00m), new SqlParameter("@s", 50), new SqlParameter("@c", catElectronica));
                        int prod3 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new SqlParameter("@n", "Teclado mecánico"), new SqlParameter("@p", 35000.00m), new SqlParameter("@s", 20), new SqlParameter("@c", catElectronica));
                        int prod4 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new SqlParameter("@n", "Clean Code"), new SqlParameter("@p", 28000.00m), new SqlParameter("@s", 15), new SqlParameter("@c", catLibros));
                        int prod5 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new SqlParameter("@n", "Lámpara LED escritorio"), new SqlParameter("@p", 15000.00m), new SqlParameter("@s", 30), new SqlParameter("@c", catHogar));

                        int cli1 = Insertar("INSERT INTO clientes (nombre, email) VALUES (@n, @e)",
                            new SqlParameter("@n", "Juan Perez"), new SqlParameter("@e", "juan@ejemplo.com"));
                        int cli2 = Insertar("INSERT INTO clientes (nombre, email) VALUES (@n, @e)",
                            new SqlParameter("@n", "Maria Lopez"), new SqlParameter("@e", "maria@ejemplo.com"));

                        int ped1 = Insertar("INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f)",
                            new SqlParameter("@c", cli1), new SqlParameter("@f", DateTime.Now));
                        int ped2 = Insertar("INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f)",
                            new SqlParameter("@c", cli2), new SqlParameter("@f", DateTime.Now));

                        void InsertarDetalle(int pedId, int prodId, int cant, decimal precio)
                        {
                            string sql = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pd, @pr, @c, @pu);";
                            using (var cmd = new SqlCommand(sql, conexion, tx))
                            {
                                cmd.Parameters.AddWithValue("@pd", pedId);
                                cmd.Parameters.AddWithValue("@pr", prodId);
                                cmd.Parameters.AddWithValue("@c", cant);
                                cmd.Parameters.AddWithValue("@pu", precio);
                                cmd.ExecuteNonQuery();
                            }
                        }

                        InsertarDetalle(ped1, prod2, 2, 12000.00m);
                        InsertarDetalle(ped1, prod1, 1, 850000.00m);
                        InsertarDetalle(ped1, prod3, 1, 35000.00m);
                        InsertarDetalle(ped2, prod4, 1, 28000.00m);
                        InsertarDetalle(ped2, prod5, 2, 15000.00m);

                        tx.Commit();
                        Console.WriteLine("Datos de prueba insertados (commit).");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        throw new Exception("Error al insertar datos, se hizo rollback: " + ex.Message);
                    }
                }
            }
        }

        public void EjecutarOperaciones()
        {
            using (var conexion = new SqlConnection(connPractico))
            {
                conexion.Open();
                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        Console.WriteLine("[C1] Productos con su categoría:");
                        string sqlC1 = @"
                            SELECT p.id, p.nombre, p.precio, c.nombre as categoria 
                            FROM productos p 
                            INNER JOIN categorias c ON p.categoria_id = c.id
                            ORDER BY p.id;";

                        using (var cmd = new SqlCommand(sqlC1, conexion, tx))
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Console.WriteLine($"#{reader.GetInt32(0)} {reader.GetString(1)} ${reader.GetDecimal(2):F2} [{reader.GetString(3)}]");
                            }
                        }

                        int pedidoId = 1;
                        Console.WriteLine($"\n[C2] Detalle y total del pedido #{pedidoId}:");
                        
                        string sqlC2Detalle = @"
                            SELECT pr.nombre, dp.cantidad, dp.precio_unitario, (dp.cantidad * dp.precio_unitario) as subtotal
                            FROM detalle_pedido dp
                            JOIN productos pr ON dp.producto_id = pr.id
                            WHERE dp.pedido_id = @pedId;";

                        using (var cmd = new SqlCommand(sqlC2Detalle, conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@pedId", pedidoId);
                            using (var reader = cmd.ExecuteReader())
                            {
                                while (reader.Read())
                                {
                                    Console.WriteLine($"{reader.GetString(0)} x{reader.GetInt32(1)} @ ${reader.GetDecimal(2):F2} = ${reader.GetDecimal(3):F2}");
                                }
                            }
                        }

                        string sqlC2Total = "SELECT SUM(cantidad * precio_unitario) FROM detalle_pedido WHERE pedido_id = @pedId;";
                        using (var cmd = new SqlCommand(sqlC2Total, conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@pedId", pedidoId);
                            var total = Convert.ToDecimal(cmd.ExecuteScalar());
                            Console.WriteLine($"TOTAL pedido #{pedidoId}: ${total:F2}");
                        }

                        int catIdUpdate = 1; 
                        string sqlU1 = "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @catId;";
                        using (var cmd = new SqlCommand(sqlU1, conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@catId", catIdUpdate);
                            int filasAfectadas = cmd.ExecuteNonQuery();
                            Console.WriteLine($"\n[U1] Subí 10% precios de categoría #{catIdUpdate} -> {filasAfectadas} filas.");
                        }

                        int prodIdDelete = 2; 
                        string sqlD1 = "DELETE FROM detalle_pedido WHERE pedido_id = @pedId AND producto_id = @prodId;";
                        using (var cmd = new SqlCommand(sqlD1, conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@pedId", pedidoId);
                            cmd.Parameters.AddWithValue("@prodId", prodIdDelete);
                            int filasAfectadas = cmd.ExecuteNonQuery();
                            Console.WriteLine($"[D1] Borré línea (pedido {pedidoId}, producto {prodIdDelete}) -> {filasAfectadas} filas.");
                        }

                        tx.Commit();
                        Console.WriteLine("Operaciones confirmadas (commit).");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        throw new Exception("Error al ejecutar operaciones: " + ex.Message);
                    }
                }
            }
        }

        public void DemostrarRollback()
        {
            using (var conexion = new SqlConnection(connPractico))
            {
                conexion.Open();

                decimal precioAntes = 0;
                string sqlSelect = "SELECT precio FROM productos WHERE id = 1;";
                using (var cmdSelect = new SqlCommand(sqlSelect, conexion))
                {
                    precioAntes = Convert.ToDecimal(cmdSelect.ExecuteScalar());
                    Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:F2}");
                }

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        string sqlUpdate = "UPDATE productos SET precio = 1 WHERE id = 1;";
                        using (var cmdUpdate = new SqlCommand(sqlUpdate, conexion, tx))
                        {
                            cmdUpdate.ExecuteNonQuery();
                            Console.WriteLine("UPDATE aplicado (precio -> 1) dentro de la transacción.");
                        }

                        throw new Exception("Error simulado: algo salió mal.");
                    }
                    catch (Exception ex)
                    {
                        tx.Rollback();
                        Console.WriteLine($"Excepción capturada -> ROLLBACK. ({ex.Message})");
                    }
                }

                decimal precioDespues = 0;
                using (var cmdSelect = new SqlCommand(sqlSelect, conexion))
                {
                    precioDespues = Convert.ToDecimal(cmdSelect.ExecuteScalar());
                    Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues:F2}");
                }

                if (precioAntes == precioDespues)
                {
                    Console.WriteLine("OK: el rollback funcionó, el dato NO cambió.");
                }
            }
        }
    }
}