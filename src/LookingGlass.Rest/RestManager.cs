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
using System.Reflection;
using System.Text;
using LookingGlass;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using OMV = OpenMetaverse;
using HttpServer;
using HttpServer.Handlers;

namespace LookingGlass.Rest {

    /// <summary>
    /// RestManager makes available HTTP connections to static and dynamic information.
    /// RestManager provides two functions: static web pages for ui and script support and
    /// REST interface capabilities for services within LookingGlass to get and present data.
    /// 
    /// The static interface presents two sets of URLs which are mapped into the filesystem:
    /// http://127.0.0.1:9144/std/xxx : 'standard' pages which are common libraries
    /// This maps to the directory "Rest.Manager.UIContentDir" which defaults to
    /// "BINDIR/../UI/std/"
    /// http://127.0.0.1:9144/static/xxx : ui pages which can be 'skinned'
    /// This maps to the directory "Rest.Manager.UIContentDir"/"Rest.Manaager.Skin" which
    /// defaults to "BINDIR/../UI/Default/".
    /// 
    /// The dynamic content is created by servers creating instances of RestHandler.
    /// This creates URLs like:
    /// http://127.0.0.1:9144/api/SERVICE/xxx
    /// where 'service' is the name of teh service and 'xxx' is whatever it wants.
    /// These implement GET and POST operations of JSON formatted data.
    /// </summary>
public class RestManager : ModuleBase, HttpServer.ILogWriter {
    private ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    protected HttpServer.HttpListener m_Server;
    public HttpServer.HttpListener Server { get { return m_Server; } }
    FileHandler m_staticHandler = null;
    FileHandler m_stdHandler = null;
    FileHandler m_faviconHandler = null;

    public const string MIMEDEFAULT = "text/html";

    protected int m_port;
    public int Port { get { return m_port; } }

    protected string m_baseURL;
    // return the full base URL with the port added
    public string BaseURL { 
        get { 
            string ret = m_baseURL;
            if (m_port != 0) {
                ret = m_baseURL + ":" + Port.ToString();
            }
            return ret;
        } 
    }

    public RestManager() {
    }

    #region IModule methods
    public override void OnLoad(string modName, IAppParameters parms) {
        ModuleName = modName;
        ModuleParams = parms;
        ModuleParams.AddDefaultParameter("Rest.Manager.Port", "9144",
                    "Local port used for rest interfaces");
        ModuleParams.AddDefaultParameter("Rest.Manager.BaseURL", "http://127.0.0.1",
                    "Base URL for rest interfaces");
        ModuleParams.AddDefaultParameter("Rest.Manager.CSSLocalURL", "/std/LookingGlass.css",
                    "CSS file for rest display");
        ModuleParams.AddDefaultParameter("Rest.Manager.Browser", @"""\Program Files\Mozilla Firefox\firefox.exe""",
                    "The browser to run");
        ModuleParams.AddDefaultParameter("Rest.Manager.UIContentDir", 
                    Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "../UI"),
                    "Directory for static HTML content");
        ModuleParams.AddDefaultParameter("Rest.Manager.Skin", "Default",
                    "If specified, the subdirectory under StaticContentDir to take files from");
    }

    override public bool AfterAllModulesLoaded() {
        m_log.Log(LogLevel.DINIT, "entered AfterAllModulesLoaded()");

        m_port = ModuleParams.ParamInt("Rest.Manager.Port");
        m_baseURL = ModuleParams.ParamString("Rest.Manager.BaseURL");
        // m_Server = new WebServer(System.Net.IPAddress.Any, Port);
        m_Server = HttpServer.HttpListener.Create(this, System.Net.IPAddress.Any, Port);
        m_Server.Start(10);

        string baseUIDir = ModuleParams.ParamString("Rest.Manager.UIContentDir");
        if (baseUIDir.EndsWith("/")) baseUIDir = baseUIDir.Substring(0, baseUIDir.Length-1);

        // things referenced as static are from the skinning directory below the UI dir
        string staticDir = baseUIDir;
        if (ModuleParams.HasParameter("Rest.Manager.Skin")) {
            string skinName = ModuleParams.ParamString("Rest.Manager.Skin");
            skinName.Replace("/", "");  // skin names shouldn't fool with directories
            skinName.Replace("\\", "");
            skinName.Replace("..", "");
            staticDir = staticDir + "/" + ModuleParams.ParamString("Rest.Manager.Skin");
        }
        staticDir += "/";
        m_log.Log(LogLevel.DINITDETAIL, "Registering FileHandler {0} -> {1}", "/static/", staticDir);
        FileHandler m_staticHandler = new FileHandler(m_Server, "/static/", staticDir, true);

        string stdDir = baseUIDir + "/std/";
        m_log.Log(LogLevel.DINITDETAIL, "Registering FileHandler {0} -> {1}", "/std/", stdDir);
        FileHandler m_stdHandler = new FileHandler(m_Server, "/std/", stdDir, true);

        m_log.Log(LogLevel.DINITDETAIL, "Registering FileHandler {0} -> {1}", "/favicon.ico", stdDir);
        FileHandler m_faviconHandler = new FileHandler(m_Server, "/favicon.ico", stdDir, true);

        m_log.Log(LogLevel.DINIT, "exiting AfterAllModulesLoaded()");
        return true;
    }

    // Routine for HttpServer.ILogWriter
    public void Write(object source, HttpServer.LogPrio prio, string msg) {
        /*
        LogLevel level = LogLevel.DREST;
        if (prio == HttpServer.LogPrio.Debug || prio == HttpServer.LogPrio.Info) {
            level = LogLevel.DRESTDETAIL;
        }
        m_log.Log(level, msg);
         */
        return;
    }

