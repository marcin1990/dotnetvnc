//  Copyright (c) 2004-2005 Rocky Lo. All Rights Reserved.
//  Copyright (C) 2002 Ultr@VNC Team Members. All Rights Reserved.
//  Copyright (C) 2000-2002 Const Kaplinsky. All Rights Reserved.
//  Copyright (C) 2002 RealVNC Ltd. All Rights Reserved.
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
// If the source code for the VNC system is not available from the place 
// whence you received this file, check http://www.uk.research.att.com/vnc or contact
// the authors on vnc@uk.research.att.com for information on obtaining it.

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using System.Collections;
using Vnc.RfbProto;
using Vnc.Security;

namespace Vnc.Viewer
{
  /// <remarks>
  ///   This class represents a connection to the server.
  ///   It prompts a user for the connection details, connects to the server,
  ///   creates a display, and then handles server messages.
  /// </remarks>
  internal class Conn
  {
    /// <summary>
    ///   This is the version number we report to the server. At the moment we
    ///   "borrow" this from the UltraVNC viewer.
    /// </summary>
    // TODO: Have we implemented everything needed in order to say that we are
    // compliant to this version?
    private const byte ViewerRfbMajorVer = 3;
    private const byte ViewerRfbMinorVer = 4;

    private static MethodInfo estConnInfo = null;
    private static MethodInfo relConnInfo = null;

    // These are cleaned up upon termination because they contain initial
    // values of a connection object.
    private ConnOpts opts = null;
    private ViewOpts viewOpts = null;

    private Int32 hConn = 0;

    // These are cleaned up upon termination because they contain
    // unmanaged resources.
    private TcpClient tcpClient = null;
    private NetworkStream stream = null;
    private BinaryReader reader = null;
    private BinaryWriter writer = null;

    // These are set each time a connection is run.
    private ServInit servInit = null;
    private string desktopName = null;
    private int majorVer = -1;
    private int minorVer = -1;
    private View view = null;
    private bool termBgThread = false;
    private byte bytesPp = 0;
    private byte bpp = 0;
    private byte depth = 0;
    private bool isBigEndian = false;
    private bool isTrueColor = false;
    private UInt16 redMax = 0;
    private UInt16 greenMax = 0;
    private UInt16 blueMax = 0;
    private byte redShift = 0;
    private byte greenShift = 0;
    private byte blueShift = 0;
    internal bool IsFmtChgPending = false;

    private EventHandler closeHdr = null;

    internal Conn()
    {
      closeHdr = new EventHandler(CloseView);
    }

    private void CloseView(object sender, EventArgs e)
    {
      // Do this in the main thread.
      view.Close();
    }

    private void ViewClosed(object sender, EventArgs e)
    {
      termBgThread = true;
      App.RemoveConn(this);
    }

    private void GetConnDetails()
    {
      SessDlg sessDlg;
      if(viewOpts == null)
        sessDlg = SessDlgFactory.Create();
      else
        sessDlg = SessDlgFactory.Create(viewOpts);
      if(sessDlg.ShowDialog() != DialogResult.OK)
        throw new QuietEx();
      opts = sessDlg.ConnOpts;
    }

    private void Connect()
    {
      try
      {
        try
        {
          IPAddress ipAdr = IPAddress.Parse(opts.Host);
          IPEndPoint ipEndPt = new IPEndPoint(ipAdr, opts.Port);
          tcpClient = new TcpClient();
          tcpClient.Connect(ipEndPt);
        }
        catch(FormatException)
        {
          tcpClient = new TcpClient(opts.Host, opts.Port);
        }
        stream = tcpClient.GetStream();
        reader = new BinaryReader(stream, Encoding.ASCII);
        writer = new BinaryWriter(stream, Encoding.ASCII);
      }
      catch(SocketException)
      {
        throw new WarnEx(App.GetStr("Unable to connect to the specified server!"));
      }
    }

    private string ReadAsciiStr(int numChars)
    {
      if(numChars == 0)
        return String.Empty;

      char[] chars = reader.ReadChars(numChars);
      return new string(chars);
    }

    private byte ReadByte()
    {
      return reader.ReadByte();
    }

    private byte[] ReadBytes(int numBytes)
    {
      if(numBytes == 0)
        return new byte[0];

      return reader.ReadBytes(numBytes);
    }

    private UInt32 ReadUInt32()
    {
      Int32 result = reader.ReadInt32();
      result = IPAddress.NetworkToHostOrder(result);
      return unchecked((UInt32)result);
    }

    private void WriteAsciiStr(string str)
    {
      writer.Write(str.ToCharArray());
    }

