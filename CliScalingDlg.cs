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
  /// <remarks>This class reads customized client-side scaling params.</remarks>
  internal class CliScalingDlg : Form
  {
    private ViewOpts viewOpts = null;

    private Label widthLbl = new Label();
    private TextBox widthBox = new TextBox();
    private Label heightLbl = new Label();
    private TextBox heightBox = new TextBox();

    private Button okBtn = null;
    private MenuItem okItem = null;
    private Button cancelBtn = null;
    private MenuItem cancelItem = null;

    internal CliScalingDlg(ViewOpts viewOpts)
    {
      this.viewOpts = viewOpts;

      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
      {
        okBtn = new Button();
        cancelBtn = new Button();
        if(App.DevCap.Lvl == DevCapLvl.PocketPc && App.DevCap.Res >= ResLvl.High)
        {
          widthBox.Height *= 2;
          heightBox.Height *= 2;
          okBtn.Width *= 2;
          okBtn.Height *= 2;
          cancelBtn.Width *= 2;
          cancelBtn.Height *= 2;
        }
      }
      else
      {
        okItem = new MenuItem();
        cancelItem = new MenuItem();
      }

      widthBox.Text = String.Empty;
      heightBox.Text = String.Empty;
      if(viewOpts.CliScaling == CliScaling.Custom)
      {
        widthBox.Text = viewOpts.CliScalingWidth.ToString();
        heightBox.Text = viewOpts.CliScalingHeight.ToString();
      }
    }

    private void Ok()
    {
      UInt16 width;
      UInt16 height;

      try
      {
        width = UInt16.Parse(widthBox.Text);
        height = UInt16.Parse(heightBox.Text);
        if(width <= 0 || height <= 0)
        {
          MessageBox.Show(App.GetStr("Width and height must be non-zero."),
                          Text,
                          MessageBoxButtons.OK,
                          MessageBoxIcon.Exclamation,
                          MessageBoxDefaultButton.Button1);
          return;
        }
      }
      catch(OverflowException)
      {
        MessageBox.Show(App.GetStr("Width or height is invalid!"),
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }
      catch(FormatException)
      {
        MessageBox.Show(App.GetStr("Width or height is invalid!"),
                        Text,
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        return;
      }

      viewOpts.CliScaling = CliScaling.Custom;
      viewOpts.CliScalingWidth = width;
      viewOpts.CliScalingHeight = height;

      DialogResult = DialogResult.OK;
    }

    private void Cancel()
    {
      DialogResult = DialogResult.Cancel;
    }

    private void OkClicked(object sender, EventArgs e)
    {
      Ok();
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
        // If focus is on OK or Cancel, we will not be here because
        // KeyDown has dismissed the dialog.
        Ok();
      }
      else if(e.KeyChar == (UInt32)Keys.Escape)
        Cancel();
      else
        e.Handled = false;
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);

      widthBox.Width = ClientRectangle.Right - App.DialogSpacing - widthBox.Left;
      heightBox.Width = ClientRectangle.Right - App.DialogSpacing - heightBox.Left;
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
      {
        cancelBtn.Location = new Point(heightBox.Right - cancelBtn.Width, heightBox.Bottom + App.DialogSpacing);
        okBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - okBtn.Width, cancelBtn.Top);
      }
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      KeyPressEventHandler keyPressHdr = new KeyPressEventHandler(KeyPressed);
      EventHandler okHdr = new EventHandler(OkClicked);

      ControlBox = false;
      MinimizeBox = false;
      MaximizeBox = false;
      Text = App.GetStr("Custom Client-Side Scaling");
      Menu = new MainMenu();

      Graphics graphics = CreateGraphics();

      widthLbl.Text = App.GetStr("Width (Pixel):");
      widthLbl.Size = graphics.MeasureString(widthLbl.Text, Font).ToSize();
      heightLbl.Text = App.GetStr("Height (Pixel):");
      heightLbl.Size = graphics.MeasureString(heightLbl.Text, Font).ToSize();

      widthLbl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      widthLbl.Width = Math.Max(widthLbl.Width, heightLbl.Width);
      Controls.Add(widthLbl);
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
        widthBox.Location = new Point(widthLbl.Right + App.DialogSpacing, widthLbl.Top);
      else
        widthBox.Location = new Point(App.DialogSpacing, widthLbl.Bottom + App.DialogSpacing);
      widthBox.Width = ClientRectangle.Right - App.DialogSpacing - widthBox.Left;
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
        widthBox.KeyPress += keyPressHdr;
      Controls.Add(widthBox);
      heightLbl.Location = new Point(widthLbl.Left, widthBox.Bottom + App.DialogSpacing);
      heightLbl.Width = widthLbl.Width;
      Controls.Add(heightLbl);
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
        heightBox.Location = new Point(heightLbl.Right + App.DialogSpacing, heightLbl.Top);
      else
        heightBox.Location = new Point(App.DialogSpacing, heightLbl.Bottom + App.DialogSpacing);
      heightBox.Width = ClientRectangle.Right - App.DialogSpacing - heightBox.Left;
      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
        heightBox.KeyPress += keyPressHdr;
      Controls.Add(heightBox);

      graphics.Dispose();

      if(App.DevCap.Lvl >= DevCapLvl.PocketPc)
      {
        cancelBtn.Location = new Point(heightBox.Right - cancelBtn.Width, heightBox.Bottom + App.DialogSpacing);
        cancelBtn.Text = App.GetStr("Cancel");
        cancelBtn.DialogResult = DialogResult.Cancel;
        cancelBtn.KeyPress += keyPressHdr;

        okBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - okBtn.Width, cancelBtn.Top);
        okBtn.Text = App.GetStr("OK");
        okBtn.Click += okHdr;
        okBtn.KeyPress += keyPressHdr;

        Controls.Add(okBtn);
        Controls.Add(cancelBtn);
      }
      else
      {
        okItem.Text = App.GetStr("OK");
        okItem.Click += okHdr;
        Menu.MenuItems.Add(okItem);
        cancelItem.Text = App.GetStr("Cancel");
        cancelItem.Click += new EventHandler(CancelClicked);
        Menu.MenuItems.Add(cancelItem);
      }

      widthBox.Focus();
    }
  }
}
