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
    private GitHubService _gitHubService;
    private const int REQUEST_CODE_UPDATE = 100;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        _gitHubService = new GitHubService("MrJakezz", "MAUITest");

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
            else
            {
                CheckForGitHubUpdate();
            }
        }));
    }

    private async void CheckForGitHubUpdate()
    {
        try
        {
            var latestVersion = await _gitHubService.GetLatestVersionFromGitHub();
            var currentVersion = AppInfo.VersionString;

            if (currentVersion != latestVersion)
            {
                ShowUpdateNotification();
                var assetUrl = await GetLatestAssetUrl();

                var latestRelease = await _gitHubService.DownloadLatestAssetFromGitHub("com.companyname.mauisilentupdatetestapplication-Signed.apk", assetUrl);
                InstallApk(latestRelease.Content);
            }
        }
        catch (Exception ex)
        {
            RunOnUiThread(() =>
            {
                if (this != null)
                {
                    Toast.MakeText(this, "GitHub update check failed: " + ex.Message, ToastLength.Short).Show();
                }
            });
        }
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

    private void InstallApk(byte[] apkBytes)
    {
        string apkFileName = "com.companyname.mauisilentupdatetestapplication-Signed.apk";
        string apkFilePath = System.IO.Path.Combine(CacheDir.AbsolutePath, apkFileName);

        try
        {
            System.IO.File.WriteAllBytes(apkFilePath, apkBytes);

            RunOnUiThread(() =>
            {
                if (this != null)
                {
                    Toast.MakeText(this, "Update downloaded, installing...", ToastLength.Short).Show();
                }
            });

            var apkUri = FileProvider.GetUriForFile(this, ApplicationContext.PackageName + ".provider", new Java.IO.File(apkFilePath));

            var intent = new Intent(Intent.ActionInstallPackage);
            intent.SetDataAndType(apkUri, "application/vnd.android.package-archive");
            intent.AddFlags(ActivityFlags.GrantReadUriPermission);
            intent.AddFlags(ActivityFlags.NewTask);

            StartActivity(intent);

            RunOnUiThread(() =>
            {
                if (this != null)
                {
                    Toast.MakeText(this, "Update installed successfully. Restarting app...", ToastLength.Short).Show();
                    var handler = new Handler();
                    handler.PostDelayed(() =>
                    {
                        var packageManager = PackageManager;
                        var intent = packageManager.GetLaunchIntentForPackage(PackageName);
                        intent.AddFlags(ActivityFlags.ClearTop | ActivityFlags.NewTask);
                        StartActivity(intent);
                        Java.Lang.JavaSystem.Exit(0);
                    }, 5000); 
                }
            });
        }
        catch (Exception ex)
        {
            RunOnUiThread(() =>
            {
                if (this != null)
                {
                    Toast.MakeText(this, "Update install failed: " + ex.Message, ToastLength.Short).Show();
                }
            });
        }
    }

    private void ShowUpdateNotification()
    {
        RunOnUiThread(() =>
        {
            if (this != null)
            {
                Toast.MakeText(this, "New update available from GitHub!", ToastLength.Short).Show();
            }
        });
    }

    private async Task<string> GetLatestAssetUrl()
    {
        var latestVersion = await _gitHubService.GetLatestVersionFromGitHub();
        string response = $"https://github.com/MrJakezz/MAUITest/releases/download/{latestVersion}/com.companyname.mauisilentupdatetestapplication-Signed.apk";

        return response;
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        if (requestCode == REQUEST_CODE_UPDATE && resultCode != Result.Ok)
        {
            Toast.MakeText(this, "Update failed!", ToastLength.Short).Show();
        }

        base.OnActivityResult(requestCode, resultCode, data);
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
