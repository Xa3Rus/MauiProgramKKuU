using MauiProgramKKuU.Services;

namespace MauiProgramKKuU.Pages;

public partial class FaqPage : ContentPage
{
    public FaqPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Title = "FAQ";
        FaqTitleLabel.Text = LocalizationService.T("HowToUse");
        Step1Label.Text = LocalizationService.T("FaqStep1");
        Step2Label.Text = LocalizationService.T("FaqStep2");
        Step3Label.Text = LocalizationService.T("FaqStep3");
        Step4Label.Text = LocalizationService.T("FaqStep4");
        Step5Label.Text = LocalizationService.T("FaqStep5");
        AnnuityLabel.Text = LocalizationService.T("FaqAnnuity");
        DiffLabel.Text = LocalizationService.T("FaqDifferentiated");
    }
}
