using SQLite;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Imperius.Modelo
{
    [Table("tbTareas")]
    public class Tarea : INotifyPropertyChanged
    {
        private string titulo = string.Empty;
        private string materia = string.Empty;
        private string descripcion = string.Empty;
        private DateTime fechaVencimiento = DateTime.Now;
        private string colorHex = "#6C1D45"; 
        private bool estaCompletada;
        private int idUsuario_FK;

        [PrimaryKey, AutoIncrement, Column("idTarea")]
        public int IdTarea { get; set; }

        [Column("titulo")]
        public string Titulo
        {
            get => titulo;
            set
            {
                if (titulo != value)
                {
                    titulo = value ?? string.Empty;
                    OnPropertyChanged(nameof(Titulo));
                }
            }
        }

        [Column("materia")]
        public string Materia
        {
            get => materia;
            set
            {
                if (materia != value)
                {
                    materia = value ?? string.Empty;
                    OnPropertyChanged(nameof(Materia));
                }
            }
        }

        [Column("descripcion")]
        public string Descripcion
        {
            get => descripcion;
            set
            {
                if (descripcion != value)
                {
                    descripcion = value ?? string.Empty;
                    OnPropertyChanged(nameof(Descripcion));
                }
            }
        }

        [Column("fecha")]
        public string FechaVencimientoString
        {
            get => FechaVencimiento.ToString("o", CultureInfo.InvariantCulture);
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    FechaVencimiento = DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
                }
                else
                {
                    FechaVencimiento = DateTime.Now;
                }
                OnPropertyChanged(nameof(FechaVencimientoString));
            }
        }

        [Ignore]
        public DateTime FechaVencimiento
        {
            get => fechaVencimiento;
            set
            {
                if (fechaVencimiento != value)
                {
                    fechaVencimiento = value;
                    OnPropertyChanged(nameof(FechaVencimiento));
                    OnPropertyChanged(nameof(FechaVencimientoString));
                    OnPropertyChanged(nameof(FechaEntregaFormateada));
                    OnPropertyChanged(nameof(VenceEn24Horas));
                    OnPropertyChanged(nameof(VenceEn7Dias));
                }
            }
        }

        [Column("colorHEX")]
        public string ColorHex
        {
            get => colorHex;
            set
            {
                if (colorHex != value)
                {
                    colorHex = value ?? "#6C1D45";  // Crimson Violet como fallback
                    OnPropertyChanged(nameof(ColorHex));
                }
            }
        }

        [Column("estaCompletada")]
        public int estaCompletadaInt
        {
            get => EstaCompletada ? 1 : 0;
            set
            {
                var nuevo = value == 1;
                if (EstaCompletada != nuevo)
                {
                    EstaCompletada = nuevo;
                    OnPropertyChanged(nameof(estaCompletadaInt));
                }
            }
        }

        [Ignore]
        public bool EstaCompletada
        {
            get => estaCompletada;
            set
            {
                if (estaCompletada != value)
                {
                    estaCompletada = value;
                    OnPropertyChanged(nameof(EstaCompletada));
                    OnPropertyChanged(nameof(estaCompletadaInt));
                }
            }
        }

        [Column("idUsuario_FK")]
        public int IdUsuario_FK
        {
            get => idUsuario_FK;
            set
            {
                if (idUsuario_FK != value)
                {
                    idUsuario_FK = value;
                    OnPropertyChanged(nameof(IdUsuario_FK));
                }
            }
        }

        // Propiedades calculadas para alertas de vencimiento
        [Ignore]
        public string FechaEntregaFormateada => FechaVencimiento.ToString("dd/MM/yyyy HH:mm");

        [Ignore]
        public bool VenceEn24Horas
        {
            get
            {
                var diferencia = FechaVencimiento - DateTime.Now;
                return diferencia.TotalHours <= 24 && diferencia.TotalHours > 0;
            }
        }

        [Ignore]
        public bool VenceEn7Dias
        {
            get
            {
                var diferencia = FechaVencimiento - DateTime.Now;
                return diferencia.TotalDays <= 7 && diferencia.TotalDays > 1 && !VenceEn24Horas;
            }
        }

        [Ignore]
        public bool EstaVencida
        {
            get
            {
                var diferencia = DateTime.Now - FechaVencimiento;
                return diferencia.TotalSeconds > 0 && !EstaCompletada;
            }
        }

        [Ignore]
        public bool DebeSerEliminada
        {
            get
            {
                var diferencia = DateTime.Now - FechaVencimiento;
                return diferencia.TotalHours > 24 && !EstaCompletada;
            }
        }

        public Tarea()
        {
        }

        public Tarea(string titulo, string materia, string descripcion, DateTime fechaVencimiento)
        {
            Titulo = titulo;
            Materia = materia;
            Descripcion = descripcion;
            FechaVencimiento = fechaVencimiento;
        }

        public override string ToString() =>
            $"{Titulo} — {Materia} — {FechaVencimiento:g}";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}