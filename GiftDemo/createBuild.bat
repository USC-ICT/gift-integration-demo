
@setlocal

set UNITY_PATH=d:\edwork\vhtoolkit\vhtoolkit\core\Unity64

set PROJECTPATH=%CD%

pushd %UNITY_PATH%
call runEditorBatch.bat -projectPath %PROJECTPATH% -batchmode -nographics -quit -logFile %PROJECTPATH%\Editor.log -executeMethod BuildPlayer.PerformWindows64Build
type %PROJECTPATH%\Editor.log
popd

@endlocal
