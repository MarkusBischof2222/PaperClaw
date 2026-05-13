using PaperClaw.Abstractions;

namespace PaperClaw.Input;

public class FileInputSource : IInputSource
{
    private readonly string _inboxPath;

    public FileInputSource(string inboxPath)
    {
        _inboxPath = inboxPath;
    }

    public IEnumerable<FileInfo> GetPendingFiles()
    {
        var directory = new DirectoryInfo(_inboxPath);
        if (!directory.Exists)
            return Enumerable.Empty<FileInfo>();

        return directory.EnumerateFiles("*", SearchOption.TopDirectoryOnly);
    }
}
