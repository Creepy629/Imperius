using System.Collections.Generic;
using System.ComponentModel;

namespace Imperius.Modelo
{
    /// <summary>
    /// Clase que representa un grupo de tareas de la misma materia
    /// </summary>
    public class TareaGrupo : INotifyPropertyChanged
    {
        private string nombreMateria = string.Empty;
        private string colorMateria = "#6C1D45";
        private List<Tarea> tareas = new List<Tarea>();

        public string NombreMateria
        {
            get => nombreMateria;
            set
            {
                if (nombreMateria != value)
                {
                    nombreMateria = value;
                    OnPropertyChanged(nameof(NombreMateria));
                }
            }
        }

        public string ColorMateria
        {
            get => colorMateria;
            set
            {
                if (colorMateria != value)
                {
                    colorMateria = value;
                    OnPropertyChanged(nameof(ColorMateria));
                }
            }
        }

        public List<Tarea> Tareas
        {
            get => tareas;
            set
            {
                if (tareas != value)
                {
                    tareas = value;
                    OnPropertyChanged(nameof(Tareas));
                    OnPropertyChanged(nameof(CantidadTareas));
                }
            }
        }

        public int CantidadTareas => Tareas?.Count ?? 0;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}