/* Copyright (c) Robert Adams
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
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.Parameters {
public class AppParameters : IAppParameters, IParameterPersist {


#pragma warning disable 0067    // disable unused event warning
    public event ParamValueModifiedCallback OnModifiedCallback;
#pragma warning restore 0067

    protected paramErrorType paramErrorMethod = paramErrorType.eNullValue;
    public paramErrorType ParamErrorMethod {
        get { return paramErrorMethod; }
        set { paramErrorMethod = value; }
    }

    protected ParameterSet m_defaultParams;
    public ParameterSet DefaultParameters { get { return m_defaultParams; } }
    protected ParameterSet m_iniParams;
    public ParameterSet IniParameters { get { return m_iniParams; } }
    protected ParameterSet m_userParams;
    public ParameterSet UserParameters { get { return m_userParams; } }
    protected ParameterSet m_overrideParams;
    public ParameterSet OverrideParameters { get { return m_overrideParams; } }

    public AppParameters() {
        m_defaultParams = new ParameterSet();
        m_iniParams = new ParameterSet();
        m_userParams = new ParameterSet();
        m_overrideParams = new ParameterSet();

        AddDefaultParameter("Settings.File", 
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "LookingGlass.json"),
            "Persistant settings configuration file");
        AddDefaultParameter("Settings.Modules", 
            Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Modules.json"),
            "Modules configuration file");
    }

    #region IParameterPersist

    public void ReadParameterPersist() {
        string moduleFile = ParamString("Settings.Modules");
        try {
            ParameterSet.ReadParameterSet(moduleFile, m_iniParams.Add);
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: COULD NOT READ MODULES FILE: " + e.ToString());
        }
        string iniFile = ParamString("Settings.File");
        try {
            ParameterSet.ReadParameterSet(iniFile, m_iniParams.Add);
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: COULD NOT READ CONFIGURATION FILE '" + iniFile + "': " + e.ToString());
        }
    }

    public void WriteParameterPersist() {
        // TODO:
        return;
    }
    #endregion IParameterPersist


    #region IAppParameters
    public void AddDefaultParameter(string key, string value, string desc) {
        m_defaultParams.Add(key, value, desc);
    }

    public void AddIniParameter(string key, string value, string desc) {
        m_iniParams.Add(key, value);
    }

    public void AddUserParameter(string key, string value, string desc) {
        m_userParams.Add(key, value);
    }

    public void AddOverrideParameter(string key, string value) {
        m_overrideParams.Add(key, value);
    }
    #endregion IAppParameters

    #region IParameters
    public void Add(string key, string value) {
        // TODO:
    }
    public void Add(string key, OMVSD.OSD value) {
        // TODO:
    }

    public bool HasParameter(string key) {
        if (m_overrideParams.HasParameter(key)) {
            return true;
        }
        if (m_userParams.HasParameter(key)) {
            return true;
        }
        if (m_iniParams.HasParameter(key)) {
            return true;
        }
        if (m_defaultParams.HasParameter(key)) {
            return true;
        }
        return false;
    }

    public void Update(string key, OMVSD.OSD value) {
        UpdateSilent(key, value);
    }

    public void Update(string key, string value) {
        UpdateSilent(key, new OMVSD.OSDString(value));
    }

    public void UpdateSilent(string key, string value) {
        UpdateSilent(key, new OMVSD.OSDString(value));
    }

    // Note that this does not do the update event thing
    public void UpdateSilent(string key, OMVSD.OSD value) {
        if (m_overrideParams.HasParameter(key)) {
            m_overrideParams.Update(key, value);
            return;
        }
        else if (m_userParams.HasParameter(key)) {
            m_userParams.Update(key, value);
            return;
        }
        else if (m_iniParams.HasParameter(key)) {
            m_iniParams.Update(key, value);
            return;
        }
    }

    public OMVSD.OSD ParamValue(string key) {
        OMVSD.OSD ret = null;
        bool set = false;
        if (m_overrideParams.HasParameter(key)) {
            ret = m_overrideParams.ParamValue(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: OverrideValue: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_userParams.HasParameter(key)) {
            ret = m_userParams.ParamValue(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: UserValue: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_iniParams.HasParameter(key)) {
            ret = m_iniParams.ParamValue(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: INIValue: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_defaultParams.HasParameter(key)) {
            ret = m_defaultParams.ParamValue(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: DefaultValue: {0}->{1}", key, ret.ToString());
            set = true;
        }
        if (!set) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: OSD parameter '{0}' not found", key);
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = new OMVSD.OSDMap();
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("OSD parameter '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = null;
                    break;
            }
        }
        return ret;
    }

    public string ParamString(string key) {
        string ret = null;
        bool set = false;
        if (m_overrideParams.HasParameter(key)) {
            ret = m_overrideParams.ParamString(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: OverrideString: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_userParams.HasParameter(key)) {
            ret = m_userParams.ParamString(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: UserString: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_iniParams.HasParameter(key)) {
            ret = m_iniParams.ParamString(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: INIString: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_defaultParams.HasParameter(key)) {
            ret = m_defaultParams.ParamString(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: DefaultString: {0}->{1}", key, ret.ToString());
            set = true;
        }
        if (!set) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: string parameter '{0}' not found", key);
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = "";
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("string parameter '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = null;
                    break;
            }
        }
        return ret;
    }

    public int ParamInt(string key) {
        int ret = -1;
        bool set = false;
        if (m_overrideParams.HasParameter(key)) {
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: OverrideInt: {0}->{1}", key, ret.ToString());
            ret = m_overrideParams.ParamInt(key);
            set = true;
        }
        else if (m_userParams.HasParameter(key)) {
            ret = m_userParams.ParamInt(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: UserInt: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_iniParams.HasParameter(key)) {
            ret = m_iniParams.ParamInt(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: INIInt: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_defaultParams.HasParameter(key)) {
            ret = m_defaultParams.ParamInt(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: DefaultInt: {0}->{1}", key, ret.ToString());
            set = true;
        }
        if (!set) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: int parameter '{0}' not found", key);
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = -1;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("int parameter '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = -1;
                    break;
            }
        }
        return ret;
    }

    public bool ParamBool(string key) {
        bool ret = false;
        bool set = false;
        if (m_overrideParams.HasParameter(key)) {
            ret = m_overrideParams.ParamBool(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: OverrideBool: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_userParams.HasParameter(key)) {
            ret = m_userParams.ParamBool(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: UserBool: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_iniParams.HasParameter(key)) {
            ret = m_iniParams.ParamBool(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: INIBool: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_defaultParams.HasParameter(key)) {
            ret = m_defaultParams.ParamBool(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: DefaultBool: {0}->{1}", key, ret.ToString());
            set = true;
        }
        if (!set) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: bool parameter '{0}' not found", key);
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = false;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("bool parameter '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = false;
                    break;
            }
        }
        return ret;
    }

    public float ParamFloat(string key) {
        float ret = 0f;
        bool set = false;
        if (m_overrideParams.HasParameter(key)) {
            ret = m_overrideParams.ParamFloat(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: OverrideFloat: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_userParams.HasParameter(key)) {
            ret = m_userParams.ParamFloat(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: UserFloat: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_iniParams.HasParameter(key)) {
            ret = m_iniParams.ParamFloat(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: INIFloat: {0}->{1}", key, ret.ToString());
            set = true;
        }
        else if (m_defaultParams.HasParameter(key)) {
            ret = m_defaultParams.ParamFloat(key);
            // LogManager.Log.Log(LogLevel.DALL, "AppParameters: DefaultFloat: {0}->{1}", key, ret.ToString());
            set = true;
        }
        if (!set) {
            LogManager.Log.Log(LogLevel.DBADERROR, "AppParameters: float parameter '{0}' not found", key);
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = 0f;
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("float parameter '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = 0f;
                    break;
            }
        }
        return ret;
    }

    public List<string> ParamStringArray(string key) {
        List<string> ret = null;
        bool set = false;
        if (m_overrideParams.HasParameter(key)) {
            ret = m_overrideParams.ParamStringArray(key);
            set = true;
        }
        else if (m_userParams.HasParameter(key)) {
            ret = m_userParams.ParamStringArray(key);
            set = true;
        }
        else if (m_iniParams.HasParameter(key)) {
            ret = m_iniParams.ParamStringArray(key);
            set = true;
        }
        else if (m_defaultParams.HasParameter(key)) {
            ret = m_defaultParams.ParamStringArray(key);
            set = true;
        }
        if (!set) {
            switch (ParamErrorMethod) {
                case paramErrorType.eDefaultValue:
                    ret = new List<string>();
                    break;
                case paramErrorType.eException:
                    throw new ParameterException("string array parameter '" + key + "' not found");
                case paramErrorType.eNullValue:
                    ret = null;
                    break;
            }
        }
        return ret;
    }

    // this makes no sense on this structure -- something better later
    public void ForEach(ParamAction act) {
        return;
    }

    #endregion IParameters

}
}
