using System;
using System.IO;
using System.Linq;
using System.Threading;
using Foundation;
using TopSpeed;
using TopSpeed.Runtime;
using UIKit;

namespace TopSpeed.iOS;

[Register("AppDelegate")]
public sealed class AppDelegate : UIApplicationDelegate
{
    private IosMotionSteeringSource? _motionSteering;
    private IosDocumentOpener? _documentOpener;
    private IosUpdatePackageInstaller? _updateInstaller;
    private UIWindow? _errorWindow;
    private int _started;
    private int _startupErrorShown;

    public override bool FinishedLaunching(UIApplication application, NSDictionary? launchOptions)
    {
        IOSLauncher.MarkMainThread();
        IOSLauncher.SetAssetRoot(NSBundle.MainBundle.ResourcePath);
        // The bundle is read only, so content the player keeps cannot live beside the shipped
        // assets. Documents is writable and is preserved across app updates.
        IOSLauncher.SetUserContentRoot(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

        _motionSteering = new IosMotionSteeringSource();
        _documentOpener = new IosDocumentOpener();
        _updateInstaller = new IosUpdatePackageInstaller();
        MotionSteeringRuntime.SetSource(_motionSteering);
        DocumentOpenRuntime.SetOpener(_documentOpener);
        UpdatePackageRuntime.SetInstaller(_updateInstaller);

        StartGameLoop();
        return true;
    }

    public override void WillTerminate(UIApplication application)
    {
        IOSLauncher.RequestClose();
        MotionSteeringRuntime.SetSource(null);
        DocumentOpenRuntime.SetOpener(null);
        UpdatePackageRuntime.SetInstaller(null);
        _errorWindow?.ResignKeyWindow();
        _errorWindow?.Dispose();
        _errorWindow = null;
        base.WillTerminate(application);
    }

    private void StartGameLoop()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
            return;

        BeginInvokeOnMainThread(RunGameLoop);
    }

    private void RunGameLoop()
    {
        try
        {
            IOSLauncher.Run();
        }
        catch (Exception ex)
        {
            HandleStartupError(ex);
        }
    }

    private void HandleStartupError(Exception ex)
    {
        if (Interlocked.Exchange(ref _startupErrorShown, 1) != 0)
            return;

        TryWriteStartupErrorLog(ex);
        ShowStartupErrorAlert(ex);
    }

    private static void TryWriteStartupErrorLog(Exception ex)
    {
        try
        {
            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var rootDir = string.IsNullOrWhiteSpace(documentsDir) ? AppContext.BaseDirectory : documentsDir;
            var path = Path.Combine(rootDir, "ios-startup-error.log");
            File.WriteAllText(path, $"[{DateTime.UtcNow:O}] {ex}{Environment.NewLine}");
        }
        catch
        {
            // Startup-error reporting must never throw.
        }
    }

    private void ShowStartupErrorAlert(Exception ex)
    {
        UIApplication.SharedApplication.BeginInvokeOnMainThread(() =>
        {
            if (_errorWindow != null)
                return;

            var hostWindow = CreateAlertWindow();
            if (hostWindow == null)
                return;

            hostWindow.WindowLevel = UIWindowLevel.Alert + 1;
            var rootController = new UIViewController();
            hostWindow.RootViewController = rootController;
            _errorWindow = hostWindow;
            hostWindow.MakeKeyAndVisible();

            var alert = UIAlertController.Create(
                "Top Speed startup failed",
                ex.ToString(),
                UIAlertControllerStyle.Alert);
            alert.AddAction(UIAlertAction.Create("Close", UIAlertActionStyle.Default, _ =>
            {
                try
                {
                    IOSLauncher.RequestClose();
                }
                catch
                {
                    // Ignore close-request failures while handling startup errors.
                }

                if (_errorWindow != null)
                {
                    _errorWindow.Hidden = true;
                    _errorWindow.Dispose();
                    _errorWindow = null;
                }
            }));
            rootController.PresentViewController(alert, true, null);
        });
    }

    private static UIWindow? CreateAlertWindow()
    {
        var windowScene = UIApplication.SharedApplication.ConnectedScenes
            .OfType<UIWindowScene>()
            .OrderByDescending(scene => scene.ActivationState == UISceneActivationState.ForegroundActive)
            .FirstOrDefault();
        if (windowScene == null)
            return null;

        return new UIWindow(windowScene);
    }
}
