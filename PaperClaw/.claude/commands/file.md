File a PDF into the PaperClaw archive: $ARGUMENTS

Steps:
1. Copy the file at path `$ARGUMENTS` into the inbox using the PowerShell tool:
   `Copy-Item -Path "$ARGUMENTS" -Destination "C:/paperclaw/inbox" -Force`
2. Run PaperClaw to process it:
   `& "C:\Users\INS-29\.dotnet\dotnet.exe" "C:\DEV\PaperClaw\PaperClaw\PaperClaw\bin\Debug\net10.0\PaperClaw.dll"`
3. If the binary is not built yet, build first:
   `& "C:\Users\INS-29\.dotnet\dotnet.exe" build "C:\DEV\PaperClaw\PaperClaw\PaperClaw.sln" --configuration Debug --nologo -v quiet`
   then repeat step 2.
4. Show the result to the user clearly (success, duplicate, failed, or skipped).

Do not modify any files in the repository. Just copy the PDF and run the command.
