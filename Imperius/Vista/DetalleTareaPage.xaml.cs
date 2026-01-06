using Imperius.Modelo;
using Microsoft.Maui.Controls;
using System.Collections.Generic;
using System;
using Imperius.Servicios;
using Microsoft.Extensions.DependencyInjection;

namespace Imperius.Vista
{
    [QueryProperty(nameof(Tarea), "Tarea")]
    public partial class DetalleTareaPage : ContentPage, IQueryAttributable
    {
        private TareaServicio _tareaServicio;

        public Tarea Tarea
        {
            get => BindingContext as Tarea;
            set => BindingContext = value;
        }

        public DetalleTareaPage()
        {
            InitializeComponent();

            if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider services)
            {
                _tareaServicio = services.GetService<TareaServicio>();
            }
        }

        public void ApplyQueryAttributes(IDictionary<string, object> query)
        {
            if (query.TryGetValue("Tarea", out object tareaObject) && tareaObject is Tarea tarea)
            {
                Tarea = tarea;
            }
        }

        private async void OnMarcarComoCompletadaClicked(object sender, EventArgs e)
        {
            if (BindingContext is Tarea selectedTarea && !selectedTarea.EstaCompletada)
            {
                if (_tareaServicio == null)
                {
                    await DisplayAlert("Error", "No se pudo acceder al servicio de tareas.", "OK");
                    return;
                }

                try
                {
                    selectedTarea.EstaCompletada = true;
                    bool resultado = await _tareaServicio.ActualizarTareaAsync(selectedTarea);

                    if (resultado)
                    {
                        await DisplayAlert("Éxito", "Tarea marcada como completada.", "OK");

                        await Shell.Current.GoToAsync("..", new Dictionary<string, object>
                        {
                            { "refresh", "true" }
                        });
                    }
                    else
                    {
                        selectedTarea.EstaCompletada = false;
                        await DisplayAlert("Error", "No se pudo actualizar la tarea.", "OK");
                    }
                }
                catch (Exception ex)
                {
                    selectedTarea.EstaCompletada = false;
                    await DisplayAlert("Error", $"Error al guardar: {ex.Message}", "OK");
                }
            }
        }
    }
}