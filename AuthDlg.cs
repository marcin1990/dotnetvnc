//  Copyright (c) 2004-2005 Rocky Lo. All Rights Reserved.
//  Copyright (C) 2002 Ultr@VNC Team Members. All Rights Reserved.
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
using System.Drawing;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  /// <remarks>
  ///   This class prompts for the password.
  ///   Please note that we don't use a factory to create an AuthDlg currently.
  ///   This is because AuthDlg is very simple and creating all the classes
  ///   (Factory, Dt, Ppc, etc.) seems to be too much for this task.
  ///   If in the future this class becomes complicated then separating the
  ///   device specific code will be necessary.
  /// </remarks>
  internal class AuthDlg : Form
  {
    private string passwd = null;

    private Label passwdLbl = new Label();
    private TextBox passwdBox = new TextBox();

    private Button logInBtn = null;
    private MenuItem logInItem = null;
    private Button cancelBtn = null;
    private MenuItem cancelItem = null;

    internal AuthDlg()
    {
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
      {
        logInBtn = new Button();
        cancelBtn = new Button();
        if(App.DevCap.Lvl == DevCapLvl.PocketPc && App.DevCap.Res >= ResLvl.High)
        {
          passwdBox.Height *= 2;
          logInBtn.Width *= 2;
          logInBtn.Height *= 2;
          cancelBtn.Width *= 2;
          cancelBtn.Height *= 2;
        }
      }
      else
      {
        logInItem = new MenuItem();
        cancelItem = new MenuItem();
      }
    }

    internal string Passwd
    {
      get
      {
        if(passwd == null)
          throw new InvalidOperationException("Password not set");
        else
          return passwd;
      }
    }

    private void LogIn()
    {
      passwd = passwdBox.Text;
      DialogResult = DialogResult.OK;
    }

    private void LogInClicked(object sender, EventArgs e)
    {
      LogIn();
    }

    private void Cancel()
    {
      DialogResult = DialogResult.Cancel;
    }

    private void CancelClicked(object sender, EventArgs e)
    {
      Cancel();
    }

    private void KeyPressed(object sender, KeyPressEventArgs e)
    {
      e.Handled = true;
      if(e.KeyChar == (UInt32)Keys.Enter)
      {
        // If focus is on LogIn and Cancel, we will not be here because
        // KeyDown has dismissed the dialog.
        LogIn();
      }
      else if(e.KeyChar == (UInt32)Keys.Escape)
        Cancel();
      else
        e.Handled = false;
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);

      passwdBox.Width = ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
      {
        cancelBtn.Location = new Point(passwdBox.Right - cancelBtn.Width, passwdBox.Bottom + App.DialogSpacing);
        logInBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - logInBtn.Width, cancelBtn.Top);
      }
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      KeyPressEventHandler keyPressHdr = new KeyPressEventHandler(KeyPressed);
      EventHandler logInHdr = new EventHandler(LogInClicked);

      ControlBox = false;
      MinimizeBox = false;
      MaximizeBox = false;
      Text = App.GetStr("VNC Authentication");
      Menu = new MainMenu();

      passwdLbl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      passwdLbl.Text = App.GetStr("Password:");
      Graphics graphics = CreateGraphics();
      passwdLbl.Size = graphics.MeasureString(passwdLbl.Text, Font).ToSize();
      graphics.Dispose();
      Controls.Add(passwdLbl);

      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
        passwdBox.Location = new Point(passwdLbl.Right + App.DialogSpacing, passwdLbl.Top);
      else
        passwdBox.Location = new Point(App.DialogSpacing, passwdLbl.Bottom + App.DialogSpacing);
      passwdBox.Width = ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      passwdBox.PasswordChar = '*';
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
        passwdBox.KeyPress += keyPressHdr;
      Controls.Add(passwdBox);

      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
      {
        cancelBtn.Location = new Point(passwdBox.Right - cancelBtn.Width, passwdBox.Bottom + App.DialogSpacing);
        cancelBtn.Text = App.GetStr("Cancel");
        cancelBtn.DialogResult = DialogResult.Cancel;
        cancelBtn.KeyPress += keyPressHdr;

        logInBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - logInBtn.Width, cancelBtn.Top);
        logInBtn.Text = App.GetStr("Log In");
        logInBtn.Click += logInHdr;
        logInBtn.KeyPress += keyPressHdr;

        Controls.Add(logInBtn);
        Controls.Add(cancelBtn);
      }
      else
      {
        logInItem.Text = App.GetStr("Log In");
        logInItem.Click += logInHdr;
        Menu.MenuItems.Add(logInItem);
        cancelItem.Text = App.GetStr("Cancel");
        cancelItem.Click += new EventHandler(CancelClicked);
        Menu.MenuItems.Add(cancelItem);
      }

      passwdBox.Focus();
    }
  }
}