    public override void Start() {
        base.Start();
    }

    override public void Stop() {
        if (m_Server != null) {
            m_Server.Stop();
        }
        return;
    }
    #endregion IModule methods

    #region HTML Helper Routines

    public delegate void ConstructResponseRoutine(ref StringBuilder buff);

    /// <summary>
    /// Construct and send the HTML response to the request. The two delegates are passed
    /// the StringBuilder for them to add to.
    /// </summary>
    /// <param name="context">The request information</param>
    /// <param name="title">The title for the HTML page</param>
    /// <param name="addHeader">Called to add HTML to the header. May be null.</param>
    /// <param name="addContent">Called to add HTML to the body. May be null.</param>
    public void ConstructResponse(IHttpResponse context, 
                    string title, 
                    ConstructResponseRoutine addHeader, 
                    ConstructResponseRoutine addContent) {
        StringBuilder buff = new StringBuilder();
        try {
            context.ContentType = MIMEDEFAULT;
            context.AddHeader("Server", Globals.ApplicationName);
            // context.Connection = ConnectionType.Close;
            
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head>\r\n");

            if (title != null) buff.AppendFormat("<title>{0}</title>\r\n", title);

            // if the user specified a CSS file, use that one else the default
            if (ModuleParams.HasParameter("Rest.Manager.CSSLocalURL")) {
                buff.AppendFormat("<link rel=\"stylesheet\" href=\"{0}/{1}\" type=\"text/css\">\r\n",
                        BaseURL, ModuleParams.ParamString("Rest.Manager.CSSLocalURL"));
            }
            else {
                buff.AppendFormat("<link rel=\"stylesheet\" href=\"{0}/{1}\" type=\"text/css\">\r\n",
                        BaseURL, "/static/Default/LookingGlass.css" );
            }

            // Always confuse the world with jquery
            buff.Append("<script type=\"application/javascript\" src=\"/static/std/jquery.js\"></script>\r\n");

            // if the user added a script file (for customized wonderfulness) add that one
            if (ModuleParams.HasParameter("Rest.Manager.JSLocalURL")) {
                buff.AppendFormat("<script type=\"application/javascript\" src=\"{0}\"></script>\r\n", 
                        ModuleParams.ParamString("Rest.Manager.JSLocalURL") );
            }

            // if the user has other header stuff to add, let them do that
            if (addHeader != null) addHeader(ref buff);
            buff.Append("</head>\r\n");

            buff.Append("<body>\r\n");
            if (addContent != null) addContent(ref buff);
            buff.Append("</body>\r\n");
            buff.Append("</html>\r\n\r\n");
            context.Status = HttpStatusCode.OK;
        }
        catch {
            buff = new StringBuilder();
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head></head><body></body></html>\r\n");
            context.Status = HttpStatusCode.InternalServerError;
        }

        byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());
        
        context.ContentLength = encodedBuff.Length;
        context.SendHeaders();
        context.SendBody(encodedBuff, 0, encodedBuff.Length);
        context.Send();
        return;
    }
    /// <summary>
    /// Just like 'ConstructResponse' but has very simplified headers. Good for AJAX reponsses.
    /// </summary>
    /// <param name="context">The request information</param>
    /// <param name="title">The title for the HTML page</param>
    /// <param name="addHeader">Called to add HTML to the header. May be null.</param>
    /// <param name="addContent">Called to add HTML to the body. May be null.</param>
    public void ConstructSimpleResponse(IHttpResponse context, 
                    string contentType,
                    ConstructResponseRoutine addContent) {
        StringBuilder buff = new StringBuilder();
        try {
            context.ContentType = contentType == null ? MIMEDEFAULT : contentType;
            context.AddHeader("Server", Globals.ApplicationName);
            // context.Connection = ConnectionType.Close;

            if (addContent != null) addContent(ref buff);
            context.Status = HttpStatusCode.OK;
        }
        catch {
            buff = new StringBuilder();
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head></head><body></body></html>\r\n");
            context.Status = HttpStatusCode.InternalServerError;
        }

        byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());
        
        context.ContentLength = encodedBuff.Length;
        context.SendHeaders();
        context.SendBody(encodedBuff, 0, encodedBuff.Length);
        context.Send();
        return;
    }

    /// <summary>
    /// Construct a response that is all abbout errors
    /// </summary>
    /// <param name="context">The request information</param>
    /// <param name="errCode">The HTTP error code toreturn</param>
    /// <param name="addContent">Called to add HTML to the body. May be null.</param>
    public void ConstructErrorResponse(IHttpResponse context, 
                    HttpStatusCode errCode, 
                    ConstructResponseRoutine addContent) {
        StringBuilder buff = new StringBuilder();
        try {
            context.ContentType = MIMEDEFAULT;
            context.AddHeader("Server", Globals.ApplicationName);
            // context.Connection = ConnectionType.Close;

            buff.Append("<body>\r\n");
            if (addContent != null) addContent(ref buff);
            buff.Append("</body>\r\n");
            buff.Append("</html>\r\n\r\n");
            context.Status = errCode;
        }
        catch {
            buff = new StringBuilder();
            buff.Append("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\">\r\n");
            buff.Append("<html xmlns=\"http://www.w3.org/1999/xhtml\">\r\n");
            buff.Append("<head></head><body></body></html>\r\n");
            context.Status = HttpStatusCode.InternalServerError;
        }

        byte[] encodedBuff = System.Text.Encoding.UTF8.GetBytes(buff.ToString());
        
        context.ContentLength = encodedBuff.Length;
        context.SendHeaders();
        context.SendBody(encodedBuff, 0, encodedBuff.Length);
        context.Send();
        return;
    }


    #endregion HTML Helper Routines
    
}
}
