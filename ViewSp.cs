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
using System.Drawing;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  internal class ViewSp : View
  {
    private const byte CursorDelta = 100; // TODO: Find an optimal value.
    private const UInt16 InputDelta = 5000; // TODO: Find an optimal value.
    private static readonly Pen RedPen = new Pen(App.Red);

    private Timer cursorTimer = new Timer();
    private int xSpeed = 0;
    private int ySpeed = 0;

    private Timer inputTimer = new Timer();
    private TextBox inputBox = new TextBox();

    private void LeftClicked(object sender, EventArgs e)
    {
      OnMouseEvent(mouseX, mouseY, true, false);
      OnMouseEvent(mouseX, mouseY, false, false);
    }

    private void RightClicked(object sender, EventArgs e)
    {
      OnMouseEvent(mouseX, mouseY, false, true);
      OnMouseEvent(mouseX, mouseY, false, false);
    }

    private void CancelExitFullScrn()
    {
      timer.Enabled = false;
      Invalidate(); // TODO: Calculate the area to invalidate.
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

      Rectangle usable = UsableRect;

      mouseX += xSpeed;
      int tempX = mouseX;
      mouseX = Math.Max(Math.Min(mouseX, usable.Right - 1), usable.Left);
      tempX -= mouseX;
      mouseY += ySpeed;
      int tempY = mouseY;
      mouseY = Math.Max(Math.Min(mouseY, usable.Bottom - 1), usable.Top);
      tempY -= mouseY;

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

      Invalidate(); // TODO: Calculate the area to invalidate.
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
        timer.Enabled = false;
        ToggleFullScrn();
      }
      else
      {
        tapHoldCnt++;
        DrawTapHoldCircles(tapHoldCnt, BlueBrush);
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
          OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
          break;
        case Keys.F2:
          if(timer.Enabled) // Repeated KeyDown in this case.
            return;
          rightBtnDown = true;
          timer.Enabled = true; // Tap-and-Hold active.
          tapHoldCnt = 0;
          break;
        case Keys.Up:
          ySpeed -= 3; // TODO: Find an optimal value.
          break;
        case Keys.Down:
          ySpeed += 3; // TODO: Find an optimal value.
          break;
        case Keys.Left:
          xSpeed -= 3; // TODO: Find an optimal value.
          break;
        case Keys.Right:
          xSpeed += 3; // TODO: Find an optimal value.
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

      if(e.KeyCode == Keys.F1)
      {
        leftBtnDown = false;
        OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
      }
      else if(e.KeyCode == Keys.F2)
      {
        rightBtnDown = false;
        OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
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
      OnKeyEvent(e.KeyCode, false);
    }

    private void OnInputKeyPress(object sender, KeyPressEventArgs e)
    {
      ResetInputTimer();
      OnKeyPress(e);
    }

    private void OnInputTextChanged(object sender, EventArgs e)
    {
      if(inputBox.TextLength > 0)
        inputBox.Text = String.Empty;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
      base.OnPaint(e);

      Graphics graphics = e.Graphics;
      graphics.DrawLine(RedPen, mouseX - BigCircleRadius / 2, mouseY, mouseX + BigCircleRadius / 2, mouseY);
      graphics.DrawLine(RedPen, mouseX, mouseY - BigCircleRadius / 2, mouseX, mouseY + BigCircleRadius / 2);
    }

    protected override void OnClosed(EventArgs e)
    {
      base.OnClosed(e);
      cursorTimer.Enabled = false;
      inputTimer.Enabled = false;
    }

    internal ViewSp(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base(conn, connOpts, width, height)
    {
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
      subItem.Text = "-";
      item.MenuItems.Add(subItem);
      item.MenuItems.Add(aboutItem);
      item.MenuItems.Add(closeConnItem);

      cursorTimer.Tick += new EventHandler(CursorTicked);
      cursorTimer.Interval = CursorDelta;
      cursorTimer.Enabled = true;

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
