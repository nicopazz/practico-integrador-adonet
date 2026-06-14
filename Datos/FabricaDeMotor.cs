using System;

namespace Practico.Starter.Datos
{
    public static class FabricaDeMotor
    {
        public static IAccesoDatos Crear(Motor m)
        {
            return m switch
            {
                Motor.PostgreSql => new AccesoPostgres(),
                Motor.SqlServer => new AccesoSqlServer(),
                Motor.MySql => new AccesoMySql(),
                _ => throw new ArgumentException("Motor no soportado")
            };
        }
    }
}