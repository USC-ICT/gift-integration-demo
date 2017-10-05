
set UNITY_PATH=d:\edwork\vhtoolkit\vhtoolkit\core\Unity64

set PROJECTPATH=%CD%

pushd %UNITY_PATH%
call runEditor.bat -projectPath %PROJECTPATH% %1 %2 %3 %4 %5 %6 %7
popd
