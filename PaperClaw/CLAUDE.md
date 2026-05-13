# PaperClaw

C# .NET 10 console app that classifies PDFs from an inbox into a structured outbox using the Claude API.

## Running the tool

dotnet path: `C:\Users\INS-29\.dotnet\dotnet.exe`  
Built binary: `C:\DEV\PaperClaw\PaperClaw\PaperClaw\bin\Debug\net10.0\PaperClaw.dll`

Build: `& "C:\Users\INS-29\.dotnet\dotnet.exe" build "C:\DEV\PaperClaw\PaperClaw\PaperClaw.sln" --configuration Debug`  
Test:  `& "C:\Users\INS-29\.dotnet\dotnet.exe" test "C:\DEV\PaperClaw\PaperClaw\PaperClaw.sln"`

## Searching the archive

When the user asks ANY question about their documents, invoices, contracts, or the archive — run the search tool immediately without asking:

```powershell
& "C:\Users\INS-29\.dotnet\dotnet.exe" "C:\DEV\PaperClaw\PaperClaw\PaperClaw\bin\Debug\net10.0\PaperClaw.dll" search "<question>"
```

Examples that should trigger this:
- "find all invoices from last month"
- "which document is about electricity?"
- "show me contracts from 2025"
- "what did I pay in April?"
- Any question about documents, PDFs, or the archive

Present the result directly. Do not ask for clarification first.
