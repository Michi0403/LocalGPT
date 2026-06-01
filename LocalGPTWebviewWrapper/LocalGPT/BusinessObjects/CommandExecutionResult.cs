namespace LocalGPT.BusinessObjects
{
    public class CommandExecutionResult
    {
        public string FileName { get; set; } = string.Empty;
        public string Arguments { get; set; } = string.Empty;
        public string WorkingDirectory { get; set; } = string.Empty;
        public int ExitCode { get; set; }
        public string StandardOutput { get; set; } = string.Empty;
        public string StandardError { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public bool Succeeded => ExitCode == 0;
    }
}
