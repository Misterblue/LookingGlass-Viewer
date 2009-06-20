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
using System.IO;
using System.Net;
using System.Text;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;
using HttpServer;

// called to process GET. The Uri is the full request uri and 'after' is everything after the 'api'
public delegate OMVSD.OSD ProcessGetCallback(Uri uri, string after);
public delegate OMVSD.OSD ProcessPostCallback(Uri uri, string after, OMVSD.OSD body);

namespace LookingGlass.Rest {

public class RestHandler {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    private RestManager m_restMgr = null;
    public RestManager RestMgr {
        get {
            if (m_restMgr == null) {
                m_restMgr = (RestManager)ModuleManager.Module("RestManager");
            }
            return m_restMgr;
        }
    }

    public const string APINAME = "api";

    public const string RESTREQUESTERRORCODE = "RESTRequestError";
    public const string RESTREQUESTERRORMSG = "RESTRequestMsg";

    string m_baseUrl = null;
    ProcessGetCallback m_processGet = null;
    ProcessPostCallback m_processPost = null;
    ParameterSet m_parameterSet = null;
    IDisplayable m_displayable = null;
    bool m_parameterSetWritable = false;

    /// <summary>
    /// Setup a rest handler that calls back for gets and posts to the specified urlBase.
    /// The 'urlBase' is like "/login/". This will create the rest interface "^/api/login/"
    /// </summary>
    /// <param name="urlBase">base of the url that's us</param>
    /// <param name="pget">called on GET operations</param>
    /// <param name="ppost">called on POST operations</param>
    public RestHandler(string urlBase, ProcessGetCallback pget, ProcessPostCallback ppost) {
        m_baseUrl = urlBase;
        m_processGet = pget;
        m_processPost = ppost;
        RestMgr.Server.AddHandler("GET", null, "^/" + APINAME + urlBase, APIGetHandler);
        RestMgr.Server.AddHandler("POST", null, "^/" + APINAME + urlBase, APIPostHandler);
    }

    /// <summary>
    /// Setup a REST handler that returns the values from a ParameterSet.
    /// </summary>
    /// <param name="urlBase">base of the url for this parameter set</param>
    /// <param name="parms">the parameter set to read and write</param>
    /// <param name="writable">if 'true', it allows POST operations to change the parameter set</param>
    public RestHandler(string urlBase, ref ParameterSet parms, bool writable) {
        m_baseUrl = urlBase;
        m_processGet = null;
        m_processPost = null;
        m_parameterSet = parms;
        m_parameterSetWritable = writable;
        RestMgr.Server.AddHandler("GET", null, "^/" + APINAME + urlBase, ParamGetHandler);
        RestMgr.Server.AddHandler("POST", null, "^/" + APINAME + urlBase, ParamPostHandler);
    }

    /// <summary>
    /// Setup a REST handler that returns the values from a ParameterSet.
    /// </summary>
    /// <param name="urlBase">base of the url for this parameter set</param>
    /// <param name="parms">the parameter set to read and write</param>
    /// <param name="writable">if 'true', it allows POST operations to change the parameter set</param>
    public RestHandler(string urlBase, IDisplayable displayable) {
        m_baseUrl = urlBase;
        m_processGet = null;
        m_processPost = null;
        m_displayable = displayable;
        RestMgr.Server.AddHandler("GET", null, "^/" + APINAME + urlBase, DisplayableGetHandler);
    }

    private void APIGetHandler(IHttpClientContext context, IHttpRequest reqContext, IHttpResponse respContext) {
        m_log.Log(LogLevel.DRESTDETAIL, "APIGetHandler: " + reqContext.Uri);
        try {
            string absURL = reqContext.Uri.AbsolutePath.ToLower();
            int afterPos = absURL.IndexOf(m_baseUrl.ToLower());
            string afterString = absURL.Substring(afterPos + m_baseUrl.Length);
            OMVSD.OSD resp = m_processGet(reqContext.Uri, afterString);
            RestMgr.ConstructSimpleResponse(respContext, "text/json",
                delegate(ref StringBuilder buff) {
                    buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                }
            );
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRESTDETAIL, "Failed getHandler: u=" 
                    + reqContext.Uri.ToString() + ":" +e.ToString());
            RestMgr.ConstructErrorResponse(respContext, HttpStatusCode.InternalServerError,
                delegate(ref StringBuilder buff) {
                    buff.Append("<div>");
                    buff.Append("FAILED GETTING '" + reqContext.Uri.ToString() + "'");
                    buff.Append("</div>");
                    buff.Append("<div>");
                    buff.Append("ERROR = '" + e.ToString() + "'");
                    buff.Append("</div>");
                }
            );
            // make up a response
        }
        return;
    }

