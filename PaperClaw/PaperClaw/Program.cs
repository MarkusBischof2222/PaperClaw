using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperClaw.Abstractions;
using PaperClaw.Classification;
using PaperClaw.Input;
using PaperClaw.Models;
using PaperClaw.Output;
using PaperClaw.Pdf;
using PaperClaw.Processing;
using PaperClaw.Search;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var apiKey = config["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Anthropic:ApiKey not configured.");
var inboxPath = config["PaperClaw:InboxPath"]
    ?? throw new InvalidOperationException("PaperClaw:InboxPath not configured.");
var outboxPath = config["PaperClaw:OutboxPath"]
    ?? throw new InvalidOperationException("PaperClaw:OutboxPath not configured.");

if (args.Length > 0 && args[0].Equals("search", StringComparison.OrdinalIgnoreCase))
{
    if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
    {
        Console.Error.WriteLine("Usage: PaperClaw search \"your question\"");
        return;
    }
    var searchService = new ClaudeSearchService(apiKey, outboxPath);
    var answer = await searchService.SearchAsync(args[1]);
    Console.WriteLine(answer);
    return;
}

var services = new ServiceCollection();
services.AddSingleton<IInputSource>(new FileInputSource(inboxPath));
services.AddSingleton<IPdfTextExtractor, PdfPigTextExtractor>();
services.AddSingleton<IAnthropicMessenger>(new AnthropicMessenger(apiKey));
services.AddSingleton<IDocumentClassifier, ClaudeDocumentClassifier>();
services.AddSingleton<IOutputTarget>(new FileOutputTarget(outboxPath));
services.AddSingleton<DocumentProcessor>();

var provider = services.BuildServiceProvider();
var processor = provider.GetRequiredService<DocumentProcessor>();

Console.WriteLine($"Processing PDFs from: {inboxPath}");
var results = await processor.ProcessAllAsync();

var successes = results.OfType<ProcessingResult.Success>().ToList();
var duplicates = results.OfType<ProcessingResult.Duplicate>().ToList();
var skipped = results.OfType<ProcessingResult.Skipped>().ToList();
var failed = results.OfType<ProcessingResult.Failed>().ToList();

Console.WriteLine($"Done. Success: {successes.Count}, Duplicate: {duplicates.Count}, Skipped: {skipped.Count}, Failed: {failed.Count}");
foreach (var d in duplicates)
    Console.WriteLine($"  DUPLICATE: {Path.GetFileName(d.FilePath)} — already in outbox");
foreach (var f in failed)
{
    Console.WriteLine($"  FAILED:    {Path.GetFileName(f.FilePath)} — {f.Reason}");
    if (f.Exception is not null)
        Console.WriteLine($"             {f.Exception.GetType().Name}: {f.Exception.Message}");
}
foreach (var s in skipped)
    Console.WriteLine($"  SKIPPED:   {Path.GetFileName(s.FilePath)} — {s.Reason}");
