namespace PaperClaw.Abstractions;

public interface IInputSource
{
    IEnumerable<FileInfo> GetPendingFiles();
}
