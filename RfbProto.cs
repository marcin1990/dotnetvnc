/*
//  Copyright (C) 2004-2005 Rocky Lo. All Rights Reserved.
//  Copyright (C) 2002 Ultr@VNC Team Members. All Rights Reserved.
//  Copyright (C) 2000-2002 Const Kaplinsky. All Rights Reserved.
 *  Copyright (C) 2002 RealVNC Ltd.  All Rights Reserved.
 *  Copyright (C) 1999 AT&T Laboratories Cambridge.  All Rights Reserved.
 *
 *  This is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This software is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this software; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307,
 *  USA.
 */

using System;
using System.Drawing;

// For more information on the classes below, consult the VNC Protocol Specification.

namespace Vnc.RfbProto
{
  /// <remarks>Sizes of some RFB structures.</remarks>
  public class RfbSize
  {
    // We don't define the values as const because doing so would not allow
    // the values to be dynamically retrieved.
    // This is useful when we move this module to a DLL.

    public static readonly UInt16 VerMsg;
    public static readonly UInt16 AuthChallenge;
    public static readonly UInt16 ServInit;
    public static readonly UInt16 FrameBufUpd;
    public static readonly UInt16 FrameBufUpdRectHdr;
    public static readonly UInt16 ServCutTxt;
    public static readonly UInt16 CopyRect;
    public static readonly UInt16 RreSubRect;
    public static readonly UInt16 CoRreSubRect;
    public static readonly UInt16 HexSubRect;

    static RfbSize()
    {
      VerMsg = 12;
      AuthChallenge = 16;
      ServInit = 24;
      FrameBufUpd = 4;
      FrameBufUpdRectHdr = 12;
      ServCutTxt = 8;
      CopyRect = 4;
      RreSubRect = 8;
      CoRreSubRect = 4;
      HexSubRect = 2;
    }

    // This class cannot be instantiated.
    private RfbSize()
    {}
  }

  public enum RfbCliMsgType : byte
  {
    SetPixelFormat = 0,
    SetEncodings = 2,
    FrameBufUpdReq = 3,
    KeyEvent = 4,
    PointerEvent = 5
  }

  public enum RfbServMsgType : byte
  {
    FrameBufUpd = 0,
    SetColorMapEntries = 1,
    Bell = 2,
    ServCutTxt = 3
  }

  public enum RfbAuthScheme : uint
  {
    ConnFailed = 0,
    NoAuth = 1,
    VncAuth = 2
  }

  public enum RfbAuthResult : uint
  {
    Ok = 0,
    Failed = 1,
    TooMany = 2
  }

  public enum RfbEncoding : uint
  {
    Raw = 0,
    CopyRect = 1,
    Rre = 2,
    CoRre = 4,
    Hex = 5
  }

  public enum RfbHexSubEncoding : byte
  {
    Raw = 1,
    BgSpec = 2,
    FgSpec = 4,
    AnySubRects = 8,
    SubRectsColored = 16
  }

  // The following classes are helpers to decode server messages.

  public class ServInit
  {
    public readonly UInt16 Width;
    public readonly UInt16 Height;
    public readonly byte Bpp;
    public readonly byte Depth;
    public readonly bool IsBigEndian;
    public readonly bool IsTrueColor;
    public readonly UInt16 RedMax;
    public readonly UInt16 GreenMax;
    public readonly UInt16 BlueMax;
    public readonly byte RedShift;
    public readonly byte GreenShift;
    public readonly byte BlueShift;
    public readonly UInt32 NameLen;

