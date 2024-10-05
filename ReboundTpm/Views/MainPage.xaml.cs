using ReboundTpm.Models;

namespace ReboundTpm.Views;

public sealed partial class MainPage : Page
{
    public TpmViewModel ViewModelTpm { get; }
    public TpmManager TpmManager { get; }  // Old method for original properties

    public MainPage()
    {
        this.InitializeComponent();

        // Initialize TpmManager for the original properties
        TpmManager = new TpmManager();
        // Initialize ViewModelTpm for new properties
        ViewModelTpm = new TpmViewModel();

        // Set DataContext to TpmManager for original properties
        this.DataContext = this;

        // Load new TPM information asynchronously
        _ = ViewModelTpm.LoadTpmInfoAsync();

        var list = TpmManager.GetTpmInfo();

        ManufacturerName.Text = list[0];
        ManufacturerVersion.Text = list[1];
        SpecificationVersion.Text = list[2];
        TpmSubVersion.Text = list[3];
        PcClientSpecVersion.Text = list[4];
        PcrValues.Text = list[5];
        Status.Text = list[6];

        StatusBar.Severity = list[6] == "Ready" ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        StatusBar.Title = $"Status: {list[6]}";
    }

    ContentDialog dial;

    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ContentDialog()
        {
            XamlRoot = this.XamlRoot,
            PrimaryButtonText = "Reset",
            SecondaryButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Title = "TPM Management",
            Content = "This action cannot be undone. Are you sure you want to proceed?",
        };
        dial = dialog;
        dialog.PrimaryButtonClick += Dialog_PrimaryButtonClick;
        dialog.SecondaryButtonClick += Dialog_SecondaryButtonClick;
        await dialog.ShowAsync(); // Show the dialog
    }

    private void Dialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        sender.Hide();
        dial = null;
        sender = null;
    }

    private async void Dialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
    {
        args.Cancel = true;
        dial.Content = "Starting TPM reset... Please wait.";
        dial.IsPrimaryButtonEnabled = false;
        dial.IsSecondaryButtonEnabled = false;

        // Call the TPM reset function
        await TpmReset.ResetTpmAsync(sender);

        var list = TpmManager.GetTpmInfo();

        ManufacturerName.Text = list[0];
        ManufacturerVersion.Text = list[1];
        SpecificationVersion.Text = list[2];
        TpmSubVersion.Text = list[3];
        PcClientSpecVersion.Text = list[4];
        PcrValues.Text = list[5];
        Status.Text = list[6];

        StatusBar.Severity = list[6] == "Ready" ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        StatusBar.Title = $"Status: {list[6]}";
    }

    private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        var list = TpmManager.GetTpmInfo();

        ManufacturerName.Text = list[0];
        ManufacturerVersion.Text = list[1];
        SpecificationVersion.Text = list[2];
        TpmSubVersion.Text = list[3];
        PcClientSpecVersion.Text = list[4];
        PcrValues.Text = list[5];
        Status.Text = list[6];

        StatusBar.Severity = list[6] == "Ready" ? InfoBarSeverity.Success : InfoBarSeverity.Error;
        StatusBar.Title = $"Status: {list[6]}";
    }
}

