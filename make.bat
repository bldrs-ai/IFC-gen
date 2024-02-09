SET ANTLR=java -jar C:\Javalib\antlr-4.7-complete.jar
SET CURR_DIR=%cd%
SET GRAMMAR_IFC=%CURR_DIR%\grammar\Express.g4
SET GRAMMAR_STEP=%CURR_DIR%\grammar\STEP.g4
::SET SCHEMA_VERSION=AP214E3_2010
SET %2
SET %3
SET SCHEMA=%CURR_DIR%\schemas\%SCHEMA_INPUT%.exp

::csharp:
	set %1
	%ANTLR% -Dlanguage=CSharp -package Express -o %CURR_DIR%\src\antlr %GRAMMAR_IFC%
	%ANTLR% -Dlanguage=CSharp -package STEP -o %CURR_DIR%\lang\csharp\src\antlr %GRAMMAR_STEP%
	dotnet build .\src\IFC-gen.csproj
	echo %OUTDIR%
	dotnet run --project .\src\IFC-gen.csproj -e %SCHEMA% -l bldrsts -o %OUTDIR% -s %SHORTNAME%

::clean:
	::rmdir /s /q .\lang\csharp\src\antlr
	::rmdir /s /q .\src\antlr
	::rmdir /s /q .\src\bin
	::rmdir /s /q .\src\obj  