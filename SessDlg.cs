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
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  /// <remarks>Prompts the user for connection details.</remarks>
  internal abstract class SessDlg : Form
  {
    /// <summary>This is default port to connect to.</summary>
    private const UInt16 VncPort = 5900;

    private string host = null;
    private int port = -1;
    private string passwd = null;
    private ViewOpts viewOpts = null;

    protected Label servLbl = new Label();
    protected TextBox remoteEndPt = new TextBox();
    protected Label remoteEndPtLbl = new Label();
    protected Label passwdLbl = new Label();
    protected TextBox passwdBox = new TextBox();
    protected Label recentLbl = new Label();

    protected CheckBox fullScrnBox = new CheckBox();
    protected Label rotateLbl = new Label();
    protected ComboBox rotateBox = new ComboBox();
    protected Label pixelSizeLbl = new Label();
    protected ComboBox pixelSizeBox = new ComboBox();

    protected CheckBox viewOnlyBox = new CheckBox();
    protected CheckBox shareServBox = new CheckBox();

    protected EventHandler okHdr = null;
    protected EventHandler cancelHdr = null;
    protected EventHandler aboutHdr = null;
    protected EventHandler saveDefsHdr = null;
    protected EventHandler restoreDefsHdr = null;

    internal SessDlg() : base()
    {
      viewOpts = new ViewOpts();
      SetupHdrs();
    }

    internal SessDlg(ViewOpts viewOpts) : base()
    {
      this.viewOpts = viewOpts;
      SetupHdrs();
    }

    private void SetupHdrs()
    {
      okHdr = new EventHandler(OkClicked);
      cancelHdr = new EventHandler(CancelClicked);
      aboutHdr = new EventHandler(AboutClicked);
      saveDefsHdr = new EventHandler(SaveDefsClicked);
      restoreDefsHdr = new EventHandler(RestoreDefsClicked);
    }

    internal ConnOpts ConnOpts
    {
      get
      {
        return new ConnOpts(host, port, passwd, viewOpts);
      }
    }

    private void AboutClicked(object sender, EventArgs e)
    {
      App.AboutBox();
    }

    protected void Ok()
    {
      if(!ValidateHostPort())
        return;
      GetPasswd();
      GetOptions();
      SaveConnHist();
      DialogResult = DialogResult.OK;
    }

    private void OkClicked(object sender, EventArgs e)
    {
      Ok();
    }

    protected void Cancel()
    {
      DialogResult = DialogResult.Cancel;
    }

    private void CancelClicked(object sender, EventArgs e)
    {
      Cancel();
    }

    protected bool ValidateHostPort()
    {
      int indexOfColon = remoteEndPt.Text.IndexOf(':');
      if(indexOfColon < 0) // Colon not found.
      {
        if(remoteEndPt.Text.Length > 0)
        {
          host = remoteEndPt.Text;
          port = VncPort;
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
          int portOffset = VncPort;
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

    protected void GetPasswd()
    {
      passwd = passwdBox.Text;
    }

    protected void GetOptions()
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

    protected void SetOptions(ViewOpts viewOpts)
    {
      this.viewOpts = viewOpts;
      fullScrnBox.Checked = viewOpts.IsFullScrn;
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
      viewOnlyBox.Checked = viewOpts.ViewOnly;
      shareServBox.Checked = !viewOpts.ShareServ;
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

    protected abstract void AddConnHistEntry(string entry);

    private void LoadConnHist()
    {
      try
      {
        ConnHist connHist = new ConnHist(App.ConnHistFileName);
        for(byte i = 0; i < connHist.Count; i++)
          AddConnHistEntry(connHist[i]);
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

    private void SaveDefsClicked(object sender, EventArgs e)
    {
      GetOptions();
      try
      {
        viewOpts.Save(App.SettingsFileName);
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to save defaults!"),
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
      SetOptions(viewOpts);
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      ControlBox = false;
      MinimizeBox = false;
      MaximizeBox = false;
      Text = App.GetStr("New VNC Connection");
      Menu = new MainMenu();

      servLbl.Text = App.GetStr("Server:");
      remoteEndPtLbl.Text = App.GetStr("(host:display or host::port)");
      passwdLbl.Text = App.GetStr("Password:");
      passwdBox.PasswordChar = '*';
      recentLbl.Text = App.GetStr("Recent:");

      fullScrnBox.Text = App.GetStr("Full screen mode");
      rotateLbl.Text = App.GetStr("Orientation:");
      rotateBox.DropDownStyle = ComboBoxStyle.DropDownList;
      rotateBox.Items.Add(App.GetStr("Portrait"));
      rotateBox.Items.Add(App.GetStr("Screen rotated clockwise"));
      rotateBox.Items.Add(App.GetStr("Screen rotated counter-clockwise"));
      rotateBox.Items.Add(App.GetStr("Upside down"));
      pixelSizeLbl.Text = App.GetStr("Pixel size:");
      pixelSizeBox.DropDownStyle = ComboBoxStyle.DropDownList;
      pixelSizeBox.Items.Add(App.GetStr("Server decides"));
      pixelSizeBox.Items.Add(App.GetStr("Force 8-bit"));
      pixelSizeBox.Items.Add(App.GetStr("Force 16-bit"));

      viewOnlyBox.Text = App.GetStr("View only (ignore input)");
      shareServBox.Text = App.GetStr("Disconnect other viewers upon connect");

      SetOptions(viewOpts);
      LoadConnHist();
    }
  }
}
