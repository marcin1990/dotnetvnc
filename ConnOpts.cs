//  Copyright (c) 2004-2005 Rocky Lo. All Rights Reserved.
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

namespace Vnc.Viewer
{
  /// <remarks>This class is responsible for loading and saving connection parameters.</remarks>
  internal class ConnOpts
  {
    private const string ConnName = "Connection";
    private const string HostName = "Host";
    private const string PortName = "Port";
    private const string PasswdName = "Password";

    internal string Host = null;
    internal int Port = -1;
    internal string Passwd = null;
    internal ViewOpts ViewOpts = null;

    internal ConnOpts(string host, int port, string passwd, ViewOpts viewOpts)
    {
      if(host == null)
        throw new ArgumentException("host", "Cannot be null");
      // TODO: Check the minimum and maximum values.
      if(port < 0)
        throw new ArgumentException("port", "Not a valid value");
      if(viewOpts == null)
        throw new ArgumentException("viewOpts", "Cannot be null");

      Host = host;
      Port = port;
      Passwd = passwd;
      ViewOpts = viewOpts;
    }

    internal ConnOpts(string fileName)
    {
      Load(fileName);
    }

    internal void Save(string fileName, bool savePwd)
    {
      XmlDocument doc = new XmlDocument();
      XmlElement rootElem = doc.CreateElement(ConnName);
      doc.AppendChild(rootElem);

      XmlElement elem = doc.CreateElement(HostName);
      elem.InnerText = Host;
      rootElem.AppendChild(elem);

      elem = doc.CreateElement(PortName);
      elem.InnerText = Port.ToString();
      rootElem.AppendChild(elem);

      if(savePwd)
      {
        elem = doc.CreateElement(PasswdName);
        elem.InnerText = Passwd;
        rootElem.AppendChild(elem);
      }

      ViewOpts.AddToXml(doc, rootElem);

      doc.Save(fileName);
    }

    internal void Load(string fileName)
    {
      XmlDocument doc = new XmlDocument();
      doc.Load(fileName);

      XmlNodeList list = doc.GetElementsByTagName(HostName);
      if(list.Count > 0)
        Host = list[0].InnerText;
      else
        throw new FormatException("Host name is not found.");

      list = doc.GetElementsByTagName(PortName);
      if(list.Count > 0)
        Port = Int32.Parse(list[0].InnerText);
      else
        throw new FormatException("Port number is not found.");

      list = doc.GetElementsByTagName(PasswdName);
      if(list.Count > 0)
        Passwd = list[0].InnerText;
      else
        Passwd = ""; // This means we did not save the password.

      ViewOpts = new ViewOpts();
      ViewOpts.ReadFromXml(doc, doc.DocumentElement);
    }
  }
}
