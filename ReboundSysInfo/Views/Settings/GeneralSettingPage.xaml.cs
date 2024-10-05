namespace ReboundSysInfo.Views;

public sealed partial class GeneralSettingPage : Page
{
    public string BreadCrumbBarItemText { get; set; }
    public GeneralSettingPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }

    private async void NavigateToLogPath_Click(object sender, RoutedEventArgs e)
    {
        string folderPath = (sender as HyperlinkButton).Content.ToString();
        if (Directory.Exists(folderPath))
        {
            Windows.Storage.StorageFolder folder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(folderPath);
            await Windows.System.Launcher.LaunchFolderAsync(folder);
        }
    }
}