    private void WriteBytes(byte[] bytes)
    {
      writer.Write(bytes);
    }

    internal void WriteBytes(byte[] bytes, RfbCliMsgType msgType)
    {
      WriteBytes(bytes);
    }

    private void NegoProtoVer()
    {
      string verMsg = ReadAsciiStr(RfbSize.VerMsg);
      if(!RfbProtoUtil.IsValidVerMsg(verMsg))
        throw new WarnEx(App.GetStr("The server is not a VNC server!"));

      RfbProtoUtil.GetVerFromMsg(verMsg, out majorVer, out minorVer);

      if(majorVer == 3 && minorVer < 3)
        throw new WarnEx(App.GetStr("This server version is not supported!"));
      else
      {
        majorVer = ViewerRfbMajorVer;
        minorVer = ViewerRfbMinorVer;
      }

      verMsg = RfbProtoUtil.GetVerMsg(majorVer, minorVer);
      WriteAsciiStr(verMsg);
    }

    private void CreateDisp()
    {
      view = ViewFactory.Create(this, opts, servInit.Width, servInit.Height);
      view.Text = desktopName;
      view.Closed += new EventHandler(ViewClosed);
      view.Show();
    }

    private void Auth()
    {
      RfbAuthScheme authScheme = (RfbAuthScheme)ReadUInt32();
      switch(authScheme)
      {
        case RfbAuthScheme.ConnFailed:
          throw new WarnEx(App.GetStr("Connection failed at authentication!"));
        case RfbAuthScheme.NoAuth:
          break;
        case RfbAuthScheme.VncAuth:

          if(opts.Passwd == null || opts.Passwd.Length == 0)
          {
            AuthDlg authDlg = new AuthDlg();
            if(authDlg.ShowDialog() != DialogResult.OK)
              throw new QuietEx();

            if(authDlg.Passwd.Length <= 0)
              throw new WarnEx(App.GetStr("Empty password!"));
            opts.Passwd = authDlg.Passwd;
          }

          byte[] challenge = ReadBytes(RfbSize.AuthChallenge);

          VncAuth.EncryptBytes(challenge, opts.Passwd);
          WriteBytes(challenge);

          RfbAuthResult authResult = (RfbAuthResult)ReadUInt32();
          switch(authResult)
          {
            case RfbAuthResult.Ok:
              break;
            case RfbAuthResult.Failed:
              throw new WarnEx(App.GetStr("Authentication failed!"));
            case RfbAuthResult.TooMany:
              throw new WarnEx(App.GetStr("Too many!"));
            default:
              throw new WarnEx(App.GetStr("Authentication failed but reason unknown!"));
          }

          break;
        default:
          throw new WarnEx(App.GetStr("Authentication scheme unknown!"));
      }
    }

    private void SendCliInit()
    {
      byte[] msg = RfbProtoUtil.GetCliInitMsg(opts.ViewOpts.ShareServ);
      WriteBytes(msg);
    }

    private void ReadServInit()
    {
      byte[] msg = ReadBytes(RfbSize.ServInit);
      servInit = new ServInit(msg);
      desktopName = ReadAsciiStr((int)servInit.NameLen);
    }

    private void SetPixelFormat()
    {
      byte[] msg;
      if(opts.ViewOpts.PixelSize == PixelSize.Force8Bit)
      {
        bpp = 8;
        depth = 8;
        isBigEndian = false;
        isTrueColor = true;
        redMax = 7;
        greenMax = 7;
        blueMax = 3;
        redShift = 0;
        greenShift = 3;
        blueShift = 6;
        bytesPp = 1;
      }
      else if(opts.ViewOpts.PixelSize == PixelSize.Force16Bit || !servInit.IsTrueColor)
      {
        bpp = 16;
        depth = 16;
        isBigEndian = false;
        isTrueColor = true;
        redMax = 63;
        greenMax = 31;
        blueMax = 31;
        redShift = 0;
        greenShift = 6;
        blueShift = 11;
        bytesPp = 2;
      }
      else
      {
        bpp = servInit.Bpp;
        depth = servInit.Depth;
        isBigEndian = false;
        isTrueColor = servInit.IsTrueColor;
        redMax = servInit.RedMax;
        greenMax = servInit.GreenMax;
        blueMax = servInit.BlueMax;
        redShift = servInit.RedShift;
        greenShift = servInit.GreenShift;
        blueShift = servInit.BlueShift;
        bytesPp = (byte)((bpp + 7) / 8);
      }
      msg = RfbProtoUtil.GetSetPixelFormatMsg(bpp, depth, isBigEndian, isTrueColor, redMax, greenMax, blueMax, redShift, greenShift, blueShift);
      WriteBytes(msg, RfbCliMsgType.SetPixelFormat);
    }

