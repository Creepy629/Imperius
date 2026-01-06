using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using System;

namespace Imperius.Vista;

public partial class Startup : ContentPage
{
	public Startup()
	{
		InitializeComponent();
	}

    protected async override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        // Inicializar servicios antes de navegar: ejemplo usando el IServiceProvider de la app
        try
        {
            if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider serviceProvider)
            {
                // Si registraste TareaServicio en DI como singleton que recibe dbPath:
                var tareaServicio = (Imperius.Servicios.TareaServicio?)serviceProvider.GetService(typeof(Imperius.Servicios.TareaServicio));
                if (tareaServicio != null)
                {
                    await tareaServicio.InitializeAsync();
                }
                else
                {
                    // Si no usas DI, crea y guarda la instancia aquí según tu arquitectura:
                    // var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Imperius.db");
                    // var ts = new TareaServicio(dbPath);
                    // await ts.InitializeAsync();
                }
            }

            await Task.Delay(1000);
            await Shell.Current.GoToAsync($"//IniciarSesion");
        }
        catch (Exception ex)
        {
            // Mostrar error en UI para depuración en caso de fallo de inicialización
            await DisplayAlert("Error init", ex.Message, "OK");
        }
    }
}