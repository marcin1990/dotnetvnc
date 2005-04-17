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

    protected ViewOpts viewOpts = null;

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

    protected Label cliScalingLbl = new Label();
    protected ComboBox cliScalingBox = new ComboBox();
    protected Label cliScalingWidthLbl = new Label();
    protected TextBox cliScalingWidthBox = new TextBox();
    protected Label cliScalingHeightLbl = new Label();
    protected TextBox cliScalingHeightBox = new TextBox();
    protected Label servScalingLbl = new Label();
    protected ComboBox servScalingBox = new ComboBox();

    protected CheckBox viewOnlyBox = new CheckBox();
    protected CheckBox shareServBox = new CheckBox();
    protected CheckBox scrnUpdAlgoBox = new CheckBox();

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
      if(!ValidateCliScaling())
        return;
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

    protected bool ValidateCliScaling()
    {
      if(cliScalingBox.Text != App.GetStr("Custom"))
        return true;

      try
      {
        if(UInt16.Parse(cliScalingWidthBox.Text) > 0 && UInt16.Parse(cliScalingHeightBox.Text) > 0)
          return true;
        else
          return false;
      }
      catch(FormatException)
      {
        MessageBox.Show(App.GetStr("Customized client-side scaling width or height is invalid!"),
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return false;
      }
      catch(OverflowException)
      {
        // TODO: Something more descriptive.
        MessageBox.Show(App.GetStr("Customized client-side scaling width or height is invalid!"),
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return false;
      }
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

    protected virtual void GetOptions()
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
      viewOpts.CliScalingWidth = 0;
      viewOpts.CliScalingHeight = 0;
      switch(cliScalingBox.SelectedIndex)
      {
        case 0:
          viewOpts.CliScaling = CliScaling.None;
          break;
        case 1:
          viewOpts.CliScaling = CliScaling.Auto;
          break;
        case 2:
          viewOpts.CliScaling = CliScaling.OneHalf;
          break;
        case 3:
          viewOpts.CliScaling = CliScaling.OneThird;
          break;
        case 4:
          viewOpts.CliScaling = CliScaling.OneFourth;
          break;
        case 5:
          viewOpts.CliScaling = CliScaling.OneFifth;
          break;
        case 6:
          viewOpts.CliScaling = CliScaling.Double;
          break;
        case 7:
          viewOpts.CliScaling = CliScaling.Custom;
          // Assuming we have validated the input.
          viewOpts.CliScalingWidth = UInt16.Parse(cliScalingWidthBox.Text);
          viewOpts.CliScalingHeight = UInt16.Parse(cliScalingHeightBox.Text);
          break;
      }
      viewOpts.ServScaling = (ServScaling)servScalingBox.SelectedIndex;
      viewOpts.ViewOnly = viewOnlyBox.Checked;
      viewOpts.ShareServ = !shareServBox.Checked;
      viewOpts.ScrnUpdAlgo = scrnUpdAlgoBox.Checked? ScrnUpdAlgo.Asap : ScrnUpdAlgo.Batch;
    }

    protected virtual void SetOptions(ViewOpts viewOpts)
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
      cliScalingWidthBox.Text = String.Empty;
      cliScalingHeightBox.Text = String.Empty;
      switch(viewOpts.CliScaling)
      {
        case CliScaling.None:
          cliScalingBox.SelectedIndex = 0;
          break;
        case CliScaling.Auto:
          cliScalingBox.SelectedIndex = 1;
          break;
        case CliScaling.OneHalf:
          cliScalingBox.SelectedIndex = 2;
          break;
        case CliScaling.OneThird:
          cliScalingBox.SelectedIndex = 3;
          break;
        case CliScaling.OneFourth:
          cliScalingBox.SelectedIndex = 4;
          break;
        case CliScaling.OneFifth:
          cliScalingBox.SelectedIndex = 5;
          break;
        case CliScaling.Double:
          cliScalingBox.SelectedIndex = 6;
          break;
        case CliScaling.Custom:
          cliScalingBox.SelectedIndex = 7;
          cliScalingWidthBox.Text = viewOpts.CliScalingWidth.ToString();
          cliScalingHeightBox.Text = viewOpts.CliScalingHeight.ToString();
          break;
      }
      servScalingBox.SelectedIndex = (int)viewOpts.ServScaling;
      viewOnlyBox.Checked = viewOpts.ViewOnly;
      shareServBox.Checked = !viewOpts.ShareServ;
      scrnUpdAlgoBox.Checked = viewOpts.ScrnUpdAlgo == ScrnUpdAlgo.Asap;
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
      if(!ValidateCliScaling())
        return;
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

    private void CliScalingBoxChanged(object sender, EventArgs e)
    {
      cliScalingWidthBox.Enabled = cliScalingBox.Text == App.GetStr("Custom");
      cliScalingHeightBox.Enabled = cliScalingBox.Text == App.GetStr("Custom");
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

      cliScalingLbl.Text = App.GetStr("Client-side scaling:");
      cliScalingBox.DropDownStyle = ComboBoxStyle.DropDownList;
      cliScalingBox.Items.Add(App.GetStr("None"));
      cliScalingBox.Items.Add(App.GetStr("Auto"));
      cliScalingBox.Items.Add(App.GetStr("1/2 of server"));
      cliScalingBox.Items.Add(App.GetStr("1/3 of server"));
      cliScalingBox.Items.Add(App.GetStr("1/4 of server"));
      cliScalingBox.Items.Add(App.GetStr("1/5 of server"));
      cliScalingBox.Items.Add(App.GetStr("2 of server"));
      cliScalingBox.Items.Add(App.GetStr("Custom"));
      cliScalingBox.SelectedIndexChanged += new EventHandler(CliScalingBoxChanged);
      cliScalingWidthLbl.Text = App.GetStr("Width (pixel):");
      cliScalingHeightLbl.Text = App.GetStr("Height (pixel):");
      servScalingLbl.Text = App.GetStr("Server-side scaling:");
      servScalingBox.DropDownStyle = ComboBoxStyle.DropDownList;
      servScalingBox.Items.Add(App.GetStr("Default"));
      servScalingBox.Items.Add(App.GetStr("None"));
      servScalingBox.Items.Add(App.GetStr("1/2"));
      servScalingBox.Items.Add(App.GetStr("1/3"));
      servScalingBox.Items.Add(App.GetStr("1/4"));
      servScalingBox.Items.Add(App.GetStr("1/5"));
      servScalingBox.Items.Add(App.GetStr("1/6"));
      servScalingBox.Items.Add(App.GetStr("1/7"));
      servScalingBox.Items.Add(App.GetStr("1/8"));
      servScalingBox.Items.Add(App.GetStr("1/9"));

      viewOnlyBox.Text = App.GetStr("View only (ignore input)");
      shareServBox.Text = App.GetStr("Disconnect other viewers upon connect");
      scrnUpdAlgoBox.Text = App.GetStr("Update screen ASAP");

      SetOptions(viewOpts);
      LoadConnHist();
    }
  }
}
