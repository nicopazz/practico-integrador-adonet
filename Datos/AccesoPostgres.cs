using System;
using Npgsql;

namespace Practico.Starter.Datos
{
    public class AccesoPostgres : IAccesoDatos
    {
        // Credenciales extraídas de las guías del TP para Postgres local
        private readonly string connAdmin = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=postgres";
        private readonly string connPractico = "Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=practico";

        public void CrearEstructura()
        {
            // PASO 1: Conectarse al motor para crear la base de datos (si no existe)
            using (var conexion = new NpgsqlConnection(connAdmin))
            {
                conexion.Open();
                
                // Consultamos el catálogo interno porque Postgres no soporta 'CREATE DATABASE IF NOT EXISTS'
                string checkSql = "SELECT 1 FROM pg_database WHERE datname = 'practico'";
                using (var cmdCheck = new NpgsqlCommand(checkSql, conexion))
                {
                    var existe = cmdCheck.ExecuteScalar();
                    if (existe == null)
                    {
                        using (var cmdCreate = new NpgsqlCommand("CREATE DATABASE practico", conexion))
                        {
                            cmdCreate.ExecuteNonQuery();
                            Console.WriteLine("Base 'practico' creada.");
                        }
                    }
                }
            }

            // PASO 2: Conectarse a la nueva base 'practico' para crear las tablas
            using (var conexion = new NpgsqlConnection(connPractico))
            {
                conexion.Open();

                // Usamos SERIAL para los autoincrementales nativos de Postgres
                string ddl = @"
                    -- Borrar primero las tablas hijas (dependientes)
                    DROP TABLE IF EXISTS detalle_pedido;
                    DROP TABLE IF EXISTS pedidos;
                    DROP TABLE IF EXISTS productos;
                    DROP TABLE IF EXISTS clientes;
                    DROP TABLE IF EXISTS categorias;

                    -- Creación de tablas independientes
                    CREATE TABLE categorias (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL
                    );

                    CREATE TABLE clientes (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        email VARCHAR(100) NOT NULL
                    );

                    -- Creación de tablas dependientes
                    CREATE TABLE productos (
                        id SERIAL PRIMARY KEY,
                        nombre VARCHAR(100) NOT NULL,
                        precio DECIMAL(18,2) NOT NULL,
                        stock INT NOT NULL,
                        categoria_id INT REFERENCES categorias(id)
                    );

                    CREATE TABLE pedidos (
                        id SERIAL PRIMARY KEY,
                        cliente_id INT REFERENCES clientes(id),
                        fecha TIMESTAMP NOT NULL
                    );

                    -- Tabla intermedia para relación N-N con PK Compuesta
                    CREATE TABLE detalle_pedido (
                        pedido_id INT REFERENCES pedidos(id),
                        producto_id INT REFERENCES productos(id),
                        cantidad INT NOT NULL,
                        precio_unitario DECIMAL(18,2) NOT NULL,
                        PRIMARY KEY (pedido_id, producto_id)
                    );
                ";

                using (var cmdDdl = new NpgsqlCommand(ddl, conexion))
                {
                    cmdDdl.ExecuteNonQuery();
                    Console.WriteLine("Base 'practico' creada.");
                    Console.WriteLine("Estructura (5 tablas) creada.");
                }
            }
        }