    private void SetEncodings()
    {
      // We use a stack here.  The least favored encoding is pushed onto the stack first.
      Stack stack = new Stack();
      stack.Push(RfbEncoding.Raw);
      stack.Push(RfbEncoding.CopyRect);
      stack.Push(RfbEncoding.Rre);

      // We do support CoRRE encoding.
      // However, Ultra-VNC server is broken.
      // If we use CoRRE and server-side scaling is enabled, the server will
      // send us frame buffer updates with number of rectangles set incorrectly.
      // So the morale of the story is not to use CoRRE. If we know the server
      // does not scale the buffer, then we can enable CoRRE.
      // But this can all be avoided by using Hextile.
      //stack.Push(RfbEncoding.CoRre);

      stack.Push(RfbEncoding.Hex);

      byte[] msg;
      msg = RfbProtoUtil.GetSetEncodingsMsgHdr((UInt16)stack.Count);
      WriteBytes(msg, RfbCliMsgType.SetEncodings);
      RfbEncoding[] encodings = new RfbEncoding[stack.Count];
      stack.ToArray().CopyTo(encodings, 0);
      msg = RfbProtoUtil.GetSetEncodingsMsg(encodings);
      WriteBytes(msg);
    }

    private void EstConn()
    {
      // We use the connection manager on PPCs and Smartphones.
      if(App.DevCap.Lvl >= DevCapLvl.Desktop)
        return;

      try
      {
        if(estConnInfo == null)
        {
          Type webReqType = WebRequest.Create("http://localhost/").GetType();
          Type connMgrType = webReqType.Assembly.GetType("System.Net.ConnMgr");
          estConnInfo = connMgrType.GetMethod("EstablishConnectionForUrl", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static);
          relConnInfo = connMgrType.GetMethod("ReleaseConnection", BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Static);
        }
        Int32 ec = 0;
        Object[] parameters = new Object[] {"http://" + opts.Host + "/", ec};
        hConn = (Int32)estConnInfo.Invoke(null, parameters);
      }
      catch(Exception)
      {
        // Eat all exceptions.
      }
    }

    private void RelConn()
    {
      // We use the connection manager on PPCs and Smartphones.
      if(App.DevCap.Lvl >= DevCapLvl.Desktop)
        return;

      try
      {
        if(hConn == 0)
          return;
        relConnInfo.Invoke(null, new Object[] {hConn});
      }
      catch(Exception)
      {
        // Eat all exceptions.
      }
    }

    private void CleanUp()
    {
      // We don't cleanup majorVer, minorVer, etc. here because
      // we will always read them when run is called.
      if(writer != null)
      {
        writer.Close();
        writer = null;
      }
      if(reader != null)
      {
        reader.Close();
        reader = null;
      }
      if(stream != null)
      {
        stream.Close();
        stream = null;
      }
      if(tcpClient != null)
      {
        tcpClient.Close();
        tcpClient = null;
      }
      RelConn();
      opts = null;
      viewOpts = null;
    }

    internal void Run(ViewOpts viewOpts)
    {
      this.viewOpts = viewOpts;
      Run();
    }

    internal void Run(ConnOpts opts)
    {
      this.opts = opts;
      Run();
    }

