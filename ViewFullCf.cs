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
using System.Windows.Forms;

namespace Vnc.Viewer
{
  internal abstract class ViewFullCf : View
  {
    protected ContextMenu ctxMenu = new ContextMenu();

    private void OnKeyEvent(Keys keyCode, bool isDown)
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

    protected override void OnKeyUp(KeyEventArgs e)
    {
      base.OnKeyUp(e);
      if(e.Handled)
        return;

      OnKeyEvent(e.KeyCode, false);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
      base.OnKeyDown(e);
      if(e.Handled)
        return;

      OnKeyEvent(e.KeyCode, true);
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

    protected override void CheckRotate()
    {
      base.CheckRotate();
      CheckRotate(ctxMenu);
    }

    internal ViewFullCf(Conn conn, ConnOpts connOpts, UInt16 width, UInt16 height) : base(conn, connOpts, width, height)
    {
      MenuItem item;

      menu.MenuItems.Add(connMenu);
      connMenu.MenuItems.Add(newConnItem);
      item = new MenuItem();
      item.Text = App.GetStr("Open...");
      item.Click += new EventHandler(LoadConnOptsClicked);
      connMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Save as...");
      item.Click += new EventHandler(SaveConnOptsClicked);
      connMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Save as (with password)...");
      item.Click += new EventHandler(SaveConnOptsPwdClicked);
      connMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = "-";
      connMenu.MenuItems.Add(item);
      connMenu.MenuItems.Add(refreshItem);
      item = new MenuItem();
      item.Text = "-";
      connMenu.MenuItems.Add(item);
      connMenu.MenuItems.Add(closeConnItem);

      menu.MenuItems.Add(viewMenu);
      menu.MenuItems.Add(keysMenu);
      menu.MenuItems.Add(optionsMenu);
      item = new MenuItem();
      item.Text = App.GetStr("Help");
      menu.MenuItems.Add(item);
      item.MenuItems.Add(aboutItem);

      item = new MenuItem();
      item.Text = App.GetStr("Full Screen");
      item.Checked = true; // If we see this we are using full screen.
      item.Click += fullScrnHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = "-";
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Portrait");
      item.Click += rotateHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated clockwise");
      item.Click += rotateHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated counter-clockwise");
      item.Click += rotateHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Upside down");
      item.Click += rotateHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = "-";
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-");
      item.Click += keysHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Alt-");
      item.Click += keysHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Alt-");
      item.Click += keysHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Alt-Del");
      item.Click += keysHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Esc (Start Menu)");
      item.Click += keysHdr;
      ctxMenu.MenuItems.Add(item);
      CheckRotate(ctxMenu);
    }
  }
}
