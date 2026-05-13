# Papwerclaw

Organize PDFs from an input source to an output target with 
classification in between to be able to answer agentic questions afterwards.

## Architecture 

+ C# Console application
+ Configuration via `appsettings.json` in the project (includes Anthropic API key)

## Input

+ My be an arbitrary input source. Start with a simple file input (inbox). Later, an email source may be added.
+ Assume PDFs; leave non-PDFs and non-processable PDFs untouched in the inbox.
+ After processing, successfully processed files are removed from the inbox.

## Classification

+ First read out only the text layer of the pdf using PdfPig (MIT, pure .NET, no native dependencies)
+ Use Claude API to classify the file into one of the fixed types based on the text layer: Invoice, Contract, Receipt, Letter, Report, Other
+ Also use Claude API to extract meta information and write it to a `transcript.md` alongside the PDF in the output folder. Fields: title, date, parties (sender/recipient), summary, key amounts/references

## Output

+ My be an arbitrary file storage. Start wit ha simple file output (outbox)
+ The layout for the output should be structured
  + Start with "Year", "Month" (from file's last modification date), then "Type" (if available), then for each file an "Id" (timestamp + random suffix), then the contents. 

## Further reasoning on the output

+ e.g. "show me all invoices of the last month"

## Security and privacy considerations

+ Files with password are not processed

## Unittesting

+ Every possible invocation of the tool is unittested.
+ nunit