    public ServInit(byte[] msg)
    {
      Width = (UInt16)((UInt16)msg[0] << 8 | (UInt16)msg[1]);
      Height = (UInt16)((UInt16)msg[2] << 8 | (UInt16)msg[3]);
      Bpp = msg[4];
      Depth = msg[5];
      IsBigEndian = (msg[6] != 0);
      IsTrueColor = (msg[7] != 0);
      RedMax = (UInt16)((UInt16)msg[8] << 8 | (UInt16)msg[9]);
      GreenMax = (UInt16)((UInt16)msg[10] << 8 | (UInt16)msg[11]);
      BlueMax = (UInt16)((UInt16)msg[12] << 8 | (UInt16)msg[13]);
      RedShift = msg[14];
      GreenShift = msg[15];
      BlueShift = msg[16];
      // 17 -> 19 are garbage.
      NameLen = (UInt32)msg[20] << 24 | (UInt32)msg[21] << 16 | (UInt32)msg[22] << 8 | (UInt32)msg[23];
    }
  }

  public class FrameBufUpdRectMsgHdr
  {
    private readonly Rectangle rect;
    public readonly RfbEncoding Encoding;

    public UInt16 X
    {
      get
      {
        return (UInt16)rect.X;
      }
    }

    public UInt16 Y
    {
      get
      {
        return (UInt16)rect.Y;
      }
    }

    public UInt16 Width
    {
      get
      {
        return (UInt16)rect.Width;
      }
    }

    public UInt16 Height
    {
      get
      {
        return (UInt16)rect.Height;
      }
    }

    public FrameBufUpdRectMsgHdr(byte[] msg)
    {
      rect = new Rectangle();
      rect.X = (int)msg[0] << 8 | (int)msg[1];
      rect.Y = (int)msg[2] << 8 | (int)msg[3];
      rect.Width = (int)msg[4] << 8 | (int)msg[5];
      rect.Height = (int)msg[6] << 8 | (int)msg[7];
      Encoding = (RfbEncoding)((UInt32)msg[8] << 24 | (UInt32)msg[9] << 16 | (UInt32)msg[10] << 8 | (UInt32)msg[11]);
    }
  }

  public class CopyRectMsg
  {
    private readonly Point pt;

    public UInt16 X
    {
      get
      {
        return (UInt16)pt.X;
      }
    }

    public UInt16 Y
    {
      get
      {
        return (UInt16)pt.Y;
      }
    }

    public CopyRectMsg(byte[] msg)
    {
      pt = new Point();
      pt.X = (int)msg[0] << 8 | (int)msg[1];
      pt.Y = (int)msg[2] << 8 | (int)msg[3];
    }
  }

  public class RreSubRectMsg
  {
    protected Rectangle rect = new Rectangle();
    protected UInt32 pixel = 0;

    public UInt32 Pixel
    {
      get
      {
        return pixel;
      }
    }

    public UInt16 X
    {
      get
      {
        return (UInt16)rect.X;
      }
    }

    public UInt16 Y
    {
      get
      {
        return (UInt16)rect.Y;
      }
    }

    public UInt16 Width
    {
      get
      {
        return (UInt16)rect.Width;
      }
    }

    public UInt16 Height
    {
      get
      {
        return (UInt16)rect.Height;
      }
    }

    protected RreSubRectMsg()
    {}

    public RreSubRectMsg(byte[] msg, byte bytesPp, bool isCompact)
    {
      pixel = RfbProtoUtil.GetPixelFromData(msg, 0, bytesPp);
      if(isCompact)
      {
        rect.X = msg[bytesPp];
        rect.Y = msg[bytesPp + 1];
        rect.Width = msg[bytesPp + 2];
        rect.Height = msg[bytesPp + 3];
      }
      else
      {
        rect.X = (UInt16)((UInt16)msg[bytesPp] << 8 | (UInt16)msg[bytesPp + 1]);
        rect.Y = (UInt16)((UInt16)msg[bytesPp + 2] << 8 | (UInt16)msg[bytesPp + 3]);
        rect.Width = (UInt16)((UInt16)msg[bytesPp + 4] << 8 | (UInt16)msg[bytesPp + 5]);
        rect.Height = (UInt16)((UInt16)msg[bytesPp + 6] << 8 | (UInt16)msg[bytesPp + 7]);
      }
    }
  }

