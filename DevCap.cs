//  Copyright (C) 2005 Rocky Lo. All Rights Reserved.
//
//  This file is part of the VNC system.
//
//  The VNC system is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307,
//  USA.
//
// If the source code for the VNC system is not available from the place 
// whence you received this file, check http://www.uk.research.att.com/vnc or contact
// the authors on vnc@uk.research.att.com for information on obtaining it.

using System;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  internal enum DevCapLvl
  {
    Smartphone,
    PocketPc,
    Desktop
  }

  internal enum ResLvl
  {
    Normal,
    High
  }

  /// <remarks>
  ///   This class is responsible for detecting what the device is capable of.
  /// </remarks>
  internal class DevCap
  {
    private const ushort PocketPcNormWidth = 240;
    private const ushort SmartphoneNormWidth = 176;

    internal readonly DevCapLvl Lvl;
    internal readonly ResLvl Res;

    internal DevCap()
    {
      if(Environment.OSVersion.Platform == PlatformID.WinCE)
      {
        try
        {
          // TODO: Is there a smarter way to determine this is a Ppc or Sp?
          TabControl ctrl = new TabControl();
          ctrl.Dispose();
          Lvl = DevCapLvl.PocketPc;
          Res = Screen.PrimaryScreen.Bounds.Width > PocketPcNormWidth? ResLvl.High : ResLvl.Normal;
        }
        catch(NotSupportedException)
        {
          Lvl = DevCapLvl.Smartphone;
          Res = Screen.PrimaryScreen.Bounds.Width > SmartphoneNormWidth? ResLvl.High : ResLvl.Normal;
        }
      }
      else
      {
        // TODO: Currently this seems to be correct. That is, if Platform is
        // not WinCE then we have full access to .NET Framework. If this ever
        // changes this branch has to be modified.
        Lvl = DevCapLvl.Desktop;
        Res = ResLvl.Normal;
      }
    }
  }
}
