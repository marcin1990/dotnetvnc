//  Copyright (C) 2005, 2007 Rocky Lo. All Rights Reserved.
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
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Vnc.Viewer
{
  internal class ViewSp : View
  {
    private const byte CursorDelta = 100; // TODO: Find an optimal value.
    private const UInt16 InputDelta = 5000; // TODO: Find an optimal value.
    private const UInt16 MouseIdleDelta = 1000; // TODO: Find an optimal value.
    private const UInt16 LowSpeed = 3; // TODO: Find an optimal value.
    private const UInt16 NormalSpeed = 5; // TODO: Find an optimal value.
    private const UInt16 HighSpeed = 7; // TODO: Find an optimal value.

    private Timer cursorTimer = new Timer();
    private int xSpeed = 0;
    private int ySpeed = 0;

    private Timer inputTimer = new Timer();
    private TextBox inputBox = new TextBox();

    private Timer mouseIdleTimer = new Timer();

    private MenuItem mouseAccelModeMenu = new MenuItem();
    private EventHandler mouseAccelModeHdr = null;

    [DllImport("coredll.dll")]
    private static extern short GetKeyState(int keyCode);

    private bool IsCapsLocked()
    {
      return ((ushort)GetKeyState((int)Keys.CapsLock) & 0xffff) != 0;
    }

    private void LeftClicked(object sender, EventArgs e)
    {
      if(isSetSingleWinPending)
        SetSingleWin(mouseX, mouseY);
      else
      {
        OnMouseEvent(mouseX, mouseY, true, rightBtnDown);
        OnMouseEvent(mouseX, mouseY, false, rightBtnDown);
      }
    }

    private void RightClicked(object sender, EventArgs e)
    {
      if(isSetSingleWinPending)
        SetSingleWin(mouseX, mouseY);
      else
      {
        OnMouseEvent(mouseX, mouseY, leftBtnDown, true);
        OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
      }
    }

    private void CancelExitFullScrn()
    {
      timer.Enabled = false;
      InvalidateTapHoldCircles();
      // Send the "delayed" mouse event.
      OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
    }

    private void CursorTicked(object sender, EventArgs e)
    {
      // This is what we call friction.
      if(xSpeed > 0)
        xSpeed--;
      else if(xSpeed < 0)
        xSpeed++;
      if(ySpeed > 0)
        ySpeed--;
      else if(ySpeed < 0)
        ySpeed++;

      if(xSpeed == 0 && ySpeed == 0)
        return;
      else
      {
        if(connOpts.ViewOpts.SendMouseLocWhenIdle)
          ResetMouseIdleTimer();
      }

      Rectangle usable = UsableRect;

      mouseX += xSpeed;
      int tempX = mouseX;
      mouseX = Math.Max(Math.Min(mouseX, usable.Right - 1), usable.Left);
      tempX -= mouseX;
      mouseY += ySpeed;
      int tempY = mouseY;
      mouseY = Math.Max(Math.Min(mouseY, usable.Bottom - 1), usable.Top);
      tempY -= mouseY;

      if(tempX == 0 && tempY == 0)
      {
        // Erase the "old" cross and draw the new one.
        Rectangle rect = new Rectangle();
        rect.X = (xSpeed > 0)? mouseX - xSpeed : mouseX;
        rect.Y = (ySpeed > 0)? mouseY - ySpeed : mouseY;
        rect.X -= BigCircleRadius / 2;
        rect.Y -= BigCircleRadius / 2;
        rect.Width = Math.Abs(xSpeed) + BigCircleRadius;
        rect.Height = Math.Abs(ySpeed) + BigCircleRadius;
        Invalidate(rect);
      }
      else
      {
        if(hScrlBar.Visible && tempX != 0)
        {
          if((hScrlBar.Value <= 0 && tempX <= 0) || (hScrlBar.Value >= hScrlBar.Maximum + 1 - hScrlBar.LargeChange && tempX >= 0))
            xSpeed = 0;
          else
            hScrlBar.Value = Math.Max(0, Math.Min(hScrlBar.Value + tempX, hScrlBar.Maximum + 1 - hScrlBar.LargeChange));
        }
        if(vScrlBar.Visible && tempY != 0)
        {
          if((vScrlBar.Value <= 0 && tempY <= 0) || (vScrlBar.Value >= vScrlBar.Maximum + 1 - vScrlBar.LargeChange && tempY >= 0))
            ySpeed = 0;
          else
            vScrlBar.Value = Math.Max(0, Math.Min(vScrlBar.Value + tempY, vScrlBar.Maximum + 1 - vScrlBar.LargeChange));
        }
        Invalidate();
      }
    }

    protected override void Ticked(object sender, EventArgs e)
    {
      if(!timer.Enabled)
      {
        // I am not entirely sure whether this will ever occur.
        // One possibility is that the Timer event is queued
        // before OnMouseUp or OnMouseMove disables the timer.
        return;
      }

      if(tapHoldCnt > NumTapHoldCircles)
      {
        // We won't get a KeyUp after going back to window mode. Set the flag here.
        rightBtnDown = false;

        timer.Enabled = false;
        ToggleFullScrn();
      }
      else
      {
        tapHoldCnt++;
        DrawTapHoldCircles(tapHoldCnt, App.Blue);
      }
    }

    private void InputTicked(object sender, EventArgs e)
    {
      // We have to set Enabled to false before hiding inputBox.
      // Otherwise the input mode will not be set correctly after exiting extended input mode.
      inputBox.Enabled = false;
      inputBox.Visible = false;
      inputTimer.Enabled = false;
      Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      base.OnKeyDown(e);
      if(e.Handled)
        return;

      if(timer.Enabled && e.KeyCode != Keys.F2)
        CancelExitFullScrn();

      switch(e.KeyCode)
      {
        case Keys.F1:
          leftBtnDown = true;
          // If isSetSingleWinPending, we just keep track of the state.
          if(isSetSingleWinPending)
            return;
          OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
          break;
        case Keys.F2:
          if(timer.Enabled) // Repeated KeyDown in this case.
            return;
          rightBtnDown = true;
          // Refer to the comment of isSetSingleWinPending.
          if(isSetSingleWinPending)
            return;
          timer.Enabled = true; // Tap-and-Hold active.
          tapHoldCnt = 0;
          break;
        case Keys.Up:
          if(connOpts.ViewOpts.MouseAccelMode)
            ySpeed -= LowSpeed;
          else
          {
            switch(connOpts.ViewOpts.MouseSpeed)
            {
              case MouseSpeed.Low:
                ySpeed = -LowSpeed;
                break;
              case MouseSpeed.High:
                ySpeed = -HighSpeed;
                break;
              default:
                ySpeed = -NormalSpeed;
                break;
            }
          }
          break;
        case Keys.Down:
          if(connOpts.ViewOpts.MouseAccelMode)
            ySpeed += LowSpeed;
          else
          {
            switch(connOpts.ViewOpts.MouseSpeed)
            {
              case MouseSpeed.Low:
                ySpeed = LowSpeed;
                break;
              case MouseSpeed.High:
                ySpeed = HighSpeed;
                break;
              default:
                ySpeed = NormalSpeed;
                break;
            }
          }
          break;
        case Keys.Left:
          if(connOpts.ViewOpts.MouseAccelMode)
            xSpeed -= LowSpeed;
          else
          {
            switch(connOpts.ViewOpts.MouseSpeed)
            {
              case MouseSpeed.Low:
                xSpeed = -LowSpeed;
                break;
              case MouseSpeed.High:
                xSpeed = -HighSpeed;
                break;
              default:
                xSpeed = -NormalSpeed;
                break;
            }
          }
          break;
        case Keys.Right:
          if(connOpts.ViewOpts.MouseAccelMode)
            xSpeed += LowSpeed;
          else
          {
            switch(connOpts.ViewOpts.MouseSpeed)
            {
              case MouseSpeed.Low:
                xSpeed = LowSpeed;
                break;
              case MouseSpeed.High:
                xSpeed = HighSpeed;
                break;
              default:
                xSpeed = NormalSpeed;
                break;
            }
          }
          break;
        case Keys.F8:
          inputTimer.Enabled = true;
          inputBox.Text = String.Empty;
          switch(connOpts.ViewOpts.Orientation)
          {
            case Orientation.Landscape90:
              inputBox.Location = new Point(ClientRectangle.Width - inputBox.Width, 0);
              break;
            case Orientation.Portrait180:
              inputBox.Location = new Point(0, 0);
              break;
            case Orientation.Landscape270:
              inputBox.Location = new Point(0, ClientRectangle.Height - inputBox.Height);
              break;
            default:
              inputBox.Location = new Point(ClientRectangle.Width - inputBox.Width, ClientRectangle.Height - inputBox.Height);
              break;
          }
          inputBox.Visible = true;
          inputBox.Enabled = true;
          inputBox.Focus();
          break;
      }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
      base.OnKeyUp(e);
      if(e.Handled)
        return;

      if(timer.Enabled)
        CancelExitFullScrn();

      switch(e.KeyCode)
      {
        case Keys.F1:
          leftBtnDown = false;
          if(isSetSingleWinPending)
            SetSingleWin(mouseX, mouseY);
          else
            OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
          break;
        case Keys.F2:
          rightBtnDown = false;
          if(isSetSingleWinPending)
            SetSingleWin(mouseX, mouseY);
          else
            OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
          break;
        case Keys.P:
        case Keys.Q:
          if(!(e.Shift ^ IsCapsLocked()))
          {
            char c = (e.KeyCode == Keys.P) ? 'p' : 'q';
            // Fixing a problem for Smartphones with Qwerty keyboards.
            KeyPressEventArgs eventArgs = new KeyPressEventArgs(c);
            OnKeyPress(eventArgs);
          }
          break;
      }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
      if(e.Handled)
        return;

      // On a Smartphone, this means the back soft key.
      // Let's treat it as a backspace instead.
      if(e.KeyChar == (char)Keys.Escape)
      {
        e.Handled = true; // Set Handled to true will stop the Smartphone from going to another form.
        e = new KeyPressEventArgs('\b');
      }

      base.OnKeyPress(e);
    }

    private void ResetInputTimer()
    {
      // TODO: Which of the following statements are need to reset the timer?
      inputTimer.Enabled = false;
      inputTimer.Interval = 0;
      inputTimer.Interval = InputDelta;
      inputTimer.Enabled = true;
    }

    private void OnInputKeyDown(object sender, KeyEventArgs e)
    {
      if(e.Handled)
        return;

      // We don't enable the inputTimer until we get a KeyPress or KeyUp.
      inputTimer.Enabled = false;
      OnKeyEvent(e.KeyCode, true);
    }

    private void OnInputKeyUp(object sender, KeyEventArgs e)
    {
      if(e.Handled)
        return;

      ResetInputTimer();
      if(e.KeyCode != Keys.F8) // Equals F8 after we just opened the text box.
        OnKeyEvent(e.KeyCode, false);
    }

    private void OnInputKeyPress(object sender, KeyPressEventArgs e)
    {
      if(e.Handled)
        return;

      ResetInputTimer();
      OnKeyPress(e);
    }

    private void OnInputTextChanged(object sender, EventArgs e)
    {
      if(inputBox.TextLength > 0)
        inputBox.Text = String.Empty;
    }

    private void ResetMouseIdleTimer()
    {
      // TODO: Which of the following statements are need to reset the timer?
      mouseIdleTimer.Enabled = false;
      mouseIdleTimer.Interval = 0;
      mouseIdleTimer.Interval = MouseIdleDelta;
      mouseIdleTimer.Enabled = true;
    }

    private void MouseIdleTicked(object sender, EventArgs e)
    {
      OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
      mouseIdleTimer.Enabled = false; // If the cursor moves again, the timer will start again.
    }

    private void SendMouseLocWhenIdleClicked(object sender, EventArgs e)
    {
      connOpts.ViewOpts.SendMouseLocWhenIdle = !connOpts.ViewOpts.SendMouseLocWhenIdle;
      for(int i = 0; i < optionsMenu.MenuItems.Count; i++)
      {
        MenuItem item = optionsMenu.MenuItems[i];
        if(item.Text == App.GetStr("Send mouse location when idle"))
          item.Checked = connOpts.ViewOpts.SendMouseLocWhenIdle;
      }

      if(connOpts.ViewOpts.SendMouseLocWhenIdle)
        ResetMouseIdleTimer();
      else
        mouseIdleTimer.Enabled = false;
    }

    private void MouseAccelModeClicked(object sender, EventArgs e)
    {
      for(int i = 0; i < mouseAccelModeMenu.MenuItems.Count; i++)
        mouseAccelModeMenu.MenuItems[i].Checked = false;
      MenuItem item = (MenuItem)sender;
      item.Checked = true;
      if(item == mouseAccelModeMenu.MenuItems[0])
      {
        connOpts.ViewOpts.MouseAccelMode = true;
        connOpts.ViewOpts.MouseSpeed = MouseSpeed.Normal;
      }
      else if(item == mouseAccelModeMenu.MenuItems[1])
      {
        connOpts.ViewOpts.MouseAccelMode = false;
        connOpts.ViewOpts.MouseSpeed = MouseSpeed.Low;
      }
      else if(item == mouseAccelModeMenu.MenuItems[2])
      {
        connOpts.ViewOpts.MouseAccelMode = false;
        connOpts.ViewOpts.MouseSpeed = MouseSpeed.Normal;
      }
      else if(item == mouseAccelModeMenu.MenuItems[3])
      {
        connOpts.ViewOpts.MouseAccelMode = false;
        connOpts.ViewOpts.MouseSpeed = MouseSpeed.High;
      }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      Rectangle rect = new Rectangle();
      rect.X = mouseX - BigCircleRadius / 2;
      rect.Y = mouseY - BigCircleRadius / 2;
      rect.Width = BigCircleRadius - 1;
      rect.Height = BigCircleRadius - 1;
      if(!e.ClipRectangle.IntersectsWith(rect))
        return;

      Graphics graphics = e.Graphics;
      viewPen.Color = App.Red;
      graphics.DrawLine(viewPen, rect.Left, mouseY, rect.Right , mouseY);
      graphics.DrawLine(viewPen, mouseX, rect.Top, mouseX, rect.Bottom);
    }

    protected override void OnClosed(EventArgs e)
    {
      base.OnClosed(e);
      cursorTimer.Enabled = false;
      inputTimer.Enabled = false;
      mouseIdleTimer.Enabled = false;
    }

    internal ViewSp(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base(conn, connOpts, width, height)
    {
      mouseAccelModeHdr = new EventHandler(MouseAccelModeClicked);

      MenuItem item;
      MenuItem subItem;

      item = new MenuItem();
      item.Text = App.GetStr("Left click");
      item.Click += new EventHandler(LeftClicked);
      menu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Menu");
      menu.MenuItems.Add(item);

      subItem = new MenuItem();
      subItem.Text = App.GetStr("Right click");
      subItem.Click += new EventHandler(RightClicked);
      item.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = "-";
      item.MenuItems.Add(subItem);
      item.MenuItems.Add(connMenu);
      connMenu.MenuItems.Add(newConnItem);
      connMenu.MenuItems.Add(refreshItem);
      item.MenuItems.Add(viewMenu);
      item.MenuItems.Add(keysMenu);
      item.MenuItems.Add(optionsMenu);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Send mouse location when idle");
      subItem.Checked = connOpts.ViewOpts.SendMouseLocWhenIdle;
      subItem.Click += new EventHandler(SendMouseLocWhenIdleClicked);
      optionsMenu.MenuItems.Add(subItem);
      mouseAccelModeMenu.Text = App.GetStr("Mouse speed");
      optionsMenu.MenuItems.Add(mouseAccelModeMenu);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Acceleration on");
      subItem.Checked = connOpts.ViewOpts.MouseAccelMode;
      subItem.Click += mouseAccelModeHdr;
      mouseAccelModeMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Low");
      subItem.Checked = !connOpts.ViewOpts.MouseAccelMode && (connOpts.ViewOpts.MouseSpeed == MouseSpeed.Low);
      subItem.Click += mouseAccelModeHdr;
      mouseAccelModeMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("Normal");
      subItem.Checked = !connOpts.ViewOpts.MouseAccelMode && (connOpts.ViewOpts.MouseSpeed == MouseSpeed.Normal);
      subItem.Click += mouseAccelModeHdr;
      mouseAccelModeMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = App.GetStr("High");
      subItem.Checked = !connOpts.ViewOpts.MouseAccelMode && (connOpts.ViewOpts.MouseSpeed == MouseSpeed.High);
      subItem.Click += mouseAccelModeHdr;
      mouseAccelModeMenu.MenuItems.Add(subItem);
      subItem = new MenuItem();
      subItem.Text = "-";
      item.MenuItems.Add(subItem);
      item.MenuItems.Add(aboutItem);
      item.MenuItems.Add(closeConnItem);

      cursorTimer.Tick += new EventHandler(CursorTicked);
      cursorTimer.Interval = CursorDelta;
      cursorTimer.Enabled = true;

      mouseIdleTimer.Tick += new EventHandler(MouseIdleTicked);
      mouseIdleTimer.Interval = MouseIdleDelta;
      mouseIdleTimer.Enabled = connOpts.ViewOpts.SendMouseLocWhenIdle;

      Graphics graphics = CreateGraphics();

      inputTimer.Tick += new EventHandler(InputTicked);
      inputTimer.Interval = InputDelta;
      inputBox.Enabled = false;
      inputBox.Visible = false;
      inputBox.MaxLength = 1;
      inputBox.Size = graphics.MeasureString("M", Font).ToSize(); // M should be the widest character
      inputBox.Width += App.DialogSpacing; // Give the textbox some more space.
      inputBox.TextChanged += new EventHandler(OnInputTextChanged);
      inputBox.KeyDown += new KeyEventHandler(OnInputKeyDown);
      inputBox.KeyUp += new KeyEventHandler(OnInputKeyUp);
      inputBox.KeyPress += new KeyPressEventHandler(OnInputKeyPress);
      Controls.Add(inputBox);

      graphics.Dispose();
    }
  }
}
