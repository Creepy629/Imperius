using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Imperius.Modelo;
using Imperius.Servicios;
using Microsoft.Maui.Controls;

namespace Imperius.VistaModelo
{
    public class SignupVistaModelo : INotifyPropertyChanged
    {
        string nombre = string.Empty;
        string apellido = string.Empty;
        string correo = string.Empty;
        string boleta = string.Empty;
        string contrasena = string.Empty;
        bool isBusy;

        public string Nombre
        {
            get => nombre;
            set { nombre = value; AlCambiarPropiedad(nameof(Nombre)); }
        }
        public string Apellido
        {
            get => apellido;
            set { apellido = value; AlCambiarPropiedad(nameof(Apellido)); }
        }
        public string Correo
        {
            get => correo;
            set { correo = value; AlCambiarPropiedad(nameof(Correo)); }
        }
        public string Boleta
        {
            get => boleta;
            set { boleta = value; AlCambiarPropiedad(nameof(Boleta)); }
        }
        public string Contrasena
        {
            get => contrasena;
            set { contrasena = value; AlCambiarPropiedad(nameof(Contrasena)); }
        }

        public ICommand RegistrarCommand => new Command(async () => await RegistrarAsync(), () => !isBusy);
        public ICommand CancelarCommand => new Command(async () => await Shell.Current.GoToAsync(".."));

        private async Task RegistrarAsync()
        {
            if (isBusy) return;
            try
            {
                isBusy = true;
                ((Command)RegistrarCommand).ChangeCanExecute();

                if (string.IsNullOrWhiteSpace(Nombre) ||
                    string.IsNullOrWhiteSpace(Apellido) ||
                    string.IsNullOrWhiteSpace(Correo) ||
                    string.IsNullOrWhiteSpace(Contrasena))
                {
                    await Shell.Current.DisplayAlert("Atención", "Rellena todos los campos obligatorios.", "OK");
                    return;
                }

                if (!Correo.Contains("@") || Correo.Length < 5)
                {
                    await Shell.Current.DisplayAlert("Atención", "Introduce un correo válido.", "OK");
                    return;
                }

                await UsuarioServicio.InitAsync();

                var usuario = new Usuario
                {
                    Nombre = Nombre.Trim(),
                    Apellido = Apellido.Trim(),
                    Correo = Correo.Trim(),
                    Boleta = Boleta?.Trim() ?? string.Empty,
                    Contrasena = Contrasena
                };

                var creado = await UsuarioServicio.CrearUsuarioAsync(usuario);
                if (creado)
                {
                    await Shell.Current.DisplayAlert("Éxito", "Usuario registrado correctamente.", "OK");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await Shell.Current.DisplayAlert("Error", "No se ha podido crear el usuario.", "OK");
                }
            }
            catch (Exception ex)
            {
                await Shell.Current.DisplayAlert("Error", $"Error al registrar: {ex.Message}", "OK");
            }
            finally
            {
                isBusy = false;
                ((Command)RegistrarCommand).ChangeCanExecute();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void AlCambiarPropiedad(string propiedad)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propiedad));
        }
    }
}