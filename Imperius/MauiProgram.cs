using System;
using System.IO;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Imperius.Vista;
using Imperius.VistaModelo;
using Imperius.Servicios;

namespace Imperius
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>();

            var dbPath = Path.Combine(FileSystem.AppDataDirectory, "Imperius.db");

#if ANDROID
            builder.Services.AddSingleton<INotificationService, Imperius.Platforms.Android.AndroidNotificationService>();
#endif

            builder.Services.AddSingleton<TareaServicio>(s =>
                new TareaServicio(dbPath, s.GetService<INotificationService>()));

            builder.Services.AddTransient<MenuVistaModelo>();
            builder.Services.AddTransient<CalendarioVistaModelo>();

            var agregarVmType = Type.GetType("Imperius.VistaModelo.AgregarTareaVistaModelo, Imperius");
            if (agregarVmType is not null)
            {
                builder.Services.AddTransient(agregarVmType);
            }

            builder.Services.AddTransient<AgregarTareaPage>();
            builder.Services.AddTransient<DetalleTareaPage>();

            return builder.Build();
        }
    }
}