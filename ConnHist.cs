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
using System.Collections;
using System.IO;

namespace Vnc.Viewer
{
  /// <remarks>This class is responsible for saving and loading the connection history.</remarks>
  internal class ConnHist
  {
    private const string HistName = "History";
    private const string EntryName = "Entry";

    private ArrayList connHist = new ArrayList();

    internal string this[byte index]
    {
      get
      {
        return (string)connHist[index];
      }
    }

    internal byte Count
    {
      get
      {
        return (byte)connHist.Count;
      }
    }

    internal ConnHist(string fileName)
    {
      Load(fileName);
    }

    internal void Save(string fileName)
    {
      XmlDocument doc = new XmlDocument();
      XmlElement rootElem = doc.CreateElement(HistName);
      doc.AppendChild(rootElem);

      for(byte i = 0; i < connHist.Count; i++)
      {
        XmlElement elem = doc.CreateElement(EntryName);
        elem.InnerText = this[i];
        rootElem.AppendChild(elem);
      }

      doc.Save(fileName);
    }

    internal void Load(string fileName)
    {
      connHist.Clear();

      XmlDocument doc = new XmlDocument();
      try
      {
        doc.Load(fileName);
      }
      catch(FileNotFoundException)
      {
        return;
      }

      XmlNodeList list = doc.GetElementsByTagName(EntryName);
      for(int i = 0; i < Math.Min(App.MaxConnHist, list.Count); i++)
        connHist.Add(list[i].InnerText);
    }

    internal void Add(string entry)
    {
      // Remove duplicates.
      if(connHist.Contains(entry))
        connHist.Remove(entry);
      // Insert only if the maximum has not reached.
      if(connHist.Count < App.MaxConnHist)
        connHist.Insert(0, entry);
    }
  }
}
