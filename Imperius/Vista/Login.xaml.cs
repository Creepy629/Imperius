using Imperius.VistaModelo;
using System.Threading.Tasks;
namespace Imperius.Vista;

public partial class Login : ContentPage
{
    LoginVistaModelo lvm;
	public Login()
	{
        lvm = new LoginVistaModelo();
        InitializeComponent();
        BindingContext = lvm;
    }

    private async void Signup_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//Signup");
    }
}