  public class HexSubRectMsg : RreSubRectMsg
  {
    public HexSubRectMsg(byte[] msg, UInt32 offset, byte bytesPp, bool subRectsColored, UInt32 fgPixel) : base()
    {
      pixel = subRectsColored? RfbProtoUtil.GetPixelFromData(msg, offset, bytesPp) : fgPixel;
      offset = subRectsColored? offset + bytesPp : offset;
      rect.X = (UInt16)((msg[offset] & 0xF0) >> 4);
      rect.Y = (UInt16)(msg[offset] & 0x0F);
      rect.Width = (UInt16)(((msg[offset + 1] & 0xF0) >> 4) + 1);
      rect.Height = (UInt16)((msg[offset + 1] & 0x0F) + 1);
    }
  }

  /// <remarks>
  ///   This class contains helper methods.
  ///   This is not strictly OO. However, this is simpler than having a lot of
  ///   small objects to perform serialization and deserialization.
  /// </remarks>
  public class RfbProtoUtil
  {
    public static void GetVerFromMsg(string verMsg, out int majorVer, out int minorVer)
    {
      string majorVerStr = verMsg.Substring(4, 3);
      string minorVerStr = verMsg.Substring(8, 3);
      majorVer = Int32.Parse(majorVerStr);
      minorVer = Int32.Parse(minorVerStr);
    }

    public static bool IsValidVerMsg(string verMsg)
    {
      if(verMsg.Length != RfbSize.VerMsg)
        return false;

      // Doing this char-by-char to prevent culture-sensitve comparison.
      if(verMsg[0] != 'R' ||
         verMsg[1] != 'F' ||
         verMsg[2] != 'B' ||
         verMsg[3] != ' ' ||
         verMsg[7] != '.' ||
         verMsg[RfbSize.VerMsg - 1] != '\n')
        return false;

      try
      {
        int majorVer;
        int minorVer;
        GetVerFromMsg(verMsg, out majorVer, out minorVer);
        if(majorVer < 0 || minorVer < 0)
          return false;
        else
          return true;
      }
      catch(FormatException)
      {
        return false;
      }
    }

    public static string GetVerMsg(int majorVer, int minorVer)
    {
      return String.Format("RFB {0:000}.{1:000}\n", majorVer, minorVer);
    }

    public static byte[] GetCliInitMsg(bool shared)
    {
      return shared? new byte[1] { 1 } : new byte[1] { 0 };
    }

    public static UInt16 GetNumRectsFromFrameBufUpdMsg(byte[] msg)
    {
      return (UInt16)((UInt16)msg[1] << 8 | (UInt16)msg[2]);
    }

    public static UInt32 GetLenFromServCutTxtMsg(byte[] msg)
    {
      return (UInt32)msg[3] << 24 | (UInt32)msg[4] << 16 | (UInt32)msg[5] << 8 | (UInt32)msg[6];
    }

    public static byte[] GetSetPixelFormatMsg
    (byte bpp,
     byte depth,
     bool isBigEndian,
     bool isTrueColor,
     UInt16 redMax,
     UInt16 greenMax,
     UInt16 blueMax,
     byte redShift,
     byte greenShift,
     byte blueShift)
    {
      byte[] msg = new byte[20];
      msg[0] = (byte)RfbCliMsgType.SetPixelFormat;
      msg[1] = 0; // Padding.
      msg[2] = 0; // Padding.
      msg[3] = 0; // Padding.
      msg[4] = bpp;
      msg[5] = depth;
      msg[6] = (byte)(isBigEndian? 1 : 0);
      msg[7] = (byte)(isTrueColor? 1 : 0);
      msg[8] = (byte)((redMax & 0xFF00) >> 8);
      msg[9] = (byte)(redMax & 0x00FF);
      msg[10] = (byte)((greenMax & 0xFF00) >> 8);
      msg[11] = (byte)(greenMax & 0x00FF);
      msg[12] = (byte)((blueMax & 0xFF00) >> 8);
      msg[13] = (byte)(blueMax & 0x00FF);
      msg[14] = redShift;
      msg[15] = greenShift;
      msg[16] = blueShift;
      msg[17] = 0; // Padding.
      msg[18] = 0; // Padding.
      msg[19] = 0; // Padding.
      return msg;
    }

