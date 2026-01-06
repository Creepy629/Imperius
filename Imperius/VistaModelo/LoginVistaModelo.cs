using Imperius.Vista;
using Imperius.Servicios;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;

namespace Imperius.VistaModelo
{
    class LoginVistaModelo : INotifyPropertyChanged
    {
        string boleta = string.Empty;
        string contraseña = string.Empty;

        public string Boleta
        {
            get => boleta;
            set
            {
                boleta = value;
                AlCambiarPropiedad(nameof(Boleta));
            }
        }
        public string Contraseña
        {
            get => contraseña;
            set
            {
                contraseña = value;
                AlCambiarPropiedad(nameof(Contraseña));
            }
        }

        public ICommand IniciaSesion => new Command(async () => await EjecutarLoginAsync());

        private async Task EjecutarLoginAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(Boleta) || string.IsNullOrWhiteSpace(Contraseña))
                {
                    await Shell.Current.DisplayAlert("Atención", "Campos vacíos.", "OK");
                    return;
                }

                var usuario = await UsuarioServicio.ObtenerPorBoletaYContrasenaAsync(Boleta.Trim(), Contraseña);
                if (usuario != null)
                {
                    await UsuarioServicio.SetCurrentUserAsync(usuario.IdUsuario, usuario.CorreoHash, usuario.BoletaHash); 
                    
                    try
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            try
                            {
                                await Shell.Current.GoToAsync($"//Menu?refresh=true");
                            }
                            catch (Exception navEx)
                            {
                                throw new InvalidOperationException("Error durante la navegación a Menu.", navEx);
                            }
                        });
                    }
                    catch (Exception exNav)
                    {
                        await Shell.Current.DisplayAlert("Error de navegación", exNav.Message, "OK");
                    }
                }
                else
                {
                    await Shell.Current.DisplayAlert("Atención", "Boleta y/o contraseña inválidos.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error al iniciar sesión: {ex.Message}", "OK");
            }
        }

        public ICommand FinSesion => new Command(async () =>
        {
            UsuarioServicio.ClearCurrentUser();
            await Shell.Current.GoToAsync("//Principal");
        });

        public event PropertyChangedEventHandler? PropertyChanged;
        public void AlCambiarPropiedad(string propiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}
