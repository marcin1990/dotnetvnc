csc /nologo /checked- /nostdlib /noconfig /optimize+ /target:winexe /win32icon:vncviewer.ico /out:vncviewer.exe /reference:\pc#\netcf\mscorlib.dll;\pc#\netcf\System.dll;\pc#\netcf\System.Xml.dll;\pc#\netcf\System.Drawing.dll;\pc#\netcf\System.Windows.Forms.dll *.cs
res2exe -c "C:\Program Files\Developer Resources for Windows Mobile 2003 Second Edition\tools\hidpi.res" vncviewer.exe
