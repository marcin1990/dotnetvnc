//  Copyright (C) 2004-2005 Rocky Lo. All Rights Reserved.
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
  internal class SessDlgSp : SessDlg
  {
    private MenuItem okItem = new MenuItem();
    private MenuItem generalItem = new MenuItem();
    private MenuItem displayItem = new MenuItem();
    private MenuItem scalingItem = new MenuItem();
    private MenuItem othersItem = new MenuItem();
    private MenuItem saveLoadItem = new MenuItem();
    private MenuItem dashItem = new MenuItem();
    private MenuItem aboutItem = new MenuItem();
    private MenuItem cancelItem = new MenuItem();
    private MenuItem optionsItem = new MenuItem();

    private Panel generalPanel = new Panel();
    private ComboBox recentBox = new ComboBox();

    private Panel displayPanel = new Panel();

    private Panel scalingPanel = new Panel();

    private Panel othersPanel = new Panel();
    private CheckBox sendMouseLocWhenIdleBox = new CheckBox();

    private MenuItem saveDefsItem = new MenuItem();
    private MenuItem restoreDefsItem = new MenuItem();

    internal SessDlgSp() : base()
    {}

    internal SessDlgSp(ViewOpts viewOpts) : base(viewOpts)
    {}

    protected override void GetOptions()
    {
      base.GetOptions();
      viewOpts.SendMouseLocWhenIdle = sendMouseLocWhenIdleBox.Checked;
    }

    protected override void SetOptions(ViewOpts viewOpts)
    {
      base.SetOptions(viewOpts);
      sendMouseLocWhenIdleBox.Checked = viewOpts.SendMouseLocWhenIdle;
    }

    protected override void AddConnHistEntry(string entry)
    {
      recentBox.Items.Add(entry);
    }

    private void RecentBoxChanged(object sender, EventArgs e)
    {
      remoteEndPt.Text = recentBox.Text;
    }

    private void SwitchPanel(MenuItem item)
    {
      generalPanel.Visible = false;
      displayPanel.Visible = false;
      scalingPanel.Visible = false;
      othersPanel.Visible = false;
      if(item == displayItem)
      {
        displayPanel.Visible = true;
        fullScrnBox.Focus();
      }
      else if(item == scalingItem)
      {
        scalingPanel.Visible = true;
        cliScalingBox.Focus();
      }
      else if(item == othersItem)
      {
        othersPanel.Visible = true;
        viewOnlyBox.Focus();
      }
      else // Assume general.
      {
        generalPanel.Visible = true;
        remoteEndPt.Focus();
      }
    }

    private void PanelItemClicked(object sender, EventArgs e)
    {
      SwitchPanel((MenuItem)sender);
    }

    protected override void OnResize(EventArgs e)
    {
      base.OnResize(e);
      generalPanel.Size = ClientRectangle.Size;
      remoteEndPt.Width = generalPanel.ClientRectangle.Right - App.DialogSpacing - remoteEndPt.Left;
      passwdBox.Width = generalPanel.ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      recentBox.Width = generalPanel.ClientRectangle.Right - App.DialogSpacing - recentBox.Left;
      displayPanel.Size = ClientRectangle.Size;
      fullScrnBox.Width = displayPanel.ClientRectangle.Right - fullScrnBox.Left;
      rotateBox.Width = displayPanel.ClientRectangle.Right - App.DialogSpacing - rotateBox.Left;
      pixelSizeBox.Width = displayPanel.ClientRectangle.Right - App.DialogSpacing - pixelSizeBox.Left;
      scalingPanel.Size = ClientRectangle.Size;
      cliScalingBox.Width = scalingPanel.ClientRectangle.Right - App.DialogSpacing - cliScalingBox.Left;
      cliScalingWidthBox.Width = scalingPanel.ClientRectangle.Right - App.DialogSpacing - cliScalingWidthBox.Left;
      cliScalingHeightBox.Width = scalingPanel.ClientRectangle.Right - App.DialogSpacing - cliScalingHeightBox.Left;
      othersPanel.Size = ClientRectangle.Size;
      viewOnlyBox.Width = othersPanel.ClientRectangle.Right - viewOnlyBox.Left;
      shareServBox.Width = othersPanel.ClientRectangle.Right - shareServBox.Left;
      sendMouseLocWhenIdleBox.Width = othersPanel.ClientRectangle.Right - sendMouseLocWhenIdleBox.Left;
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      EventHandler panelItemHdr = new EventHandler(PanelItemClicked);

      okItem.Text = App.GetStr("OK");
      okItem.Click += okHdr;
      Menu.MenuItems.Add(okItem);
      optionsItem.Text = App.GetStr("Options");
      Menu.MenuItems.Add(optionsItem);
      generalItem.Text = App.GetStr("General...");
      generalItem.Click += panelItemHdr;
      optionsItem.MenuItems.Add(generalItem);
      displayItem.Text = App.GetStr("Display...");
      displayItem.Click += panelItemHdr;
      optionsItem.MenuItems.Add(displayItem);
      scalingItem.Text = App.GetStr("Scaling...");
      scalingItem.Click += panelItemHdr;
      optionsItem.MenuItems.Add(scalingItem);
      othersItem.Text = App.GetStr("Others...");
      othersItem.Click += panelItemHdr;
      optionsItem.MenuItems.Add(othersItem);
      saveLoadItem.Text = App.GetStr("Re/Store");
      optionsItem.MenuItems.Add(saveLoadItem);
      dashItem.Text = "-";
      optionsItem.MenuItems.Add(dashItem);
      aboutItem.Text = App.GetStr("About");
      aboutItem.Click += aboutHdr;
      optionsItem.MenuItems.Add(aboutItem);
      cancelItem.Text = App.GetStr("Cancel");
      cancelItem.Click += cancelHdr;
      optionsItem.MenuItems.Add(cancelItem);

      Graphics graphics = CreateGraphics();

      generalPanel.Size = ClientRectangle.Size;
      Controls.Add(generalPanel);
      servLbl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      servLbl.Size = graphics.MeasureString(servLbl.Text, Font).ToSize();
      generalPanel.Controls.Add(servLbl);
      remoteEndPt.Location = new Point(App.DialogSpacing, servLbl.Bottom + App.DialogSpacing);
      remoteEndPt.Width = generalPanel.ClientRectangle.Right - App.DialogSpacing - remoteEndPt.Left;
      generalPanel.Controls.Add(remoteEndPt);
      remoteEndPtLbl.Location = new Point(App.DialogSpacing, remoteEndPt.Bottom + App.DialogSpacing);
      remoteEndPtLbl.Size = graphics.MeasureString(remoteEndPtLbl.Text, Font).ToSize();
      generalPanel.Controls.Add(remoteEndPtLbl);
      passwdLbl.Location = new Point(App.DialogSpacing, remoteEndPtLbl.Bottom + App.DialogSpacing);
      passwdLbl.Size = graphics.MeasureString(passwdLbl.Text, Font).ToSize();
      generalPanel.Controls.Add(passwdLbl);
      passwdBox.Location = new Point(App.DialogSpacing, passwdLbl.Bottom + App.DialogSpacing);
      passwdBox.Width = generalPanel.ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      generalPanel.Controls.Add(passwdBox);
      recentLbl.Location = new Point(App.DialogSpacing, passwdBox.Bottom + App.DialogSpacing);
      recentLbl.Size = graphics.MeasureString(recentLbl.Text, Font).ToSize();
      generalPanel.Controls.Add(recentLbl);
      recentBox.Location = new Point(App.DialogSpacing, recentLbl.Bottom + App.DialogSpacing);
      recentBox.Width = generalPanel.ClientRectangle.Right - App.DialogSpacing - recentBox.Left;
      recentBox.SelectedIndexChanged += new EventHandler(RecentBoxChanged);
      generalPanel.Controls.Add(recentBox);

      displayPanel.Size = ClientRectangle.Size;
      Controls.Add(displayPanel);
      fullScrnBox.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      fullScrnBox.Width = displayPanel.ClientRectangle.Right - fullScrnBox.Left;
      displayPanel.Controls.Add(fullScrnBox);
      rotateLbl.Location = new Point(App.DialogSpacing, fullScrnBox.Bottom + App.DialogSpacing);
      rotateLbl.Size = graphics.MeasureString(rotateLbl.Text, Font).ToSize();
      displayPanel.Controls.Add(rotateLbl);
      rotateBox.Location = new Point(App.DialogSpacing, rotateLbl.Bottom + App.DialogSpacing);
      rotateBox.Width = displayPanel.ClientRectangle.Right - App.DialogSpacing - rotateBox.Left;
      displayPanel.Controls.Add(rotateBox);
      pixelSizeLbl.Location = new Point(App.DialogSpacing, rotateBox.Bottom + App.DialogSpacing);
      pixelSizeLbl.Size = graphics.MeasureString(pixelSizeLbl.Text, Font).ToSize();
      displayPanel.Controls.Add(pixelSizeLbl);
      pixelSizeBox.Location = new Point(App.DialogSpacing, pixelSizeLbl.Bottom + App.DialogSpacing);
      pixelSizeBox.Width = displayPanel.ClientRectangle.Right - App.DialogSpacing - pixelSizeBox.Left;
      displayPanel.Controls.Add(pixelSizeBox);

      scalingPanel.Size = ClientRectangle.Size;
      Controls.Add(scalingPanel);
      cliScalingLbl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      cliScalingLbl.Size = graphics.MeasureString(cliScalingLbl.Text, Font).ToSize();
      scalingPanel.Controls.Add(cliScalingLbl);
      cliScalingBox.Location = new Point(App.DialogSpacing, cliScalingLbl.Bottom + App.DialogSpacing);
      cliScalingBox.Width = scalingPanel.ClientRectangle.Right - App.DialogSpacing - cliScalingBox.Left;
      scalingPanel.Controls.Add(cliScalingBox);
      cliScalingWidthLbl.Location = new Point(App.DialogSpacing, cliScalingBox.Bottom + App.DialogSpacing);
      cliScalingWidthLbl.Size = graphics.MeasureString(cliScalingWidthLbl.Text, Font).ToSize();
      scalingPanel.Controls.Add(cliScalingWidthLbl);
      cliScalingWidthBox.Location = new Point(cliScalingWidthLbl.Right + App.DialogSpacing, cliScalingWidthLbl.Top);
      cliScalingWidthBox.Width = scalingPanel.ClientRectangle.Right - App.DialogSpacing - cliScalingWidthBox.Left;
      scalingPanel.Controls.Add(cliScalingWidthBox);
      cliScalingHeightLbl.Location = new Point(App.DialogSpacing, cliScalingWidthBox.Bottom + App.DialogSpacing);
      cliScalingHeightLbl.Size = graphics.MeasureString(cliScalingHeightLbl.Text, Font).ToSize();
      scalingPanel.Controls.Add(cliScalingHeightLbl);
      cliScalingHeightBox.Location = new Point(cliScalingHeightLbl.Right + App.DialogSpacing, cliScalingHeightLbl.Top);
      cliScalingHeightBox.Width = scalingPanel.ClientRectangle.Right - App.DialogSpacing - cliScalingHeightBox.Left;
      scalingPanel.Controls.Add(cliScalingHeightBox);

      othersPanel.Size = ClientRectangle.Size;
      Controls.Add(othersPanel);
      viewOnlyBox.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      viewOnlyBox.Width = othersPanel.ClientRectangle.Right - viewOnlyBox.Left;
      othersPanel.Controls.Add(viewOnlyBox);
      shareServBox.Location = new Point(App.DialogSpacing, viewOnlyBox.Bottom + App.DialogSpacing);
      shareServBox.Width = othersPanel.ClientRectangle.Right - shareServBox.Left;
      othersPanel.Controls.Add(shareServBox);
      sendMouseLocWhenIdleBox.Text = App.GetStr("Send mouse location when idle");
      sendMouseLocWhenIdleBox.Location = new Point(App.DialogSpacing, shareServBox.Bottom + App.DialogSpacing);
      sendMouseLocWhenIdleBox.Width = othersPanel.ClientRectangle.Right - sendMouseLocWhenIdleBox.Left;
      othersPanel.Controls.Add(sendMouseLocWhenIdleBox);

      graphics.Dispose();

      saveDefsItem.Text = App.GetStr("Save settings as default");
      saveDefsItem.Click += saveDefsHdr;
      saveLoadItem.MenuItems.Add(saveDefsItem);
      restoreDefsItem.Text = App.GetStr("Restore default settings");
      restoreDefsItem.Click += restoreDefsHdr;
      saveLoadItem.MenuItems.Add(restoreDefsItem);

      SwitchPanel(generalItem);
    }
  }
}
