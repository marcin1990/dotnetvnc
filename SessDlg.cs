//  Copyright (C) 2004-2005 Rocky Lo. All Rights Reserved.
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
using System.Xml;
using System.IO;
using System.Drawing;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  /// <remarks>Prompts the user for connection details.</remarks>
  internal class SessDlg : Form
  {
    private string host = null;
    private int port = -1;
    private string passwd = null;
    private ViewOpts viewOpts = null;

    private TabControl tabCtrl = new TabControl();
    private Button okBtn = new Button();
    private Button cancelBtn = new Button();
    private Button aboutBtn = new Button();

    private TabPage generalPage = new TabPage();
    private Label servLbl = new Label();
    private TextBox remoteEndPt = new TextBox();
    private Label remoteEndPtLbl = new Label();
    private Label passwdLbl = new Label();
    private TextBox passwdBox = new TextBox();
    private Label recentLbl = new Label();
    private ListBox recentBox = new ListBox();

    private TabPage displayPage = new TabPage();
    private CheckBox fullScrnBox = new CheckBox();
    private Label rotateLbl = new Label();
    private ComboBox rotateBox = new ComboBox();
    private Label pixelSizeLbl = new Label();
    private ComboBox pixelSizeBox = new ComboBox();

    private TabPage othersPage = new TabPage();
    private CheckBox viewOnlyBox = new CheckBox();
    private CheckBox shareServBox = new CheckBox();

    private TabPage saveLoadPage = new TabPage();
    private Button saveConnOptsBtn = new Button();
    private Button saveConnOptsPwdBtn = new Button();
    private Button loadConnOptsBtn = new Button();
    private Button saveDefsBtn = new Button();
    private Button restoreDefsBtn = new Button();

    internal SessDlg() : base()
    {
      viewOpts = new ViewOpts();
    }

    internal SessDlg(ViewOpts viewOpts) : base()
    {
      this.viewOpts = viewOpts;
    }

    internal ConnOpts ConnOpts
    {
      get
      {
        return new ConnOpts(host, port, passwd, viewOpts);
      }
    }

    private bool ValidateHostPort()
    {
      int indexOfColon = remoteEndPt.Text.IndexOf(':');
      if(indexOfColon < 0) // Colon not found.
      {
        if(remoteEndPt.Text.Length > 0)
        {
          host = remoteEndPt.Text;
          port = App.VncPort;
        }
        else // Empty string.
        {
          MessageBox.Show(App.GetStr("Please specify the VNC server to connect to!"),
                          Text,
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Exclamation,
                          MessageBoxDefaultButton.Button1);
          return false;
        }
      }
      // Colon at the beginning or at the end.
      else if(indexOfColon == 0 || indexOfColon == remoteEndPt.Text.Length - 1)
      {
        MessageBox.Show(App.GetStr("Please specify the host and/or the display/port to connect to!"),
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return false;
      }
      else
      {
        // Double colon at the end.
        if(indexOfColon == remoteEndPt.Text.Length - 2 && remoteEndPt.Text[indexOfColon + 1] == ':')
        {
          MessageBox.Show(App.GetStr("Please specify the port to connect to!"),
                          Text,
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Exclamation,
                          MessageBoxDefaultButton.Button1);
          return false;
        }
        else
        {
          int portOffset = App.VncPort;
          int portStart = indexOfColon + 1;
          if(remoteEndPt.Text[indexOfColon + 1] == ':') // Double colon.
          {
            portOffset = 0;
            portStart = indexOfColon + 2;
          }
          string portStr = remoteEndPt.Text.Substring(portStart);
          try
          {
            port = Int32.Parse(portStr);
            if(port < 0)
              throw new OverflowException();
          }
          catch(FormatException)
          {
            MessageBox.Show(App.GetStr("The port is invalid!"),
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation,
                            MessageBoxDefaultButton.Button1);
            return false;
          }
          catch(OverflowException)
          {
            MessageBox.Show(App.GetStr("The port is invalid!"),
                            Text,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation,
                            MessageBoxDefaultButton.Button1);
            return false;
          }
          port += portOffset;
          // TODO: Compare this with min/max port numbers.

          host = remoteEndPt.Text.Substring(0, indexOfColon);
        }
      }
      return true;
    }

    private void Ok()
    {
      if(!ValidateHostPort())
        return;
      passwd = passwdBox.Text;
      GetOptions();
      SaveConnHist();
      DialogResult = DialogResult.OK;
    }

    private void GetOptions()
    {
      viewOpts.IsFullScrn = fullScrnBox.Checked;
      switch(rotateBox.SelectedIndex)
      {
        case 0:
          viewOpts.Orientation = Orientation.Portrait;
          break;
        case 1:
          viewOpts.Orientation = Orientation.Landscape90;
          break;
        case 2:
          viewOpts.Orientation = Orientation.Landscape270;
          break;
        case 3:
          viewOpts.Orientation = Orientation.Portrait180;
          break;
      }
      switch(pixelSizeBox.SelectedIndex)
      {
        case 0:
          viewOpts.PixelSize = PixelSize.Unspec;
          break;
        case 1:
          viewOpts.PixelSize = PixelSize.Force8Bit;
          break;
        case 2:
          viewOpts.PixelSize = PixelSize.Force16Bit;
          break;
      }
      viewOpts.ViewOnly = viewOnlyBox.Checked;
      viewOpts.ShareServ = !shareServBox.Checked;
    }

    private void SaveConnHist()
    {
      try
      {
        ConnHist connHist = new ConnHist(App.ConnHistFileName);
        connHist.Add(host + "::" + port);
        connHist.Save(App.ConnHistFileName);
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to save the history file!"),
                        App.GetStr("Error!"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
      catch(XmlException)
      {
        MessageBox.Show(App.GetStr("The history file is corrupted!"),
                        App.GetStr("Error!"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
    }

    private void AboutClicked(object sender, EventArgs e)
    {
      App.AboutBox();
    }

    private void OkClicked(object sender, EventArgs e)
    {
      Ok();
    }

    private void Cancel()
    {
      DialogResult = DialogResult.Cancel;
    }

    private void KeyPressed(object sender, KeyPressEventArgs e)
    {
      e.Handled = true;
      if(e.KeyChar == (UInt32)Keys.Enter)
      {
        // For buttons, we will not be here because KeyDown has dismissed the dialog.
        Ok();
      }
      else if(e.KeyChar == (UInt32)Keys.Escape)
        Cancel();
      else
        e.Handled = false;
    }

    private void SaveConnOpts(bool savePwd)
    {
      if(!ValidateHostPort())
        return;
      passwd = passwdBox.Text;
      GetOptions();

      SaveFileDialog dlg = new SaveFileDialog();
      dlg.Filter = App.GetStr("VNC files (*.vncxml)|*.vncxml|All files (*.*)|*.*");
      if(dlg.ShowDialog() != DialogResult.OK)
        return;

      try
      {
        ConnOpts.Save(dlg.FileName, savePwd);
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

      ConnOpts connOpts;
      try
      {
        connOpts = new ConnOpts(dlg.FileName);
      }
      catch(FileNotFoundException)
      {
        MessageBox.Show(App.GetStr("Unable to open the file!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to read from the file!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      catch(XmlException)
      {
        MessageBox.Show(App.GetStr("The file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      catch(FormatException)
      {
        MessageBox.Show(App.GetStr("The file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      remoteEndPt.Text = connOpts.Host + "::" + connOpts.Port;
      passwdBox.Text = connOpts.Passwd;
      viewOpts = connOpts.ViewOpts;
      SetOptions();
    }

    private void SaveDefsClicked(object sender, EventArgs e)
    {
      GetOptions();
      try
      {
        viewOpts.Save(App.SettingsFileName);
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

    private void RestoreDefsClicked(object sender, EventArgs e)
    {
      try
      {
        viewOpts.Load(App.SettingsFileName);
      }
      catch(FileNotFoundException)
      {
        viewOpts = new ViewOpts();
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to read from the setting file!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      catch(XmlException)
      {
        MessageBox.Show(App.GetStr("The setting file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      catch(FormatException)
      {
        MessageBox.Show(App.GetStr("The setting file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      SetOptions();
    }

    private void SetOptions()
    {
      fullScrnBox.Checked = viewOpts.IsFullScrn;
      SetOrientation();
      SetPixelSize();
      viewOnlyBox.Checked = viewOpts.ViewOnly;
      shareServBox.Checked = !viewOpts.ShareServ;
    }

    private void LoadConnHist()
    {
      try
      {
        ConnHist connHist = new ConnHist(App.ConnHistFileName);
        for(byte i = 0; i < connHist.Count; i++)
          recentBox.Items.Add(connHist[i]);
      }
      catch(XmlException)
      {
        MessageBox.Show(App.GetStr("The history file is corrupted."),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
    }

    private void RecentBoxChanged(object sender, EventArgs e)
    {
      remoteEndPt.Text = recentBox.Text;
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);
      aboutBtn.Location = new Point(ClientRectangle.Right - App.DialogSpacing - aboutBtn.Width, ClientRectangle.Bottom - App.DialogSpacing - aboutBtn.Height);
      cancelBtn.Location = new Point(aboutBtn.Left - App.DialogSpacing - cancelBtn.Width, ClientRectangle.Bottom - App.DialogSpacing - cancelBtn.Height);
      okBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - okBtn.Width, cancelBtn.Top);
      tabCtrl.Size = new Size(aboutBtn.Right - tabCtrl.Left, aboutBtn.Top - App.DialogSpacing - tabCtrl.Top);
      remoteEndPt.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - remoteEndPt.Left;
      remoteEndPtLbl.Width = generalPage.ClientRectangle.Right - remoteEndPtLbl.Left;
      passwdBox.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      recentBox.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - recentBox.Left;
      recentBox.Height = generalPage.ClientRectangle.Bottom - App.DialogSpacing - recentBox.Top;
      rotateBox.Width = displayPage.ClientRectangle.Right - App.DialogSpacing - rotateBox.Left;
      pixelSizeBox.Width = displayPage.ClientRectangle.Right - App.DialogSpacing - pixelSizeBox.Left;
      viewOnlyBox.Width = displayPage.ClientRectangle.Right - viewOnlyBox.Left;
      shareServBox.Width = displayPage.ClientRectangle.Right - shareServBox.Left;
      saveConnOptsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveConnOptsBtn.Left;
      saveConnOptsPwdBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveConnOptsPwdBtn.Left;
      loadConnOptsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - loadConnOptsBtn.Left;
      saveDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveDefsBtn.Left;
      restoreDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - restoreDefsBtn.Left;
    }

    private void SetPixelSize()
    {
      switch(viewOpts.PixelSize)
      {
        case PixelSize.Unspec:
          pixelSizeBox.SelectedIndex = 0;
          break;
        case PixelSize.Force8Bit:
          pixelSizeBox.SelectedIndex = 1;
          break;
        case PixelSize.Force16Bit:
          pixelSizeBox.SelectedIndex = 2;
          break;
      }
    }

    private void SetOrientation()
    {
      switch(viewOpts.Orientation)
      {
        case Orientation.Portrait:
          rotateBox.SelectedIndex = 0;
          break;
        case Orientation.Landscape90:
          rotateBox.SelectedIndex = 1;
          break;
        case Orientation.Portrait180:
          rotateBox.SelectedIndex = 3;
          break;
        case Orientation.Landscape270:
          rotateBox.SelectedIndex = 2;
          break;
      }
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      ControlBox = false;
      MinimizeBox = false;
      MaximizeBox = false;
      Text = App.GetStr("New VNC Connection");
      Menu = new MainMenu();

      Graphics graphics = CreateGraphics();
      KeyPressEventHandler keyPressHdr = new KeyPressEventHandler(KeyPressed);

      aboutBtn.Location = new Point(ClientRectangle.Right - App.DialogSpacing - aboutBtn.Width, ClientRectangle.Bottom - App.DialogSpacing - aboutBtn.Height);
      aboutBtn.Text = App.GetStr("About");
      aboutBtn.Click += new EventHandler(AboutClicked);
      aboutBtn.KeyPress += keyPressHdr;
      cancelBtn.Location = new Point(aboutBtn.Left - App.DialogSpacing - cancelBtn.Width, ClientRectangle.Bottom - App.DialogSpacing - cancelBtn.Height);
      cancelBtn.Text = App.GetStr("Cancel");
      cancelBtn.DialogResult = DialogResult.Cancel;
      cancelBtn.KeyPress += keyPressHdr;
      okBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - okBtn.Width, cancelBtn.Top);
      okBtn.Text = App.GetStr("OK");
      okBtn.Click += new EventHandler(OkClicked);
      okBtn.KeyPress += keyPressHdr;
      tabCtrl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      tabCtrl.Size = new Size(aboutBtn.Right - tabCtrl.Left, aboutBtn.Top - App.DialogSpacing - tabCtrl.Top);
      Controls.Add(tabCtrl);
      Controls.Add(okBtn);
      Controls.Add(cancelBtn);
      Controls.Add(aboutBtn);

      tabCtrl.TabPages.Add(generalPage);
      generalPage.Text = App.GetStr("General");
      servLbl.Text = App.GetStr("Server:");
      servLbl.Size = graphics.MeasureString(servLbl.Text, Font).ToSize();
      passwdLbl.Text = App.GetStr("Password:");
      passwdLbl.Size = graphics.MeasureString(passwdLbl.Text, Font).ToSize();
      recentLbl.Text = App.GetStr("Recent:");
      recentLbl.Size = graphics.MeasureString(recentLbl.Text, Font).ToSize();
      servLbl.Width = Math.Max(Math.Max(servLbl.Width, passwdLbl.Width), recentLbl.Width);
      passwdLbl.Width = servLbl.Width;
      recentLbl.Width = passwdLbl.Width;
      servLbl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      generalPage.Controls.Add(servLbl);
      remoteEndPt.Location = new Point(servLbl.Right + App.DialogSpacing, servLbl.Top);
      remoteEndPt.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - remoteEndPt.Left;
      remoteEndPt.KeyPress += keyPressHdr;
      generalPage.Controls.Add(remoteEndPt);
      remoteEndPtLbl.Location = new Point(remoteEndPt.Left, remoteEndPt.Bottom + App.DialogSpacing);
      remoteEndPtLbl.Text = App.GetStr("(host:display or host::port)");
      remoteEndPtLbl.Width = generalPage.ClientRectangle.Right - remoteEndPt.Left;
      remoteEndPtLbl.Height = graphics.MeasureString(remoteEndPtLbl.Text, Font).ToSize().Height;
      generalPage.Controls.Add(remoteEndPtLbl);
      passwdLbl.Location = new Point(servLbl.Left, remoteEndPtLbl.Bottom + App.DialogSpacing);
      generalPage.Controls.Add(passwdLbl);
      passwdBox.Location = new Point(passwdLbl.Right + App.DialogSpacing, passwdLbl.Top);
      passwdBox.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      passwdBox.KeyPress += keyPressHdr;
      passwdBox.PasswordChar = '*';
      generalPage.Controls.Add(passwdBox);
      recentLbl.Location = new Point(passwdLbl.Left, passwdBox.Bottom + App.DialogSpacing);
      generalPage.Controls.Add(recentLbl);
      recentBox.Location = new Point(recentLbl.Right + App.DialogSpacing, recentLbl.Top);
      recentBox.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - recentBox.Left;
      recentBox.Height = generalPage.ClientRectangle.Bottom - App.DialogSpacing - recentBox.Top;
      recentBox.KeyPress += keyPressHdr;
      recentBox.SelectedIndexChanged += new EventHandler(RecentBoxChanged);
      generalPage.Controls.Add(recentBox);

      tabCtrl.TabPages.Add(displayPage);
      displayPage.Text = App.GetStr("Display");
      rotateLbl.Text = App.GetStr("Orientation:");
      rotateLbl.Size = graphics.MeasureString(rotateLbl.Text, Font).ToSize();
      pixelSizeLbl.Text = App.GetStr("Pixel size:");
      pixelSizeLbl.Size = graphics.MeasureString(pixelSizeLbl.Text, Font).ToSize();
      rotateLbl.Width = Math.Max(rotateLbl.Width, pixelSizeLbl.Width);
      pixelSizeLbl.Width = rotateLbl.Width;
      fullScrnBox.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      fullScrnBox.Text = App.GetStr("Full screen mode");
      fullScrnBox.KeyPress += keyPressHdr;
      fullScrnBox.Width = displayPage.ClientRectangle.Right - fullScrnBox.Left;
      displayPage.Controls.Add(fullScrnBox);
      rotateLbl.Location = new Point(fullScrnBox.Left, fullScrnBox.Bottom + App.DialogSpacing);
      displayPage.Controls.Add(rotateLbl);
      rotateBox.Location = new Point(rotateLbl.Right + App.DialogSpacing, rotateLbl.Top);
      rotateBox.Width = displayPage.ClientRectangle.Right - App.DialogSpacing - rotateBox.Left;
      rotateBox.DropDownStyle = ComboBoxStyle.DropDownList;
      rotateBox.KeyPress += keyPressHdr;
      rotateBox.Items.Add(App.GetStr("Portrait"));
      rotateBox.Items.Add(App.GetStr("Screen rotated clockwise"));
      rotateBox.Items.Add(App.GetStr("Screen rotated counter-clockwise"));
      rotateBox.Items.Add(App.GetStr("Upside down"));
      displayPage.Controls.Add(rotateBox);
      pixelSizeLbl.Location = new Point(rotateLbl.Left, rotateBox.Bottom + App.DialogSpacing);
      displayPage.Controls.Add(pixelSizeLbl);
      pixelSizeBox.Location = new Point(pixelSizeLbl.Right + App.DialogSpacing, pixelSizeLbl.Top);
      pixelSizeBox.Width = displayPage.ClientRectangle.Right - App.DialogSpacing - pixelSizeBox.Left;
      pixelSizeBox.DropDownStyle = ComboBoxStyle.DropDownList;
      pixelSizeBox.KeyPress += keyPressHdr;
      pixelSizeBox.Items.Add(App.GetStr("Server decides"));
      pixelSizeBox.Items.Add(App.GetStr("Force 8-bit"));
      pixelSizeBox.Items.Add(App.GetStr("Force 16-bit"));
      displayPage.Controls.Add(pixelSizeBox);

      tabCtrl.TabPages.Add(othersPage);
      othersPage.Text = App.GetStr("Others");
      viewOnlyBox.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      viewOnlyBox.Text = App.GetStr("View only (ignore input)");
      viewOnlyBox.KeyPress += keyPressHdr;
      viewOnlyBox.Width = displayPage.ClientRectangle.Right - viewOnlyBox.Left;
      othersPage.Controls.Add(viewOnlyBox);
      shareServBox.Location = new Point(viewOnlyBox.Left, viewOnlyBox.Bottom + App.DialogSpacing);
      shareServBox.Text = App.GetStr("Disconnect other viewers upon connect");
      shareServBox.KeyPress += keyPressHdr;
      shareServBox.Width = displayPage.ClientRectangle.Right - shareServBox.Left;
      othersPage.Controls.Add(shareServBox);

      tabCtrl.TabPages.Add(saveLoadPage);
      saveLoadPage.Text = App.GetStr("Re/Store");
      saveConnOptsBtn.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      saveConnOptsBtn.Text = App.GetStr("Save as...");
      saveConnOptsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveConnOptsBtn.Left;
      saveConnOptsBtn.KeyPress += keyPressHdr;
      saveConnOptsBtn.Click += new EventHandler(SaveConnOptsClicked);
      saveLoadPage.Controls.Add(saveConnOptsBtn);
      saveConnOptsPwdBtn.Location = new Point(saveConnOptsBtn.Left, saveConnOptsBtn.Bottom + App.DialogSpacing);
      saveConnOptsPwdBtn.Text = App.GetStr("Save as (with password)...");
      saveConnOptsPwdBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveConnOptsPwdBtn.Left;
      saveConnOptsPwdBtn.KeyPress += keyPressHdr;
      saveConnOptsPwdBtn.Click += new EventHandler(SaveConnOptsPwdClicked);
      saveLoadPage.Controls.Add(saveConnOptsPwdBtn);
      loadConnOptsBtn.Location = new Point(saveConnOptsPwdBtn.Left, saveConnOptsPwdBtn.Bottom + App.DialogSpacing);
      loadConnOptsBtn.Text = App.GetStr("Open...");
      loadConnOptsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - loadConnOptsBtn.Left;
      loadConnOptsBtn.KeyPress += keyPressHdr;
      loadConnOptsBtn.Click += new EventHandler(LoadConnOptsClicked);
      saveLoadPage.Controls.Add(loadConnOptsBtn);
      saveDefsBtn.Location = new Point(loadConnOptsBtn.Left, loadConnOptsBtn.Bottom + App.DialogSpacing);
      saveDefsBtn.Text = App.GetStr("Save settings as default");
      saveDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveDefsBtn.Left;
      saveDefsBtn.KeyPress += keyPressHdr;
      saveDefsBtn.Click += new EventHandler(SaveDefsClicked);
      saveLoadPage.Controls.Add(saveDefsBtn);
      restoreDefsBtn.Location = new Point(saveDefsBtn.Left, saveDefsBtn.Bottom + App.DialogSpacing);
      restoreDefsBtn.Text = App.GetStr("Restore default settings");
      restoreDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - restoreDefsBtn.Left;
      restoreDefsBtn.KeyPress += keyPressHdr;
      restoreDefsBtn.Click += new EventHandler(RestoreDefsClicked);
      saveLoadPage.Controls.Add(restoreDefsBtn);

      graphics.Dispose();

      SetOptions();
      LoadConnHist();

      remoteEndPt.Focus();
    }
  }
}