    public static byte[] GetFrameBufUpdReqMsg(UInt16 x, UInt16 y, UInt16 width, UInt16 height, bool incremental)
    {
      byte[] msg = new byte[10];
      msg[0] = (byte)RfbCliMsgType.FrameBufUpdReq;
      msg[1] = (byte)(incremental? 1 : 0);
      msg[2] = (byte)((x & 0xFF00) >> 8);
      msg[3] = (byte)(x & 0x00FF);
      msg[4] = (byte)((y & 0xFF00) >> 8);
      msg[5] = (byte)(y & 0x00FF);
      msg[6] = (byte)((width & 0xFF00) >> 8);
      msg[7] = (byte)(width & 0x00FF);
      msg[8] = (byte)((height & 0xFF00) >> 8);
      msg[9] = (byte)(height & 0x00FF);
      return msg;
    }

    public static byte[] GetPointerEventMsg(UInt16 x, UInt16 y, bool leftBtnPressed, bool rightBtnPressed)
    {
      byte[] msg = new byte[6];
      msg[0] = (byte)RfbCliMsgType.PointerEvent;
      msg[1] = (byte)(leftBtnPressed? 0x01 : 0);
      msg[1] |= (byte)(rightBtnPressed? 0x04 : 0);
      msg[2] = (byte)((x & 0xFF00) >> 8);
      msg[3] = (byte)(x & 0xFF);
      msg[4] = (byte)((y & 0xFF00) >> 8);
      msg[5] = (byte)(y & 0xFF);
      return msg;
    }

    public static byte[] GetKeyEventMsg(bool isDown, UInt32 key)
    {
      byte[] msg = new byte[8];
      msg[0] = (byte)RfbCliMsgType.KeyEvent;
      msg[1] = (byte)(isDown? 1 : 0);
      msg[2] = 0; // Padding
      msg[3] = 0; // Padding
      msg[4] = (byte)((key & 0xFF000000) >> 24);
      msg[5] = (byte)((key & 0x00FF0000) >> 16);
      msg[6] = (byte)((key & 0x0000FF00) >> 8);
      msg[7] = (byte)(key & 0x000000FF);
      return msg;
    }

    public static byte[] GetSetEncodingsMsgHdr(UInt16 numEncodings)
    {
      byte[] msg = new byte[4];
      msg[0] = (byte)RfbCliMsgType.SetEncodings;
      msg[1] = 0; // Padding
      msg[2] = (byte)((numEncodings & 0xFF00) >> 8);
      msg[3] = (byte)(numEncodings & 0x00FF);
      return msg;
    }

    public static byte[] GetSetEncodingsMsg(RfbEncoding[] encodings)
    {
      byte[] msg = new byte[encodings.Length * 4];
      for(int i = 0; i < encodings.Length; i++)
      {
        msg[i * 4] = (byte)(((UInt32)encodings[i] & 0xFF000000) >> 24);
        msg[i * 4 + 1] = (byte)(((UInt32)encodings[i] & 0x00FF0000) >> 16);
        msg[i * 4 + 2] = (byte)(((UInt32)encodings[i] & 0x0000FF00) >> 8);
        msg[i * 4 + 3] = (byte)((UInt32)encodings[i] & 0x000000FF);
      }
      return msg;
    }

    public static UInt32 GetPixelFromData(byte[] data, UInt32 offset, byte bytesPp)
    {
      // Assuming Little Endian.
      switch(bytesPp)
      {
        case 1:
          return data[offset];
        case 2:
          return (UInt32)((UInt16)data[offset + 1] << 8 | (UInt16)data[offset]);
        case 4:
          return (UInt32)data[offset + 3] << 24 | (UInt32)data[offset + 2] << 16 | (UInt32)data[offset + 1] << 8 | (UInt32)data[offset];
        default:
          throw new ArgumentException("bytesPp", "Number of bytes per pixel not supported");
      }
    }

    // This class cannot be instantiated.
    private RfbProtoUtil()
    {}
  }
}
