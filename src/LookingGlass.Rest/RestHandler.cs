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

// called to process GET. The Uri is the full request uri and 'after' is everything after the 'api'
public delegate OMVSD.OSD ProcessGetCallback(LookingGlass.Rest.RestHandler handler, Uri uri, string after);
public delegate OMVSD.OSD ProcessPostCallback(LookingGlass.Rest.RestHandler handler, Uri uri, string after, OMVSD.OSD body);

namespace LookingGlass.Rest {

public class RestHandler : IDisposable {
    public ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    public const string APINAME = "api";

    public const string RESTREQUESTERRORCODE = "RESTRequestError";
    public const string RESTREQUESTERRORMSG = "RESTRequestMsg";

    public HttpListener m_Handler = null;
    public string m_baseUrl = null;
    public ProcessGetCallback m_processGet = null;
    public ProcessPostCallback m_processPost = null;
    public ParameterSet m_parameterSet = null;
    public IDisplayable m_displayable = null;
    public string m_dir = null;
    public bool m_parameterSetWritable = false;

    public HttpListenerContext m_context;
    public HttpListenerRequest m_request;
    public HttpListenerResponse m_response;

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
        m_Handler = new HttpListener();
        string prefix = RestManager.Instance.BaseURL + APINAME + urlBase;
        m_Handler.Prefixes.Add(prefix);
        m_Handler.Start();
        m_Handler.BeginGetContext(new AsyncCallback(GetPostAsync), this);
        m_log.Log(LogLevel.DRESTDETAIL, "Register GET/POST handler for {0}", prefix);
    }

    /// <summary>
    /// Setup a REST handler that returns the values from a ParameterSet.
    /// </summary>
    /// <param name="urlBase">base of the url for this parameter set</param>
    /// <param name="parms">the parameter set to read and write</param>
    /// <param name="writable">if 'true', it allows POST operations to change the parameter set</param>
    public RestHandler(string urlBase, ref ParameterSet parms, bool writable) {
        m_baseUrl = urlBase;
        m_parameterSet = parms;
        m_parameterSetWritable = writable;
        m_processGet = ProcessGetParam;
        m_Handler = new HttpListener();
        string prefix = RestManager.Instance.BaseURL + APINAME + urlBase;
        m_Handler.Prefixes.Add(prefix);
        m_Handler.Start();
        m_Handler.BeginGetContext(new AsyncCallback(GetPostAsync), this);
        m_log.Log(LogLevel.DRESTDETAIL, "Register GET/POST param handler for {0}", prefix);
    }

    /// <summary>
    /// Setup a REST handler that returns the values from a ParameterSet.
    /// </summary>
    /// <param name="urlBase">base of the url for this parameter set</param>
    /// <param name="parms">the parameter set to read and write</param>
    /// <param name="writable">if 'true', it allows POST operations to change the parameter set</param>
    public RestHandler(string urlBase, IDisplayable displayable) {
        m_baseUrl = urlBase;
        m_displayable = displayable;
        m_processGet = ProcessGetParam;
        m_Handler = new HttpListener();
        string prefix = RestManager.Instance.BaseURL + APINAME + urlBase;
        m_Handler.Prefixes.Add(prefix);
        m_Handler.Start();
        m_Handler.BeginGetContext(new AsyncCallback(GetPostAsync), this);
        m_log.Log(LogLevel.DRESTDETAIL, "Register GET/POST displayable handler for {0}", prefix);
    }

    /// <summary>
    /// Setup a REST handler that returns the contents of a file
    /// </summary>
    /// <param name="urlBase"></param>
    /// <param name="directory"></param>
    public RestHandler(string urlBase, string directory) {
        m_dir = directory;
        m_Handler = new HttpListener();
        string prefix = RestManager.Instance.BaseURL + urlBase;
        m_Handler.Prefixes.Add(prefix);
        m_Handler.Start();
        m_Handler.BeginGetContext(new AsyncCallback(GetPostAsync), this);
        m_log.Log(LogLevel.DRESTDETAIL, "Register GET/POST displayable handler for {0}", prefix);
    }
    
    public void Dispose() {
        if (m_Handler != null) {
            m_Handler.Stop();
            m_Handler = null;
        }
    }

