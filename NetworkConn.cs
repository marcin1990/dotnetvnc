//  Copyright (c) 2005 - 2006 Rocky Lo. All Rights Reserved.
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
using System.IO;
using System.Runtime.InteropServices;

namespace SystemEx.WindowCE.Net
{
  /// <remarks>This class wraps the connection manager on Windows CE.</remarks>
  public class NetworkConn
  {
    private const int ConnInfoSize = 64;

    private IntPtr hConn = IntPtr.Zero;
    private string url = null;

    [DllImport("cellcore.dll")]
    private static extern UInt32 ConnMgrMapURL(string pwszUrl, out Guid pguid, ref UInt32 pdwIndex);
    [DllImport("cellcore.dll")]
    private static extern UInt32 ConnMgrEstablishConnectionSync(byte[] pConnInfo, out IntPtr phConnection, UInt32 dwTimeout, out UInt32 pdwStatus);
    [DllImport("cellcore.dll")]
    private static extern UInt32 ConnMgrReleaseConnection(IntPtr hConnection, bool bCache);

    public NetworkConn(string url)
    {
      this.url = url;
    }

    public void Est()
    {
      // TODO: Validate the parameter.

      try
      {
        UInt32 index = 0;
        Guid guid;
        ConnMgrMapURL(url, out guid, ref index);

        // TODO: Validate the guid.

        byte[] connInfo = new byte[ConnInfoSize];
        BinaryWriter writer = new BinaryWriter(new MemoryStream(connInfo));
        writer.Write((UInt32)ConnInfoSize); // cbSize
        writer.Write((UInt32)0x00000001); // dwParams = CONNMGR_PARAM_DESTNETID;
        writer.Write((UInt32)0); // dwFlags = 0;
        writer.Write((UInt32)0x00008000); // dwPriority = CONNMGR_PRIORITY_USERINTERACTIVE;
        writer.Write(Convert.ToInt32(false)); // bExclusive = FALSE;
        writer.Write(Convert.ToInt32(false)); // bDisabled = FALSE;
        writer.Write(guid.ToByteArray()); // guidDestNet;
        writer.Write(IntPtr.Zero.ToInt32()); // hWnd = 0;
        writer.Write((UInt32)0); // uMsg = 0;
        writer.Write((UInt32)0); // lParam = 0;
        writer.Write((UInt32)0); // ulMaxCost = 0;
        writer.Write((UInt32)0); // ulMinRcvBw = 0;
        writer.Write((UInt32)0); // ulMaxConnLatency = 0;

        UInt32 status;
        ConnMgrEstablishConnectionSync(connInfo, out hConn, UInt32.MaxValue, out status);

        // TODO: Report error and raise an exception.
      }
      catch(MissingMethodException)
      {
        // There is no connection manager on some .NET CF devices, so just swallow
        // the exception in this case.
      }
    }

    public void Rel()
    {
      try
      {
        if(hConn != IntPtr.Zero)
          ConnMgrReleaseConnection(hConn, true);
        // TODO: Report error and raise an exception.
      }
      catch(MissingMethodException)
      {
        // There is no connection manager on some .NET CF devices, so just swallow
        // the exception in this case.
      }
    }
  }
}
