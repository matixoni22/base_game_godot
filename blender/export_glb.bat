@echo off
REM ============================================================
REM Export all .blend project objects into separate folders
REM Author: ChatGPT
REM ============================================================

REM Set the path to Blender executable
set "BLENDER_PATH=C:\Program Files\Blender Foundation\Blender 4.0\blender.exe"

REM Set the source directory containing .blend files
set "SOURCE_DIR=%~dp0"

REM Set the output directory where exports will be saved
set "EXPORT_DIR=%~dp0..\models"

echo.
echo ============================================================
echo Exporting all .blend projects from:
echo   %SOURCE_DIR%
echo to:
echo   %EXPORT_DIR%
echo ============================================================
echo.

REM Create export directory if it doesn’t exist
if not exist "%EXPORT_DIR%" mkdir "%EXPORT_DIR%"

REM Loop through each .blend file in the source directory
for %%F in ("%SOURCE_DIR%\*.blend") do (
    set "FILE=%%~nF"
    set "OUT_DIR=%EXPORT_DIR%\%%~nF"
    echo Processing: %%~nxF
    if not exist "%EXPORT_DIR%\%%~nF" mkdir "%EXPORT_DIR%\%%~nF"

    REM Use Blender to export all objects (FBX format in this example)
	blender -b "%%F" --python-expr ^
    "import bpy, os; bpy.ops.export_scene.gltf(filepath=os.path.join(r'%EXPORT_DIR%', r'%%~nF', r'%%~nF.glb'), export_format='GLB', export_apply=True)"

    echo Finished exporting %%~nxF
    echo.
)