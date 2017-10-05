@setlocal

set PROJECT_ROOT=..
set JAVA_HOME_LOCAL=%ICT_JAVA_HOME%

if %PROCESSOR_ARCHITECTURE%==AMD64 GOTO AMD64
if !%PROCESSOR_ARCHITEW6432%==!AMD64 GOTO AMD64
if %PROCESSOR_ARCHITECTURE%==x86 GOTO x86

:AMD64
if not defined ICT_JAVA_HOME set JAVA_HOME_LOCAL=%PROJECT_ROOT%\java\jre
goto endProcessorDetection

:x86
if not defined ICT_JAVA_HOME set JAVA_HOME_LOCAL=%PROJECT_ROOT%\java\jre32
goto endProcessorDetection

:endProcessorDetection

set JAVA_EXE=%JAVA_HOME_LOCAL%\bin\java.exe

pushd ..\stanford-parser
"%JAVA_EXE%" -mx150m -cp "stanford-parser.jar;" edu.stanford.nlp.parser.lexparser.LexicalizedParser -outputFormat "penn,typedDependencies" englishPCFG.ser.gz %1
popd

@endlocal