       public void InsertarDatosPrueba()
{
    using (var conexion = new NpgsqlConnection(connPractico))
    {
        conexion.Open();
        
        // Iniciamos LA transacción para todo el bloque (RF3)
        using (var tx = conexion.BeginTransaction())
        {
            try
            {
                // Función local (helper) para no repetir 20 veces el mismo código de ADO.NET
                // Ejecuta el insert, pasa los parámetros y devuelve el ID generado.
                int Insertar(string sql, params NpgsqlParameter[] parametros)
                {
                    using (var cmd = new NpgsqlCommand(sql, conexion, tx))
                    {
                        if (parametros != null) cmd.Parameters.AddRange(parametros);
                        // ExecuteScalar lee la primera columna de la primera fila (nuestro RETURNING id)
                        return Convert.ToInt32(cmd.ExecuteScalar());
                    }
                }

                // 1. Insertar Categorías (>=3)
                int catElectronica = Insertar("INSERT INTO categorias (nombre) VALUES (@n) RETURNING id;", new NpgsqlParameter("@n", "Electrónica"));
                int catHogar = Insertar("INSERT INTO categorias (nombre) VALUES (@n) RETURNING id;", new NpgsqlParameter("@n", "Hogar"));
                int catLibros = Insertar("INSERT INTO categorias (nombre) VALUES (@n) RETURNING id;", new NpgsqlParameter("@n", "Libros"));

                // 2. Insertar Productos (5)
                int prod1 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c) RETURNING id;",
                    new NpgsqlParameter("@n", "Notebook 14\""), new NpgsqlParameter("@p", 850000.00m), new NpgsqlParameter("@s", 10), new NpgsqlParameter("@c", catElectronica));
                int prod2 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c) RETURNING id;",
                    new NpgsqlParameter("@n", "Mouse inalámbrico"), new NpgsqlParameter("@p", 12000.00m), new NpgsqlParameter("@s", 50), new NpgsqlParameter("@c", catElectronica));
                int prod3 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c) RETURNING id;",
                    new NpgsqlParameter("@n", "Teclado mecánico"), new NpgsqlParameter("@p", 35000.00m), new NpgsqlParameter("@s", 20), new NpgsqlParameter("@c", catElectronica));
                int prod4 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c) RETURNING id;",
                    new NpgsqlParameter("@n", "Clean Code"), new NpgsqlParameter("@p", 28000.00m), new NpgsqlParameter("@s", 15), new NpgsqlParameter("@c", catLibros));
                int prod5 = Insertar("INSERT INTO productos (nombre, precio, stock, categoria_id) VALUES (@n, @p, @s, @c) RETURNING id;",
                    new NpgsqlParameter("@n", "Lámpara LED escritorio"), new NpgsqlParameter("@p", 15000.00m), new NpgsqlParameter("@s", 30), new NpgsqlParameter("@c", catHogar));

                // 3. Insertar Clientes (2)
                int cli1 = Insertar("INSERT INTO clientes (nombre, email) VALUES (@n, @e) RETURNING id;",
                    new NpgsqlParameter("@n", "Juan Perez"), new NpgsqlParameter("@e", "juan@ejemplo.com"));
                int cli2 = Insertar("INSERT INTO clientes (nombre, email) VALUES (@n, @e) RETURNING id;",
                    new NpgsqlParameter("@n", "Maria Lopez"), new NpgsqlParameter("@e", "maria@ejemplo.com"));

                // 4. Insertar Pedidos (2)
                int ped1 = Insertar("INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f) RETURNING id;",
                    new NpgsqlParameter("@c", cli1), new NpgsqlParameter("@f", DateTime.Now));
                int ped2 = Insertar("INSERT INTO pedidos (cliente_id, fecha) VALUES (@c, @f) RETURNING id;",
                    new NpgsqlParameter("@c", cli2), new NpgsqlParameter("@f", DateTime.Now));

                // 5. Insertar DetallePedido (N-N)
                // Acá no usamos RETURNING id porque tiene una clave primaria compuesta, usamos ExecuteNonQuery normal.
                void InsertarDetalle(int pedId, int prodId, int cant, decimal precio)
                {
                    string sql = "INSERT INTO detalle_pedido (pedido_id, producto_id, cantidad, precio_unitario) VALUES (@pd, @pr, @c, @pu);";
                    using (var cmd = new NpgsqlCommand(sql, conexion, tx))
                    {
                        cmd.Parameters.AddWithValue("@pd", pedId);
                        cmd.Parameters.AddWithValue("@pr", prodId);
                        cmd.Parameters.AddWithValue("@c", cant);
                        cmd.Parameters.AddWithValue("@pu", precio);
                        cmd.ExecuteNonQuery();
                    }
                }

                InsertarDetalle(ped1, prod2, 2, 12000.00m);  // Juan compra 2 mouses
                InsertarDetalle(ped1, prod1, 1, 850000.00m); // Juan compra 1 notebook
                InsertarDetalle(ped1, prod3, 1, 35000.00m);  // Juan compra 1 teclado

                InsertarDetalle(ped2, prod4, 1, 28000.00m);  // Maria compra 1 libro
                InsertarDetalle(ped2, prod5, 2, 15000.00m);  // Maria compra 2 lámparas

                // Si llegamos hasta acá sin errores, confirmamos TODOS los cambios juntos
                tx.Commit();
                Console.WriteLine("Datos de prueba insertados (commit).");
            }
            catch (Exception ex)
            {
                // Si explota cualquier cosa (ej: falta un campo o error de FK), deshacemos todo
                tx.Rollback();
                throw new Exception("Error al insertar datos, se hizo rollback: " + ex.Message);
            }
        }
    }
}

        public void EjecutarOperaciones()
{
    using (var conexion = new NpgsqlConnection(connPractico))
    {
        conexion.Open();

        // RF4 requiere que TODO ocurra dentro de UNA sola transacción
        using (var tx = conexion.BeginTransaction())
        {
            try
            {
                // =========================================================
                // [C1] INNER JOIN - Leer con DataReader
                // =========================================================
                Console.WriteLine("[C1] Productos con su categoría:");
                string sqlC1 = @"
                    SELECT p.id, p.nombre, p.precio, c.nombre as categoria 
                    FROM productos p 
                    INNER JOIN categorias c ON p.categoria_id = c.id
                    ORDER BY p.id;";

                using (var cmd = new NpgsqlCommand(sqlC1, conexion, tx))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        // reader.GetInt32(0) lee la primera columna (id)
                        // reader.GetString(1) lee la segunda (nombre), etc.
                        Console.WriteLine($"#{reader.GetInt32(0)} {reader.GetString(1)} ${reader.GetDecimal(2):F2} [{reader.GetString(3)}]");
                    }
                }

                // =========================================================
                // [C2] JOIN + GROUP BY / SUM - Detalle y total de un pedido
                // =========================================================
                int pedidoId = 1;
                Console.WriteLine($"\n[C2] Detalle y total del pedido #{pedidoId}:");
                
                string sqlC2Detalle = @"
                    SELECT pr.nombre, dp.cantidad, dp.precio_unitario, (dp.cantidad * dp.precio_unitario) as subtotal
                    FROM detalle_pedido dp
                    JOIN productos pr ON dp.producto_id = pr.id
                    WHERE dp.pedido_id = @pedId;";

                using (var cmd = new NpgsqlCommand(sqlC2Detalle, conexion, tx))
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

                // Calculamos el Total general con la función de agregación SUM
                string sqlC2Total = "SELECT SUM(cantidad * precio_unitario) FROM detalle_pedido WHERE pedido_id = @pedId;";
                using (var cmd = new NpgsqlCommand(sqlC2Total, conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@pedId", pedidoId);
                    var total = Convert.ToDecimal(cmd.ExecuteScalar());
                    Console.WriteLine($"TOTAL pedido #{pedidoId}: ${total:F2}");
                }

                // =========================================================
                // [U1] UPDATE - Subir un porcentaje
                // =========================================================
                int catIdUpdate = 1; // 1 = Electrónica
                string sqlU1 = "UPDATE productos SET precio = precio * 1.10 WHERE categoria_id = @catId;";
                using (var cmd = new NpgsqlCommand(sqlU1, conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@catId", catIdUpdate);
                    int filasAfectadas = cmd.ExecuteNonQuery(); // ExecuteNonQuery devuelve la cantidad de filas modificadas
                    Console.WriteLine($"\n[U1] Subí 10% precios de categoría #{catIdUpdate} -> {filasAfectadas} filas.");
                }

                // =========================================================
                // [D1] DELETE - Borrar una línea de detalle
                // =========================================================
                int prodIdDelete = 2; // Borramos el Mouse inalámbrico del pedido 1
                string sqlD1 = "DELETE FROM detalle_pedido WHERE pedido_id = @pedId AND producto_id = @prodId;";
                using (var cmd = new NpgsqlCommand(sqlD1, conexion, tx))
                {
                    cmd.Parameters.AddWithValue("@pedId", pedidoId);
                    cmd.Parameters.AddWithValue("@prodId", prodIdDelete);
                    int filasAfectadas = cmd.ExecuteNonQuery();
                    Console.WriteLine($"[D1] Borré línea (pedido {pedidoId}, producto {prodIdDelete}) -> {filasAfectadas} filas.");
                }

                // Si ninguna consulta tiró excepción, guardamos los cambios.
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
    using (var conexion = new NpgsqlConnection(connPractico))
    {
        conexion.Open();

        // 1. Consultar el precio ANTES de la transacción
        decimal precioAntes = 0;
        string sqlSelect = "SELECT precio FROM productos WHERE id = 1;";
        using (var cmdSelect = new NpgsqlCommand(sqlSelect, conexion))
        {
            precioAntes = Convert.ToDecimal(cmdSelect.ExecuteScalar());
            Console.WriteLine($"Precio del producto #1 ANTES: ${precioAntes:F2}");
        }

        // 2. Abrir la transacción y simular el desastre
        using (var tx = conexion.BeginTransaction())
        {
            try
            {
                // Hacemos un UPDATE malicioso o erróneo
                string sqlUpdate = "UPDATE productos SET precio = 1 WHERE id = 1;";
                using (var cmdUpdate = new NpgsqlCommand(sqlUpdate, conexion, tx))
                {
                    cmdUpdate.ExecuteNonQuery();
                    Console.WriteLine("UPDATE aplicado (precio -> 1) dentro de la transacción.");
                }

                // Acá forzamos a que el sistema falle ANTES del Commit
                throw new Exception("Error simulado: algo salió mal.");
                
                // tx.Commit(); <-- Nunca va a llegar a esta línea
            }
            catch (Exception ex)
            {
                // Como falló, deshacemos todos los cambios que estaban en el aire
                tx.Rollback();
                Console.WriteLine($"Excepción capturada -> ROLLBACK. ({ex.Message})");
            }
        }

        // 3. Consultar el precio DESPUÉS para confirmar la atomicidad
        decimal precioDespues = 0;
        using (var cmdSelect = new NpgsqlCommand(sqlSelect, conexion))
        {
            precioDespues = Convert.ToDecimal(cmdSelect.ExecuteScalar());
            Console.WriteLine($"Precio del producto #1 DESPUÉS: ${precioDespues:F2}");
        }

        // Verificamos el resultado final
        if (precioAntes == precioDespues)
        {
            Console.WriteLine("OK: el rollback funcionó, el dato NO cambió.");
        }
    }
}
    }
}