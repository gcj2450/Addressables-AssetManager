echo Clearning Up Build Directory
rm -rf ../TestProject/build/

echo Starting Build Process
'C:/Program Files/Unity/Hub/Editor/2021.3.6f1c1/Editor/Unity.exe' -quit -batchmode -projectPath ../TestProject/ -executeMethod BuildScript.PerformBuild
echo Ended Build Process