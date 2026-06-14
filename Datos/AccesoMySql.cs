using System;
using MySqlConnector;

namespace Practico.Starter.Datos
{
    public class AccesoMySql : IAccesoDatos
    {
        // Credenciales extraídas del PDF para MySQL en Docker
        private readonly string connServer = "Server=localhost;Port=3307;Uid=root;Pwd=Curso.NET2026;";
        private readonly string connPractico = "Server=localhost;Port=3307;Uid=root;Pwd=Curso.NET2026;Database=practico;";

        public void CrearEstructura()
        {
            // PASO 1: Conectarse al motor para crear la base
            using (var conexion = new MySqlConnection(connServer))
            {
                conexion.Open();
                string sqlDb = "CREATE DATABASE IF NOT EXISTS practico;";
                using (var cmd = new MySqlCommand(sqlDb, conexion))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Base 'practico' creada (o ya existía).");
                }
            }

            // PASO 2: Conectarse a la nueva base para crear las tablas
            using (var conexion = new MySqlConnection(connPractico))
            {
                conexion.Open();

                // DDL en MySQL hace COMMIT implícito, por lo que va sin transacción
                string ddl = @"
                    DROP TABLE IF EXISTS detalle_pedido;
                    DROP TABLE IF EXISTS pedidos;
                    DROP TABLE IF EXISTS productos;
                    DROP TABLE IF EXISTS clientes;
                    DROP TABLE IF EXISTS categorias;

                    CREATE TABLE categorias (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL
                    );

                    CREATE TABLE clientes (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        email VARCHAR(100) NOT NULL
                    );

                    CREATE TABLE productos (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        precio DECIMAL(18,2) NOT NULL,
                        stock INT NOT NULL,
                        categoria_id INT,
                        FOREIGN KEY (categoria_id) REFERENCES categorias(id)
                    );

                    CREATE TABLE pedidos (
                        id INT AUTO_INCREMENT PRIMARY KEY,
                        cliente_id INT,
                        fecha DATETIME NOT NULL,
                        FOREIGN KEY (cliente_id) REFERENCES clientes(id)
                    );

                    CREATE TABLE detalle_pedido (
                        pedido_id INT,
                        producto_id INT,
                        cantidad INT NOT NULL,
                        precio_unitario DECIMAL(18,2) NOT NULL,
                        PRIMARY KEY (pedido_id, producto_id),
                        FOREIGN KEY (pedido_id) REFERENCES pedidos(id),
                        FOREIGN KEY (producto_id) REFERENCES productos(id)
                    );
                ";

                using (var cmd = new MySqlCommand(ddl, conexion))
                {
                    cmd.ExecuteNonQuery();
                    Console.WriteLine("Estructura (5 tablas) creada.");
                }
            }
        }

