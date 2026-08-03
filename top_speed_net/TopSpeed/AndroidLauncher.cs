namespace TopSpeed
{
    public static class AndroidLauncher
    {
        public static void SetAssetRoot(string? path)
        {
            MobileLauncher.SetAssetRoot(path);
        }

        public static void SetUserContentRoot(string? path)
        {
            MobileLauncher.SetUserContentRoot(path);
        }

        public static void Run()
        {
            MobileLauncher.Run();
        }

        public static void RequestClose()
        {
            MobileLauncher.RequestClose();
        }
    }
}
