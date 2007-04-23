Introduction
============
.NET VNC Viewer is a VNC viewer written entirely in C#. It is binary compatible with Smartphones, Pocket PCs and Windows desktops (with .NET Compact Framework or .NET Framework). I write this mainly because other VNC viewers on Pocket PC do not do full screen and screen rotation.

In addition, I have the following in my mind when I write .NET VNC Viewer:
-To ensure source and binary compatibility on Smartphones, Pocket PCs and Windows desktops.
-To make sure that it can be built freely on Pocket PCs (with Pocket C# at http://mifki.ru/pcsharp/) and Windows desktops (only .NET Framework SDK 1.1 needed and Visual Studio not required).
-To learn C# and .NET (Compact) Framework in its pure form. Avoid P/Invoke calls and try not to link to any other libraries except the Framework.

Features
========
-Of course, basic VNC viewer functionalities.
-Full screen mode.
-Screen rotation.
-Client-side scaling.
-Server-side scaling and single window mode.
-Session history.
-Hi-Res support for VGA Pocket PC and QVGA Smartphones.
-Listen mode.
-etc.

Requirements
============
It works on the following devices:
-It should work on all Windows desktops with .NET Framework 1.1 (or later). I only tested it on my workstation with Windows XP Home and Professional, however.
-It should work on all Pocket PCs with .NET Compact Framework 1.0 (or later). I only tested it on my HP 4150 with Windows Mobile 2003 though.
-It should work on all Smartphones with .NET Compact Framework 1.0 (or later). I only tested it with an emulator.
-It should work on other Windows CE devices with .NET Compact Framework 1.0 installed. I haven't tested with any.

It may or may not work on previous versions of .NET Framework.

Installation
============
Just copy the exe to a directory on your device and execute from there.

Execution
=========
vncviewer.exe [vncxml file]
	[vncxml file] is an xml file created by .NET VNC Viewer. It contains the details for .NET VNC Viewer to initiate / wait for a connection to/from a VNC server.

Usage
=====
Most of the time it should be pretty straight forward, but there are some features that are not so obvious.

To exit full screen mode on a desktop, "tap-and-hold" your right mouse button. A context menu will appear that let you go back to window mode. To exit full screen mode on a Pocket PC, tap-and-hold on the touch screen. After the dot goes around the big circle once, release your stylus and the context menu will popup (if it goes around the big circle twice a right mouse click is sent to the server). To exit full screen mode on a Smartphone, "tap-and-hold" soft key 2.

To enter letters on a Smartphone, press "*" on your keypad. A textbox will show at the lower right corner and you can enter letters as well as the arrow keys, backspace, enter, etc. The textbox will dismiss itself automatically after idle for several seconds.

Support
=======
Any comments and questions should be directed to the corresponding forum or tracker at http://sourceforge.net/forum/?group_id=128549.

Known Issues
============
-It crashes if executed from a share.
-There is a bug in UltraVNC. Don't mix server-side scaling with single window mode. (Try to do this with UltraVNC viewer and you will know what I mean)
-It does not work with UltraVNC 1.0.0 RC 19.5 to RC 20.4 due to a bug in UltraVNC. Please stay with RC 18 or upgrade to at least RC 20.5.

Comments on C# and .NET (Compact) Framework
===========================================
-C# and its base library are excellent in terms of the features they provide and ease of programming. For example, threading classes and collection classes are easy to use and they work in pretty much the same way on Smartphones, Pocket PCs and Windows desktops. Many lines of code would have been needed if these classes did not exist.
-Performance is pretty good on Windows desktops. On Pocket PCs the performance difference between native and managed code is more apparent.
-Some very important features are still missing from .NET Compact Framework 1.0. E.g., clipboard support is missing, multi-media support is missing, and editable combo box control does not yet exist. Hopefully these features will be added in .NET Compact Framework 2.0.

History
=======
1.0.1.17 (Apr 24, 2007)
-Implemented listen mode.
-Added an option for Smartphone users to turn off mouse acceleration.
-Added an option for Smartphone users to choose the mouse speed after turning off mouse acceleration.
-For Smartphones with a QWERTY keyboard, now the lower case "p" and "q" keys should work as expected.
-More keys were added to the Keys menu (Esc, Tab, and Fn keys).
-Avoided an exception for some .NET CF devices with no connection manager.
1.0.1.16 (Apr 24, 2005)
-Server-side scaling and single window mode implemented.
-On a PPC or a Smartphone, now the viewer calls into the connection manager to initiate a network connection (e.g. GPRS) before connecting to the server. From now on the viewer is not P/Invoke-free anymore.
-Added an icon.
-Added an option for users to choose whether to update the screen ASAP or in a batch.
-Added a sample build script.
-Fixed a bug that would hang the viewer if the connection is terminated unexpectedly.
1.0.1.10 (Mar 22, 2005)
-Client-side scaling implemented.
-Fixed a problem with Smartphone key input mode which prevents the viewer from going back to mouse mode after exiting extended input mode.
-For Smartphones, the back soft key is now backspace even not in key input mode.
-Improved the code for painting.
-Made adjustments to thread priority to provide a more responsive UI.
-Added "Shift Down" and "Shift Up" to the "Keys" menu.
-For Smartphones, added an option to send the mouse location to the server when the cursor is idle.
-Fixed a bug on Smartphones that would cause the right mouse button to not properly function after exiting full screen mode.
1.0.1 (Feb 27, 2005)
-Smartphone support.
-Hi-Res support for VGA Pocket PCs and QVGA Smartphones.
-Better full screen support (remote desktop centered with black background).
-Better desktop support (intuitive right click handling).
-Workaround for a bug in .NET CF prior to SP3 that hangs the viewer upon exit.
-Workaround for a bug in .NET CF prior to SP3 that crashes the viewer when the server terminates the connection.
-Other cosmetic changes.
-Merged content of TODO.txt and HISTORY.txt into README.txt. README.txt was revised.
1.0.0 (Jan 19, 2005)
-Initial release.

TODOs
=====
-UltraVNC proxy support.
-ZRLE encoding.
-Status reporting.
-etc.

References
==========
-RealVNC (http://www.realvnc.com)
The officially VNC site.
-UltraVNC (http://ultravnc.sourceforge.net)
This is a variant of VNC on Windows. It supports many features not in the original VNC suite. Most of the code of .NET VNC Viewer is derived from UltraVNC's codebase.
-SourceForge (http://sourceforge.net)
Currently .NET VNC Viewer is hosted on SourceForge. For news and releases of .NET VNC Viewer please visit the project web at http://dotnetvnc.sourceforge.net.
