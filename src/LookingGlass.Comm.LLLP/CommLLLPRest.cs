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
using System.Text;
using LookingGlass;
using LookingGlass.Comm;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Rest;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;
using HttpServer;

namespace LookingGlass.Comm.LLLP {

    /// <summary>
    /// Provides interface to LLLP communication stack.
    /// The LLLP stack makes a parameter set available which contains the necessary login
    /// values as well as the current state of the connection.
    /// This handles the following REST operations:
    /// GET http://127.0.0.0:port/api/LLLP/ : returns the JSON of the comm parameter block
    /// POST http://127.0.0.1:port/api/LLLP/connection/login : take JSON body as parameters to use to login
    ///    parameters are: LOGINFIRST, LOGINLAST, LOGINPASS, LOGINGRID, LOGINSIM
    /// POST http://127.0.0.1:port/api/LLLP/connection/logout : perform a logout
    /// </summary>
public class CommLLLPRest : ModuleBase, IRestUser {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    ICommProvider m_comm = null;
    string m_apiName;
    RestHandler m_paramGetHandler = null;
    RestHandler m_actionHandler = null;

    public CommLLLPRest() {
    }

    public override void OnLoad(string name, LookingGlassBase lgbase) {
        base.OnLoad(name, lgbase);
        m_apiName = "LLLP";
        ModuleParams.AddDefaultParameter(ModuleName + ".Comm", "Comm", "Name of comm module to connect to");
        ModuleParams.AddDefaultParameter(ModuleName + ".APIName", m_apiName, "Name of api for this comm control");
    }

    public override void Start() {
        string commName = ModuleParams.ParamString(ModuleName + ".Comm");
        try {
            m_comm = (ICommProvider)LGB.ModManager.Module(commName);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "CommLLLPRest COULD NOT CONNECT TO COMM MODULE NAMED " + commName);
            m_log.Log(LogLevel.DBADERROR, "CommLLLPRest error = " + e.ToString());
        }
        try {
            m_apiName = ModuleParams.ParamString(ModuleName + ".APIName");

            ParameterSet connParams = m_comm.ConnectionParams;
            m_paramGetHandler = new RestHandler("/" + m_apiName + "/status/", ref connParams, false);
            m_actionHandler = new RestHandler("/" + m_apiName + "/connect/", null, ProcessPost);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "CommLLLPRest COULD NOT REGISTER REST OPERATION: " + e.ToString());
        }
    }

    // OBSOLETE: not used now that RestHandler does ParameterSets
    public OMVSD.OSD ProcessGet(Uri uri, string after) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        if (m_comm == null) {
            m_log.Log(LogLevel.DBADERROR, "GET WITHOUT COMM CONNECTION!! URL=" + uri.ToString());
            return new OMVSD.OSD();
        }
        m_log.Log(LogLevel.DCOMMDETAIL, "Parameter request: {0}", uri.ToString());
        string[] segments = after.Split('/');
        // the after should be "/NAME/param" where "NAME" is my apiname. If 'param' is there return one
        if (segments.Length > 2) {
            string paramName = segments[2];
            if (m_comm.ConnectionParams.HasParameter(paramName)) {
                ret.Add(paramName, new OMVSD.OSDString(m_comm.ConnectionParams.ParamString(paramName)));
            }
        }
        else {
            // they want the whole set
            ret = m_comm.ConnectionParams.GetDisplayable();
        }
        return ret;
    }

    /// <summary>
    /// Posting to this communication instance. The URI comes in as "/api/MYNAME/ACTION" where
    /// ACTION is "login", "logout".
    /// </summary>
    /// <param name="uri"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public OMVSD.OSD ProcessPost(Uri uri, string after, OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        if (m_comm == null) {
            m_log.Log(LogLevel.DBADERROR, "POST WITHOUT COMM CONNECTION!! URL=" + uri.ToString());
            return new OMVSD.OSD();
        }
        m_log.Log(LogLevel.DCOMMDETAIL, "Post action: {0}", uri.ToString());
        switch (after) {
            case "login":
                ret = PostActionLogin(body);
                break;
            case "logout":
                ret = PostActionLogout(body);
                break;
            default:
                m_log.Log(LogLevel.DBADERROR, "UNKNOWN ACTION: " + uri.ToString());
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Unknown action"));
                break;
        }
        return ret;
    }

    private OMVSD.OSDMap PostActionLogin(OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        ParameterSet loginParams = new ParameterSet();
        try {
            OMVSD.OSDMap paramMap = (OMVSD.OSDMap)body;
            loginParams.Add(CommLLLP.FIELDFIRST, paramMap["LOGINFIRST"].AsString());
            loginParams.Add(CommLLLP.FIELDLAST, paramMap["LOGINLAST"].AsString());
            loginParams.Add(CommLLLP.FIELDPASS, paramMap["LOGINPASS"].AsString());
            loginParams.Add(CommLLLP.FIELDGRID, paramMap["LOGINGRID"].AsString());
            loginParams.Add(CommLLLP.FIELDSIM, paramMap["LOGINSIM"].AsString());
        }
        catch {
            m_log.Log(LogLevel.DBADERROR, "MISFORMED POST REQUEST: ");
            ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
            ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Misformed POST request"));
            return ret;
        }

        try {
            if (!m_comm.Connect(loginParams)) {
                m_log.Log(LogLevel.DBADERROR, "CONNECT FAILED");
                ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
                ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Could not log in"));
                return ret;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "CONNECT EXCEPTION: " + e.ToString());
            ret.Add(RestHandler.RESTREQUESTERRORCODE, new OMVSD.OSDInteger(1));
            ret.Add(RestHandler.RESTREQUESTERRORMSG, new OMVSD.OSDString("Connection threw exception: " + e.ToString()));
            return ret;
        }

        return ret;
    }

    private OMVSD.OSDMap PostActionLogout(OMVSD.OSD body) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        m_comm.Disconnect();
        return ret;
    }
}
}
