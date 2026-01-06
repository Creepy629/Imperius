using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Imperius.Modelo;
using Imperius.VistaModelo;
using Imperius.Servicios;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imperius.Vista;

public partial class Menu : ContentPage, IQueryAttributable
{
    public Menu(MenuVistaModelo viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public Menu()
    {
        InitializeComponent();

        try
        {
            if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider services)
            {
                var tareaServicio = services.GetService<TareaServicio>();
                if (tareaServicio != null)
                {
                    BindingContext = new MenuVistaModelo(tareaServicio);
                    return;
                }
                else
                {
                    Console.WriteLine("Advertencia: TareaServicio no pudo ser resuelto por DI en Menu(). Se intentará el fallback.");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al inicializar Menu sin DI: {ex.Message}");
        }

        if (BindingContext == null)
        {
            var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "Imperius.db");

            var localTareaServicio = new TareaServicio(localDbPath, null);

            _ = InitializeServiceAndSetVmAsync(localTareaServicio);
        }
    }

    private async Task InitializeServiceAndSetVmAsync(TareaServicio tareaServicio)
    {
        try
        {
            await tareaServicio.InitializeAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al inicializar TareaServicio: {ex.Message}");
        }

        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            BindingContext = new MenuVistaModelo(tareaServicio);
        });
    }

    private async void OnAddDebugTareaClicked(object sender, EventArgs e)
    {
        if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider serviceProvider)
        {
            var page = serviceProvider.GetRequiredService<AgregarTareaPage>();
            await Navigation.PushModalAsync(page);
        }
        else
        {
            Console.WriteLine("Error: No se pudo acceder al IServiceProvider de la aplicación.");
            await DisplayAlert("Error", "No se pudieron obtener los servicios de la aplicación para añadir una tarea.", "OK");
        }
    }

    private async void OnTareaSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is Tarea selectedTarea)
        {
            await Shell.Current.GoToAsync($"{nameof(DetalleTareaPage)}", new Dictionary<string, object>
            {
                { "Tarea", selectedTarea }
            });

            ((CollectionView)sender).SelectedItem = null;
        }
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("refresh", out object refreshValue) &&
            refreshValue is string refreshString &&
            bool.TryParse(refreshString, out bool shouldRefresh) &&
            shouldRefresh)
        {
            if (BindingContext is MenuVistaModelo viewModel)
            {
                _ = viewModel.RefreshDataAsync();
            }
        }
    }
}