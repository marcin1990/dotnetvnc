//  Copyright (C) Rocky Lo. All Rights Reserved.
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
using System.Threading;
using System.Collections;
using System.Drawing;
using System.Windows.Forms;
using Vnc.RfbProto;

namespace Vnc.Viewer
{
  /// <remarks>
  ///   This class is responsible for maintaining the server view.
  ///   This is also responsible for accepting user input and send to the server accordingly.
  /// </remars>
  internal abstract class View : Form
  {
    private const byte Delta = 50; // TODO: Find an optimal value.
    private const byte BgDelta = 100;  // TODO: Find an optimal value.

    private const UInt32 CtrlKey = 0x0000FFE3;
    private const UInt32 AltKey = 0x0000FFE9;
    private const UInt32 EnterKey = 0x0000FF0D;
    private const UInt32 DelKey = 0x0000FFFF;
    private const UInt32 EscKey = 0x0000FF1B;
    private const UInt32 ShiftKey = 0x0000FFE1;

    protected const byte NumTapHoldCircles = 8;
    protected byte TapHoldRadius = 3;
    protected byte BigCircleRadius = 15;
    protected byte TapHoldCircleRadius = 3;

    // .NET CF does not have Brushes, SystemBrushes, and Pens...
    protected Pen viewPen = new Pen(App.Black);
    private SolidBrush viewBrush = new SolidBrush(App.Black);
    private SolidBrush frameBufBrush = new SolidBrush(App.Black);

    protected System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    protected HScrollBar hScrlBar = new HScrollBar();
    protected VScrollBar vScrlBar = new VScrollBar();

    protected MainMenu menu = new MainMenu();
    protected MenuItem connMenu = new MenuItem();
    protected MenuItem newConnItem = new MenuItem();
    protected MenuItem refreshItem = new MenuItem();
    protected MenuItem closeConnItem = new MenuItem();
    protected MenuItem viewMenu = new MenuItem();
    private MenuItem rotateMenu = new MenuItem();
    private MenuItem cliScalingMenu = new MenuItem();
    private MenuItem pixelSizeMenu = new MenuItem();
    protected MenuItem keysMenu = new MenuItem();
    protected MenuItem optionsMenu = new MenuItem();
    protected MenuItem aboutItem = new MenuItem();

    private Bitmap frameBuf = null;
    private Graphics frameBufGraphics = null;
    private Conn conn = null;

    protected ConnOpts connOpts = null;
    private UInt16 rawFBWidth = 0;
    private UInt16 scaledFBWidth = 0;
    private UInt16 rawFBHeight = 0;
    private UInt16 scaledFBHeight = 0;

    protected int mouseX = 0;
    protected int mouseY = 0;
    protected bool leftBtnDown = false;
    protected bool rightBtnDown = false;
    protected UInt16 tapHoldCnt = 0;

    private bool toKeyUpCtrl = false;
    private bool toKeyUpAlt = false;

    protected EventHandler fullScrnHdr = null;
    protected EventHandler rotateHdr = null;
    protected EventHandler cliScalingHdr = null;
    protected EventHandler pixelSizeHdr = null;
    protected EventHandler keysHdr = null;

    private System.Windows.Forms.Timer bgTimer = new System.Windows.Forms.Timer();
    private Rectangle invalidRect = new Rectangle();
    private Object invalidRectLock = null; // This is the mutex protecting invalidRect.

    private bool toSendUpdReq = false;

    // "Real" coordinates => server coordinates.
    // "FrameBuf" coordinates => coordinates of in-memory buffer. This is the same as
    // "Real" coordinates when no rotation is effective.
    // "Scrn" coordinates => screen coordinates.

    private void RealToFrameBufXY(ref UInt16 x, ref UInt16 y, Orientation orientation, UInt16 frameBufWidth, UInt16 frameBufHeight)
    {
      UInt16 tempX = x;
      switch(orientation)
      {
        case Orientation.Landscape90:
          x = y;
          y = (UInt16)(frameBufHeight - 1 - tempX);
          break;
        case Orientation.Portrait180:
          x = (UInt16)(frameBufWidth - 1 - x);
          y = (UInt16)(frameBufHeight - 1 - y);
          break;
        case Orientation.Landscape270:
          x = (UInt16)(frameBufWidth - 1 - y);
          y = tempX;
          break;
      }
    }

    private void RealToFrameBufXY(ref UInt16 x, ref UInt16 y)
    {
      RealToFrameBufXY(ref x, ref y, connOpts.ViewOpts.Orientation, rawFBWidth, rawFBHeight);
    }

    private void RealToFrameBufRect(ref Rectangle rect)
    {
      Rectangle tempRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
          rect.X = tempRect.Y;
          rect.Y = rawFBHeight - tempRect.X - tempRect.Width;
          rect.Width = tempRect.Height;
          rect.Height = tempRect.Width;
          break;
        case Orientation.Portrait180:
          rect.X = rawFBWidth - tempRect.X - tempRect.Width;
          rect.Y = rawFBHeight - tempRect.Y - tempRect.Height;
          break;
        case Orientation.Landscape270:
          rect.X = rawFBWidth - tempRect.Y - tempRect.Height;
          rect.Y = tempRect.X;
          rect.Width = tempRect.Height;
          rect.Height = tempRect.Width;
          break;
      }
    }

    private void RealToScrnXY(ref int x, ref int y)
    {
      UInt16 x16 = (UInt16)x;
      UInt16 y16 = (UInt16)y;
      RealToFrameBufXY(ref x16, ref y16);
      x = x16;
      y = y16;
      FrameBufToScrnXY(ref x, ref y);
    }

    private void RealToScrnRect(ref Rectangle rect)
    {
      RealToFrameBufRect(ref rect);
      FrameBufToScrnRect(ref rect);
    }

    private void FrameBufToScrnXY(ref int x, ref int y)
    {
      if(connOpts.ViewOpts.CliScaling != CliScaling.None)
      {
        x = x * scaledFBWidth / rawFBWidth;
        y = y * scaledFBHeight / rawFBHeight;
      }

      // Scrolling
      if(hScrlBar.Visible)
        x -= hScrlBar.Value;
      if(vScrlBar.Visible)
        y -= vScrlBar.Value;

      Rectangle viewable = ViewableRect;
      x += viewable.X;
      y += viewable.Y;
    }