    private void APIPostHandler(IHttpClientContext context, IHttpRequest reqContext, IHttpResponse respContext) {
        m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: " + reqContext.Uri);
        StreamReader rdr = new StreamReader(reqContext.Body);
        string strBody = rdr.ReadToEnd();
        rdr.Close();
        // m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
        try {
            string absURL = reqContext.Uri.AbsolutePath.ToLower();
            int afterPos = absURL.IndexOf(m_baseUrl.ToLower());
            string afterString = absURL.Substring(afterPos + m_baseUrl.Length);
            OMVSD.OSD body = MapizeTheBody(reqContext, strBody);
            OMVSD.OSD resp = m_processPost(reqContext.Uri, afterString, body);
            if (resp != null) {
                RestMgr.ConstructSimpleResponse(respContext, "text/json",
                    delegate(ref StringBuilder buff) {
                        buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                    }
                );
            }
            else {
                // the requester didn't like it at all
                m_log.Log(LogLevel.DRESTDETAIL, "Call to postHandler return null. url="
                    + reqContext.Uri.ToString());
                RestMgr.ConstructErrorResponse(respContext, HttpStatusCode.InternalServerError,
                    delegate(ref StringBuilder buff) {
                        buff.Append("<div>");
                        buff.Append("FAILED PROCESSING '" + reqContext.Uri.ToString() + "'");
                        buff.Append("</div>");
                    }
                );
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRESTDETAIL, "Failed getHandler: u=" 
                    + reqContext.Uri.ToString() + ":" +e.ToString());
            RestMgr.ConstructErrorResponse(respContext, HttpStatusCode.InternalServerError,
                delegate(ref StringBuilder buff) {
                    buff.Append("<div>");
                    buff.Append("FAILED GETTING '" + reqContext.Uri.ToString() + "'");
                    buff.Append("</div>");
                    buff.Append("<div>");
                    buff.Append("ERROR = '" + e.ToString() + "'");
                    buff.Append("</div>");
                }
            );
            // make up a response
        }
        return;
    }

