Introduction
============
.NET VNC Viewer is a VNC viewer written entirely in C#. It is binary compatible with Pocket PCs (.NET Compact Framework) and Windows desktops (.NET Framework). I write this mainly because other VNC viewers on Pocket PC do not do full screen and rotation.

In addition, I have the following in my mind when I write .NET VNC Viewer:
-To ensure source and binary compatability on Pocket PCs and Windows desktops.
-To make sure that it can be built freely on Pocket PCs (Pocket C#) and Windows desktops (only .NET Framework SDK 1.1 needed and Visual Studio not required).
-To learn C# and .NET (Compact) Framework in its pure form. There are no P/Invoke calls, and it is not linked to any other libraries except the Framework.

Features
========
-Of course, basic VNC viewer functionalities.
-Full screen mode.
-Screen rotation.
-Session history.
-etc.

Requirements
============
It should work on anything with .NET Compact Framework 1.0 (or later) or .NET Framework 1.1 (or later) installed. That said, it has only been tested on a HP 4150 (Pocket PC 2003) and a desktop with Windows XP Professional (.NET Framework 1.1).
It may or may not work on previous versions of .NET Framework.

Installation
============
Just copy the exe to a directory on a Pocket PC or a Windows desktop and execute from there.

Execution
=========
vncviewer.exe [vncxml file]
	[vncxml file] is an xml file created by .NET VNC Viewer. It contains the details for .NET VNC Viewer to initiate a connection to a VNC server.

Support
=======
Any comments and questions should be directed to the corresponding forum at http://www.sourceforge.net/projects/dotnetvnc.

Known Issues
============
-It crashes if executed from a share.

Comments on C# and .NET (Compact) Framework
===========================================
-C# and its base library are excellent in terms of the features they provide and ease of programming. For example, threading classes and collection classes are easy to use and they work in pretty much the same way on Pocket PCs and Windows desktops. Many lines of code would have been needed if these classes did not exist.
-Performance is pretty good on Windows desktops. On Pocket PCs the performance difference between native and managed code is more apparent.
-Some very important features are still missing from .NET Compact Framework 1.0. E.g., clipboard support is missing, multi-media support is missing, and combo box control does not yet exist. Hopefully these features will be added in .NET Compact Framework 2.0.

History
=======
1.0.1 (?)
-Merged content of TODO.txt and HISTORY.txt into README.txt. At the same time, README.txt was revised.
1.0.0 (Jan 19, 2005)
-Initial release.

TODOs
=====
-Client side scaling.
-Server side scaling.
-ZRLE encoding.
-Dynamic desktop resize.
-Single window mode.
-etc.

References
==========
-RealVNC (http://www.realvnc.com)
The officially VNC site.
-UltraVNC (http://ultravnc.sourceforge.net)
This is a variant of VNC on Windows. It supports many features not in the original VNC suite. Most of the code of .NET VNC Viewer is derived from UltraVNC's codebase.
-SourceForge.net (http://www.sourceforge.net)
Currently .NET VNC Viewer is hosted on SourceForge. For news and releases of .NET VNC Viewer please visit the project web at http://dotnetvnc.sourceforge.net.