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
using System.Drawing;
using System.Threading;
using System.Collections;
using System.Reflection;
using System.IO;
using System.Xml;
using System.Windows.Forms;

namespace Vnc.Viewer
{
  /// <remarks>
  ///   This class contains the entry point of the program. It is also
  ///   responsible for decoding command-line parameters, loading configurations,
  ///   etc.
  /// </remarks>
  public class App
  {
    /// <summary>This is the default spacing between UI elements.</summary>
    internal const byte DialogSpacing = 5;

    /// <summary>This determines how frequently the event loop wakes up.</summary>
    // TODO: Choose an optimal value.
    internal const byte Delta = 10;

    /// <summary>DevCap automatically detects device capabilities.</summary>
    internal static readonly DevCap DevCap = new DevCap();

    /// <summary>In .NET CF we don't have the system defined colors.</summary>
    internal static readonly Color Black = Color.FromArgb(0, 0, 0);
    internal static readonly Color Red = Color.FromArgb(255, 0, 0);
    internal static readonly Color Blue = Color.FromArgb(0, 0, 255);

    /// <summary>This determines where the default settings are stored.</summary>
    internal static readonly string SettingsFileName;

    /// <summary>This determines where the connection history is stored.</summary>
    internal static readonly string ConnHistFileName;

    /// <summary>This table stores all the system messages.</summary>
    // TODO: Implement this.
    private static Hashtable strTbl = new Hashtable();

    /// <summary>This list contains references to all the connections.</summary>
    private static ArrayList connList = new ArrayList();

    /// <summary>These are used for decoding command-line parameters.</summary>
    private static bool isVncFileSpec = false;
    private static string vncFileName = "";

    static App()
    {
      String fullAppName = Assembly.GetExecutingAssembly().GetName().CodeBase;
      String fullAppPath = Path.GetDirectoryName(fullAppName);
      SettingsFileName = Path.Combine(fullAppPath, "settings.xml");
      ConnHistFileName = Path.Combine(fullAppPath, "history.xml");

      // This ensures that we can run on both desktop and PPC.
      // On a desktop there is a prefix "file:\" and XmlDocument.Save does not like it.
      if(SettingsFileName.StartsWith("file:\\"))
        SettingsFileName = SettingsFileName.Substring(6);
      if(ConnHistFileName.StartsWith("file:\\"))
        ConnHistFileName = ConnHistFileName.Substring(6);
    }

    internal static string GetStr(string key)
    {
      string val = (string)strTbl[key];
      if(val == null)
        return key;
      else
        return val;
    }

    internal static void AddConn(Conn conn)
    {
      connList.Add(conn);
    }

    internal static void RemoveConn(Conn conn)
    {
      connList.Remove(conn);
    }

    internal static void AboutBox()
    {
      MessageBox.Show(App.GetStr(@"
.NET VNC Viewer 1.0.1.1
Mar 5, 2005

Copyright (C) 2004-2005 Rocky Lo. All Rights Reserved.
Copyright (C) 2002 Ultr@VNC Team Members. All Rights Reserved.
Copyright (C) 2000-2002 Const Kaplinsky. All Rights Reserved.
Copyright (C) 2002 RealVNC Ltd. All Rights Reserved.
Copyright (C) 1999 AT&T Laboratories Cambridge. All Rights Reserved.
"),
                      App.GetStr("About"),
                      MessageBoxButtons.OK,
                      MessageBoxIcon.Asterisk,
                      MessageBoxDefaultButton.Button1);
    }

    internal static void NewConn(string fileName)
    {
      try
      {
        ConnOpts connOpts = new ConnOpts(fileName);
        Conn conn = new Conn();
        conn.Run(connOpts);
      }
      catch(FileNotFoundException)
      {
        MessageBox.Show(App.GetStr("Unable to open the file!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to read from the file!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
      catch(XmlException)
      {
        MessageBox.Show(App.GetStr("The file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
      catch(FormatException)
      {
        MessageBox.Show(App.GetStr("The file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
      }
    }

    internal static void NewConn()
    {
      Conn conn = new Conn();
      try
      {
        ViewOpts viewOpts = new ViewOpts(App.SettingsFileName);
        conn.Run(viewOpts);
      }
      catch(FileNotFoundException)
      {
        conn.Run();
      }
      catch(IOException)
      {
        MessageBox.Show(App.GetStr("Unable to read from the setting file!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        conn.Run();
      }
      catch(XmlException)
      {
        MessageBox.Show(App.GetStr("The setting file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        conn.Run();
      }
      catch(FormatException)
      {
        MessageBox.Show(App.GetStr("The setting file is corrupted!"),
                        App.GetStr("Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Exclamation,
                        MessageBoxDefaultButton.Button1);
        conn.Run();
      }
    }

    private static void ParseArgs(string[] args)
    {
      for(int i = 0; i < args.Length; i++)
      {
        if(!args[i].StartsWith("-"))
        {
          isVncFileSpec = true;
          vncFileName = args[i];
        }
      }
    }

    public static int Main(string[] args)
    {
      ParseArgs(args);

      if(isVncFileSpec)
        NewConn(vncFileName);
      else
      {
        if(DevCap.Lvl >= DevCapLvl.Desktop)
          NewConn();
        else
        {
          // This is a hack. If we don't have at least one window visible,
          // new windows wil be created in the background on Pocket PCs.
          // TODO: Is this necessary for Smartphones?
          Form dummy = new DummyForm();
          dummy.Show();
          NewConn();

          // By now a View should have been created so we can close the dummy.
          dummy.Close();
        }
      }

      while(connList.Count > 0)
      {
        try
        {
          Application.DoEvents();
          Thread.Sleep(Delta);
        }
        catch(ObjectDisposedException)
        {
          // Catching ObjectDisposedException to work around a bug in .NET CF prior to SP3.
          // This exception is thrown when the server drops a connection.
        }
      }

      return 0;
    }
  }
}
