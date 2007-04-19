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
using System.IO;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  internal abstract class ViewFullCf : View
  {
    protected ContextMenu ctxMenu = new ContextMenu();
    private MenuItem ctxRotateMenu = new MenuItem();
    private MenuItem ctxCliScalingMenu = new MenuItem();
    private MenuItem ctxServScalingMenu = new MenuItem();
    private MenuItem ctxKeysMenu = new MenuItem();

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
      CheckRotate(ctxRotateMenu);
    }

    protected override void CheckCliScaling()
    {
      base.CheckCliScaling();
      CheckCliScaling(ctxCliScalingMenu);
    }

    protected override void CheckServScaling()
    {
      base.CheckServScaling();
      CheckServScaling(ctxServScalingMenu);
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
      item.Text = App.GetStr("Full screen");
      item.Checked = true; // If we see this we are using full screen.
      item.Click += fullScrnHdr;
      ctxMenu.MenuItems.Add(item);
      ctxRotateMenu.Text = App.GetStr("Rotate");
      ctxMenu.MenuItems.Add(ctxRotateMenu);
      item = new MenuItem();
      item.Text = App.GetStr("Portrait");
      item.Click += rotateHdr;
      ctxRotateMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated clockwise");
      item.Click += rotateHdr;
      ctxRotateMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Screen rotated counter-clockwise");
      item.Click += rotateHdr;
      ctxRotateMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Upside down");
      item.Click += rotateHdr;
      ctxRotateMenu.MenuItems.Add(item);
      CheckRotate(ctxRotateMenu);
      ctxCliScalingMenu.Text = App.GetStr("Client-side scaling");
      ctxMenu.MenuItems.Add(ctxCliScalingMenu);
      item = new MenuItem();
      item.Text = App.GetStr("None");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Auto");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/2 of server");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/3 of server");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/4 of server");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/5 of server");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("2 of server");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Custom...");
      item.Click += cliScalingHdr;
      ctxCliScalingMenu.MenuItems.Add(item);
      CheckCliScaling(ctxCliScalingMenu);
      ctxServScalingMenu.Text = App.GetStr("Server-side scaling");
      ctxMenu.MenuItems.Add(ctxServScalingMenu);
      item = new MenuItem();
      item.Text = App.GetStr("None");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/2");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/3");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/4");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/5");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/6");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/7");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/8");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("1/9");
      item.Click += servScalingHdr;
      ctxServScalingMenu.MenuItems.Add(item);
      CheckServScaling(ctxServScalingMenu);
      ctxKeysMenu.Text = App.GetStr("Keys");
      ctxMenu.MenuItems.Add(ctxKeysMenu);
      item = new MenuItem();
      item.Text = App.GetStr("Shift down");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Shift up");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Esc");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Tab");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Alt-");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Alt-");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Alt-Del");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("Ctrl-Esc (Start Menu)");
      item.Click += keysHdr;
      ctxKeysMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = "-";
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("View single window");
      item.Click += viewWinHdr;
      ctxMenu.MenuItems.Add(item);
      item = new MenuItem();
      item.Text = App.GetStr("View desktop");
      item.Click += viewWinHdr;
      ctxMenu.MenuItems.Add(item);
    }
  }
}
