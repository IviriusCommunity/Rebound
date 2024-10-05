namespace ReboundSysInfo.Views;

public sealed partial class AboutUsSettingPage : Page
{
    public string AppInfo = $"{App.Current.AppName} v{App.Current.AppVersion}";
    public string BreadCrumbBarItemText { get; set; }
    public AboutUsSettingPage()
    {
        this.InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        BreadCrumbBarItemText = e.Parameter as string;
    }
}


