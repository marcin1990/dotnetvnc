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
  internal class ViewPpc : ViewFullCf
  {
    private void SimRightClick()
    {
      timer.Enabled = false;
      InvalidateTapHoldCircles();

      // This stylus tap is for a right mouse click.
      leftBtnDown = false;

      // Simulate a right click.
      OnMouseEvent(mouseX, mouseY, leftBtnDown, true);
      OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
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

      if(connOpts.ViewOpts.IsFullScrn)
      {
        if(tapHoldCnt > 2 * NumTapHoldCircles)
          SimRightClick();
        else
        {
          tapHoldCnt++;
          Color color = (tapHoldCnt > NumTapHoldCircles)? App.Blue : App.Red;
          UInt16 numCircles = (UInt16)((tapHoldCnt > NumTapHoldCircles)? tapHoldCnt - NumTapHoldCircles : tapHoldCnt);
          DrawTapHoldCircles(numCircles, color);
        }
      }
      else
      {
        if(tapHoldCnt > NumTapHoldCircles)
          SimRightClick();
        else
        {
          tapHoldCnt++;
          DrawTapHoldCircles(tapHoldCnt, App.Blue);
        }
      }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
      base.OnMouseUp(e);

      if(timer.Enabled)
      {
        timer.Enabled = false;
        InvalidateTapHoldCircles();

        if(connOpts.ViewOpts.IsFullScrn && tapHoldCnt > NumTapHoldCircles)
        {
          // I don't have a clue why I need to lock the frame buffer.
          // But the PPC hangs if I don't do so before showing the context menu.
          LockFrameBuf();
          ctxMenu.Show(this, new Point(e.X, e.Y));
          UnlockFrameBuf();
        }
        else
        {
          // Send the "delayed" mouse event.
          OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
          OnMouseEvent(e.X, e.Y, false, false);
        }
      }
      else if(isSetSingleWinPending)
      {
        // If isSetSingleWinPending, then we don't send a mouse event.
        // Please note that timer is not enabled if we have isSetSingleWinPending.
        SetSingleWin(e.X, e.Y);
      }
      else
        OnMouseEvent(e.X, e.Y, false, false);

      mouseX = e.X;
      mouseY = e.Y;
      leftBtnDown = false;
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
      base.OnMouseDown(e);

      mouseX = e.X;
      mouseY = e.Y;
      leftBtnDown = true;

      // If isSetSingleWinPending, then we need only to keep track of the coordinates.
      if(isSetSingleWinPending)
        return;

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
        InvalidateTapHoldCircles();

        // Send the "delayed" mouse event.
        OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
      }

      mouseX = e.X;
      mouseY = e.Y;

      // See the comment regarding isSetSingleWinPending.
      if(isSetSingleWinPending)
        return;

      OnMouseEvent(mouseX, mouseY, leftBtnDown, false);
    }

    internal ViewPpc(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base(conn, connOpts, width, height)
    {
      if(App.DevCap.Res >= ResLvl.High)
      {
        TapHoldRadius *= 2;
        BigCircleRadius *= 2;
        TapHoldCircleRadius *= 2;
        hScrlBar.Height *= 2;
        vScrlBar.Width *= 2;
      }
    }
  }
}
