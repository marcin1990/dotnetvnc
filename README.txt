Introduction
============
.NET VNC Viewer is yet another VNC viewer. It is written entirely in C# and targets both Pocket PCs (to be exact, .NET Compact Framework) and Windows desktops (.NET Framework).

Installation
============
Just copy the exe to a directory on PPC or Windows desktop and execute from there.

Execution
=========
vncviewer.exe [vncxml file]
	[vncxml file] is an xml file created by .NET VNC Viewer. It contains the details for .NET VNC Viewer to initiate a connection to a VNC server.

Known issues
============
-It crashes if executed from a UNC share.

Requirements
============
It should work on anything with .NET Compact Framework 1.0 (or later) or .NET Framework 1.1 (or later) installed. That said, it has only been tested on a HP 4150 (Pocket PC 2003) and Windows XP Professional (.NET Framework 1.1).
It may or may not work on previous versions of .NET Framework.

Why
===
-I got Pocket C# and would like to write something interesting with it.
-Current VNC viewers that work on PPC are missing some features that I need, such as full screen mode.
-I would like to explore C#, .NET Framework, and .NET Compact Framework.

Restrictions enforced on source code
====================================
-Binary compatibility between PPC and Windows desktop.
-"Build-able" on PPC and Windows desktop.
-No P/Invoke calls.

Findings
========
-C# and its base library are excellent in terms of the features they provide and ease of programming. For example, threading classes and collection classes are easy to use and they work in pretty much the same way on PPC and Windows desktop. Many lines of C code would have been needed if these classes didn't exist.
-Performance is pretty good on Windows desktop. On PPC the performance difference between native and managed code is more apparent.
-Some very important features are still missing from .NET Compact Framework 1.0. E.g., clipboard support is missing, multi-media support is missing, and combo box control does not yet exist. Hopefully these features will be added in .NET Compact Framework 2.0.