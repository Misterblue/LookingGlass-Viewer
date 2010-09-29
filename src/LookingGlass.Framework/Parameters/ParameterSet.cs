/* Copyright (c) 2008 Robert Adams
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
using System.Globalization;
using System.IO;
using System.Text;
using LookingGlass.Framework.Logging;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.Parameters {
    
public class ParameterSet : IParameters, IDisplayable {

    public event ParamValueModifiedCallback OnModifiedCallback;
    
    protected paramErrorType paramErrorMethod = paramErrorType.eException;
    public paramErrorType ParamErrorMethod {
        get { return paramErrorMethod; }
        set { paramErrorMethod = value; }
    }

    protected OMVSD.OSDMap m_params = null;
    protected OMVSD.OSDMap m_paramDescription = null;

    // A parameter can have the value of a delegate that is called when referenced
    // to get the actual runtime value.
    public delegate OMVSD.OSD ParameterSetRuntimeValue(string key);
    // If a parameter has a delegate value, the value is overlayed with this
    // structure. This is checked first before looking in m_params.
    // The key is always lower case.
    protected Dictionary<string, ParameterSetRuntimeValue> m_runtimeValues = null;

    public ParameterSet() {
        m_params = new OMVSD.OSDMap();
        m_paramDescription = new OMVSD.OSDMap();
        ParamErrorMethod = paramErrorType.eNullValue;
        m_runtimeValues = new Dictionary<string, ParameterSetRuntimeValue>();
    }

    /// <summary>
    /// Depending on the setting of the error type, generate the correct
    /// type of error.
    /// </summary>
    /// <param name="err"></param>
    /// <returns></returns>
    protected string ReturnParamError(string err) {
        string ret = null;
        switch (ParamErrorMethod) {
            case paramErrorType.eDefaultValue:
                ret = "";
                break;
            case paramErrorType.eException:
                throw new ParameterException(err);
            case paramErrorType.eNullValue:
                ret = null;
                break;
        }
        return ret;
    }

    public void Update(string key, OMVSD.OSD value) {
        if (InternalUpdate(key, value)) {
            if (OnModifiedCallback != null) OnModifiedCallback(this, key, value);
        }
    }

    public void UpdateSilent(string key, OMVSD.OSD value) {
        InternalUpdate(key, value);
    }

    // updates and returns true if the value changed
    private bool InternalUpdate(string key, OMVSD.OSD value) {
        bool ret = false;
        lock (m_params) {
            if (m_params.ContainsKey(key.ToLower())) {
                m_params[key.ToLower()] = value;
                ret = true;
            }
        }
        return ret;
    }

    public bool HasParameter(string key) {
        return m_params.ContainsKey(key.ToLower());
    }

    public void Add(string key, string value, string desc) {
        string lkey = key.ToLower();
        lock (m_paramDescription) {
            if (m_paramDescription.ContainsKey(lkey)) {
                m_paramDescription.Remove(lkey);
            }
            m_paramDescription.Add(lkey, new OMVSD.OSDString(desc));
            Add(lkey, value);
        }
    }

    public void Add(string key, string value) {
        string lkey = key.ToLower();
        lock (m_params) {
            if (m_params.ContainsKey(lkey)) {
                m_params.Remove(lkey);
            }
            m_params.Add(lkey, new OMVSD.OSDString(value));
        }
    }

    public void Add(string key, OMVSD.OSD value) {
        string lkey = key.ToLower();
        lock (m_params) {
            if (m_params.ContainsKey(lkey)) {
                m_params.Remove(lkey);
            }
            m_params.Add(lkey, value);
        }
    }

    /// <summary>
    /// These overlay what is in the parameter set. They are string values
    /// that are fetched at runtime.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="runtimeVal"></param>
    public void Add(string key, ParameterSetRuntimeValue runtimeVal) {
        string lkey = key.ToLower();
        lock (m_params) {
            if (m_runtimeValues.ContainsKey(lkey)) {
                m_runtimeValues.Remove(lkey);
            }
            m_runtimeValues.Add(lkey, runtimeVal);
        }
    }

    public void Add(string key, ParameterSetRuntimeValue runtimeVal, string desc) {
        string lkey = key.ToLower();
        lock (m_params) {
            if (m_paramDescription.ContainsKey(lkey)) {
                m_paramDescription.Remove(lkey);
            }
            m_paramDescription.Add(lkey, new OMVSD.OSDString(desc));
            Add(lkey, runtimeVal);
        }
    }

    public OMVSD.OSD ParamValue(string key) {
        OMVSD.OSD ret = null;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(lkey)) {
                    ret = m_runtimeValues[lkey](key);
                    success = true;
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        ret = m_params[lkey];
                        success = true;
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = new OMVSD.OSDMap();
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("OSD parameter " + key + " not found");
                case paramErrorType.eNullValue:
                    ret = null;
                    break;
            }
        }
        return ret;
    }

    public string ParamString(string key) {
        string ret = null;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(lkey)) {
                    ret = m_runtimeValues[lkey](lkey).AsString();
                    success = true;
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        ret = m_params[lkey].AsString();
                        success = true;
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = "";
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("Parameter " + key + " not found");
                case paramErrorType.eNullValue:
                    ret = null;
                    break;
            }
        }
        return ret;
    }

    public int ParamInt(string key) {
        int ret = -1;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(lkey)) {
                    ret = m_runtimeValues[lkey](lkey).AsInteger();
                    success = true;
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        ret = m_params[lkey].AsInteger();
                        success = true;
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = -1;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("Int param '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = -1;
                    break;
            }
        }
        return ret;
    }

    public bool ParamBool(string key) {
        bool ret = false;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(key)) {
                    ret = m_runtimeValues[lkey](lkey).AsBoolean();
                    success = true;
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        ret = m_params[lkey].AsBoolean();
                        success = true;
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = false;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("Bool param '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = false;
                    break;
            }
        }
        return ret;
    }

    public float ParamFloat(string key) {
        float ret = 0f;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(key)) {
                    ret = (float)m_runtimeValues[lkey](lkey).AsReal();
                    success = true;
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        ret = (float)m_params[lkey].AsReal();
                        success = true;
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = 0f;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("Float param '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = 0f;
                    break;
            }
        }
        return ret;
    }

    public OMV.Vector3 ParamVector3(string key) {
        OMV.Vector3 ret = OMV.Vector3.Zero;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(key)) {
                    if (OMV.Vector3.TryParse(m_runtimeValues[lkey](lkey).AsString(), out ret)) {
                        success = true;
                    }
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        if (OMV.Vector3.TryParse(m_params[lkey].AsString(), out ret)) {
                            success = true;
                        }
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = OMV.Vector3.Zero;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("Float param '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = OMV.Vector3.Zero;
                    break;
            }
        }
        return ret;
    }

    public OMV.Vector4 ParamVector4(string key) {
        OMV.Vector4 ret = OMV.Vector4.Zero;
        string lkey = key.ToLower();
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(key)) {
                    if (OMV.Vector4.TryParse(m_runtimeValues[lkey](lkey).AsString(), out ret)) {
                        success = true;
                    }
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        if (OMV.Vector4.TryParse(m_params[lkey].AsString(), out ret)) {
                            success = true;
                        }
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = OMV.Vector4.Zero;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("Float param '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = OMV.Vector4.Zero;
                    break;
            }
        }
        return ret;
    }

    /// <summary>
    /// Return all of the values for the key. This finds multiple values in the
    /// ini array only.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public List<string> ParamStringArray(string key) {
        string lkey = key.ToLower();
        List<string> ret = new List<string>();
        OMVSD.OSD paramVal = null;
        bool success = false;
        lock (m_params) {
            try {
                if (m_runtimeValues.ContainsKey(lkey)) {
                    paramVal = m_runtimeValues[lkey](lkey);
                }
                else {
                    if (m_params.ContainsKey(lkey)) {
                        paramVal = m_params[lkey];
                    }
                }
                if (paramVal != null) {
                    if (paramVal.Type == OMVSD.OSDType.Array) {
                        OMVSD.OSDArray values = (OMVSD.OSDArray)paramVal;
                        foreach (OMVSD.OSD val in values) {
                            ret.Add(val.AsString());
                        }
                        success = true;
                    }
                }
            }
            catch {
                success = false;
            }
        }
        if (!success) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    // return empty list
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("String array param '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = null;
                    break;
            }
        }
        return ret;
    }

    /// <summary>
    /// Perform an action on all the values in the top level of the ParameterSet.
    /// This hides the fact that there are delegate values
    /// </summary>
    /// <param name="act"></param>
    public void ForEach(ParamAction act) {
        string lastKey = "NOTSET";
        try {
            lock (m_params) {
                foreach (KeyValuePair<string, OMVSD.OSD> kvp in m_params) {
                    lastKey = kvp.Key;
                    act(kvp.Key, kvp.Value);
                }
                foreach (KeyValuePair<string, ParameterSetRuntimeValue> kvp in m_runtimeValues) {
                    lastKey = kvp.Key;
                    act(kvp.Key, kvp.Value(kvp.Key));
                }
            }
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "ParameterSet.ForEach: lastkey={0}, e={0}", lastKey, e.ToString());
        }
    }

    /// <summary>
    /// The parameterset values can change because of teh delgates. This returns a current
    /// view of teh parameterset.
    /// </summary>
    /// <returns></returns>
    public OMVSD.OSDMap GetDisplayable() {
        try {
            OMVSD.OSDMap built = new OMVSD.OSDMap();
            lock (m_params) {
                this.ForEach(delegate(string k, OMVSD.OSD v) {
                    OMVSD.OSDMap valueMap = new OMVSD.OSDMap();
                    valueMap.Add("value", v);
                    if (m_paramDescription.ContainsKey(k)) {
                        valueMap.Add("description", m_paramDescription[k]);
                    }
                    built.Add(k, valueMap);
                });
            }
            return built;
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "GetDisplayable: exception {0}", e.ToString());
        }
        return new OMVSD.OSDMap();
    }

    /// <summary>
    /// Add parameters to this ParameterSet from a file. Bad things happen if  there
    /// are duplicates.
    /// </summary>
    /// <param name="fileName">Name of the file to read from. Ends either in .json or .ini</param>
    /// <returns>true if the addition worked, false if something happened</returns>
    public bool AddFromFile(string fileName) {
        return ReadParameterSet(fileName, this.Add);
    }

    // ROUTINES TO READ IN A PARAMETER SET
    public delegate void InsertParam(string k, OMVSD.OSD v);

    /// <summary>
    /// A funny little static routine to read in a parameter set. You pass it the delegate
    /// that is called to actually add the key/value to the ParameterSet.
    /// </summary>
    /// <param name="inFile"></param>
    /// <param name="storeParam"></param>
    public static bool ReadParameterSet(string inFile, InsertParam storeParam) {
        bool ret = true;
        if (File.Exists(inFile)) {
            try {
                using (Stream inStream = new FileStream(inFile, FileMode.Open) ) {
                    if (inFile.EndsWith(".json")) {
                        // READ JSON
                        OMVSD.OSD parms = OMVSD.OSDParser.DeserializeJson(inStream);
                        if (parms.Type == OpenMetaverse.StructuredData.OSDType.Map) {
                            OMVSD.OSDMap mapParms = (OMVSD.OSDMap)parms;
                            foreach (KeyValuePair<string, OMVSD.OSD> kvp in mapParms) {
                                storeParam(kvp.Key, kvp.Value);
                            }
                        }
                        else {
                            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: CONFIG FILE WAS NOT A JSON MAP");
                            ret = false;
                        }
                    }
                    else {
                        // READ INI
                        using (StreamReader sr = new StreamReader(inStream)) {
                            string inLine;
                            while ((inLine = sr.ReadLine()) != null) {
                                int pos = inLine.IndexOf(";");
                                string desc = null;
                                if (pos >= 0) {
                                    desc = inLine.Substring(pos+1).Trim();
                                    inLine = inLine.Substring(0, pos);
                                }
                                pos = inLine.IndexOf("=");
                                if (pos >= 0) {
                                    string key = inLine.Substring(0, pos).Trim();
                                    string value = inLine.Substring(pos + 1).Trim();
                                    storeParam(key, new OMVSD.OSDString(value));
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e) {
                LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: FAILURE PARSING CONFIG FILE'"
                        + inFile + "':" + e.ToString());
                ret = false;
            }
        }
        return ret;
    }
}
}