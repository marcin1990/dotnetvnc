//  Copyright (C) 2004-2005 Rocky Lo. All Rights Reserved.
//  Copyright (C) 1999 AT&T Laboratories Cambridge. All Rights Reserved.
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
// If the source code for the VNC system is not available from the place whence 
// you received this file, check http://www.uk.research.att.com/vnc or contact
// the authors on vnc@uk.research.att.com for information on obtaining it.

using Vnc.RfbProto;

namespace Vnc.Security
{
  public class VncAuth
  {
    public static readonly int MaxPasswdLen;

    // For why this is needed, please refer to RfbProto.cs
    static VncAuth()
    {
      MaxPasswdLen = 8;
    }

    public static void EncryptBytes(byte[] bytes, string passwd)
    {
      byte[] rawKey = new byte[MaxPasswdLen];
      for(int i = 0; i < MaxPasswdLen; i++)
      {
        if(i < passwd.Length)
          rawKey[i] = (byte)passwd[i];
        else
          rawKey[i] = 0;
      }

      D3DesKey key = new D3DesKey(rawKey, EncryptMode.EN0);

      D3Des.Des(bytes, bytes, RfbSize.AuthChallenge, key);
    }
  }
}
