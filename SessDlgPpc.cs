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

namespace Vnc.Viewer
{
  /// <remarks>
  ///   OO-wise this is not correct... A SessDlgPpc is not a SessDlgDt.
  ///   Practically a SessDlgPpc shares the same code with SessDlgDt, at
  ///   least for now. So SessDlgPpc inherits SessDlgDt. In the future this
  ///   may change.
  /// </remarks>
  internal class SessDlgPpc : SessDlgDt
  {
    internal SessDlgPpc() : base()
    {
      AdjustSizes();
    }

    internal SessDlgPpc(ViewOpts viewOpts) : base(viewOpts)
    {
      AdjustSizes();
    }

    private void AdjustSizes()
    {
      if(App.DevCap.Res <= ResLvl.Normal)
        return;

      remoteEndPt.Height *= 2;
      passwdBox.Height *= 2;
      fullScrnBox.Height *= 2;
      viewOnlyBox.Height *= 2;
      shareServBox.Height *= 2;
      okBtn.Height *= 2;
      okBtn.Width *= 2;
      cancelBtn.Height *= 2;
      cancelBtn.Width *= 2;
      aboutBtn.Height *= 2;
      aboutBtn.Width *= 2;
      saveConnOptsBtn.Height *= 2;
      saveConnOptsBtn.Width *= 2;
      saveConnOptsPwdBtn.Height *= 2;
      saveConnOptsPwdBtn.Width *= 2;
      loadConnOptsBtn.Height *= 2;
      loadConnOptsBtn.Width *= 2;
      saveDefsBtn.Height *= 2;
      saveDefsBtn.Width *= 2;
      restoreDefsBtn.Height *= 2;
      restoreDefsBtn.Width *= 2;
    }
  }
}