    internal void Run()
    {
      try
      {
        // Get connection details from the user.
        if(opts == null)
          GetConnDetails();

        // Establish a connection with the connection manager.
        EstConn();

        // Make a connection to the server.
        Connect();

        // Negotiate the protocol version with the server.
        NegoProtoVer();

        // Authenticate the user.
        Auth();

        // Send client initialization message.
        SendCliInit();

        // Read server initialization message.
        ReadServInit();

        SetPixelFormat();

        SetEncodings();

        // Create a display locally.
        CreateDisp();

        // Ask the server to send us the whole desktop.
        SendUpdReq(0, 0, servInit.Width, servInit.Height, false);

        termBgThread = false;
        (new Thread(new ThreadStart(Start))).Start();

        App.AddConn(this);
      }
      catch(IOException)
      {
        CleanUp();
        MessageBox.Show(App.GetStr("Unable to communicate with the server!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
      catch(QuietEx)
      {
        CleanUp();
      }
      catch(WarnEx ex)
      {
        CleanUp();
        MessageBox.Show(ex.Message,
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
    }

    private void SendUpdReq(UInt16 x, UInt16 y, UInt16 width, UInt16 height, bool incremental)
    {
      byte[] msg = RfbProtoUtil.GetFrameBufUpdReqMsg(x, y, width, height, incremental);
      WriteBytes(msg, RfbCliMsgType.FrameBufUpdReq);
    }

    private Color GetColorFromData(byte[] data, UInt32 offset)
    {
      return GetColorFromPixel(RfbProtoUtil.GetPixelFromData(data, offset, bytesPp));
    }

    private Color GetColorFromPixel(UInt32 pixel)
    {
      int red = (int)((pixel >> redShift) & redMax) * 255 / redMax;
      int green = (int)((pixel >> greenShift) & greenMax) * 255 / greenMax;
      int blue = (int)((pixel >> blueShift) & blueMax) * 255 / blueMax;
      return Color.FromArgb(red, green, blue);
    }

    private void ReadRawRect(Rectangle rect)
    {
      byte[] rectBytes = ReadBytes(rect.Width * rect.Height * bytesPp);

      view.LockFrameBuf();

      // TODO: Can do this faster via native code?
      int curPos = 0;
      for(int i = 0; i < rect.Height; i++)
      {
        for(int j = 0; j < rect.Width; j++)
        {
          view[(UInt16)(rect.X + j), (UInt16)(rect.Y + i)] = GetColorFromData(rectBytes, (UInt32)curPos);
          curPos += bytesPp;
        }
      }

      view.UnlockFrameBuf();
    }

    private void ReadCopyRect(Rectangle rect)
    {
      byte[] rectBytes = ReadBytes(RfbSize.CopyRect);
      CopyRectMsg srcRect = new CopyRectMsg(rectBytes);

      view.LockFrameBuf();
      view.CopyRect(rect, srcRect.X, srcRect.Y);
      view.UnlockFrameBuf();
    }

    private void FillRreRect(Rectangle rect, Color bgColor, RreSubRectMsg[] subRects)
    {
      view.LockFrameBuf();

      view.FillRect(rect, bgColor);
      for(int i = 0; i < subRects.Length; i++)
      {
        Rectangle subRect = new Rectangle();
        subRect.X = rect.X + subRects[i].X;
        subRect.Y = rect.Y + subRects[i].Y;
        subRect.Width = subRects[i].Width;
        subRect.Height = subRects[i].Height;
        view.FillRect(subRect, GetColorFromPixel(subRects[i].Pixel));
      }

      view.UnlockFrameBuf();
    }

    private void ReadRreRectCore(Rectangle rect, bool isCompact)
    {
      UInt32 numSubRects = ReadUInt32();
      byte[] data = ReadBytes(bytesPp);
      Color bgColor = GetColorFromData(data, 0);

      int subRectSize = bytesPp;
      if(isCompact)
        subRectSize += RfbSize.CoRreSubRect;
      else
        subRectSize += RfbSize.RreSubRect;

      RreSubRectMsg[] subRects = new RreSubRectMsg[numSubRects];
      for(int i = 0; i < numSubRects; i++)
      {
        byte[] rectBytes = ReadBytes(subRectSize);
        subRects[i] = new RreSubRectMsg(rectBytes, bytesPp, isCompact);
      }

      FillRreRect(rect, bgColor, subRects);
    }

    private void ReadRreRect(Rectangle rect)
    {
      ReadRreRectCore(rect, false);
    }

    private void ReadCoRreRect(Rectangle rect)
    {
      ReadRreRectCore(rect, true);
    }

    private void ReadHexRect(Rectangle rect)
    {
      Color bgColor = App.Black;
      UInt32 fgPixel = 0;
      Rectangle tile = new Rectangle();

      for(tile.Y = rect.Y; tile.Y < rect.Bottom; tile.Y += 16)
      {
        for(tile.X = rect.X; tile.X < rect.Right; tile.X += 16)
        {
          tile.Width = 16;
          tile.Height = 16;
          tile.Width = Math.Min(tile.Right, rect.Right) - tile.Left;
          tile.Height = Math.Min(tile.Bottom, rect.Bottom) - tile.Top;

          RfbHexSubEncoding encoding = (RfbHexSubEncoding)ReadByte();

          if((encoding & RfbHexSubEncoding.Raw) != 0)
          {
            ReadRawRect(tile);
            view.InvalidateRect(tile);
            continue;
          }

          byte[] data;
          if((encoding & RfbHexSubEncoding.BgSpec) != 0)
          {
            data = ReadBytes(bytesPp);
            bgColor = GetColorFromData(data, 0);
          }

          if((encoding & RfbHexSubEncoding.FgSpec) != 0)
          {
            data = ReadBytes(bytesPp);
            fgPixel = RfbProtoUtil.GetPixelFromData(data, 0, bytesPp);
          }

          byte numSubRects = 0;
          if((encoding & RfbHexSubEncoding.AnySubRects) != 0)
            numSubRects = ReadByte();

          bool subRectsColored = ((encoding & RfbHexSubEncoding.SubRectsColored) != 0);

          int subRectSize = RfbSize.HexSubRect;
          subRectSize += subRectsColored? (int)bytesPp : 0;
          RreSubRectMsg[] subRects = new RreSubRectMsg[numSubRects];
          byte[] rectBytes = ReadBytes(subRectSize * numSubRects);
          for(int i = 0; i < numSubRects; i++)
            subRects[i] = new HexSubRectMsg(rectBytes, (UInt32)(i * subRectSize), bytesPp, subRectsColored, fgPixel);
          FillRreRect(tile, bgColor, subRects);
          view.InvalidateRect(tile);
        }
      }
    }

    internal void SendUpdReq(bool incremental)
    {
      if(IsFmtChgPending)
      {
        IsFmtChgPending = false;
        SetPixelFormat();
        SendUpdReq(0, 0, servInit.Width, servInit.Height, false);
      }
      else
        SendUpdReq(0, 0, servInit.Width, servInit.Height, incremental);
    }

    private void ReadScrnUpd()
    {
      byte[] msg = ReadBytes(RfbSize.FrameBufUpd - 1);
      UInt16 numRects = RfbProtoUtil.GetNumRectsFromFrameBufUpdMsg(msg);

      for(int i = 0; i < numRects; i++)
      {
        msg = ReadBytes(RfbSize.FrameBufUpdRectHdr);
        FrameBufUpdRectMsgHdr frameBufUpdRectHdr = new FrameBufUpdRectMsgHdr(msg);
        Rectangle rect = new Rectangle(frameBufUpdRectHdr.X, frameBufUpdRectHdr.Y, frameBufUpdRectHdr.Width, frameBufUpdRectHdr.Height);

        switch(frameBufUpdRectHdr.Encoding)
        {
          case RfbEncoding.Raw:
            ReadRawRect(rect);
            view.InvalidateRect(rect);
            break;
          case RfbEncoding.CopyRect:
            ReadCopyRect(rect);
            view.InvalidateRect(rect);
            break;
          case RfbEncoding.Rre:
            ReadRreRect(rect);
            view.InvalidateRect(rect);
            break;
          case RfbEncoding.CoRre:
            ReadCoRreRect(rect);
            view.InvalidateRect(rect);
            break;
          case RfbEncoding.Hex:
            ReadHexRect(rect);
            break;
          default:
            throw new WarnEx(App.GetStr("Server is using unknown encoding!"));
        }
      }

      // Do this in the UI thread so we always send data in one thread.
      view.SendUpdReq();
    }

    private void ReadServCutTxt()
    {
      // .NET CF does not have built-in support for the clipboard.
      // We ignore this message at the moment.

      byte[] msg = ReadBytes(RfbSize.ServCutTxt - 1);
      UInt32 len = RfbProtoUtil.GetLenFromServCutTxtMsg(msg);
      string txt = ReadAsciiStr((int)len);
    }

    /// <summary>
    ///   This is the entry point of the background thread that handles
    ///   server messages.
    /// </summary>
    private void Start()
    {
      // This is a background thread so the priority should be lower.
      Thread.CurrentThread.Priority = ThreadPriority.BelowNormal;

      try
      {
        while(!termBgThread)
        {
          // Yield to other threads so this thread does not dominate.
          Thread.Sleep(App.Delta);

          // Avoid the blocking ReadByte() if we have nothing to read.
          if(!stream.DataAvailable)
            continue;

          RfbServMsgType msgType = (RfbServMsgType)ReadByte();
          switch(msgType)
          {
            case RfbServMsgType.FrameBufUpd:
              ReadScrnUpd();
              break;
            case RfbServMsgType.SetColorMapEntries:
              throw new WarnEx(App.GetStr("The server is sending SetColorMapEntries!"));
            case RfbServMsgType.Bell:
              break;
            case RfbServMsgType.ServCutTxt:
              ReadServCutTxt();
              break;
            default:
              throw new WarnEx(App.GetStr("The server is sending unknown message!"));
          }
        }
      }
      catch(SocketException)
      {
        MessageBox.Show(App.GetStr("Unable to communicate with the server!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        view.Invoke(closeHdr);
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to communicate with the server!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        view.Invoke(closeHdr);
      }
      catch(QuietEx)
      {
        view.Invoke(closeHdr);
      }
      catch(WarnEx ex)
      {
        MessageBox.Show(ex.Message,
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        view.Invoke(closeHdr);
      }
      finally
      {
        CleanUp();
      }
    }
  }
}
