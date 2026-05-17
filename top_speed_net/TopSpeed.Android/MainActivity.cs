using System;
using System.IO;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Java.Interop;

namespace TopSpeed.Android;

[Register("org/libsdl/app/SDLActivity", DoNotGenerateAcw = true)]
public class SdlActivityBase : Activity
{
    private static readonly JniPeerMembers Members = new XAPeerMembers("org/libsdl/app/SDLActivity", typeof(SdlActivityBase));

    public override JniPeerMembers JniPeerMembers => Members;

    protected override IntPtr ThresholdClass => Members.JniPeerType.PeerReference.Handle;

    protected override Type ThresholdType => Members.ManagedPeerType;

    public unsafe SdlActivityBase()
        : base(IntPtr.Zero, JniHandleOwnership.DoNotTransfer)
    {
        const string id = "()V";

        if (Handle != IntPtr.Zero)
            return;

        var result = Members.InstanceMethods.StartCreateInstance(id, GetType(), null);
        SetHandle(result.Handle, JniHandleOwnership.TransferLocalRef);
        Members.InstanceMethods.FinishCreateInstance(id, this, null);
    }

    protected SdlActivityBase(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

}

[Activity(
    Label = "@string/app_name",
    Theme = "@style/TopSpeed.Fullscreen",
    MainLauncher = true,
    Exported = true,
    ScreenOrientation = ScreenOrientation.Landscape,
    ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize | ConfigChanges.KeyboardHidden)]
public class MainActivity : SdlActivityBase
{
    private string? _assetRoot;
    private AndroidMotionSteeringSource? _motionSteering;
    private AndroidSpeechThreadDispatcher? _speechDispatcher;
    private AndroidUpdatePackageInstaller? _updateInstaller;
    private AndroidDocumentOpener? _documentOpener;
    private AndroidMicrophonePermissionService? _microphonePermission;

    public MainActivity()
    {
    }

