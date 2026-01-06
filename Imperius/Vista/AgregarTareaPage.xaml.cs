using Imperius.Modelo;
using Imperius.Servicios;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Imperius.Vista
{
    public partial class AgregarTareaPage : ContentPage
    {
        private readonly TareaServicio _tareaServicio;
        private string _selectedColorHex;
        private List<string> _materiasExistentes;

        public AgregarTareaPage(TareaServicio tareaServicio)
        {
            InitializeComponent();
            _tareaServicio = tareaServicio;

            DpFecha.MinimumDate = DateTime.Today;
            DpFecha.Date = DateTime.Today;
            TpHora.Time = DateTime.Now.TimeOfDay.Add(TimeSpan.FromHours(1));

            // Color por defecto: Crimson Violet (primer color de la jerarquía)
            _selectedColorHex = "#6C1D45";

            _materiasExistentes = new List<string>();

            _ = CargarMateriasExistentesAsync();
        }

        private async Task CargarMateriasExistentesAsync()
        {
            try
            {
                string? boletaHashActivo = await Imperius.Servicios.UsuarioServicio.GetCurrentUserBoletaHashAsync();
                int? idUsuarioActivo = null;

                if (!string.IsNullOrWhiteSpace(boletaHashActivo))
                {
                    idUsuarioActivo = await Imperius.Servicios.UsuarioServicio.GetUserIdByBoletaHashAsync(boletaHashActivo);
                }

                if (idUsuarioActivo.HasValue && idUsuarioActivo.Value > 0)
                {
                    var tareas = await _tareaServicio.ObtenerTodasLasTareasAsync(idUsuarioActivo.Value);
                    _materiasExistentes = tareas
                        .Select(t => t.Materia)
                        .Where(m => !string.IsNullOrWhiteSpace(m))
                        .Distinct()
                        .OrderBy(m => m)
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al cargar materias: {ex.Message}");
            }
        }

        private void OnMateriaTextChanged(object sender, TextChangedEventArgs e)
        {
            // Ocultar la lista cuando el usuario escribe
            if (!string.IsNullOrEmpty(e.NewTextValue))
            {
                ListaMaterias.IsVisible = false;
            }
        }

        private void OnSelectorMateriaClicked(object sender, EventArgs e)
        {
            if (_materiasExistentes.Any())
            {
                ListaMaterias.ItemsSource = _materiasExistentes;
                ListaMaterias.IsVisible = !ListaMaterias.IsVisible;
            }
            else
            {
                DisplayAlert("Información", "No hay materias registradas aún.", "OK");
            }
        }

        private void OnMateriaSeleccionada(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is string materiaSeleccionada)
            {
                EntMateria.Text = materiaSeleccionada;
                ListaMaterias.IsVisible = false;
                ((CollectionView)sender).SelectedItem = null;
            }
        }

        private void OnColorRadioButtonCheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            if (sender is RadioButton radioButton && e.Value)
            {
                _selectedColorHex = radioButton.Value?.ToString() ?? "#6C1D45";
            }
        }

        private async void OnGuardarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EntTitulo.Text) || string.IsNullOrWhiteSpace(EntMateria.Text))
            {
                await DisplayAlert("Error", "El título y la materia no pueden estar vacíos.", "OK");
                return;
            }

            DateTime fechaSeleccionada = DpFecha.Date;
            TimeSpan horaSeleccionada = TpHora.Time;
            DateTime fechaVencimiento = new DateTime(
                fechaSeleccionada.Year,
                fechaSeleccionada.Month,
                fechaSeleccionada.Day,
                horaSeleccionada.Hours,
                horaSeleccionada.Minutes,
                horaSeleccionada.Seconds
            );

            if (fechaVencimiento < DateTime.Now)
            {
                await DisplayAlert("Error", "La fecha y la hora de vencimiento no pueden ser anteriores a la fecha y hora actuales.", "OK");
                return;
            }

            string? boletaHashActivo = await Imperius.Servicios.UsuarioServicio.GetCurrentUserBoletaHashAsync();
            int? idUsuarioActivo = null;

            if (!string.IsNullOrWhiteSpace(boletaHashActivo))
            {
                idUsuarioActivo = await Imperius.Servicios.UsuarioServicio.GetUserIdByBoletaHashAsync(boletaHashActivo);
            }

            Console.WriteLine($"DEBUG: boletaHashActivo obtenido de SecureStorage: {boletaHashActivo ?? "null"}");
            Console.WriteLine($"DEBUG: idUsuarioActivo obtenido de GetUserIdByBoletaHashAsync: {idUsuarioActivo?.ToString() ?? "null"}");

            if (idUsuarioActivo == null || idUsuarioActivo.Value == 0)
            {
                await DisplayAlert("Error", "No se pudo obtener el ID del usuario activo. Por favor, inicie sesión de nuevo.", "OK");
                return;
            }

            var nuevaTarea = new Tarea
            {
                Titulo = EntTitulo.Text,
                Materia = EntMateria.Text.Trim(),
                Descripcion = EntDescripcion.Text,
                FechaVencimiento = fechaVencimiento,
                ColorHex = _selectedColorHex,
                IdUsuario_FK = idUsuarioActivo.Value
            };

            await _tareaServicio.AgregarTareaAsync(nuevaTarea);
            await Navigation.PopModalAsync();
        }

        private async void OnCancelarClicked(object sender, EventArgs e)
        {
            await Navigation.PopModalAsync();
        }
    }
}