    public static void GetPostAsync(IAsyncResult result) {
        RestHandler handler = (RestHandler)result.AsyncState;
        HttpListener listener = handler.m_Handler;
        handler.m_context = listener.EndGetContext(result);
        handler.m_request = handler.m_context.Request;
        handler.m_response = handler.m_context.Response;
        if (handler.m_request.HttpMethod.ToUpper().Equals("GET")) {
            if (handler.m_processGet == null && handler.m_dir != null) {
                handler.m_log.Log(LogLevel.DRESTDETAIL, "GET: file: " + handler.m_request.Url);
                string absURL = handler.m_request.Url.AbsolutePath.ToLower();
                int afterPos = absURL.IndexOf(handler.m_baseUrl.ToLower());
                string afterString = absURL.Substring(afterPos + handler.m_baseUrl.Length);
                string filename = handler.m_dir + "/" + afterString;
                if (File.Exists(filename)) {
                    RestManager.Instance.ConstructSimpleResponse(handler.m_response, "text/json",
                        delegate(ref StringBuilder buff) {
                            // TODO

                        }
                    );
                }
                else {
                }
                return;
            }
            try {
                if (handler.m_processGet == null) {
                    throw new LookingGlassException("HTTP GET with no processing routine");
                }
                handler.m_log.Log(LogLevel.DRESTDETAIL, "GET: " + handler.m_request.Url);
                string absURL = handler.m_request.Url.AbsolutePath.ToLower();
                int afterPos = absURL.IndexOf(handler.m_baseUrl.ToLower());
                string afterString = absURL.Substring(afterPos + handler.m_baseUrl.Length);
                OMVSD.OSD resp = handler.m_processGet(handler, handler.m_request.Url, afterString);
                RestManager.Instance.ConstructSimpleResponse(handler.m_response, "text/json",
                    delegate(ref StringBuilder buff) {
                        buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                    }
                );
            }
            catch (Exception e) {
                handler.m_log.Log(LogLevel.DREST, "Failed getHandler: u=" 
                        + handler.m_request.Url.ToString() + ":" +e.ToString());
                RestManager.Instance.ConstructErrorResponse(handler.m_response, HttpStatusCode.InternalServerError,
                    delegate(ref StringBuilder buff) {
                        buff.Append("<div>");
                        buff.Append("FAILED GETTING '" + handler.m_request.Url.ToString() + "'");
                        buff.Append("</div>");
                        buff.Append("<div>");
                        buff.Append("ERROR = '" + e.ToString() + "'");
                        buff.Append("</div>");
                    }
                );
            }
            return;
        }
        if (handler.m_request.HttpMethod.ToUpper().Equals("POST")) {
            handler.m_log.Log(LogLevel.DRESTDETAIL, "POST: " + handler.m_request.Url);
            if (handler.m_processPost == null && handler.m_parameterSet != null) {
                handler.ParamPostHandler(handler, handler.m_request, handler.m_response);
                return;
            }
            string strBody = "";
            using (StreamReader rdr = new StreamReader(handler.m_request.InputStream)) {
                strBody = rdr.ReadToEnd();
                // m_log.Log(LogLevel.DRESTDETAIL, "APIPostHandler: Body: '" + strBody + "'");
            }
            try {
                if (handler.m_processPost == null) {
                    throw new LookingGlassException("HTTP POST with no processing routine");
                }
                string absURL = handler.m_request.Url.AbsolutePath.ToLower();
                int afterPos = absURL.IndexOf(handler.m_baseUrl.ToLower());
                string afterString = absURL.Substring(afterPos + handler.m_baseUrl.Length);
                OMVSD.OSD body = handler.MapizeTheBody(strBody);
                OMVSD.OSD resp = handler.m_processPost(handler, handler.m_request.Url, afterString, body);
                if (resp != null) {
                    RestManager.Instance.ConstructSimpleResponse(handler.m_response, "text/json",
                        delegate(ref StringBuilder buff) {
                            buff.Append(OMVSD.OSDParser.SerializeJsonString(resp));
                        }
                    );
                }
                else {
                    handler.m_log.Log(LogLevel.DREST, "Failure which creating POST response");
                    throw new LookingGlassException("Failure processing POST");
                }
            }
            catch (Exception e) {
                handler.m_log.Log(LogLevel.DREST, "Failed postHandler: u=" 
                        + handler.m_request.Url.ToString() + ":" +e.ToString());
                RestManager.Instance.ConstructErrorResponse(handler.m_response, HttpStatusCode.InternalServerError,
                    delegate(ref StringBuilder buff) {
                        buff.Append("<div>");
                        buff.Append("FAILED GETTING '" + handler.m_request.Url.ToString() + "'");
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
    }

    private OMVSD.OSDMap MapizeTheBody(string body) {
        OMVSD.OSDMap retMap = new OMVSD.OSDMap();
        if (body.Substring(0, 1).Equals("{")) { // kludge test for JSON formatted body
            try {
                retMap = (OMVSD.OSDMap)OMVSD.OSDParser.DeserializeJson(body);
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DREST, "Failed parsing of JSON body: " + e.ToString());
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
                m_log.Log(LogLevel.DREST, "Failed parsing of query body: " + e.ToString());
            }
        }
        return retMap;
    }

    public OMVSD.OSD ProcessGetParam(RestHandler handler, Uri uri, string afterString) {
        OMVSD.OSD ret = new OMVSD.OSDMap();
        OMVSD.OSDMap paramValues;
        if (handler.m_displayable == null) {
            paramValues = handler.m_parameterSet.GetDisplayable();
        }
        else {
            paramValues = handler.m_displayable.GetDisplayable();
        }
        try {
            if (afterString.Length > 0) {
                // look to see if asking for one particular value
                OMVSD.OSD oneValue;
                if (paramValues.TryGetValue(afterString, out oneValue)) {
                    ret = oneValue;
                }
                else {
                    // asked for a specific value but we don't have one of those. return empty
                }
            }
            else {
                // didn't specify a name. Return the whole parameter set
                ret = paramValues;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DREST, "Failed fetching GetParam value: {0}", e);
        }
        return ret;
    }
}
}
