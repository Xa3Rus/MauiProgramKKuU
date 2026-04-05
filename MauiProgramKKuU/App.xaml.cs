using MauiProgramKKuU.Services;

namespace MauiProgramKKuU
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            var settings = AppSettingsService.Get();
            // Force premium purple palette across the whole app.
            settings.Theme = ThemeService.Purple;
            AppSettingsService.Save(settings);
            ThemeService.ApplyTheme(settings.Theme);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}