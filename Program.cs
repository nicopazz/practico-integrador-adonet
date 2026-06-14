using System;
using Practico.Starter.Datos;

Motor motorElegido = Motor.PostgreSql;
bool motorValido = false;

if (args.Length > 0)
{
  motorValido = Enum.TryParse(args[0], true, out motorElegido);
}

while (!motorValido)
{
  Console.WriteLine("=== PRÁCTICO INTEGRADOR ADO.NET ===");
  Console.WriteLine("1. PostgreSQL");
  Console.WriteLine("2. SQL Server");
  Console.WriteLine("3. MySQL");
  Console.Write("Elija un motor: ");

  string input = Console.ReadLine() ?? string.Empty;
  if (input == "1") { motorElegido = Motor.PostgreSql; motorValido = true; }
  else if (input == "2") { motorElegido = Motor.SqlServer; motorValido = true; }
  else if (input == "3") { motorElegido = Motor.MySql; motorValido = true; }
  else { Console.WriteLine("Opción inválida.\n"); }
}

Console.WriteLine($"\n==== MOTOR: {motorElegido} ====");

try
{
  IAccesoDatos acceso = FabricaDeMotor.Crear(motorElegido);

  Console.WriteLine("\n RF2 - Crear estructura");
  acceso.CrearEstructura();

  Console.WriteLine("\n RF3 - Insertar datos de prueba");
  acceso.InsertarDatosPrueba();

  Console.WriteLine("\n RF4 - Ejecutar operaciones (C1, C2, U1, D1)");
  acceso.EjecutarOperaciones();

  Console.WriteLine("\n RF5 - Demostrar rollback");
  acceso.DemostrarRollback();

  Console.WriteLine($"\n===== FIN ({motorElegido}) =====");
}
catch (NotImplementedException)
{
  Console.WriteLine("\n[!] Todavía falta implementar este método.");
}
catch (Exception ex)
{
  Console.WriteLine($"\nERROR: {ex.Message}");
}
