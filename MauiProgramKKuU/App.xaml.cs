using MauiProgramKKuU.Services;

namespace MauiProgramKKuU
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            var settings = AppSettingsService.Get();
            ThemeService.ApplyTheme(settings.Theme);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}