    private OMVSD.OSDMap MapizeTheBody(IHttpRequest reqContext, string body) {
        OMVSD.OSDMap retMap = new OMVSD.OSDMap();
        if (body.Substring(0, 1).Equals("{")) { // kludge test for JSON formatted body
            try {
                retMap = (OMVSD.OSDMap)OMVSD.OSDParser.DeserializeJson(body);
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DRESTDETAIL, "Failed parsing of JSON body: " + e.ToString());
            }
        }
        else {
            try {
                string[] amp = body.Split('&');
                if (amp.Length > 0) {
                    foreach (string kvp in amp) {
                        string[] kvpPieces = kvp.Split('=');
                        if (kvpPieces.Length == 2) {
                            retMap.Add(kvpPieces[0].Trim(), new OMVSD.OSDString(kvpPieces[1].Trim()));
                        }
                    }
                }
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DRESTDETAIL, "Failed parsing of query body: " + e.ToString());
            }
        }
        return retMap;
    }

    private void ParamGetHandler(IHttpClientContext context, IHttpRequest reqContext, IHttpResponse respContext) {
        m_log.Log(LogLevel.DRESTDETAIL, "ParamGetHandler: " + reqContext.Uri);
        OMVSD.OSDMap paramValues = m_parameterSet.GetDisplayable();
        ReturnGetResponse(paramValues, context, reqContext, respContext);
        return;
    }

    private void ReturnGetResponse(OMVSD.OSDMap paramValues, IHttpClientContext context, IHttpRequest reqContext, IHttpResponse respContext) {
        try {
            string absURL = reqContext.Uri.AbsolutePath.ToLower();
            string[] segments = absURL.Split('/');
            int afterPos = absURL.IndexOf(m_baseUrl.ToLower());
            string afterString = absURL.Substring(afterPos + m_baseUrl.Length);
            if (afterString.Length > 0) {
                // look to see if asking for one particular value
                OMVSD.OSD oneValue;
                if (paramValues.TryGetValue(afterString, out oneValue)) {
                    // asking for one value from the set
                    RestMgr.ConstructSimpleResponse(respContext, "text/json",
                        delegate(ref StringBuilder buff) {
                            buff.Append(OMVSD.OSDParser.SerializeJsonString(oneValue));
                        }
                    );
                }
                else {
                    // asked for a specific value but we don't have one of those. return empty
                    RestMgr.ConstructSimpleResponse(respContext, "text/json",
                        delegate(ref StringBuilder buff) {
                            buff.Append("{}");
                        }
                    );
                }
            }
            else {
                // didn't specify a name. Return the whole parameter set
                RestMgr.ConstructSimpleResponse(respContext, "text/json",
                    delegate(ref StringBuilder buff) {
                        buff.Append(OMVSD.OSDParser.SerializeJsonString(paramValues));
                    }
                );
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DRESTDETAIL, "Failed getHandler: u=" 
                    + reqContext.Uri.ToString() + ":" +e.ToString());
            RestMgr.ConstructErrorResponse(respContext, HttpStatusCode.InternalServerError,
                delegate(ref StringBuilder buff) {
                    buff.Append("<div>");
                    buff.Append("FAILED GETTING '" + reqContext.Uri.ToString() + "'");
                    buff.Append("</div>");
                    buff.Append("<div>");
                    buff.Append("ERROR = '" + e.ToString() + "'");
                    buff.Append("</div>");
                }
            );
            // make up a response
        }
        return;
    }

    private void ParamPostHandler(IHttpClientContext context, IHttpRequest reqContext, IHttpResponse respContext) {
        m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: " + reqContext.Uri);
        if (m_parameterSetWritable) {
            StreamReader rdr = new StreamReader(reqContext.Body);
            string strBody = rdr.ReadToEnd();
            rdr.Close();
            // m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
            try {
                string absURL = reqContext.Uri.AbsolutePath.ToLower();
                int afterPos = absURL.IndexOf(m_baseUrl.ToLower());
                string afterString = absURL.Substring(afterPos + m_baseUrl.Length + 1);
                OMVSD.OSDMap body = MapizeTheBody(reqContext, strBody);
                foreach (string akey in body.Keys) {
                    if (m_parameterSet.HasParameter(akey)) {
                        m_parameterSet.Update(akey, body[akey]);
                    }
                }
                RestMgr.ConstructSimpleResponse(respContext, "text/json",
                    delegate(ref StringBuilder buff) {
                        buff.Append(OMVSD.OSDParser.SerializeJsonString(m_parameterSet.GetDisplayable()));
                    }
                );
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DRESTDETAIL, "Failed postHandler: u="
                        + reqContext.Uri.ToString() + ":" + e.ToString());
                RestMgr.ConstructErrorResponse(respContext, HttpStatusCode.InternalServerError,
                    delegate(ref StringBuilder buff) {
                        buff.Append("<div>");
                        buff.Append("FAILED POSTING '" + reqContext.Uri.ToString() + "'");
                        buff.Append("</div>");
                        buff.Append("<div>");
                        buff.Append("ERROR = '" + e.ToString() + "'");
                        buff.Append("</div>");
                    }
                );
                // make up a response
            }
        }
        return;
    }

    private void DisplayableGetHandler(IHttpClientContext context, IHttpRequest reqContext, IHttpResponse respContext) {
        m_log.Log(LogLevel.DRESTDETAIL, "DisplayableGetHandler: " + reqContext.Uri);
        OMVSD.OSDMap statValues = m_displayable.GetDisplayable();
        ReturnGetResponse(statValues, context, reqContext, respContext);
        return;
    }

}
}
