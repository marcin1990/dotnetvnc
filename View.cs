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
    protected MenuItem keysMenu = new MenuItem();
    protected MenuItem optionsMenu = new MenuItem();
    protected MenuItem aboutItem = new MenuItem();

    private Bitmap frameBuf = null;
    private Graphics frameBufGraphics = null;
    private Conn conn = null;

    protected ConnOpts connOpts = null;
    private UInt16 frameBufWidth = 0;
    private UInt16 frameBufHeight = 0;

    protected int mouseX = 0;
    protected int mouseY = 0;
    protected bool leftBtnDown = false;
    protected bool rightBtnDown = false;
    protected UInt16 tapHoldCnt = 0;

    private bool toKeyUpCtrl = false;
    private bool toKeyUpAlt = false;

    protected EventHandler rotateHdr = null;
    protected EventHandler fullScrnHdr = null;
    protected EventHandler keysHdr = null;

    private System.Windows.Forms.Timer bgTimer = new System.Windows.Forms.Timer();
    private Rectangle invalidRect = new Rectangle();
    private Object invalidRectLock = null; // This is the mutex protecting invalidRect.

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
      // Scrolling
      if(hScrlBar.Visible)
        x -= hScrlBar.Value;
      if(vScrlBar.Visible)
        y -= vScrlBar.Value;

      Rectangle viewable = ViewableRect;
      x += viewable.X;
      y += viewable.Y;
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
      // Scrolling
      if(hScrlBar.Visible)
        x += hScrlBar.Value;
      if(vScrlBar.Visible)
        y += vScrlBar.Value;

      Rectangle viewable = ViewableRect;
      x -= viewable.X;
      y -= viewable.Y;

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
      ClientSize = new Size(frameBufWidth, frameBufHeight);
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
          rect.X = usable.X + (usable.Width - frameBufWidth) / 2;
          rect.Width = frameBufWidth;
        }
        if(vScrlBar.Visible)
        {
          rect.Y = usable.Y;
          rect.Height = usable.Height;
        }
        else
        {
          rect.Y = usable.Y + (usable.Height - frameBufHeight) / 2;
          rect.Height = frameBufHeight;
        }
        return rect;
      }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);
      Graphics graphics = e.Graphics;
      Rectangle usable = UsableRect;

      int x = 0;
      int y = 0;
      FrameBufToScrnXY(ref x, ref y);
      Rectangle frameBufRect = new Rectangle(x, y, frameBufWidth, frameBufHeight);

      Rectangle destRect = Rectangle.Intersect(Rectangle.Intersect(frameBufRect, e.ClipRectangle), usable);
      if(destRect != Rectangle.Empty)
      {
        Rectangle srcRect = new Rectangle(destRect.X - x, destRect.Y - y, destRect.Width, destRect.Height);
        LockFrameBuf();
        graphics.DrawImage(frameBuf, destRect.X, destRect.Y, srcRect, GraphicsUnit.Pixel);
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
        if(!frameBufRect.Contains(e.ClipRectangle))
        {
          Region border = new Region(usable);
          border.Exclude(frameBufRect);
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
      CheckRotate(viewMenu);
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
      for(int i = 0; i < optionsMenu.MenuItems.Count; i++)
      {
        MenuItem item = optionsMenu.MenuItems[i];
        if(item.Text != App.GetStr("Pixel size"))
          continue;
        for(int j = 0; j < item.MenuItems.Count; j++)
        {
          MenuItem subItem = item.MenuItems[j];
          if(subItem.Text == App.GetStr("Server decides"))
            serverDecides = subItem;
          else if(subItem.Text == App.GetStr("Force 8-bit"))
            force8Bit = subItem;
          else if(subItem.Text == App.GetStr("Force 16-bit"))
            force16Bit = subItem;
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
        if(item.Text == App.GetStr("Ctrl-"))
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
        }
        else if(item.Text == App.GetStr("Ctrl-Esc (Start Menu)"))
        {
          OnKeyEvent(CtrlKey, true);
          OnKeyEvent(EscKey, true);
          OnKeyEvent(EscKey, false);
          OnKeyEvent(CtrlKey, false);
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
      MenuItem subItem;
      rotateHdr = new EventHandler(RotateClicked);
      fullScrnHdr = new EventHandler(FullScrnClicked);
      keysHdr = new EventHandler(KeysClicked);
      EventHandler pixelSizeHdr = new EventHandler(PixelSizeClicked);

      connMenu.Text = App.GetStr("Connection");
      newConnItem.Text = App.GetStr("New...");
      newConnItem.Click += new EventHandler(NewConnClicked);
      refreshItem.Text = App.GetStr("Refresh whole screen");
      refreshItem.Click += new EventHandler(RefreshClicked);
      closeConnItem.Text = App.GetStr("Close");
      closeConnItem.Click += new EventHandler(CloseClicked);

      viewMenu.Text = App.GetStr("View");
      item = new MenuItem();
      item.Text = App.GetStr("Full Screen");
      item.Checked = false; // If we see this we are not using full screen.
      item.Click += fullScrnHdr;
      viewMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = "-";
      viewMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Portrait");
      item.Click += rotateHdr;
      viewMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated clockwise");
      item.Click += rotateHdr;
      viewMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated counter-clockwise");
      item.Click += rotateHdr;
      viewMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Upside down");
      item.Click += rotateHdr;
      viewMenu.MenuItems.Add(item);
      CheckRotate(viewMenu);

      keysMenu.Text = App.GetStr("Keys");
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
      item.Text = App.GetStr("Pixel size");
      optionsMenu.MenuItems.Add(item);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Server decides");
      subItem.Click += pixelSizeHdr;
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Force 8-bit");
      subItem.Click += pixelSizeHdr;
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Force 16-bit");
      subItem.Click += pixelSizeHdr;
      item.MenuItems.Add(subItem);
      CheckPixelSize();
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
