using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Imperius.VistaModelo;
using Imperius.Servicios;

namespace Imperius.Vista
{
    public partial class Calendario : ContentPage
    {
        public Calendario(CalendarioVistaModelo viewModel)
        {
            InitializeComponent();
            BindingContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        }

        public Calendario()
        {
            InitializeComponent();
            _ = InitializeViewModelAsync();
        }

        private async Task InitializeViewModelAsync()
        {
            try
            {
                if (Application.Current?.Handler?.MauiContext?.Services is IServiceProvider services)
                {
                    var vm = services.GetService(typeof(CalendarioVistaModelo)) as CalendarioVistaModelo;
                    if (vm != null)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() => BindingContext = vm);
                        return;
                    }

                    var tareaServicio = services.GetService(typeof(TareaServicio)) as TareaServicio;
                    if (tareaServicio != null)
                    {
                        try { await tareaServicio.InitializeAsync(); } catch { }

                        var vmFromService = new CalendarioVistaModelo(tareaServicio);
                        await MainThread.InvokeOnMainThreadAsync(() => BindingContext = vmFromService);
                        return;
                    }
                }
            }
            catch
            { }

            try
            {
                var localDbPath = Path.Combine(FileSystem.AppDataDirectory, "Imperius.db");

                if (!File.Exists(localDbPath))
                {
                    try
                    {
                        using var stream = await FileSystem.OpenAppPackageFileAsync("Imperius.db");
                        using var outStream = File.Create(localDbPath);
                        await stream.CopyToAsync(outStream);
                    }
                    catch
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(localDbPath) ?? FileSystem.AppDataDirectory);
                        using var fs = File.Create(localDbPath);
                    }
                }

                var localTareaServicio = new TareaServicio(localDbPath, null);

                try { await localTareaServicio.InitializeAsync(); } catch { }

                var vm = new CalendarioVistaModelo(localTareaServicio);
                await MainThread.InvokeOnMainThreadAsync(() => BindingContext = vm);
                return;
            }
            catch
            {
                await MainThread.InvokeOnMainThreadAsync(() => BindingContext = new object());
            }
        }
    }
}