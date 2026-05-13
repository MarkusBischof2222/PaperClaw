using NUnit.Framework;
using PaperClaw.Input;

namespace PaperClaw.Tests.Input;

[TestFixture]
public class FileInputSourceTests
{
    private string _tempDir = null!;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
    }

    [TearDown]
    public void TearDown() => Directory.Delete(_tempDir, recursive: true);

    [Test]
    public void ReturnsAllFilesInDirectory()
    {
        File.WriteAllText(Path.Combine(_tempDir, "doc.pdf"), "");
        File.WriteAllText(Path.Combine(_tempDir, "readme.txt"), "");
        File.WriteAllText(Path.Combine(_tempDir, "image.png"), "");

        var source = new FileInputSource(_tempDir);
        var files = source.GetPendingFiles().ToList();

        Assert.That(files, Has.Count.EqualTo(3));
    }

    [Test]
    public void ReturnsEmptyForEmptyDirectory()
    {
        var source = new FileInputSource(_tempDir);
        var files = source.GetPendingFiles().ToList();

        Assert.That(files, Is.Empty);
    }

    [Test]
    public void ReturnsEmptyForNonExistentDirectory()
    {
        var source = new FileInputSource(Path.Combine(_tempDir, "does-not-exist"));
        var files = source.GetPendingFiles().ToList();

        Assert.That(files, Is.Empty);
    }

    [Test]
    public void DoesNotRecurseIntoSubdirectories()
    {
        File.WriteAllText(Path.Combine(_tempDir, "top.pdf"), "");
        var subDir = Directory.CreateDirectory(Path.Combine(_tempDir, "sub"));
        File.WriteAllText(Path.Combine(subDir.FullName, "nested.pdf"), "");

        var source = new FileInputSource(_tempDir);
        var files = source.GetPendingFiles().ToList();

        Assert.That(files, Has.Count.EqualTo(1));
        Assert.That(files[0].Name, Is.EqualTo("top.pdf"));
    }
}
