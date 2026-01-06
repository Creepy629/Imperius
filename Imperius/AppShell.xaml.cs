using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Imperius.Servicios;

namespace Imperius
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Vista.DetalleTareaPage), typeof(Vista.DetalleTareaPage));

            _ = InitializeServicesAsync();
        }

        private async Task InitializeServicesAsync()
        {
            try
            {
                if (this.Handler?.MauiContext?.Services is IServiceProvider services)
                {
                    var tareaServicio = services.GetService(typeof(TareaServicio)) as TareaServicio;
                    if (tareaServicio != null)
                    {
                        await tareaServicio.InitializeAsync().ConfigureAwait(false);
                    }
                }
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await DisplayAlert("Error inicializando servicios", ex.Message, "OK");
                });
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            UsuarioServicio.ClearCurrentUser();
            await Shell.Current.GoToAsync("//Principal");
        }
    }
}
