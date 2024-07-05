namespace MAUISilentUpdateTestApplication;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        DisplayAppVersion();
    }

    private void DisplayAppVersion()
    {
        var appVersion = AppInfo.VersionString;
        VersionLabel.Text = $"Version: {appVersion}";
    }
}

