using MauiProgramKKuU.Services;

namespace MauiProgramKKuU
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            var settings = AppSettingsService.Get();
            UserAppTheme = settings.UseDarkTheme ? AppTheme.Dark : AppTheme.Light;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}