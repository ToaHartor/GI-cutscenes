@echo off
echo Drag and drop your USM file here, or enter the path manually:
set /p file="Enter the path to the file: "
set "path=%file:"=%"
GICutscenes demuxUsm "%path%" -m -s
