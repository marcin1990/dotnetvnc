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
using System.IO;
using System.Xml;
using System.Drawing;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  internal class SessDlgDt : SessDlg
  {
    protected TabControl tabCtrl = new TabControl();
    protected Button okBtn = new Button();
    protected Button cancelBtn = new Button();
    protected Button aboutBtn = new Button();

    protected TabPage generalPage = new TabPage();
    protected ListBox recentBox = new ListBox();

    protected TabPage displayPage = new TabPage();

    protected TabPage othersPage = new TabPage();

    protected TabPage saveLoadPage = new TabPage();
    protected Button saveConnOptsBtn = new Button();
    protected Button saveConnOptsPwdBtn = new Button();
    protected Button loadConnOptsBtn = new Button();
    protected Button saveDefsBtn = new Button();
    protected Button restoreDefsBtn = new Button();

    internal SessDlgDt() : base()
    {}

    internal SessDlgDt(ViewOpts viewOpts) : base(viewOpts)
    {}

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
      GetPasswd();
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
      SetOptions(connOpts.ViewOpts);
    }

    protected override void AddConnHistEntry(string entry)
    {
      recentBox.Items.Add(entry);
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
      viewOnlyBox.Width = othersPage.ClientRectangle.Right - viewOnlyBox.Left;
      shareServBox.Width = othersPage.ClientRectangle.Right - shareServBox.Left;
      saveConnOptsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveConnOptsBtn.Left;
      saveConnOptsPwdBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveConnOptsPwdBtn.Left;
      loadConnOptsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - loadConnOptsBtn.Left;
      saveDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - saveDefsBtn.Left;
      restoreDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - restoreDefsBtn.Left;
    }

    protected override void OnLoad(EventArgs e)
    {
      base.OnLoad(e);

      Graphics graphics = CreateGraphics();
      KeyPressEventHandler keyPressHdr = new KeyPressEventHandler(KeyPressed);

      aboutBtn.Location = new Point(ClientRectangle.Right - App.DialogSpacing - aboutBtn.Width, ClientRectangle.Bottom - App.DialogSpacing - aboutBtn.Height);
      aboutBtn.Text = App.GetStr("About");
      aboutBtn.Click += aboutHdr;
      aboutBtn.KeyPress += keyPressHdr;
      cancelBtn.Location = new Point(aboutBtn.Left - App.DialogSpacing - cancelBtn.Width, ClientRectangle.Bottom - App.DialogSpacing - cancelBtn.Height);
      cancelBtn.Text = App.GetStr("Cancel");
      cancelBtn.DialogResult = DialogResult.Cancel;
      cancelBtn.KeyPress += keyPressHdr;
      okBtn.Location = new Point(cancelBtn.Left - App.DialogSpacing - okBtn.Width, cancelBtn.Top);
      okBtn.Text = App.GetStr("OK");
      okBtn.Click += okHdr;
      okBtn.KeyPress += keyPressHdr;
      tabCtrl.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      tabCtrl.Size = new Size(aboutBtn.Right - tabCtrl.Left, aboutBtn.Top - App.DialogSpacing - tabCtrl.Top);
      Controls.Add(tabCtrl);
      Controls.Add(okBtn);
      Controls.Add(cancelBtn);
      Controls.Add(aboutBtn);

      tabCtrl.TabPages.Add(generalPage);
      generalPage.Text = App.GetStr("General");
      servLbl.Size = graphics.MeasureString(servLbl.Text, Font).ToSize();
      passwdLbl.Size = graphics.MeasureString(passwdLbl.Text, Font).ToSize();
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
      remoteEndPtLbl.Width = generalPage.ClientRectangle.Right - remoteEndPt.Left;
      remoteEndPtLbl.Height = graphics.MeasureString(remoteEndPtLbl.Text, Font).ToSize().Height;
      generalPage.Controls.Add(remoteEndPtLbl);
      passwdLbl.Location = new Point(servLbl.Left, remoteEndPtLbl.Bottom + App.DialogSpacing);
      generalPage.Controls.Add(passwdLbl);
      passwdBox.Location = new Point(passwdLbl.Right + App.DialogSpacing, passwdLbl.Top);
      passwdBox.Width = generalPage.ClientRectangle.Right - App.DialogSpacing - passwdBox.Left;
      passwdBox.KeyPress += keyPressHdr;
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
      rotateLbl.Size = graphics.MeasureString(rotateLbl.Text, Font).ToSize();
      pixelSizeLbl.Size = graphics.MeasureString(pixelSizeLbl.Text, Font).ToSize();
      rotateLbl.Width = Math.Max(rotateLbl.Width, pixelSizeLbl.Width);
      pixelSizeLbl.Width = rotateLbl.Width;
      fullScrnBox.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      fullScrnBox.KeyPress += keyPressHdr;
      fullScrnBox.Width = displayPage.ClientRectangle.Right - fullScrnBox.Left;
      displayPage.Controls.Add(fullScrnBox);
      rotateLbl.Location = new Point(fullScrnBox.Left, fullScrnBox.Bottom + App.DialogSpacing);
      displayPage.Controls.Add(rotateLbl);
      rotateBox.Location = new Point(rotateLbl.Right + App.DialogSpacing, rotateLbl.Top);
      rotateBox.Width = displayPage.ClientRectangle.Right - App.DialogSpacing - rotateBox.Left;
      rotateBox.KeyPress += keyPressHdr;
      displayPage.Controls.Add(rotateBox);
      pixelSizeLbl.Location = new Point(rotateLbl.Left, rotateBox.Bottom + App.DialogSpacing);
      displayPage.Controls.Add(pixelSizeLbl);
      pixelSizeBox.Location = new Point(pixelSizeLbl.Right + App.DialogSpacing, pixelSizeLbl.Top);
      pixelSizeBox.Width = displayPage.ClientRectangle.Right - App.DialogSpacing - pixelSizeBox.Left;
      pixelSizeBox.KeyPress += keyPressHdr;
      displayPage.Controls.Add(pixelSizeBox);

      tabCtrl.TabPages.Add(othersPage);
      othersPage.Text = App.GetStr("Others");
      viewOnlyBox.Location = new Point(App.DialogSpacing, App.DialogSpacing);
      viewOnlyBox.KeyPress += keyPressHdr;
      viewOnlyBox.Width = othersPage.ClientRectangle.Right - viewOnlyBox.Left;
      othersPage.Controls.Add(viewOnlyBox);
      shareServBox.Location = new Point(viewOnlyBox.Left, viewOnlyBox.Bottom + App.DialogSpacing);
      shareServBox.KeyPress += keyPressHdr;
      shareServBox.Width = othersPage.ClientRectangle.Right - shareServBox.Left;
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
      saveDefsBtn.Click += saveDefsHdr;
      saveLoadPage.Controls.Add(saveDefsBtn);
      restoreDefsBtn.Location = new Point(saveDefsBtn.Left, saveDefsBtn.Bottom + App.DialogSpacing);
      restoreDefsBtn.Text = App.GetStr("Restore default settings");
      restoreDefsBtn.Width = saveLoadPage.ClientRectangle.Right - App.DialogSpacing - restoreDefsBtn.Left;
      restoreDefsBtn.KeyPress += keyPressHdr;
      restoreDefsBtn.Click += restoreDefsHdr;
      saveLoadPage.Controls.Add(restoreDefsBtn);

      graphics.Dispose();

      remoteEndPt.Focus();
    }
  }
}
