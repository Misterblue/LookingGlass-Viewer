﻿/* Copyright 2008 (c) Robert Adams
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * The name of the copyright holder may not be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.World {
/// <summary>
/// Keeps a list of the possible grids and returns info as requested
/// </summary>
public class Grids {

    public readonly static string Current = "CURRENT";

    private ParameterSet m_gridInfo = null;
    private string m_currentGrid = "UnknownXXYYZZ";

    public Grids() {
        LookingGlassBase.Instance.AppParams.AddDefaultParameter("Grids.Filename.Directory",
            Utilities.GetDefaultApplicationStorageDir(null),
            "Directory that should contain the grid filename");
        LookingGlassBase.Instance.AppParams.AddDefaultParameter("Grids.Filename", "Grids.json",
            "Filename of grid specs");
    }

    // cause the grid information to be reloaded
    public void Reload() {
        m_gridInfo = null;
    }

    // set the grid name so Grids.Current works
    public void SetCurrentGrid(string currentGrid) {
        m_currentGrid = currentGrid;
    }

    public string GridParameter(string gridName, string parm) {
        CheckInit();
        string ret = null;
        string lookupGrid = gridName;
        if (gridName == "CURRENT") lookupGrid = m_currentGrid;
        try {
            if (m_gridInfo.HasParameter(lookupGrid)) {
                OMVSD.OSDMap gInfo = (OMVSD.OSDMap)m_gridInfo.ParamValue(lookupGrid);
                ret = gInfo[parm].AsString();
            }
        }
        catch {
            ret = null;
        }
        return ret;
    }

    public string GridLoginURI(string gridName) {
        CheckInit();
        string ret = null;
        string lookupGrid = gridName;
        if (gridName == "CURRENT") lookupGrid = m_currentGrid;
        try {
            if (m_gridInfo.HasParameter(lookupGrid)) {
                OMVSD.OSDMap gInfo = (OMVSD.OSDMap)m_gridInfo.ParamValue(lookupGrid);
                ret = gInfo["LoginURL"].AsString();
            }
        }
        catch {
            ret = null;
        }
        return ret;
    }

    // Performs an action on each map which describes a grid ("Name", "LoginURL", ...)
    public void ForEach(Action<OMVSD.OSDMap> act) {
        CheckInit();
        try {
            m_gridInfo.ForEach(delegate(string k, OMVSD.OSD v) {
                act((OMVSD.OSDMap)v);
            });
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "GridList.ForEach: Exception: {0}", e.ToString());
        }
    }
    
    // see that the grid info is read in. Called at the beginning of every data access method
    private void CheckInit() {
        if (m_gridInfo == null) {
            string gridsFilename = "";
            try {
                m_gridInfo = new ParameterSet();
                gridsFilename = Path.Combine(LookingGlassBase.Instance.AppParams.ParamString("Grids.Filename.Directory"),
                                    LookingGlassBase.Instance.AppParams.ParamString("Grids.Filename"));
                if (!File.Exists(gridsFilename)) {
                    // if the user copy of the config file doesn't exist, copy the default into place
                    string gridsDefaultFilename = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, 
                                    LookingGlassBase.Instance.AppParams.ParamString("Grids.Filename"));
                    if (File.Exists(gridsDefaultFilename)) {
                        File.Copy(gridsDefaultFilename, gridsFilename);
                    }
                    else {
                        LogManager.Log.Log(LogLevel.DBADERROR, "GridManager: GRIDS FILE DOES NOT EXIST: {0}", gridsFilename);
                        gridsFilename = null;
                    }
                }
                if (gridsFilename != null) {
                    m_gridInfo.AddFromFile(gridsFilename);
                }
            }
            catch (Exception e) {
                LogManager.Log.Log(LogLevel.DBADERROR, "GridManager: FAILED READING GRIDS FILE '{0}': {1}",
                        gridsFilename, e.ToString());

            }
        }
    }

}
}
