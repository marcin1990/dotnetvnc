# .NET VNC Viewer

.NET VNC Viewer is a VNC viewer written entirely in C#. It is binary compatible with Smartphones, Pocket PCs, Windows desktops (with .NET Compact Framework or .NET Framework) and even with Mono. Written primarily because other VNC viewers on Pocket PC do not do full screen and screen rotation.

For more information please refer to the [Features](#features) section and the [Screenshots](#screenshots) section. You may also go directly to the [Download](https://github.com/marcin1990/dotnetvnc/releases) section to get a copy of .NET VNC Viewer.

Written and maintained primarily by Rocky Lo at [SourceForge](http://dotnetvnc.sourceforge.net/), now hosted at GitHub and maintained by marcin1990.


## Table of Contents


- [Features](#features)
- [Screenshots](#screenshots)
- [Requirements](#requirements)
- [Download](#download)
- [Installation](#installation)
- [Usage](#usage)
- [Known issues](#known-issues)
- [TODO](#todo)
- [License](#license)
- [References](#references)


---


## Features
- Basic VNC viewer functionalities.
- Full screen mode.
- Client-side scaling.
- Server-side scaling and single window mode.
- Screen rotation.
- Session history.
- Hi-Res support for VGA Pocket PCs and QVGA Smartphones.
- Listen mode.
- etc.

---


## Screenshots

[![Running on Windows 10](https://i.imgur.com/D0kTZyjb.png)](https://i.imgur.com/D0kTZyj.png)   [![Connecting to Ubuntu 19.10](https://i.imgur.com/TV4PL4Nb.png)](https://i.imgur.com/TV4PL4N.png)   [![Running on Ubuntu 18.04.3 LTS using Mono](https://i.imgur.com/FFPYKz4b.png)](https://i.imgur.com/FFPYKz4.png)  
[![Running on smartphone](https://i.imgur.com/tT8nIiab.jpg)](https://i.imgur.com/tT8nIia.jpg)   [![Running on PPC](https://i.imgur.com/EAHiXjkb.jpg)](https://i.imgur.com/EAHiXjk.jpg)   [![Running on Windows XP](https://i.imgur.com/fX63Yz0b.jpg)](https://i.imgur.com/fX63Yz0.jpg)   

---

## Requirements
It works on the following devices:
- It should work on all Windows desktops with .NET Framework 1.1 (or later).
- It should work on all Pocket PCs with .NET Compact Framework 1.0 (or later, tested on HP 4150 with Windows Mobile 2003).
- It should work on all Smartphones with .NET Compact Framework 1.0 (or later, tested with an emulator).
- It should work on other Windows CE devices with .NET Compact Framework 1.0 installed (not tested).

It may or may not work on previous versions of .NET Framework.

There is possibility to run application using Mono at Linux/Unix systems but due to differences in Compact Framework libraries it should be only a desktop build described in BUILDING.TXT.

---


## Download

Releases of .NET VNC Viewer can be downloaded from [this page](https://github.com/marcin1990/dotnetvnc/releases).

---

## Installation

Just copy the exe to a directory on your device and execute from there.

---

## Usage

Most of the time it should be pretty straight forward, but there are some features that are not so obvious.

To exit full screen mode on a desktop, "tap-and-hold" your right mouse button. A context menu will appear that let you go back to window mode. To exit full screen mode on a Pocket PC, tap-and-hold on the touch screen. After the dot goes around the big circle once, release your stylus and the context menu will popup (if it goes around the big circle twice a right mouse click is sent to the server). To exit full screen mode on a Smartphone, "tap-and-hold" soft key 2.

To enter letters on a Smartphone, press "*" on your keypad. A textbox will show at the lower right corner and you can enter letters as well as the arrow keys, backspace, enter, etc. The textbox will dismiss itself automatically after idle for several seconds.

When file 'autostart.vncxml' is present next to application binary file then saved connection will start automatically.

You could pass connection filename as a parameter to start desired connection too.

---

## Known issues

- It crashes if executed from a share.
- There is a bug in UltraVNC. Don't mix server-side scaling with single window mode. (Try to do this with UltraVNC viewer and you will know what I mean)
- It does not work with UltraVNC 1.0.0 RC 19.5 to RC 20.4 due to a bug in UltraVNC. Please stay with RC 18 or upgrade to at least RC 20.5.

---

## TODO

- UltraVNC proxy support.
- ZRLE encoding.
- Status reporting.
- Clipboard support.
- etc.

---

## References

### RealVNC

The official [RealVNC](https://www.realvnc.com/) site.

### UltraVNC

This is a variant of VNC on Windows. It supports many features not in the original VNC suite. Most of the code of .NET VNC Viewer is derived from [UltraVNC](https://www.uvnc.com/)'s codebase.

### SourceForge

Previously .NET VNC Viewer was hosted on [SourceForge](http://dotnetvnc.sourceforge.net/). 

---

## License

GNU GENERAL PUBLIC LICENSE Version 2, June 1991

Copyright (C) 2020 marcin1990. All Rights Reserved.

Copyright (C) 2004-2005, 2007 Rocky Lo. All Rights Reserved.

---

## Contribute

Please do contribute! Issues and pull requests are welcome.

---