namespace PaperClaw.Models;

public record DocumentMetadata(
    string Title,
    string Date,
    string Sender,
    string Recipient,
    string Summary,
    string KeyReferences
);
