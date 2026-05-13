Search the PaperClaw document archive with the question: $ARGUMENTS

Steps:
1. Run the PaperClaw search tool using the PowerShell tool:
   `& "C:\Users\INS-29\.dotnet\dotnet.exe" "C:\DEV\PaperClaw\PaperClaw\PaperClaw\bin\Debug\net10.0\PaperClaw.dll" search "$ARGUMENTS"`
2. If the binary is not built yet, build first:
   `& "C:\Users\INS-29\.dotnet\dotnet.exe" build "C:\DEV\PaperClaw\PaperClaw\PaperClaw\PaperClaw.csproj" -c Debug --nologo -v quiet`
   then run step 1.
3. Present the output to the user clearly.

Do not modify any files. Just run the command and show the result.
