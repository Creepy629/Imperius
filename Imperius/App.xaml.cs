namespace Imperius
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            Window window = new Window(new AppShell());

            window.Width = 400;
            window.Height = 700;
            window.X = 500;
            window.Y = 20;
            return window;
        }
    }
}