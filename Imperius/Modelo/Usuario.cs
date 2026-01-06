using System;
using SQLite;

namespace Imperius.Modelo
{
    [Table("tbUsuarios")]
    public class Usuario
    {
        [PrimaryKey, AutoIncrement, Column("idUsuario")]
        public int IdUsuario { get; set; }

        [Column("correo")]
        public string Correo { get; set; } = string.Empty;

        [Column("correoHash")]
        public string CorreoHash { get; set; } = string.Empty;

        [Column("contraseña")]
        public string Contrasena { get; set; } = string.Empty;

        [Column("nombre")]
        public string Nombre { get; set; } = string.Empty;

        [Column("apellido")]
        public string Apellido { get; set; } = string.Empty;

        [Column("boleta")]
        public string Boleta { get; set; } = string.Empty;

        [Column("boletaHash")]
        public string BoletaHash { get; set; } = string.Empty;
    }
}