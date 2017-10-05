
@rem Steps:
@rem - Open 00_HybridMasterLines.xlsx
@rem - Insert a new column A
@rem - Copy/paste column F (sound id) to column A
@rem - Select column A and B,  Copy
@rem - Open 00_MasterLines.txt
@rem - Select All,  Paste
@rem - Remove first line (header)
@rem - run 00_splitmasterlines.bat
@rem - run 00_runvisemeschedulerfacefx.bat


@echo off

setlocal enabledelayedexpansion

for /F "tokens=1*" %%i in (00_MasterLines.txt) do (
   set File=%%i
   set FileNoExt=!File:~0,-4!
   set Text=%%j
   set TextNoQuotes=!Text:~1,-1!
   @rem @echo !TextNoQuotes! > !FileNoExt!.txt
   @echo !Text! > !File!.txt
   )

endlocal
