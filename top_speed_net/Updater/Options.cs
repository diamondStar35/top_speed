namespace TopSpeed.Updater
{
    internal sealed class Options
    {
        public int ProcessId { get; set; }
        public string ZipPath { get; set; } = string.Empty;
        public string TargetDir { get; set; } = string.Empty;
        public string GameName { get; set; } = string.Empty;
        public string SkipName { get; set; } = string.Empty;
        public bool EnableLog { get; set; }
        public bool Finalize { get; set; }
        public int WorkerProcessId { get; set; }
        public string WorkerDirectory { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }
}