    private void FrameBufToScrnRect(ref Rectangle rect)
    {
      int x = rect.Left;
      int y = rect.Top;
      FrameBufToScrnXY(ref x, ref y);
      rect.X = x;
      rect.Y = y;
      if(connOpts.ViewOpts.CliScaling != CliScaling.None)
      {
        rect.Width = rect.Width * scaledFBWidth / rawFBWidth;
        rect.Height = rect.Height * scaledFBHeight / rawFBHeight;
      }
    }

    private void FrameBufToRealXY(ref UInt16 x, ref UInt16 y)
    {
      UInt16 tempX = x;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
          x = (UInt16)(rawFBHeight - 1 - y);
          y = tempX;
          break;
        case Orientation.Portrait180:
          x = (UInt16)(rawFBWidth - 1 - x);
          y = (UInt16)(rawFBHeight - 1 - y);
          break;
        case Orientation.Landscape270:
          x = y;
          y = (UInt16)(rawFBWidth - 1 - tempX);
          break;
      }
    }

    private void ScrnToFrameBufXY(ref int x, ref int y)
    {
      // Scrolling
      if(hScrlBar.Visible)
        x += hScrlBar.Value;
      if(vScrlBar.Visible)
        y += vScrlBar.Value;

      Rectangle viewable = ViewableRect;
      x -= viewable.X;
      y -= viewable.Y;

      if(connOpts.ViewOpts.CliScaling != CliScaling.None)
      {
        x = x * rawFBWidth / scaledFBWidth;
        y = y * rawFBHeight / scaledFBHeight;
      }

      x = Math.Max(x, 0);
      x = Math.Min(x, rawFBWidth - 1);
      y = Math.Max(y, 0);
      y = Math.Min(y, rawFBHeight - 1);
    }

    private void ScrnToFrameBufRect(ref Rectangle rect)
    {
      int x = rect.Left;
      int y = rect.Top;
      ScrnToFrameBufXY(ref x, ref y);
      rect.X = x;
      rect.Y = y;
      if(connOpts.ViewOpts.CliScaling != CliScaling.None)
      {
        rect.Width = rect.Width * rawFBWidth / scaledFBWidth;
        rect.Height = rect.Height * rawFBHeight / scaledFBHeight;
      }
    }

    private void ScrnToRealXY(ref int x, ref int y)
    {
      ScrnToFrameBufXY(ref x, ref y);
      UInt16 x16 = (UInt16)x;
      UInt16 y16 = (UInt16)y;
      FrameBufToRealXY(ref x16, ref y16);
      x = x16;
      y = y16;
    }

    internal Color this[UInt16 x, UInt16 y]
    {
      get
      {
        RealToFrameBufXY(ref x, ref y);
        return frameBuf.GetPixel(x, y);
      }
      set
      {
        RealToFrameBufXY(ref x, ref y);
        // The caller should lock the frame buffer before changing its content.
        // TODO: Anything faster?
        frameBuf.SetPixel(x, y, value);
      }
    }

    internal void FillRect(Rectangle rect, Color color)
    {
      RealToFrameBufRect(ref rect);
      frameBufBrush.Color = color;
      frameBufGraphics.FillRectangle(frameBufBrush, rect);
    }

    internal void CopyRect(Rectangle rect, UInt16 x, UInt16 y)
    {
      // TODO: Anything more efficient?
      Rectangle srcRect = new Rectangle(x, y, rect.Width, rect.Height);
      RealToFrameBufRect(ref rect);
      RealToFrameBufRect(ref srcRect);
      Bitmap image = new Bitmap(frameBuf);
      frameBufGraphics.DrawImage(image, rect.X, rect.Y, srcRect, GraphicsUnit.Pixel);
      image.Dispose();
    }

    internal void InvalidateRect(Rectangle rect)
    {
      // The timer event will pick this up and invalidate the area.
      Monitor.Enter(invalidRectLock);
      invalidRect = Rectangle.Union(invalidRect, rect);
      Monitor.Exit(invalidRectLock);
    }

    internal void SendUpdReq()
    {
      toSendUpdReq = true;
    }

    private void BgTicked(object sender, EventArgs e)
    {
      Monitor.Enter(invalidRectLock);
      Rectangle rect = invalidRect;
      invalidRect = Rectangle.Empty;
      Monitor.Exit(invalidRectLock);

      if(rect != Rectangle.Empty)
      {
        RealToScrnRect(ref rect);
        Invalidate(rect);
      }

      if(toSendUpdReq)
      {
        toSendUpdReq = false;
        try
        {
          conn.SendUpdReq(true);
        }
        catch(IOException)
        {
          Close();
        }
      }
    }

    internal void LockFrameBuf()
    {
      Monitor.Enter(this);
    }

    internal void UnlockFrameBuf()
    {
      Monitor.Exit(this);
    }

    // HScrlBarVal and VScrlBarVal are needed when we resize the window.
    // The idea is to try not to change the current position when the window is resized.
    // This is quite hard to archieve when rotation is effective.

    private UInt16 HScrlBarVal
    {
      get
      {
        if(!hScrlBar.Visible)
          return 0;

        switch(connOpts.ViewOpts.Orientation)
        {
          case Orientation.Portrait180:
          case Orientation.Landscape270:
            return (UInt16)(hScrlBar.Maximum + 1 - hScrlBar.Value - hScrlBar.LargeChange);
          default:
            return (UInt16)hScrlBar.Value;
        }
      }
      set
      {
        if(!hScrlBar.Visible)
          value = 0;

        switch(connOpts.ViewOpts.Orientation)
        {
          case Orientation.Portrait180:
          case Orientation.Landscape270:
            hScrlBar.Value = hScrlBar.Maximum + 1 - Math.Min(value, hScrlBar.Maximum + 1 - hScrlBar.LargeChange) - hScrlBar.LargeChange;
            break;
          default:
            hScrlBar.Value = Math.Min(value, hScrlBar.Maximum + 1 - hScrlBar.LargeChange);
            break;
        }
      }
    }

    private UInt16 VScrlBarVal
    {
      get
      {
        if(!vScrlBar.Visible)
          return 0;

        switch(connOpts.ViewOpts.Orientation)
        {
          case Orientation.Landscape90:
          case Orientation.Portrait180:
            return (UInt16)(vScrlBar.Maximum + 1 - vScrlBar.Value - vScrlBar.LargeChange);
          default:
            return (UInt16)vScrlBar.Value;
        }
      }
      set
      {
        if(!vScrlBar.Visible)
          value = 0;

        switch(connOpts.ViewOpts.Orientation)
        {
          case Orientation.Landscape90:
          case Orientation.Portrait180:
            vScrlBar.Value = vScrlBar.Maximum + 1 - Math.Min(value, vScrlBar.Maximum + 1 - vScrlBar.LargeChange) - vScrlBar.LargeChange;
            break;
          default:
            vScrlBar.Value = Math.Min(value, vScrlBar.Maximum + 1 - vScrlBar.LargeChange);
            break;
        }
      }
    }

    private void SetupScrlBars()
    {
      UInt16 oldHScrlBarVal = HScrlBarVal;
      UInt16 oldVScrlBarVal = VScrlBarVal;

      bool hScrlBarUnknown = false;
      bool vScrlBarUnknown = false;
      if(ClientSize.Width < scaledFBWidth)
        hScrlBar.Visible = true;
      else if(ClientSize.Width >= scaledFBWidth + vScrlBar.Width)
        hScrlBar.Visible = false;
      else
        hScrlBarUnknown = true;
      if(ClientSize.Height < scaledFBHeight)
        vScrlBar.Visible = true;
      else if(ClientSize.Height >= scaledFBHeight + hScrlBar.Height)
        vScrlBar.Visible = false;
      else
        vScrlBarUnknown = true;
      if(hScrlBarUnknown && vScrlBarUnknown)
      {
        hScrlBar.Visible = false;
        vScrlBar.Visible = false;
      }
      else if(hScrlBarUnknown)
        hScrlBar.Visible = vScrlBar.Visible;
      else if(vScrlBarUnknown)
        vScrlBar.Visible = hScrlBar.Visible;

      if(hScrlBar.Visible)
      {
        if(vScrlBar.Visible)
        {
          hScrlBar.Width = ClientSize.Width - vScrlBar.Width;
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              hScrlBar.Location = new Point(0, 0);
              break;
            case Orientation.Portrait180:
              hScrlBar.Location = new Point(vScrlBar.Width, 0);
              break;
            case Orientation.Landscape270:
              hScrlBar.Location = new Point(vScrlBar.Width, ClientSize.Height - hScrlBar.Height);
              break;
            default:
              hScrlBar.Location = new Point(0, ClientSize.Height - hScrlBar.Height);
              break;
          }
        }
        else
        {
          hScrlBar.Width = ClientSize.Width;
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
            case Orientation.Portrait180:
              hScrlBar.Location = new Point(0, 0);
              break;
            default:
              hScrlBar.Location = new Point(0, ClientSize.Height - hScrlBar.Height);
              break;
          }
        }
        hScrlBar.LargeChange = hScrlBar.Width;
        hScrlBar.SmallChange = hScrlBar.LargeChange / 10; // TODO: Make this configurable
        hScrlBar.Minimum = 0;
        hScrlBar.Maximum = scaledFBWidth - 1;
        HScrlBarVal = oldHScrlBarVal;
      }

      if(vScrlBar.Visible)
      {
        if(hScrlBar.Visible)
        {
          vScrlBar.Height = ClientSize.Height - hScrlBar.Height;
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              vScrlBar.Location = new Point(ClientSize.Width - vScrlBar.Width, hScrlBar.Height);
              break;
            case Orientation.Portrait180:
              vScrlBar.Location = new Point(0, hScrlBar.Height);
              break;
            case Orientation.Landscape270:
              vScrlBar.Location = new Point(0, 0);
              break;
            default:
              vScrlBar.Location = new Point(ClientSize.Width - vScrlBar.Width, 0);
              break;
          }
        }
        else
        {
          vScrlBar.Height = ClientSize.Height;
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Portrait180:
            case Orientation.Landscape270:
              vScrlBar.Location = new Point(0, 0);
              break;
            default:
              vScrlBar.Location = new Point(ClientSize.Width - vScrlBar.Width, 0);
              break;
          }
        }
        vScrlBar.LargeChange = vScrlBar.Height;
        vScrlBar.SmallChange = vScrlBar.LargeChange / 10; // TODO: Make this configurable
        vScrlBar.Minimum = 0;
        vScrlBar.Maximum = scaledFBHeight - 1;
        VScrlBarVal = oldVScrlBarVal;
      }
    }

    private void ResizeCore()
    {
      if(connOpts.ViewOpts.CliScaling == CliScaling.Auto)
      {
        scaledFBWidth = (UInt16)ClientSize.Width;
        scaledFBHeight = (UInt16)ClientSize.Height;
      }
      SetupScrlBars();
      Invalidate();

      if(connOpts.ViewOpts.CliScaling == CliScaling.Auto)
        return;

      // .NET CF does not support MaximumSize...
      if(WindowState == FormWindowState.Maximized)
      {
        if(FormBorderStyle != FormBorderStyle.None)
        {
          WindowState = FormWindowState.Normal;
          ClientSize = new Size(scaledFBWidth, scaledFBHeight);
        }
      }
      else
      {
        Rectangle usableRect = UsableRect;
        if(usableRect.Width > scaledFBWidth || usableRect.Height > scaledFBHeight)
        {
          int width = Math.Min(usableRect.Width, scaledFBWidth);
          int height = Math.Min(usableRect.Height, scaledFBHeight);
          width += vScrlBar.Visible? vScrlBar.Width : 0;
          height += hScrlBar.Visible? hScrlBar.Height : 0;
          ClientSize = new Size(width, height);
        }
      }
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);
      ResizeCore();
    }

    private void Scrled(object sender, EventArgs e)
    {
      Invalidate();
    }

    private void RotateClicked(object sender, EventArgs e)
    {
      Orientation newOrientation;
      string menuText = ((MenuItem)sender).Text;
      if(menuText == App.GetStr("Screen rotated clockwise"))
        newOrientation = Orientation.Landscape90;
      else if(menuText == App.GetStr("Screen rotated counter-clockwise"))
        newOrientation = Orientation.Landscape270;
      else if(menuText == App.GetStr("Upside down"))
        newOrientation = Orientation.Portrait180;
      else
        newOrientation = Orientation.Portrait;

      UInt16 newRawFBWidth;
      UInt16 newScaledFBWidth;
      UInt16 newRawFBHeight;
      UInt16 newScaledFBHeight;
      UInt16 newHScrlBarVal;
      UInt16 newVScrlBarVal;
      if(((connOpts.ViewOpts.Orientation == Orientation.Portrait || connOpts.ViewOpts.Orientation == Orientation.Portrait180) &&
          (newOrientation == Orientation.Landscape90 || newOrientation == Orientation.Landscape270)) ||
         ((connOpts.ViewOpts.Orientation == Orientation.Landscape90 || connOpts.ViewOpts.Orientation == Orientation.Landscape270) &&
          (newOrientation == Orientation.Portrait || newOrientation == Orientation.Portrait180)))
      {
        newRawFBWidth = rawFBHeight;
        newScaledFBWidth = scaledFBHeight;
        newRawFBHeight = rawFBWidth;
        newScaledFBHeight = scaledFBWidth;
        newHScrlBarVal = VScrlBarVal;
        newVScrlBarVal = HScrlBarVal;
      }
      else
      {
        newRawFBWidth = rawFBWidth;
        newScaledFBWidth = scaledFBWidth;
        newRawFBHeight = rawFBHeight;
        newScaledFBHeight = scaledFBHeight;
        newHScrlBarVal = HScrlBarVal;
        newVScrlBarVal = VScrlBarVal;
      }
      Bitmap newFrameBuf = new Bitmap(newRawFBWidth, newRawFBHeight);
      Graphics newFrameBufGraphics = Graphics.FromImage(newFrameBuf);

      // For rotation without sending an update request. See the comment below.
      /*
      UInt16 realWidth;
      UInt16 realHeight;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
        case Orientation.Landscape270:
          realWidth = rawFBHeight;
          realHeight = rawFBWidth;
          break;
        default:
          realWidth = rawFBWidth;
          realHeight = rawFBHeight;
          break;
      }
      */

      LockFrameBuf();
      // This "should" be faster when the network is slow because no extra network traffic is needed.
      // However, this is painfully slow on a PPC... probably due to memory constraint.
      // So at the moment we just send a full screen update request instead.
      /*
      for(UInt16 y = 0; y < realHeight; y++)
      {
        for(UInt16 x = 0; x < realWidth; x++)
        {
          UInt16 desX = x;
          UInt16 desY = y;
          RealToFrameBufXY(ref desX, ref desY, newOrientation, newRawFBWidth, newRawFBHeight);
          newFrameBuf.SetPixel(desX, desY, this[x, y]);
        }
      }
      */
      frameBufGraphics.Dispose();
      frameBuf.Dispose();
      frameBuf = newFrameBuf;
      frameBufGraphics = newFrameBufGraphics;
      connOpts.ViewOpts.Orientation = newOrientation;
      rawFBWidth = newRawFBWidth;
      rawFBHeight = newRawFBHeight;
      UnlockFrameBuf();

      // These values do not affect the other thread and do not need to be in the critical section.
      scaledFBWidth = newScaledFBWidth;
      scaledFBHeight = newScaledFBHeight;

      try
      {
        conn.SendUpdReq(false); // It is possible to NOT send this update. See the comment above regarding transforming pixel data.
      }
      catch(IOException)
      {
        Close();
      }

      ResizeCore();
      HScrlBarVal = newHScrlBarVal;
      VScrlBarVal = newVScrlBarVal;

      CheckRotate();
    }

    private void CloseClicked(object sender, EventArgs e)
    {
      Close();
    }

    private void FullScrn()
    {
      // The order of execution is very important.
      // It has a big impact on the window size when we quit full screen mode.
      Menu = null;
      FormBorderStyle = FormBorderStyle.None;
      WindowState = FormWindowState.Maximized; // This has to be after setting FormBorderStyle to have Resize work correctly.
    }

    private void QuitFullScrn()
    {
      // The order of execution is very important.
      // If the order is altered, the program can actually loop forever.
      WindowState = FormWindowState.Normal;
      FormBorderStyle = FormBorderStyle.Sizable;
      Menu = menu;
      ClientSize = new Size(scaledFBWidth, scaledFBHeight);
    }

    protected void ToggleFullScrn()
    {
      connOpts.ViewOpts.IsFullScrn = !connOpts.ViewOpts.IsFullScrn;
      if(connOpts.ViewOpts.IsFullScrn)
        FullScrn();
      else
        QuitFullScrn();
    }

    private void FullScrnClicked(object sender, EventArgs e)
    {
      ToggleFullScrn();
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      if(connOpts.ViewOpts.IsFullScrn)
        FullScrn();
      else
        QuitFullScrn();

      SetupScrlBars();
      HScrlBarVal = 0;
      VScrlBarVal = 0;
      Controls.Add(hScrlBar);
      Controls.Add(vScrlBar);
    }

    protected Rectangle UsableRect
    {
      get
      {
        Rectangle rect = new Rectangle();
        if(hScrlBar.Visible)
        {
          rect.Height = ClientRectangle.Height - hScrlBar.Height;
          if(hScrlBar.Top <= 0) // At the top
            rect.Y = hScrlBar.Height;
          else
            rect.Y = 0;
        }
        else
        {
          rect.Height = ClientRectangle.Height;
          rect.Y = 0;
        }
        if(vScrlBar.Visible)
        {
          rect.Width = ClientRectangle.Width - vScrlBar.Width;
          if(vScrlBar.Left <= 0) // At the left edge
            rect.X = vScrlBar.Width;
          else
            rect.X = 0;
        }
        else
        {
          rect.Width = ClientRectangle.Width;
          rect.X = 0;
        }
        return rect;
      }
    }

    protected Rectangle ViewableRect
    {
      get
      {
        Rectangle usable = UsableRect;
        Rectangle rect = new Rectangle();
        if(hScrlBar.Visible)
        {
          rect.X = usable.X;
          rect.Width = usable.Width;
        }
        else
        {
          rect.X = usable.X + (usable.Width - scaledFBWidth) / 2;
          rect.Width = scaledFBWidth;
        }
        if(vScrlBar.Visible)
        {
          rect.Y = usable.Y;
          rect.Height = usable.Height;
        }
        else
        {
          rect.Y = usable.Y + (usable.Height - scaledFBHeight) / 2;
          rect.Height = scaledFBHeight;
        }
        return rect;
      }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      Graphics graphics = e.Graphics;
      Rectangle usable = UsableRect;
      Rectangle viewable = ViewableRect;

      Rectangle destRect = Rectangle.Intersect(viewable, e.ClipRectangle);
      if(destRect != Rectangle.Empty)
      {
        Rectangle srcRect = new Rectangle(destRect.X, destRect.Y, destRect.Width, destRect.Height);
        ScrnToFrameBufRect(ref srcRect);
        LockFrameBuf();
        graphics.DrawImage(frameBuf, destRect, srcRect, GraphicsUnit.Pixel);
        UnlockFrameBuf();
      }

      if(hScrlBar.Visible && vScrlBar.Visible)
      {
        // Draw the little rectangle at the lower right corner of the form.
        Rectangle smallRect = new Rectangle(vScrlBar.Location.X, hScrlBar.Location.Y, vScrlBar.Width, hScrlBar.Height);
        if(e.ClipRectangle.IntersectsWith(smallRect))
        {
          viewBrush.Color = SystemColors.Control;
          graphics.FillRectangle(viewBrush, smallRect);
        }
      }
      else
      {
        // Draw the border.
        if(!viewable.Contains(e.ClipRectangle))
        {
          Region border = new Region(usable);
          border.Exclude(viewable);
          viewBrush.Color = App.Black;
          graphics.FillRegion(viewBrush, border);
        }
      }
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
      // Don't erase the background to reduce flicker.
    }

    protected abstract void Ticked(object sender, EventArgs e);

    protected void DrawTapHoldCircles(UInt16 numCircles, Color color)
    {
      Graphics graphics = CreateGraphics();

      Rectangle circleRect = new Rectangle();
      circleRect.Width = TapHoldCircleRadius * 2;
      circleRect.Height = TapHoldCircleRadius * 2;
      for(int i = 0; i < numCircles; i++)
      {
        double angle;
        switch(connOpts.ViewOpts.Orientation)
        {
          case Orientation.Landscape90:
            angle = -Math.PI;
            break;
          case Orientation.Portrait180:
            angle = Math.PI / 2;
            break;
          case Orientation.Landscape270:
            angle = 0;
            break;
          default:
            angle = -Math.PI / 2;
            break;
        }
        angle += 2 * Math.PI * i / NumTapHoldCircles;
        circleRect.X = (int)(Math.Cos(angle) * BigCircleRadius + mouseX - TapHoldCircleRadius);
        circleRect.Y = (int)(Math.Sin(angle) * BigCircleRadius + mouseY - TapHoldCircleRadius);
        viewBrush.Color = color;
        graphics.FillEllipse(viewBrush, circleRect);
        viewPen.Color = App.Black;
        graphics.DrawEllipse(viewPen, circleRect);
      }

      graphics.Dispose();
    }

    protected void InvalidateTapHoldCircles()
    {
      Rectangle rect = new Rectangle();
      rect.X = mouseX - BigCircleRadius - 2 * TapHoldCircleRadius;
      rect.Y = mouseY - BigCircleRadius - 2 * TapHoldCircleRadius;
      rect.Width = (BigCircleRadius + 2 * TapHoldCircleRadius) * 2;
      rect.Height = rect.Width;
      Invalidate(rect);
    }

    protected void OnMouseEvent(int x, int y, bool leftBtnDown, bool rightBtnDown)
    {
      if(connOpts.ViewOpts.ViewOnly)
        return;

      ScrnToRealXY(ref x, ref y);
      byte[] msg = RfbProtoUtil.GetPointerEventMsg((UInt16)x, (UInt16)y, leftBtnDown, rightBtnDown);
      try
      {
        conn.WriteBytes(msg, RfbCliMsgType.PointerEvent);
      }
      catch(IOException)
      {
        Close();
      }
    }

    protected void OnKeyEvent(UInt32 keyChar, bool isDown)
    {
      byte[] msg = RfbProtoUtil.GetKeyEventMsg(isDown, keyChar);
      conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
    }

    protected void OnKeyEvent(Keys keyCode, bool isDown)
    {
      if(connOpts.ViewOpts.ViewOnly)
        return;

      UInt32 keyChar = 0;
      bool isProcessed = true;
      switch(keyCode)
      {
        case Keys.Enter:
          keyChar = EnterKey;
          break;
        case Keys.Tab:
          keyChar = 0x0000FF09;
          break;
        case Keys.Escape:
          keyChar = EscKey;
          break;
        case Keys.ShiftKey:
          keyChar = 0x0000FFE1;
          break;
        case Keys.ControlKey:
          keyChar = CtrlKey;
          break;
        case Keys.Menu:
          keyChar = AltKey;
          break;
        case Keys.Insert:
          keyChar = 0x0000FF63;
          break;
        case Keys.Delete:
          keyChar = DelKey;
          break;
        case Keys.Home:
          keyChar = 0x0000FF50;
          break;
        case Keys.End:
          keyChar = 0x0000FF57;
          break;
        case Keys.PageUp:
          keyChar = 0x0000FF55;
          break;
        case Keys.PageDown:
          keyChar = 0x0000FF56;
          break;
        case Keys.Left:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              keyChar = 0x0000FF52;
              break;
            case Orientation.Portrait180:
              keyChar = 0x0000FF53;
              break;
            case Orientation.Landscape270:
              keyChar = 0x0000FF54;
              break;
            default:
              keyChar = 0x0000FF51;
              break;
          }
          break;
        case Keys.Up:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              keyChar = 0x0000FF53;
              break;
            case Orientation.Portrait180:
              keyChar = 0x0000FF54;
              break;
            case Orientation.Landscape270:
              keyChar = 0x0000FF51;
              break;
            default:
              keyChar = 0x0000FF52;
              break;
          }
          break;
        case Keys.Right:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              keyChar = 0x0000FF54;
              break;
            case Orientation.Portrait180:
              keyChar = 0x0000FF51;
              break;
            case Orientation.Landscape270:
              keyChar = 0x0000FF52;
              break;
            default:
              keyChar = 0x0000FF53;
              break;
          }
          break;
        case Keys.Down:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              keyChar = 0x0000FF51;
              break;
            case Orientation.Portrait180:
              keyChar = 0x0000FF52;
              break;
            case Orientation.Landscape270:
              keyChar = 0x0000FF53;
              break;
            default:
              keyChar = 0x0000FF54;
              break;
          }
          break;
        case Keys.F1:
        case Keys.F2:
        case Keys.F3:
        case Keys.F4:
        case Keys.F5:
        case Keys.F6:
        case Keys.F7:
        case Keys.F8:
        case Keys.F9:
        case Keys.F10:
        case Keys.F11:
        case Keys.F12:
          keyChar = 0x0000FFBE + ((UInt32)keyCode - (UInt32)Keys.F1);
          break;
        default:
          isProcessed = false;
          break;
      }

      if(isProcessed)
      {
        try
        {
          OnKeyEvent(keyChar, isDown);
          if(!isDown)
            SpecKeyUp();
        }
        catch(IOException)
        {
          Close();
        }
      }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
      base.OnKeyPress(e);
      if(e.Handled)
        return;

      if(connOpts.ViewOpts.ViewOnly)
        return;

      try
      {
        if(Char.IsLetterOrDigit(e.KeyChar) ||
           Char.IsPunctuation(e.KeyChar) ||
           Char.IsWhiteSpace(e.KeyChar) ||
           e.KeyChar == '~' || e.KeyChar == '`' || e.KeyChar == '<' || e.KeyChar == '>' ||
           e.KeyChar == '|' || e.KeyChar == '=' || e.KeyChar == '+' || e.KeyChar == '$' ||
           e.KeyChar == '^')
        {
          OnKeyEvent((UInt32)e.KeyChar, true);
          OnKeyEvent((UInt32)e.KeyChar, false);
        }
        else if(e.KeyChar == '\b')
        {
          UInt32 keyChar = (UInt32)e.KeyChar;
          keyChar |= 0x0000FF00;
          OnKeyEvent(keyChar, true);
          OnKeyEvent(keyChar, false);
        }
        SpecKeyUp();
      }
      catch(IOException)
      {
        Close();
      }
    }

    protected void SpecKeyUp()
    {
      if(toKeyUpAlt)
      {
        OnKeyEvent(AltKey, false);
        toKeyUpAlt = false;
      }
      if(toKeyUpCtrl)
      {
        OnKeyEvent(CtrlKey, false);
        toKeyUpCtrl = false;
      }
    }

    protected virtual void CheckRotate()
    {
      CheckRotate(rotateMenu);
    }

    protected void CheckRotate(Menu menu)
    {
      MenuItem normal = null;
      MenuItem rotateCW = null;
      MenuItem rotateCCW = null;
      MenuItem upsideDown = null;
      for(int i = 0; i < menu.MenuItems.Count; i++)
      {
        MenuItem item = menu.MenuItems[i];
        if(item.Text == App.GetStr("Portrait"))
          normal = item;
        else if(item.Text == App.GetStr("Screen rotated clockwise"))
          rotateCW = item;
        else if(item.Text == App.GetStr("Screen rotated counter-clockwise"))
          rotateCCW = item;
        else if(item.Text == App.GetStr("Upside down"))
          upsideDown = item;
      }
      normal.Checked = false;
      rotateCW.Checked = false;
      rotateCCW.Checked = false;
      upsideDown.Checked = false;

      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
          rotateCW.Checked = true;
          break;
        case Orientation.Portrait180:
          upsideDown.Checked = true;
          break;
        case Orientation.Landscape270:
          rotateCCW.Checked = true;
          break;
        default:
          normal.Checked = true;
          break;
      }
    }

    private void SetScaledDims()
    {
      scaledFBWidth = rawFBWidth;
      scaledFBHeight = rawFBHeight;
      switch(connOpts.ViewOpts.CliScaling)
      {
        case CliScaling.OneHalf:
          scaledFBWidth /= 2;
          scaledFBHeight /= 2;
          break;
        case CliScaling.OneThird:
          scaledFBWidth /= 3;
          scaledFBHeight /= 3;
          break;
        case CliScaling.OneFourth:
          scaledFBWidth /= 4;
          scaledFBHeight /= 4;
          break;
        case CliScaling.OneFifth:
          scaledFBWidth /= 5;
          scaledFBHeight /= 5;
          break;
        case CliScaling.Double:
          scaledFBWidth *= 2;
          scaledFBHeight *= 2;
          break;
        case CliScaling.Custom:
          if(connOpts.ViewOpts.Orientation == Orientation.Landscape90 ||
             connOpts.ViewOpts.Orientation == Orientation.Landscape270)
          {
            scaledFBWidth = connOpts.ViewOpts.CliScalingHeight;
            scaledFBHeight = connOpts.ViewOpts.CliScalingWidth;
          }
          else
          {
            scaledFBWidth = connOpts.ViewOpts.CliScalingWidth;
            scaledFBHeight = connOpts.ViewOpts.CliScalingHeight;
          }
          break;
        // We don't set the dimensions when Auto is used because we may not have ClientSize.
      }
    }

    private void CliScalingClicked(object sender, EventArgs e)
    {
      MenuItem item = (MenuItem)sender;
      if(item.Text == App.GetStr("None"))
        connOpts.ViewOpts.CliScaling = CliScaling.None;
      else if(item.Text == App.GetStr("Auto"))
        connOpts.ViewOpts.CliScaling = CliScaling.Auto;
      else if(item.Text == App.GetStr("1/2 of server"))
        connOpts.ViewOpts.CliScaling = CliScaling.OneHalf;
      else if(item.Text == App.GetStr("1/3 of server"))
        connOpts.ViewOpts.CliScaling = CliScaling.OneThird;
      else if(item.Text == App.GetStr("1/4 of server"))
        connOpts.ViewOpts.CliScaling = CliScaling.OneFourth;
      else if(item.Text == App.GetStr("1/5 of server"))
        connOpts.ViewOpts.CliScaling = CliScaling.OneFifth;
      else if(item.Text == App.GetStr("2 of server"))
        connOpts.ViewOpts.CliScaling = CliScaling.Double;
      else if(item.Text == App.GetStr("Custom..."))
      {
        CliScalingDlg dlg = new CliScalingDlg(connOpts.ViewOpts);
        dlg.ShowDialog();
      }

      UInt16 oldScaledFBWidth = scaledFBWidth;
      UInt16 oldScaledFBHeight = scaledFBHeight;
      UInt16 oldHScrlBarVal = HScrlBarVal;
      UInt16 oldVScrlBarVal = VScrlBarVal;

      SetScaledDims();
      ResizeCore();
      HScrlBarVal = (UInt16)(oldHScrlBarVal * scaledFBWidth / oldScaledFBWidth);
      VScrlBarVal = (UInt16)(oldVScrlBarVal * scaledFBHeight / oldScaledFBHeight);

      CheckCliScaling();
    }

    protected virtual void CheckCliScaling()
    {
      CheckCliScaling(cliScalingMenu);
    }

    protected void CheckCliScaling(Menu menu)
    {
      MenuItem noneItem = null;
      MenuItem autoItem = null;
      MenuItem oneHalfItem = null;
      MenuItem oneThirdItem = null;
      MenuItem oneFourthItem = null;
      MenuItem oneFifthItem = null;
      MenuItem doubleItem = null;
      MenuItem customItem = null;
      for(int i = 0; i < menu.MenuItems.Count; i++)
      {
        MenuItem item = menu.MenuItems[i];
        if(item.Text == App.GetStr("None"))
          noneItem = item;
        else if(item.Text == App.GetStr("Auto"))
          autoItem = item;
        else if(item.Text == App.GetStr("1/2 of server"))
          oneHalfItem = item;
        else if(item.Text == App.GetStr("1/3 of server"))
          oneThirdItem = item;
        else if(item.Text == App.GetStr("1/4 of server"))
          oneFourthItem = item;
        else if(item.Text == App.GetStr("1/5 of server"))
          oneFifthItem = item;
        else if(item.Text == App.GetStr("2 of server"))
          doubleItem = item;
        else if(item.Text == App.GetStr("Custom..."))
          customItem = item;
      }
      noneItem.Checked = false;
      autoItem.Checked = false;
      oneHalfItem.Checked = false;
      oneThirdItem.Checked = false;
      oneFourthItem.Checked = false;
      oneFifthItem.Checked = false;
      doubleItem.Checked = false;
      customItem.Checked = false;

      switch(connOpts.ViewOpts.CliScaling)
      {
        case CliScaling.Auto:
          autoItem.Checked = true;
          break;
        case CliScaling.OneHalf:
          oneHalfItem.Checked = true;
          break;
        case CliScaling.OneThird:
          oneThirdItem.Checked = true;
          break;
        case CliScaling.OneFourth:
          oneFourthItem.Checked = true;
          break;
        case CliScaling.OneFifth:
          oneFifthItem.Checked = true;
          break;
        case CliScaling.Double:
          doubleItem.Checked = true;
          break;
        case CliScaling.Custom:
          customItem.Checked = true;
          break;
        default:
          noneItem.Checked = true;
          break;
      }
    }

    private void NewConnClicked(object sender, EventArgs e)
    {
      App.NewConn();
    }

    private void ViewOnlyClicked(object sender, EventArgs e)
    {
      connOpts.ViewOpts.ViewOnly = !connOpts.ViewOpts.ViewOnly;
      for(int i = 0; i < optionsMenu.MenuItems.Count; i++)
      {
        MenuItem item = optionsMenu.MenuItems[i];
        if(item.Text == App.GetStr("View only"))
          item.Checked = connOpts.ViewOpts.ViewOnly;
      }
    }

    private void RefreshClicked(object sender, EventArgs e)
    {
      try
      {
        conn.SendUpdReq(false);
      }
      catch(IOException)
      {
        Close();
      }
    }

    private void PixelSizeClicked(object sender, EventArgs e)
    {
      MenuItem item = (MenuItem)sender;
      if(item.Text == App.GetStr("Server decides") && connOpts.ViewOpts.PixelSize != PixelSize.Unspec)
      {
        connOpts.ViewOpts.PixelSize = PixelSize.Unspec;
        conn.IsFmtChgPending = true;
      }
      else if(item.Text == App.GetStr("Force 8-bit") && connOpts.ViewOpts.PixelSize != PixelSize.Force8Bit)
      {
        connOpts.ViewOpts.PixelSize = PixelSize.Force8Bit;
        conn.IsFmtChgPending = true;
      }
      else if(item.Text == App.GetStr("Force 16-bit") && connOpts.ViewOpts.PixelSize != PixelSize.Force16Bit)
      {
        connOpts.ViewOpts.PixelSize = PixelSize.Force16Bit;
        conn.IsFmtChgPending = true;
      }
      CheckPixelSize();
    }

    protected virtual void CheckPixelSize()
    {
      CheckPixelSize(pixelSizeMenu);
    }

    protected void CheckPixelSize(Menu menu)
    {
      MenuItem serverDecides = null;
      MenuItem force8Bit = null;
      MenuItem force16Bit = null;
      for(int i = 0; i < menu.MenuItems.Count; i++)
      {
        MenuItem item = menu.MenuItems[i];
        if(item.Text == App.GetStr("Server decides"))
          serverDecides = item;
        else if(item.Text == App.GetStr("Force 8-bit"))
          force8Bit = item;
        else if(item.Text == App.GetStr("Force 16-bit"))
          force16Bit = item;
      }
      serverDecides.Checked = false;
      force8Bit.Checked = false;
      force16Bit.Checked = false;

      switch(connOpts.ViewOpts.PixelSize)
      {
        case PixelSize.Force8Bit:
          force8Bit.Checked = true;
          break;
        case PixelSize.Force16Bit:
          force16Bit.Checked = true;
          break;
        case PixelSize.Unspec:
          serverDecides.Checked = true;
          break;
      }
    }

    private void KeysClicked(object sender, EventArgs e)
    {
      if(connOpts.ViewOpts.ViewOnly)
        return;

      MenuItem item = (MenuItem)sender;
      try
      {
        if(item.Text == App.GetStr("Shift down"))
        {
          OnKeyEvent(ShiftKey, true);
        }
        else if(item.Text == App.GetStr("Shift up"))
        {
          OnKeyEvent(ShiftKey, false);
          SpecKeyUp();
        }
        else if(item.Text == App.GetStr("Ctrl-"))
        {
          OnKeyEvent(CtrlKey, true);
          toKeyUpCtrl = true;
        }
        else if(item.Text == App.GetStr("Alt-"))
        {
          OnKeyEvent(AltKey, true);
          toKeyUpAlt = true;
        }
        else if(item.Text == App.GetStr("Ctrl-Alt-"))
        {
          OnKeyEvent(CtrlKey, true);
          toKeyUpCtrl = true;
          OnKeyEvent(AltKey, true);
          toKeyUpAlt = true;
        }
        else if(item.Text == App.GetStr("Ctrl-Alt-Del"))
        {
          OnKeyEvent(CtrlKey, true);
          OnKeyEvent(AltKey, true);
          OnKeyEvent(DelKey, true);
          OnKeyEvent(DelKey, false);
          OnKeyEvent(AltKey, false);
          OnKeyEvent(CtrlKey, false);
          toKeyUpCtrl = false;
          toKeyUpAlt = false;
        }
        else if(item.Text == App.GetStr("Ctrl-Esc (Start Menu)"))
        {
          OnKeyEvent(CtrlKey, true);
          OnKeyEvent(EscKey, true);
          OnKeyEvent(EscKey, false);
          OnKeyEvent(CtrlKey, false);
          toKeyUpCtrl = false;
        }
      }
      catch(IOException)
      {
        Close();
      }
    }

    private void AboutClicked(object sender, EventArgs e)
    {
      App.AboutBox();
    }

    protected override void OnClosed(EventArgs e)
    {
      base.OnClosed(e);
      bgTimer.Enabled = false;
    }

    internal View(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base()
    {
      this.conn = conn;
      this.connOpts = connOpts;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
        case Orientation.Landscape270:
          rawFBWidth = height;
          rawFBHeight = width;
          break;
        default:
          rawFBWidth = width;
          rawFBHeight = height;
          break;
      }
      frameBuf = new Bitmap(rawFBWidth, rawFBHeight);
      frameBufGraphics = Graphics.FromImage(frameBuf);
      SetScaledDims();

      invalidRectLock = invalidRect;  // Box invalidRect as the lock.
      bgTimer.Tick += new EventHandler(BgTicked);
      bgTimer.Interval = BgDelta;
      bgTimer.Enabled = true;

      timer.Tick += new EventHandler(Ticked);
      timer.Interval = Delta;

      EventHandler scrlHdr = new EventHandler(Scrled);
      hScrlBar.ValueChanged += scrlHdr;
      vScrlBar.ValueChanged += scrlHdr;

      MenuItem item;
      fullScrnHdr = new EventHandler(FullScrnClicked);
      rotateHdr = new EventHandler(RotateClicked);
      cliScalingHdr = new EventHandler(CliScalingClicked);
      pixelSizeHdr = new EventHandler(PixelSizeClicked);
      keysHdr = new EventHandler(KeysClicked);

      connMenu.Text = App.GetStr("Connection");
      newConnItem.Text = App.GetStr("New...");
      newConnItem.Click += new EventHandler(NewConnClicked);
      refreshItem.Text = App.GetStr("Refresh whole screen");
      refreshItem.Click += new EventHandler(RefreshClicked);
      closeConnItem.Text = App.GetStr("Close");
      closeConnItem.Click += new EventHandler(CloseClicked);

      viewMenu.Text = App.GetStr("View");
      item = new MenuItem();
      item.Text = App.GetStr("Full screen");
      item.Checked = false; // If we see this we are not using full screen.
      item.Click += fullScrnHdr;
      viewMenu.MenuItems.Add(item);
      rotateMenu.Text = App.GetStr("Rotate");
      viewMenu.MenuItems.Add(rotateMenu);
      item = new MenuItem();
      item.Text = App.GetStr("Portrait");
      item.Click += rotateHdr;
      rotateMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated clockwise");
      item.Click += rotateHdr;
      rotateMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated counter-clockwise");
      item.Click += rotateHdr;
      rotateMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Upside down");
      item.Click += rotateHdr;
      rotateMenu.MenuItems.Add(item);
      CheckRotate(rotateMenu);
      cliScalingMenu.Text = App.GetStr("Client-side scaling");
      viewMenu.MenuItems.Add(cliScalingMenu);
      item = new MenuItem();
      item.Text = App.GetStr("None");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Auto");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/2 of server");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/3 of server");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/4 of server");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/5 of server");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("2 of server");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Custom...");
      item.Click += cliScalingHdr;
      cliScalingMenu.MenuItems.Add(item);
      CheckCliScaling(cliScalingMenu);
      pixelSizeMenu.Text = App.GetStr("Pixel size");
      viewMenu.MenuItems.Add(pixelSizeMenu);
      item = new MenuItem();
      item.Text = App.GetStr("Server decides");
      item.Click += pixelSizeHdr;
      pixelSizeMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Force 8-bit");
      item.Click += pixelSizeHdr;
      pixelSizeMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Force 16-bit");
      item.Click += pixelSizeHdr;
      pixelSizeMenu.MenuItems.Add(item);
      CheckPixelSize();

      keysMenu.Text = App.GetStr("Keys");
      item = new MenuItem();
      item.Text = App.GetStr("Shift down");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Shift up");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Alt-");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Alt-");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Alt-Del");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Esc (Start Menu)");
      item.Click += keysHdr;
      keysMenu.MenuItems.Add(item);

      optionsMenu.Text = App.GetStr("Options");
      item = new MenuItem();
      item.Text = App.GetStr("View only");
      item.Checked = connOpts.ViewOpts.ViewOnly;
      item.Click += new EventHandler(ViewOnlyClicked);
      optionsMenu.MenuItems.Add(item);

      aboutItem = new MenuItem();
      aboutItem.Text = App.GetStr("About");
      aboutItem.Click += new EventHandler(AboutClicked);
    }
  }
}
