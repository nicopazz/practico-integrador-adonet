# Práctico Integrador: ADO.NET Puro (Multi-Motor) 🚀

Este proyecto es una aplicación de consola en C# .NET que demuestra la implementación de acceso a datos clásico utilizando **ADO.NET puro**. 

El objetivo principal es gestionar un dominio de e-commerce (5 tablas con relaciones 1-N y N-N) conectándose a tres motores de bases de datos relacionales diferentes (**PostgreSQL, MySQL y SQL Server**) sin alterar la lógica de negocio de la aplicación, logrando esto a través de patrones de diseño de software.

## 🎯 Características Principales

- **Independencia del Motor:** Implementación de los patrones **Strategy** y **Factory** para cambiar el motor de base de datos en tiempo de ejecución de forma transparente para el cliente.
- **Transacciones Seguras (ACID):** Manejo explícito de `Commit` y `Rollback` para asegurar la atomicidad de operaciones complejas (ej. inserciones múltiples en tablas relacionadas).
- **Prevención de Inyección SQL:** Uso estricto de parámetros parametrizados en todas las consultas a la base de datos.
- **Recuperación de IDs Generados:** Extracción de identificadores autogenerados (`RETURNING`, `LAST_INSERT_ID()`, `SCOPE_IDENTITY()`) adaptados de forma nativa al dialecto de cada motor.
- **DDL Re-ejecutable:** Scripts de creación de estructura (tablas y relaciones) diseñados para ser idempotentes, respetando el orden de las Foreign Keys.

## 🛠️ Stack Tecnológico

- **Lenguaje:** C# (.NET)
- **Acceso a Datos:** ADO.NET (Sin ORMs)
- **Drivers:** - `Npgsql` (PostgreSQL)
  - `MySqlConnector` (MySQL)
  - `Microsoft.Data.SqlClient` (SQL Server)
- **Infraestructura:** Docker (contenedores locales para los motores de base de datos)

## 📁 Arquitectura y Estructura del Proyecto

El proyecto está dividido conceptualmente para separar las responsabilidades:

```text
📂 Dominio/      # Entidades POCO (Categoria, Cliente, Producto, Pedido, DetallePedido)
📂 Datos/        # Capa de acceso a datos
   ├── IAccesoDatos.cs       # Interfaz común (Patrón Strategy)
   ├── FabricaDeMotor.cs     # Instanciación de conexiones (Patrón Factory)
   ├── AccesoPostgres.cs     # Implementación concreta para dialecto Postgres
   ├── AccesoMySql.cs        # Implementación concreta para dialecto MySQL
   └── AccesoSqlServer.cs    # Implementación concreta para dialecto SQL Server
```

## 🚀 Guía de Ejecución

### 1. Requisitos previos
Es necesario tener los servicios de base de datos corriendo localmente. En este entorno de desarrollo, se utilizan contenedores Docker expuestos en los siguientes puertos:
- **PostgreSQL:** `localhost:5432`
- **MySQL:** `localhost:3307`
- **SQL Server:** `localhost:1433`

### 2. Ejecutar la aplicación
Abrir una terminal en la raíz del proyecto y ejecutar la CLI de .NET:

```bash
# Para abrir el menú interactivo de selección de motor:
dotnet run

# Para ejecutar directamente contra un motor específico por argumentos:
dotnet run postgres
dotnet run mysql
dotnet run sqlserver
```

## 📝 Flujo de Operaciones (Requisitos del TP)

Al iniciar, la aplicación interactúa con la base de datos seleccionada realizando el siguiente flujo de forma automática:

1. **RF2 (Estructura):** Creación de la base de datos (`practico`) y DDL de las tablas (fuera de transacción).
2. **RF3 (Sembrado de Datos):** Inserción de catálogo y clientes de prueba completos dentro de una única transacción recuperando las PKs en tiempo real.
3. **RF4 (Operaciones CRUD):** Ejecución de lecturas complejas (`INNER JOIN`, `GROUP BY`, agregaciones), un `UPDATE` masivo porcentual y un `DELETE` confirmados con un Commit.
4. **RF5 (Atomicidad):** Demostración de un `ROLLBACK` forzando una excepción luego de un intento de `UPDATE`, verificando con una lectura posterior que el estado original del registro quedó intacto.