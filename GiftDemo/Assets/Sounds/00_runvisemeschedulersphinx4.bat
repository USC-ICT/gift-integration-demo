
@rem usage:  VisemeSchedulerSphinx4 <folder containing sounds>

set CUR_DIR=%CD%

pushd ..\..\..\..\tools\VisemeSchedulerSphinx4
call process.bat %CUR_DIR%
popd
