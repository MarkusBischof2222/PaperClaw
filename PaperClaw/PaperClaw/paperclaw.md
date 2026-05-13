# Papwerclaw

Organize PDFs from an input source to an output target with classification in between to be able to answer agentic questions afterwards.

## Architecture 

+ C# Console application that starts with parameters

## Input

+ My be an arbitrary input source. Start with a simple file input (inbox).
+ Assume PDFs, leave non processable untouched
+ After processing, successful processed files are removed.

## Classification

+ First read out only the text layer of the pdf
+ Find a target directory for the file. Create a folder there and place the file and it's textlayer there.
+ Also extract some useful meta information about the semantic of the document and put it in a transcript md file

## Output

+ My be an arbitrary file storage. Start wit ha simple file output (outbox)
+ The layout for the output should be structured
  + Start with "Year", "Month", then "Type" (if available), the for each file an "Id", then the contents. 

## Further reasoning on the output

+ Later

## Unittesting

+ Every possible invocation of the tool is unittested.
+ nunit