        public void InsertarDatosPrueba()
        {
            using (var conexion = new MySqlConnection(connPractico))
            {
                conexion.Open();
                
                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        // Helper: Ojo a la diferencia con Postgres. Acá sumamos SELECT LAST_INSERT_ID()
                        int Insertar(string sql, params MySqlParameter[] parametros)
                        {
                            // Agregamos la consulta para recuperar el ID en la misma ejecución
                            sql += "; SELECT LAST_INSERT_ID();";
                            using (var cmd = new MySqlCommand(sql, conexion, tx))
                            {
                                if (parametros != null) cmd.Parameters.AddRange(parametros);
                                return Convert.ToInt32(cmd.ExecuteScalar());
                            }
                        }

                        int catElectronica = Insertar("INSERT INTO categorias (nombre) VALUES (@n)", new MySqlParameter("@n", "Electrónica"));
                        int catHogar = Insertar("INSERT INTO categorias (nombre) VALUES (@n)", new MySqlParameter("@n", "Hogar"));
                        int catLibros = Insertar("INSERT INTO categorias (nombre) VALUES (@n)", new MySqlParameter("@n", "Libros"));

                        int prod1 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new MySqlParameter("@n", "Notebook 14\""), new MySqlParameter("@p", 850000.00m), new MySqlParameter("@s", 10), new MySqlParameter("@c", catElectronica));
                        int prod2 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new MySqlParameter("@n", "Mouse inalámbrico"), new MySqlParameter("@p", 12000.00m), new MySqlParameter("@s", 50), new MySqlParameter("@c", catElectronica));
                        int prod3 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new MySqlParameter("@n", "Teclado mecánico"), new MySqlParameter("@p", 35000.00m), new MySqlParameter("@s", 20), new MySqlParameter("@c", catElectronica));
                        int prod4 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new MySqlParameter("@n", "Clean Code"), new MySqlParameter("@p", 28000.00m), new MySqlParameter("@s", 15), new MySqlParameter("@c", catLibros));
                        int prod5 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c)",
                            new MySqlParameter("@n", "Lámpara LED escritorio"), new MySqlParameter("@p", 15000.00m), new MySqlParameter("@s", 30), new MySqlParameter("@c", catHogar));

                        int cli1 = Insertar("INSERT INTO clientes (nombre, email) VALUES (@n, @e)",
                            new MySqlParameter("@n", "Juan Perez"), new MySqlParameter("@e", "juan@ejemplo.com"));
                        int cli2 = Insertar("INSERT INTO clientes (nombre, email) VALUES (@n, @e)",
                            new MySqlParameter("@n", "Maria Lopez"), new MySqlParameter("@e", "maria@ejemplo.com"));

                        int ped1 = Insertar("INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f)",
                            new MySqlParameter("@c", cli1), new MySqlParameter("@f", DateTime.Now));
                        int ped2 = Insertar("INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f)",
                            new MySqlParameter("@c", cli2), new MySqlParameter("@f", DateTime.Now));

                        void InsertarDetalle(int pedId, int prodId, int cant, decimal precio)
                        {
                            string sql = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pd, @pr, @c, @pu);";
                            using (var cmd = new MySqlCommand(sql, conexion, tx))
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
            using (var conexion = new MySqlConnection(connPractico))
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

                        using (var cmd = new MySqlCommand(sqlC1, conexion, tx))
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

                        using (var cmd = new MySqlCommand(sqlC2Detalle, conexion, tx))
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
                        using (var cmd = new MySqlCommand(sqlC2Total, conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@pedId", pedidoId);
                            var total = Convert.ToDecimal(cmd.ExecuteScalar());
                            Console.WriteLine($"TOTAL pedido #{pedidoId}: ${total:F2}");
                        }

                        int catIdUpdate = 1; 
                        string sqlU1 = "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @catId;";
                        using (var cmd = new MySqlCommand(sqlU1, conexion, tx))
                        {
                            cmd.Parameters.AddWithValue("@catId", catIdUpdate);
                            int filasAfectadas = cmd.ExecuteNonQuery();
                            Console.WriteLine($"\n[U1] Subí 10% precios de categoría #{catIdUpdate} -> {filasAfectadas} filas.");
                        }

                        int prodIdDelete = 2; 
                        string sqlD1 = "DELETE FROM detalle_pedido WHERE pedido_id = @pedId AND producto_id = @prodId;";
                        using (var cmd = new MySqlCommand(sqlD1, conexion, tx))
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
            using (var conexion = new MySqlConnection(connPractico))
            {
                conexion.Open();

                decimal precioAntes = 0;
                string sqlSelect = "SELECT precio FROM productos WHERE id = 1;";
                using (var cmdSelect = new MySqlCommand(sqlSelect, conexion))
                {
                    precioAntes = Convert.ToDecimal(cmdSelect.ExecuteScalar());
                    Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:F2}");
                }

                using (var tx = conexion.BeginTransaction())
                {
                    try
                    {
                        string sqlUpdate = "UPDATE productos SET precio = 1 WHERE id = 1;";
                        using (var cmdUpdate = new MySqlCommand(sqlUpdate, conexion, tx))
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
                using (var cmdSelect = new MySqlCommand(sqlSelect, conexion))
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