using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaperClaw.Abstractions;
using PaperClaw.Classification;
using PaperClaw.Input;
using PaperClaw.Models;
using PaperClaw.Output;
using PaperClaw.Pdf;
using PaperClaw.Processing;

var config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var apiKey = config["Anthropic:ApiKey"]
    ?? throw new InvalidOperationException("Anthropic:ApiKey not configured.");
var inboxPath = config["PaperClaw:InboxPath"]
    ?? throw new InvalidOperationException("PaperClaw:InboxPath not configured.");
var outboxPath = config["PaperClaw:OutboxPath"]
    ?? throw new InvalidOperationException("PaperClaw:OutboxPath not configured.");

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
var skipped = results.OfType<ProcessingResult.Skipped>().ToList();
var failed = results.OfType<ProcessingResult.Failed>().ToList();

Console.WriteLine($"Done. Success: {successes.Count}, Skipped: {skipped.Count}, Failed: {failed.Count}");
foreach (var f in failed)
    Console.WriteLine($"  FAILED:   {Path.GetFileName(f.FilePath)} — {f.Reason}");
foreach (var s in skipped)
    Console.WriteLine($"  SKIPPED:  {Path.GetFileName(s.FilePath)} — {s.Reason}");
