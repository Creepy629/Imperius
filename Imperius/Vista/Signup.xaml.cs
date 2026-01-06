using System;
using Microsoft.Maui.Controls;
using Imperius.VistaModelo;

namespace Imperius.Vista
{
    public partial class Signup : ContentPage
    {
        public Signup()
        {
            InitializeComponent();
            BindingContext = new SignupVistaModelo();
        }

        private async void Back_Clicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//Principal");
        }
    }
}