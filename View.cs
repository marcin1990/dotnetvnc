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
  internal class View : Form
  {
    private const byte Delta = 50;
    private const byte TapHoldRadius = 2;
    private const byte NumTapHoldCircles = 8;
    private const byte BigCircleRadius = 15;
    private const byte TapHoldCircleRadius = 3;
    private const UInt32 CtrlKey = 0x0000FFE3;
    private const UInt32 AltKey = 0x0000FFE9;
    private const UInt32 DelKey = 0x0000FFFF;
    private const UInt32 EscKey = 0x0000FF1B;

    // .NET CF does not have SystemBrushes...
    private static readonly Brush CtrlBrush = new SolidBrush(SystemColors.Control);

    // .NET CF does not have Brushes and Pens...
    private static readonly Pen BlackPen = new Pen(App.Black);
    private static readonly Brush RedBrush = new SolidBrush(App.Red);
    private static readonly Brush BlueBrush = new SolidBrush(App.Blue);

    private System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
    private HScrollBar hScrlBar = new HScrollBar();
    private VScrollBar vScrlBar = new VScrollBar();
    private MainMenu menu = new MainMenu();
    private ContextMenu ctxMenu = new ContextMenu();
    private Bitmap frameBuf = null;
    private Graphics frameBufGraphics = null;
    private Conn conn = null;
    private Hashtable brushTable = new Hashtable();

    private ConnOpts connOpts = null;
    private UInt16 frameBufWidth = 0;
    private UInt16 frameBufHeight = 0;

    // Maintain the state of the mouse on our own.
    // On PPC the MouseButtons property does not reflect the state
    // correctly if ContextMenu is non-null.
    private int mouseX = 0;
    private int mouseY = 0;
    private bool leftBtnDown = false;
    private UInt16 tapHoldCnt = 0;

    private bool toKeyUpCtrl = false;
    private bool toKeyUpAlt = false;

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
      RealToFrameBufXY(ref x, ref y, connOpts.ViewOpts.Orientation, frameBufWidth, frameBufHeight);
    }

    private void RealToFrameBufRect(ref Rectangle rect)
    {
      Rectangle tempRect = new Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
          rect.X = tempRect.Y;
          rect.Y = frameBufHeight - tempRect.X - tempRect.Width;
          rect.Width = tempRect.Height;
          rect.Height = tempRect.Width;
          break;
        case Orientation.Portrait180:
          rect.X = frameBufWidth - tempRect.X - tempRect.Width;
          rect.Y = frameBufHeight - tempRect.Y - tempRect.Height;
          break;
        case Orientation.Landscape270:
          rect.X = frameBufWidth - tempRect.Y - tempRect.Height;
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
      int x = rect.Left;
      int y = rect.Top;
      FrameBufToScrnXY(ref x, ref y);
      rect.X = x;
      rect.Y = y;
    }

    private void FrameBufToScrnXY(ref int x, ref int y)
    {
      if(hScrlBar.Visible)
        x += -hScrlBar.Value;
      if(vScrlBar.Visible)
        y += -vScrlBar.Value;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
          y += hScrlBar.Visible? hScrlBar.Height : 0;
          break;
        case Orientation.Portrait180:
          x += vScrlBar.Visible? vScrlBar.Width : 0;
          y += hScrlBar.Visible? hScrlBar.Height : 0;
          break;
        case Orientation.Landscape270:
          x += vScrlBar.Visible? vScrlBar.Width : 0;
          break;
      }
    }

    private void FrameBufToRealXY(ref UInt16 x, ref UInt16 y)
    {
      UInt16 tempX = x;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
          x = (UInt16)(frameBufHeight - 1 - y);
          y = tempX;
          break;
        case Orientation.Portrait180:
          x = (UInt16)(frameBufWidth - 1 - x);
          y = (UInt16)(frameBufHeight - 1 - y);
          break;
        case Orientation.Landscape270:
          x = y;
          y = (UInt16)(frameBufWidth - 1 - tempX);
          break;
      }
    }

    private void ScrnToFrameBufXY(ref int x, ref int y)
    {
      if(hScrlBar.Visible)
      {
        x += hScrlBar.Value;
        if(connOpts.ViewOpts.Orientation == Orientation.Landscape90 ||
           connOpts.ViewOpts.Orientation == Orientation.Portrait180)
        {
          y -= hScrlBar.Height;
        }
      }
      if(vScrlBar.Visible)
      {
        y += vScrlBar.Value;
        if(connOpts.ViewOpts.Orientation == Orientation.Portrait180 ||
           connOpts.ViewOpts.Orientation == Orientation.Landscape270)
        {
          x -= vScrlBar.Width;
        }
      }
      x = Math.Max(x, 0);
      x = Math.Min(x, frameBufWidth - 1);
      y = Math.Max(y, 0);
      y = Math.Min(y, frameBufHeight - 1);
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

    private void Scrled(object sender, EventArgs e)
    {
      // TODO: Anything smarter?
      Invalidate();
    }

    internal void FillRect(Rectangle rect, Color color)
    {
      // Creating brushes is very costly in terms of processing time.
      // So we store all the brushes we have used in the past, and
      // try to reuse them if possible.
      // This gives a boost in performance, but it requires much more
      // memory.
      // Practically it seems the viewer still performs well when true
      // color is used.
      // TODO: Some "smart" algorithm can be used such as LRU, etc.

      SolidBrush brush = (SolidBrush)brushTable[color];
      bool isNewBrush = (brush == null);
      if(isNewBrush)
        brush = new SolidBrush(color);
      RealToFrameBufRect(ref rect);
      frameBufGraphics.FillRectangle(brush, rect);
      if(isNewBrush)
        brushTable.Add(color, brush);
    }

    internal void CopyRect(Rectangle rect, UInt16 x, UInt16 y)
    {
      Rectangle srcRect = new Rectangle(x, y, rect.Width, rect.Height);
      RealToFrameBufRect(ref rect);
      RealToFrameBufRect(ref srcRect);
      Bitmap image = new Bitmap(frameBuf);
      frameBufGraphics.DrawImage(image, rect.X, rect.Y, srcRect, GraphicsUnit.Pixel);
      image.Dispose();
    }

    internal void InvalidateRect(Rectangle rect)
    {
      // TODO: This does not work. It hangs on PPC. Need to find out why.
      // RealToScrnRect(ref rect);
      // Invalidate(rect);
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
      if(ClientSize.Width < frameBufWidth)
        hScrlBar.Visible = true;
      else if(ClientSize.Width >= frameBufWidth + vScrlBar.Width)
        hScrlBar.Visible = false;
      else
        hScrlBarUnknown = true;
      if(ClientSize.Height < frameBufHeight)
        vScrlBar.Visible = true;
      else if(ClientSize.Height >= frameBufHeight + hScrlBar.Height)
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
        hScrlBar.Maximum = frameBufWidth - 1;
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
        vScrlBar.Maximum = frameBufHeight - 1;
        VScrlBarVal = oldVScrlBarVal;
      }
    }

    private void ResizeCore()
    {
      SetupScrlBars();
      // TODO: calculate the area to be redrawn.
      Invalidate();

      // .NET CF does not support MaximumSize...
      if(WindowState == FormWindowState.Maximized)
      {
        if(FormBorderStyle != FormBorderStyle.None)
        {
          WindowState = FormWindowState.Normal;
          ClientSize = new Size(frameBufWidth, frameBufHeight);
        }
      }
      else
      {
        int usefulWidth = ClientSize.Width;
        int usefulHeight = ClientSize.Height;
        usefulWidth -= vScrlBar.Visible? vScrlBar.Width : 0;
        usefulHeight -= hScrlBar.Visible? hScrlBar.Height : 0;
        if(usefulWidth > frameBufWidth || usefulHeight > frameBufHeight)
        {
          int width = Math.Min(usefulWidth, frameBufWidth);
          int height = Math.Min(usefulHeight, frameBufHeight);
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

      UInt16 newFrameBufWidth;
      UInt16 newFrameBufHeight;
      UInt16 newHScrlBarVal;
      UInt16 newVScrlBarVal;
      if(((connOpts.ViewOpts.Orientation == Orientation.Portrait || connOpts.ViewOpts.Orientation == Orientation.Portrait180) &&
          (newOrientation == Orientation.Landscape90 || newOrientation == Orientation.Landscape270)) ||
         ((connOpts.ViewOpts.Orientation == Orientation.Landscape90 || connOpts.ViewOpts.Orientation == Orientation.Landscape270) &&
          (newOrientation == Orientation.Portrait || newOrientation == Orientation.Portrait180)))
      {
        newFrameBufWidth = frameBufHeight;
        newFrameBufHeight = frameBufWidth;
        newHScrlBarVal = VScrlBarVal;
        newVScrlBarVal = HScrlBarVal;
      }
      else
      {
        newFrameBufWidth = frameBufWidth;
        newFrameBufHeight = frameBufHeight;
        newHScrlBarVal = HScrlBarVal;
        newVScrlBarVal = VScrlBarVal;
      }
      Bitmap newFrameBuf = new Bitmap(newFrameBufWidth, newFrameBufHeight);
      Graphics newFrameBufGraphics = Graphics.FromImage(newFrameBuf);

      UInt16 realWidth;
      UInt16 realHeight;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
        case Orientation.Landscape270:
          realWidth = frameBufHeight;
          realHeight = frameBufWidth;
          break;
        default:
          realWidth = frameBufWidth;
          realHeight = frameBufHeight;
          break;
      }

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
          RealToFrameBufXY(ref desX, ref desY, newOrientation, newFrameBufWidth, newFrameBufHeight);
          newFrameBuf.SetPixel(desX, desY, this[x, y]);
        }
      }
      */
      frameBufGraphics.Dispose();
      frameBuf.Dispose();
      frameBuf = newFrameBuf;
      frameBufGraphics = newFrameBufGraphics;
      connOpts.ViewOpts.Orientation = newOrientation;
      frameBufWidth = newFrameBufWidth;
      frameBufHeight = newFrameBufHeight;
      UnlockFrameBuf();
      conn.ScrnRefresh(); // It is possible to NOT send this update. See the comment above regarding transforming pixel data.

      ResizeCore();
      HScrlBarVal = newHScrlBarVal;
      VScrlBarVal = newVScrlBarVal;

      for(int i = 0; i < menu.MenuItems.Count; i++)
        if(menu.MenuItems[i].Text == App.GetStr("View"))
          CheckRotate(menu.MenuItems[i]);
      CheckRotate(ctxMenu);
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
      ClientSize = new Size(frameBufWidth, frameBufHeight);
    }

    private void FullScrnClicked(object sender, EventArgs e)
    {
      connOpts.ViewOpts.IsFullScrn = !connOpts.ViewOpts.IsFullScrn;
      if(connOpts.ViewOpts.IsFullScrn)
        FullScrn();
      else
        QuitFullScrn();
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

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      Graphics graphics = e.Graphics;

      int x = 0;
      int y = 0;
      FrameBufToScrnXY(ref x, ref y);
      LockFrameBuf();
      graphics.DrawImage(frameBuf, x, y);
      UnlockFrameBuf();

      // Don't draw on the small rectangle at the bottom right corner
      if(hScrlBar.Visible && vScrlBar.Visible)
        graphics.FillRectangle(CtrlBrush, vScrlBar.Location.X, hScrlBar.Location.Y, vScrlBar.Width, hScrlBar.Height);
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
      // Don't erase the background to reduce flicker.
    }

    internal void LockFrameBuf()
    {
      Monitor.Enter(this);
    }

    internal void UnlockFrameBuf()
    {
      Monitor.Exit(this);
    }

    private void OnMouseEvent(int x, int y, bool leftBtnDown, bool rightBtnDown)
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

    private void DrawTapHoldCircles(UInt16 numCircles, Brush brush)
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
        graphics.FillEllipse(brush, circleRect);
        graphics.DrawEllipse(BlackPen, circleRect);
      }

      graphics.Dispose();
    }

    private void SimRightClick()
    {
      timer.Enabled = false;
      Invalidate(); // TODO: Calculate the area to invalidate.

      // It was a tap-and-hold and the left button was not clicked.
      leftBtnDown = false;

      // Simulate a right click.
      OnMouseEvent(mouseX, mouseY, leftBtnDown, true);
      OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
    }

    private void Ticked(object sender, EventArgs e)
    {
      if(!timer.Enabled)
      {
        // I am not entirely sure whether this will ever occur.
        // One possibility is that the Timer event is queued
        // before OnMouseUp or OnMouseMove disables the timer.
        return;
      }

      if(connOpts.ViewOpts.IsFullScrn)
      {
        if(tapHoldCnt > 2 * NumTapHoldCircles)
          SimRightClick();
        else
        {
          tapHoldCnt++;
          Brush brush = (tapHoldCnt > NumTapHoldCircles)? BlueBrush : RedBrush;
          UInt16 numCircles = (UInt16)((tapHoldCnt > NumTapHoldCircles)? tapHoldCnt - NumTapHoldCircles : tapHoldCnt);
          DrawTapHoldCircles(numCircles, brush);
        }
      }
      else
      {
        if(tapHoldCnt > NumTapHoldCircles)
          SimRightClick();
        else
        {
          tapHoldCnt++;
          DrawTapHoldCircles(tapHoldCnt, BlueBrush);
        }
      }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      // We only monitor the left mouse button.
      if((e.Button & MouseButtons.Left) == 0)
        return;

      int mouseX = this.mouseX;
      int mouseY = this.mouseY;
      bool leftBtnDown = this.leftBtnDown;

      this.mouseX = e.X;
      this.mouseY = e.Y;
      this.leftBtnDown = false;

      if(timer.Enabled)
      {
        timer.Enabled = false;
        Invalidate(); // TODO: Calculate the area to invalidate.

        if(connOpts.ViewOpts.IsFullScrn && tapHoldCnt > NumTapHoldCircles)
        {
          // I don't have a clue why I need to lock the frame buffer.
          // But the PPC hangs if I don't do so before showing the context menu.
          LockFrameBuf();
          ctxMenu.Show(this, new Point(this.mouseX, this.mouseY));
          UnlockFrameBuf();
        }
        else
        {
          // Send the "delayed" mouse event.
          OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
          OnMouseEvent(this.mouseX, this.mouseY, this.leftBtnDown, false);
        }
      }
      else
        OnMouseEvent(this.mouseX, this.mouseY, this.leftBtnDown, false);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      // We only monitor the left mouse button.
      if((e.Button & MouseButtons.Left) == 0)
        return;

      mouseX = e.X;
      mouseY = e.Y;
      leftBtnDown = true;
      timer.Enabled = true; // Tap-and-Hold active.
      tapHoldCnt = 0;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      base.OnMouseMove(e);

      if(timer.Enabled)
      {
        if(e.X > mouseX - TapHoldRadius && e.X < mouseX + TapHoldRadius &&
           e.Y > mouseY - TapHoldRadius && e.Y < mouseY + TapHoldRadius)
          return; // Tap-and-Hold is active and valid. Take no action.

        // "Far away" from where the user taps, dismiss tap-and-hold.
        timer.Enabled = false;
        Invalidate(); // TODO: Calculate the area to invalidate.

        // Send the "delayed" mouse event.
        OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
      }

      mouseX = e.X;
      mouseY = e.Y;
      OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
    }

    private void OnKeyEvent(KeyEventArgs e, bool isDown)
    {
      if(connOpts.ViewOpts.ViewOnly)
        return;

      UInt32 key = 0;
      bool isProcessed = true;
      switch(e.KeyCode)
      {
        case Keys.Enter:
          key = 0x0000FF0D;
          break;
        case Keys.Tab:
          key = 0x0000FF09;
          break;
        case Keys.Escape:
          key = EscKey;
          break;
        case Keys.ShiftKey:
          key = 0x0000FFE1;
          break;
        case Keys.ControlKey:
          key = CtrlKey;
          break;
        case Keys.Menu:
          key = AltKey;
          break;
        case Keys.Insert:
          key = 0x0000FF63;
          break;
        case Keys.Delete:
          key = DelKey;
          break;
        case Keys.Home:
          key = 0x0000FF50;
          break;
        case Keys.End:
          key = 0x0000FF57;
          break;
        case Keys.PageUp:
          key = 0x0000FF55;
          break;
        case Keys.PageDown:
          key = 0x0000FF56;
          break;
        case Keys.Left:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              key = 0x0000FF52;
              break;
            case Orientation.Portrait180:
              key = 0x0000FF53;
              break;
            case Orientation.Landscape270:
              key = 0x0000FF54;
              break;
            default:
              key = 0x0000FF51;
              break;
          }
          break;
        case Keys.Up:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              key = 0x0000FF53;
              break;
            case Orientation.Portrait180:
              key = 0x0000FF54;
              break;
            case Orientation.Landscape270:
              key = 0x0000FF51;
              break;
            default:
              key = 0x0000FF52;
              break;
          }
          break;
        case Keys.Right:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              key = 0x0000FF54;
              break;
            case Orientation.Portrait180:
              key = 0x0000FF51;
              break;
            case Orientation.Landscape270:
              key = 0x0000FF52;
              break;
            default:
              key = 0x0000FF53;
              break;
          }
          break;
        case Keys.Down:
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              key = 0x0000FF51;
              break;
            case Orientation.Portrait180:
              key = 0x0000FF52;
              break;
            case Orientation.Landscape270:
              key = 0x0000FF53;
              break;
            default:
              key = 0x0000FF54;
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
          key = 0x0000FFBE + ((UInt32)e.KeyCode - (UInt32)Keys.F1);
          break;
        default:
          isProcessed = false;
          break;
      }

      if(isProcessed)
      {
        try
        {
          byte[] msg = RfbProtoUtil.GetKeyEventMsg(isDown, key);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          SpecKeyUp();
        }
        catch(IOException)
        {
          Close();
        }
      }
    }

    private void SpecKeyUp()
    {
      byte[] msg;
      if(toKeyUpAlt)
      {
        msg = RfbProtoUtil.GetKeyEventMsg(false, AltKey);
        conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
        toKeyUpAlt = false;
      }
      if(toKeyUpCtrl)
      {
        msg = RfbProtoUtil.GetKeyEventMsg(false, CtrlKey);
        conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
        toKeyUpCtrl = false;
      }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
      base.OnKeyUp(e);
      if(e.Handled)
        return;

      OnKeyEvent(e, false);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      base.OnKeyDown(e);
      if(e.Handled)
        return;

      OnKeyEvent(e, true);
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
        byte[] msg;
        if(Char.IsLetterOrDigit(e.KeyChar) ||
           Char.IsPunctuation(e.KeyChar) ||
           Char.IsWhiteSpace(e.KeyChar) ||
           e.KeyChar == '~' || e.KeyChar == '`' || e.KeyChar == '<' || e.KeyChar == '>' ||
           e.KeyChar == '|' || e.KeyChar == '=' || e.KeyChar == '+' || e.KeyChar == '$' ||
           e.KeyChar == '^')
        {
          msg = RfbProtoUtil.GetKeyEventMsg(true, (UInt32)e.KeyChar);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, (UInt32)e.KeyChar);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
        }
        else if(e.KeyChar == '\b')
        {
          UInt32 key = (UInt32)e.KeyChar;
          key |= 0x0000FF00;
          msg = RfbProtoUtil.GetKeyEventMsg(true, key);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, key);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
        }
        SpecKeyUp();
      }
      catch(IOException)
      {
        Close();
      }
    }

    private void CheckRotate(Menu menu)
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

    private void NewConnClicked(object sender, EventArgs e)
    {
      App.NewConn();
    }

    private void ViewOnlyClicked(object sender, EventArgs e)
    {
      connOpts.ViewOpts.ViewOnly = !connOpts.ViewOpts.ViewOnly;
      for(int i = 0; i < menu.MenuItems.Count; i++)
      {
        MenuItem item = menu.MenuItems[i];
        if(item.Text != App.GetStr("Options"))
          continue;
        for(int j = 0; j < item.MenuItems.Count; j++)
        {
          MenuItem subItem = item.MenuItems[j];
          if(subItem.Text == App.GetStr("View only"))
            subItem.Checked = connOpts.ViewOpts.ViewOnly;
        }
      }
    }

    private void RefreshClicked(object sender, EventArgs e)
    {
      conn.ScrnRefresh();
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

    private void CheckPixelSize()
    {
      MenuItem serverDecides = null;
      MenuItem force8Bit = null;
      MenuItem force16Bit = null;
      for(int i = 0; i < menu.MenuItems.Count; i++)
      {
        MenuItem item = menu.MenuItems[i];
        if(item.Text != App.GetStr("Options"))
          continue;
        for(int j = 0; j < item.MenuItems.Count; j++)
        {
          MenuItem subItem = item.MenuItems[j];
          if(subItem.Text != App.GetStr("Pixel size"))
            continue;
          for(int k = 0; k < subItem.MenuItems.Count; k++)
          {
            MenuItem smallItem = subItem.MenuItems[k];
            if(smallItem.Text == App.GetStr("Server decides"))
              serverDecides = smallItem;
            else if(smallItem.Text == App.GetStr("Force 8-bit"))
              force8Bit = smallItem;
            else if(smallItem.Text == App.GetStr("Force 16-bit"))
              force16Bit = smallItem;
          }
        }
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
        byte[] msg;
        if(item.Text == App.GetStr("Ctrl-"))
        {
          msg = RfbProtoUtil.GetKeyEventMsg(true, CtrlKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          toKeyUpCtrl = true;
        }
        else if(item.Text == App.GetStr("Alt-"))
        {
          msg = RfbProtoUtil.GetKeyEventMsg(true, AltKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          toKeyUpAlt = true;
        }
        else if(item.Text == App.GetStr("Ctrl-Alt-"))
        {
          msg = RfbProtoUtil.GetKeyEventMsg(true, CtrlKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          toKeyUpCtrl = true;
          msg = RfbProtoUtil.GetKeyEventMsg(true, AltKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          toKeyUpAlt = true;
        }
        else if(item.Text == App.GetStr("Ctrl-Alt-Del"))
        {
          msg = RfbProtoUtil.GetKeyEventMsg(true, CtrlKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(true, AltKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(true, DelKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, DelKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, AltKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, CtrlKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
        }
        else if(item.Text == App.GetStr("Ctrl-Esc (Start Menu)"))
        {
          msg = RfbProtoUtil.GetKeyEventMsg(true, CtrlKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(true, EscKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, EscKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
          msg = RfbProtoUtil.GetKeyEventMsg(false, CtrlKey);
          conn.WriteBytes(msg, RfbCliMsgType.KeyEvent);
        }
      }
      catch(IOException)
      {
        Close();
      }
    }

    private void SaveConnOpts(bool savePwd)
    {
      SaveFileDialog dlg = new SaveFileDialog();
      dlg.Filter = App.GetStr("VNC files (*.vncxml)|*.vncxml|All files (*.*)|*.*");
      if(dlg.ShowDialog() != DialogResult.OK)
        return;

      try
      {
        connOpts.Save(dlg.FileName, savePwd);
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to save!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
    }

    private void SaveConnOptsClicked(object sender, EventArgs e)
    {
      SaveConnOpts(false);
    }

    private void SaveConnOptsPwdClicked(object sender, EventArgs e)
    {
      SaveConnOpts(true);
    }

    private void LoadConnOptsClicked(object sender, EventArgs e)
    {
      OpenFileDialog dlg = new OpenFileDialog();
      dlg.Filter = App.GetStr("VNC files (*.vncxml)|*.vncxml|All files (*.*)|*.*");
      if(dlg.ShowDialog() != DialogResult.OK)
        return;

      App.NewConn(dlg.FileName);
    }

    private void AboutClicked(object sender, EventArgs e)
    {
      App.AboutBox();
    }

    internal View(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base()
    {
      this.conn = conn;
      this.connOpts = connOpts;
      switch(connOpts.ViewOpts.Orientation)
      {
        case Orientation.Landscape90:
        case Orientation.Landscape270:
          frameBufWidth = height;
          frameBufHeight = width;
          break;
        default:
          frameBufWidth = width;
          frameBufHeight = height;
          break;
      }
      frameBuf = new Bitmap(frameBufWidth, frameBufHeight);
      frameBufGraphics = Graphics.FromImage(frameBuf);

      timer.Tick += new EventHandler(Ticked);
      timer.Interval = Delta;

      EventHandler scrlHdr = new EventHandler(Scrled);
      hScrlBar.ValueChanged += scrlHdr;
      vScrlBar.ValueChanged += scrlHdr;

      MenuItem item;
      MenuItem subItem;
      MenuItem smallItem;
      EventHandler rotateHdr = new EventHandler(RotateClicked);
      EventHandler pixelSizeHdr = new EventHandler(PixelSizeClicked);

      item = new MenuItem();
      item.Text = App.GetStr("Connection");
      menu.MenuItems.Add(item);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("New...");
      subItem.Click += new EventHandler(NewConnClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Open...");
      subItem.Click += new EventHandler(LoadConnOptsClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Save as...");
      subItem.Click += new EventHandler(SaveConnOptsClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Save as (with password)...");
      subItem.Click += new EventHandler(SaveConnOptsPwdClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Refresh whole screen");
      subItem.Click += new EventHandler(RefreshClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Close");
      subItem.Click += new EventHandler(CloseClicked);
      item.MenuItems.Add(subItem);

      item = new MenuItem();
      item.Text = App.GetStr("View");
      menu.MenuItems.Add(item);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Full Screen");
      subItem.Checked = false; // If we see this we are not using full screen.
      subItem.Click += new EventHandler(FullScrnClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Portrait");
      subItem.Click += rotateHdr;
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Screen rotated clockwise");
      subItem.Click += rotateHdr;
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Screen rotated counter-clockwise");
      subItem.Click += rotateHdr;
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Upside down");
      subItem.Click += rotateHdr;
      item.MenuItems.Add(subItem);
      CheckRotate(item);

      item = new MenuItem();
      item.Text = App.GetStr("Keys");
      menu.MenuItems.Add(item);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-");
      subItem.Click += new EventHandler(KeysClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Alt-");
      subItem.Click += new EventHandler(KeysClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-Alt-");
      subItem.Click += new EventHandler(KeysClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-Alt-Del");
      subItem.Click += new EventHandler(KeysClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-Esc (Start Menu)");
      subItem.Click += new EventHandler(KeysClicked);
      item.MenuItems.Add(subItem);

      item = new MenuItem();
      item.Text = App.GetStr("Options");
      menu.MenuItems.Add(item);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Pixel size");
      item.MenuItems.Add(subItem);
      smallItem = new MenuItem();
      smallItem.Text = App.GetStr("Server decides");
      smallItem.Click += pixelSizeHdr;
      subItem.MenuItems.Add(smallItem);
      smallItem = new MenuItem();
      smallItem.Text = App.GetStr("Force 8-bit");
      smallItem.Click += pixelSizeHdr;
      subItem.MenuItems.Add(smallItem);
      smallItem = new MenuItem();
      smallItem.Text = App.GetStr("Force 16-bit");
      smallItem.Click += pixelSizeHdr;
      subItem.MenuItems.Add(smallItem);
      CheckPixelSize();
      subItem = new MenuItem();
      subItem.Text = App.GetStr("View only");
      subItem.Checked = connOpts.ViewOpts.ViewOnly;
      subItem.Click += new EventHandler(ViewOnlyClicked);
      item.MenuItems.Add(subItem);

      item = new MenuItem();
      item.Text = App.GetStr("Help");
      menu.MenuItems.Add(item);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("About");
      subItem.Click += new EventHandler(AboutClicked);
      item.MenuItems.Add(subItem);

      subItem = new MenuItem();
      subItem.Text = App.GetStr("Full Screen");
      subItem.Checked = true; // If we see this we are using full screen.
      subItem.Click += new EventHandler(FullScrnClicked);
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Portrait");
      subItem.Click += rotateHdr;
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Screen rotated clockwise");
      subItem.Click += rotateHdr;
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Screen rotated counter-clockwise");
      subItem.Click += rotateHdr;
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Upside down");
      subItem.Click += rotateHdr;
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-");
      subItem.Click += new EventHandler(KeysClicked);
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Alt-");
      subItem.Click += new EventHandler(KeysClicked);
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-Alt-");
      subItem.Click += new EventHandler(KeysClicked);
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-Alt-Del");
      subItem.Click += new EventHandler(KeysClicked);
      ctxMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Ctrl-Esc (Start Menu)");
      subItem.Click += new EventHandler(KeysClicked);
      ctxMenu.MenuItems.Add(subItem);
      CheckRotate(ctxMenu);
    }
  }
}