    protected MainActivity(IntPtr javaReference, JniHandleOwnership transfer)
        : base(javaReference, transfer)
    {
    }

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        RequestWindowFeature(WindowFeatures.NoTitle);
        Window?.AddFlags(WindowManagerFlags.Fullscreen);
        Window?.ClearFlags(WindowManagerFlags.ForceNotFullscreen);
        EnforceLandscapeOrientation();
        _assetRoot = EnsureRuntimeAssets();
        _motionSteering = new AndroidMotionSteeringSource(this);
        _speechDispatcher = new AndroidSpeechThreadDispatcher();
        _updateInstaller = new AndroidUpdatePackageInstaller(this);
        _documentOpener = new AndroidDocumentOpener(this);
        _microphonePermission = new AndroidMicrophonePermissionService(this);
        global::TopSpeed.Runtime.MotionSteeringRuntime.SetSource(_motionSteering);
        global::TopSpeed.Runtime.SpeechThreadRuntime.SetDispatcher(_speechDispatcher);
        global::TopSpeed.Runtime.UpdatePackageRuntime.SetInstaller(_updateInstaller);
        global::TopSpeed.Runtime.DocumentOpenRuntime.SetOpener(_documentOpener);
        global::TopSpeed.Runtime.MicrophonePermissionRuntime.SetService(_microphonePermission);
        base.OnCreate(savedInstanceState);
        EnforceLandscapeOrientation();
        ApplyImmersiveMode();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);
        if (hasFocus)
            ApplyImmersiveMode();
    }

    protected override void OnResume()
    {
        base.OnResume();
        EnforceLandscapeOrientation();
    }

    protected override void OnPause()
    {
        base.OnPause();
    }

    public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {
        _microphonePermission?.HandlePermissionResult(requestCode);
        base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
    }

    protected override void OnDestroy()
    {
        global::TopSpeed.Runtime.MotionSteeringRuntime.SetSource(null);
        global::TopSpeed.Runtime.SpeechThreadRuntime.SetDispatcher(null);
        global::TopSpeed.Runtime.UpdatePackageRuntime.SetInstaller(null);
        global::TopSpeed.Runtime.DocumentOpenRuntime.SetOpener(null);
        global::TopSpeed.Runtime.MicrophonePermissionRuntime.SetService(null);
        _speechDispatcher?.Dispose();
        _speechDispatcher = null;
        _motionSteering = null;
        _updateInstaller = null;
        _documentOpener = null;
        _microphonePermission = null;
        global::TopSpeed.AndroidLauncher.RequestClose();
        base.OnDestroy();
    }

    [Export("getLibraries")]
    public string[] GetLibraries()
    {
        return
        [
            "SDL3"
        ];
    }

    [Export("loadLibraries")]
    public void LoadLibraries()
    {
        Java.Lang.JavaSystem.LoadLibrary("SDL3");
    }

    [Export("main")]
    public void RunSdlMain()
    {
        try
        {
            global::TopSpeed.AndroidLauncher.SetAssetRoot(_assetRoot ?? EnsureRuntimeAssets());
            global::TopSpeed.AndroidLauncher.Run();
        }
        catch (Exception ex)
        {
            ShowLaunchError(ex);
        }
    }

    private void ShowLaunchError(Exception ex)
    {
        RunOnUiThread(() =>
        {
            if (IsFinishing || IsDestroyed)
                return;

            var dialogBuilder = new AlertDialog.Builder(this);
            dialogBuilder.SetTitle("Top Speed startup failed");
            dialogBuilder.SetMessage(ex.ToString());
            dialogBuilder.SetCancelable(false);
            dialogBuilder.SetPositiveButton("Close", (_, _) => FinishAffinity());
            var dialog = dialogBuilder.Create();
            if (dialog == null)
                return;

            dialog.Show();
        });
    }

    private string EnsureRuntimeAssets()
    {
        var filesRoot = FilesDir?.AbsolutePath;
        var targetRoot = Path.Combine(
            string.IsNullOrWhiteSpace(filesRoot) ? AppContext.BaseDirectory : filesRoot!,
            "topspeed_assets");

        var versionStamp = ResolveAssetVersionStamp();
        var versionFile = Path.Combine(targetRoot, ".asset-version");
        if (!IsAssetVersionCurrent(versionFile, versionStamp) && Directory.Exists(targetRoot))
            Directory.Delete(targetRoot, recursive: true);

        CopyAssetTree("Sounds", Path.Combine(targetRoot, "Sounds"));
        CopyAssetTree("Tracks", Path.Combine(targetRoot, "Tracks"));
        CopyAssetTree("Vehicles", Path.Combine(targetRoot, "Vehicles"));
        CopyAssetTree("languages", Path.Combine(targetRoot, "languages"));
        CopyAssetTree("docs", Path.Combine(targetRoot, "docs"));
        WriteAssetVersion(versionFile, versionStamp);
        return targetRoot;
    }

    private void CopyAssetTree(string assetPath, string targetPath)
    {
        var assetManager = Assets;
        if (assetManager == null)
            return;

        string[] entries;
        try
        {
            entries = assetManager.List(assetPath) ?? Array.Empty<string>();
        }
        catch
        {
            return;
        }

        if (entries.Length == 0)
        {
            CopyAssetFile(assetPath, targetPath);
            return;
        }

        Directory.CreateDirectory(targetPath);
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (string.IsNullOrWhiteSpace(entry))
                continue;

            var childAssetPath = string.IsNullOrEmpty(assetPath) ? entry : $"{assetPath}/{entry}";
            var childTargetPath = Path.Combine(targetPath, entry);
            CopyAssetTree(childAssetPath, childTargetPath);
        }
    }

    private void CopyAssetFile(string assetPath, string targetPath)
    {
        if (File.Exists(targetPath))
            return;

        var directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory!);

        try
        {
            var assetManager = Assets;
            if (assetManager == null)
                return;

            using var source = assetManager.Open(assetPath);
            using var destination = File.Create(targetPath);
            source.CopyTo(destination);
        }
        catch
        {
            // Ignore extraction failures for optional assets.
        }
    }

    private string ResolveAssetVersionStamp()
    {
        try
        {
            var packageManager = PackageManager;
            if (packageManager == null || string.IsNullOrWhiteSpace(PackageName))
                return "unknown";

            var packageInfo = packageManager.GetPackageInfo(PackageName!, 0);
            if (packageInfo == null)
                return "unknown";

            return packageInfo.LongVersionCode.ToString();
        }
        catch
        {
            return "unknown";
        }
    }

    private static bool IsAssetVersionCurrent(string versionFile, string versionStamp)
    {
        try
        {
            return File.Exists(versionFile)
                && string.Equals(File.ReadAllText(versionFile).Trim(), versionStamp, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }

    private static void WriteAssetVersion(string versionFile, string versionStamp)
    {
        try
        {
            var directory = Path.GetDirectoryName(versionFile);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory!);

            File.WriteAllText(versionFile, versionStamp);
        }
        catch
        {
            // Asset extraction still works without a stamp; the next launch will recopy if needed.
        }
    }

    private void ApplyImmersiveMode()
    {
        try
        {
            var window = Window;
            if (window == null)
                return;

            #pragma warning disable CA1422
            window.SetDecorFitsSystemWindows(false);
            #pragma warning restore CA1422
            var controller = window.InsetsController;
            if (controller == null)
                return;

            controller.Hide(WindowInsets.Type.StatusBars() | WindowInsets.Type.NavigationBars());
            controller.SystemBarsBehavior = 2;
        }
        catch
        {
            // Window insets controller may not be available during early activity creation.
        }
    }

    private void EnforceLandscapeOrientation()
    {
        try
        {
            RequestedOrientation = ScreenOrientation.Landscape;
        }
        catch
        {
            // Ignore orientation assignment failures during activity startup.
        }
    }

}
