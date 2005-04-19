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
using System.Drawing;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  internal class ViewDt : ViewFullCf
  {
    private void GetMouseButtons(out bool leftBtnDown, out bool rightBtnDown)
    {
      leftBtnDown = ((MouseButtons & MouseButtons.Left) != 0);
      rightBtnDown = ((MouseButtons & MouseButtons.Right) != 0);
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
        InvalidateTapHoldCircles();
        ctxMenu.Show(this, new Point(mouseX, mouseY));
      }
      else
      {
        tapHoldCnt++;
        DrawTapHoldCircles(tapHoldCnt, App.Blue);
      }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      if(timer.Enabled)
      {
        timer.Enabled = false;
        InvalidateTapHoldCircles();
        // Send the "delayed" mouse event.
        OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
      }

      mouseX = e.X;
      mouseY = e.Y;
      GetMouseButtons(out leftBtnDown, out rightBtnDown);

      // Send the coordinates to the server.
      // Notice that if isSetSingleWinPending, then the timer will not be enabled.
      if(isSetSingleWinPending)
      {
        SetSingleWin(mouseX, mouseY);
        return;
      }

      OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      if(timer.Enabled)
      {
        timer.Enabled = false;
        InvalidateTapHoldCircles();
        // Send the "delayed" mouse event.
        OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
      }

      mouseX = e.X;
      mouseY = e.Y;
      GetMouseButtons(out leftBtnDown, out rightBtnDown);

      // If isSetSingleWinPending, we just need to store the state of the mouse and we don't
      // send a mouse event to the server.
      if(isSetSingleWinPending)
        return;

      if(connOpts.ViewOpts.IsFullScrn && ((e.Button & MouseButtons.Right) != 0))
      {
        timer.Enabled = true; // Tap-and-Hold active.
        tapHoldCnt = 0;
      }
      else
        OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
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
        InvalidateTapHoldCircles();

        // Send the "delayed" mouse event.
        OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
      }

      mouseX = e.X;
      mouseY = e.Y;
      GetMouseButtons(out leftBtnDown, out rightBtnDown);

      // See the comment regarding isSetSingleWinPending.
      if(isSetSingleWinPending)
        return;

      OnMouseEvent(mouseX, mouseY, leftBtnDown, rightBtnDown);
    }

    internal ViewDt(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base(conn, connOpts, width, height)
    {}
  }
}
