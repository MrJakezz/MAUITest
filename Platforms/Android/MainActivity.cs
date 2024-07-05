using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Gms.Tasks;
using Android.OS;
using Android.Runtime;
using Android.Widget;
using MAUISilentUpdateTestApplication.Platforms.Android.Services;
using Xamarin.Google.Android.Play.Core.AppUpdate;
using Xamarin.Google.Android.Play.Core.AppUpdate.Install.Model;

namespace MAUISilentUpdateTestApplication;
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    public required IAppUpdateManager _appUpdateManager;
    private const int REQUEST_CODE_UPDATE = 100;
    private GitLabService _gitLabService;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        _gitLabService = new GitLabService("glpat-9V-M4dbMuX6EteoArhp2", "117");

        _appUpdateManager = AppUpdateManagerFactory.Create(this);
        CheckForUpdate();
    }

    private void CheckForUpdate()
    {
        var appUpdateInfoTask = _appUpdateManager.GetAppUpdateInfo();

        appUpdateInfoTask.AddOnSuccessListener(new OnSuccessListener(appUpdateInfo =>
        {
            if (appUpdateInfo.UpdateAvailability() == UpdateAvailability.UpdateAvailable && appUpdateInfo.UpdatePriority() == AppUpdateType.Immediate)
            {
                StartUpdate(appUpdateInfo);
            }
        }));
    }

    private void StartUpdate(AppUpdateInfo appUpdateInfo)
    {
        _appUpdateManager.StartUpdateFlow(appUpdateInfo, this, AppUpdateOptions.NewBuilder(AppUpdateType.Immediate).Build())
            .AddOnSuccessListener(new OnSuccessListener(task => { }))
            .AddOnFailureListener(new OnFailureListener(exception =>
            {
                RunOnUiThread(() =>
                {
                    if (this != null)
                    {
                        Toast.MakeText(this, "Update failed: " + exception.Message, ToastLength.Short).Show();
                    }
                });
            }));
    }

    private async void StartUpdateFromGitLab()
    {
        string apkUrl = "";
        string apkFileName = "update.apk";
        string apkFilePath = System.IO.Path.Combine(CacheDir.AbsolutePath, apkFileName);

        using (var client = new HttpClient())
        {
            try
            {
                var response = await client.GetAsync(apkUrl);
                response.EnsureSuccessStatusCode();
                var apkBytes = await response.Content.ReadAsByteArrayAsync();
                System.IO.File.WriteAllBytes(apkFilePath, apkBytes);

                RunOnUiThread(() =>
                {
                    if (this != null)
                    {
                        Toast.MakeText(this, "Update downloaded, installing...", ToastLength.Short).Show();
                    }
                });

                InstallApk(apkFilePath);
            }
            catch (Exception ex)
            {
                RunOnUiThread(() =>
                {
                    if (this != null)
                    {
                        Toast.MakeText(this, "Update download failed: " + ex.Message, ToastLength.Short).Show();
                    }
                });
            }
        }
    }

    private void InstallApk(string apkFilePath)
    {
        var apkUri = FileProvider.GetUriForFile(this, ApplicationContext.PackageName + ".provider", new Java.IO.File(apkFilePath));

        var intent = new Intent(Intent.ActionView);
        intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
        intent.AddFlags(ActivityFlags.GrantReadUriPermission);
        intent.AddFlags(ActivityFlags.NewTask);

        StartActivity(intent);
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode == REQUEST_CODE_UPDATE && resultCode != Result.Ok)
        {
            Toast.MakeText(this, "Update failed!", ToastLength.Short).Show();
        }

        base.OnActivityResult(requestCode, resultCode, data);
    }

    private async void CheckForGitLabUpdate()
    {
        var latestVersion = await _gitLabService.GetLatestVersionFromGitLab();
        var currentVersion = AppInfo.VersionString;

        if (currentVersion != latestVersion)
        {
            ShowUpdateNotification();
            StartUpdateFromGitLab();
        }
    }

    private void ShowUpdateNotification()
    {
        RunOnUiThread(() =>
        {
            if (this != null)
            {
                Toast.MakeText(this, "New update available from GitLab!", ToastLength.Short).Show();
            }
        });
    }
}

public class OnSuccessListener : Java.Lang.Object, IOnSuccessListener
{
    private readonly Action<AppUpdateInfo> _onSuccess;

    public OnSuccessListener(Action<AppUpdateInfo> onSuccess)
    {
        _onSuccess = onSuccess;
    }

    public void OnSuccess(Java.Lang.Object result)
    {
        _onSuccess.Invoke(result.JavaCast<AppUpdateInfo>());
    }
}

public class OnFailureListener : Java.Lang.Object, IOnFailureListener
{
    private readonly Action<Exception> _onFailure;

    public OnFailureListener(Action<Exception> onFailure)
    {
        _onFailure = onFailure;
    }

    public void OnFailure(Java.Lang.Exception exception)
    {
        _onFailure.Invoke(exception);
    }
}
