namespace TopSpeed.Menu
{
    internal readonly struct MenuUpdateResult
    {
        public static MenuUpdateResult None => new MenuUpdateResult(false, null);
        public static MenuUpdateResult Back => new MenuUpdateResult(true, null);

        public bool BackRequested { get; }
        public MenuItem? ActivatedItem { get; }

        public MenuUpdateResult(bool backRequested, MenuItem? activatedItem)
        {
            BackRequested = backRequested;
            ActivatedItem = activatedItem;
        }

        public static MenuUpdateResult Activated(MenuItem item) => new MenuUpdateResult(false, item);
    }
}
