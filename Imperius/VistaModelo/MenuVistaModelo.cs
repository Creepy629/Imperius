using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Imperius.Modelo;
using Imperius.Servicios;

namespace Imperius.VistaModelo
{
    public class MenuVistaModelo : INotifyPropertyChanged
    {
        private readonly TareaServicio _tareaServicio;
        private ObservableCollection<TareaGrupo> tareasAgrupadas;
        private string nombreUsuario = "Usuario";

        public ObservableCollection<TareaGrupo> TareasAgrupadas
        {
            get => tareasAgrupadas;
            set
            {
                if (tareasAgrupadas != value)
                {
                    tareasAgrupadas = value;
                    OnPropertyChanged(nameof(TareasAgrupadas));
                }
            }
        }

        public string NombreUsuario
        {
            get => nombreUsuario;
            set
            {
                if (nombreUsuario != value)
                {
                    nombreUsuario = value;
                    OnPropertyChanged(nameof(NombreUsuario));
                }
            }
        }

        public MenuVistaModelo(TareaServicio tareaServicio)
        {
            _tareaServicio = tareaServicio;
            tareasAgrupadas = new ObservableCollection<TareaGrupo>();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await CargarNombreUsuarioAsync();
            await RefreshDataAsync();
        }

        private async Task CargarNombreUsuarioAsync()
        {
            try
            {
                string? nombreGuardado = await UsuarioServicio.GetCurrentUserNameAsync();
                if (!string.IsNullOrWhiteSpace(nombreGuardado))
                {
                    NombreUsuario = nombreGuardado;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar nombre de usuario: {ex.Message}");
            }
        }

        public async Task RefreshDataAsync()
        {
            try
            {
                string? boletaHashActivo = await UsuarioServicio.GetCurrentUserBoletaHashAsync();
                int? idUsuarioActivo = null;

                if (!string.IsNullOrWhiteSpace(boletaHashActivo))
                {
                    idUsuarioActivo = await UsuarioServicio.GetUserIdByBoletaHashAsync(boletaHashActivo);
                }

                if (!idUsuarioActivo.HasValue || idUsuarioActivo.Value == 0)
                {
                    Console.WriteLine("No hay usuario activo para cargar tareas.");
                    TareasAgrupadas = new ObservableCollection<TareaGrupo>();
                    return;
                }

                var todasLasTareas = await _tareaServicio.ObtenerTodasLasTareasAsync(idUsuarioActivo.Value);

                // Agrupar tareas por materia
                var gruposPorMateria = todasLasTareas
                    .Where(t => !string.IsNullOrWhiteSpace(t.Materia))
                    .GroupBy(t => t.Materia.Trim())
                    .Select(grupo => new TareaGrupo
                    {
                        NombreMateria = grupo.Key,
                        // Usar el color de la primera tarea como color del grupo
                        ColorMateria = grupo.FirstOrDefault()?.ColorHex ?? "#6C1D45",
                        Tareas = grupo.OrderBy(t => t.FechaVencimiento).ToList()
                    })
                    .OrderBy(g => ObtenerJerarquiaColor(g.ColorMateria))
                    .ThenBy(g => g.NombreMateria)
                    .ToList();

                TareasAgrupadas = new ObservableCollection<TareaGrupo>(gruposPorMateria);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al refrescar datos: {ex.Message}");
                TareasAgrupadas = new ObservableCollection<TareaGrupo>();
            }
        }

        /// <summary>
        /// Define la jerarquía de colores para ordenamiento de grupos
        /// </summary>
        private int ObtenerJerarquiaColor(string colorHex)
        {
            return colorHex?.ToUpper() switch
            {
                "#6C1D45" => 1,  // Crimson Violet
                "#8E5123" => 2,  // Saddle Brown
                "#B08400" => 3,  // Dark Goldenrod
                "#877800" => 4,  // Olive
                "#5E6B00" => 5,  // Olive Leaf
                "#0C5100" => 6,  // Black Forest
                _ => 999         // Color desconocido va al final
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}