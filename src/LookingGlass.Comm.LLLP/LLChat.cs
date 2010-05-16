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
using LookingGlass.Comm;
using LookingGlass.Comm.LLLP;
using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;
using LookingGlass.Rest;
using OMV = OpenMetaverse;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Comm.LLLP {
public class LLChat : IChatProvider, IModule {

    protected ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    public enum ChatEntryType {
        Normal = 0,
        StatusBlue, StatusDarkBlue, LindenChat, ObjectChat,
        StartupTitle, Error, Alert, OwnerSay, Invisible
    };
    string[] ChatEntryTypeString  = {
        "ChatTypeNormal",
        "ChatTypeStatusBlue", "ChatTypeStatusDarkBlue", "ChatTypeLindenChat", "ChatTypeObjectChat",
        "ChatTypeStartupTitle", "ChatTypeError", "ChatTypeAlert", "ChatTypeOwnerSay", "ChatTypeInvisible"
    };
    protected class ChatEntry {
        public DateTime time;
        public ChatEntryType chatEntryType;
        public string fromName;
        public string message;
        public OMV.Vector3 position;
        public OMV.ChatSourceType sourceType;
        public OMV.ChatType chatType;
        public string chatTypeString;
        public OMV.UUID ownerID;
        public ChatEntry() {
            time = DateTime.Now;
        }
    }

    #region IModule
    protected string m_moduleName;
    public string ModuleName { get { return m_moduleName; } set { m_moduleName = value; } }

    protected LookingGlassBase m_lgb = null;
    public LookingGlassBase LGB { get { return m_lgb; } }

    public IAppParameters ModuleParams { get { return m_lgb.AppParams; } }

    protected CommLLLP m_comm;
    protected RestHandler m_restHandler;

    protected Queue<ChatEntry> m_chats;

    public LLChat() {
        // default to the class name. The module code can set it to something else later.
        m_moduleName = System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name;
    }

    // IModule.OnLoad
    public virtual void OnLoad(string modName, LookingGlassBase lgbase) {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".OnLoad()");
        m_moduleName = modName;
        m_lgb = lgbase;

        m_chats = new Queue<ChatEntry>();

        ModuleParams.AddDefaultParameter(m_moduleName + ".Comm.Name", "Comm",
                    "Name of LLLP comm to connect to");
    }

    // IModule.AfterAllModulesLoaded
    public virtual bool AfterAllModulesLoaded() {
        LogManager.Log.Log(LogLevel.DINIT, ModuleName + ".AfterAllModulesLoaded()");

        try {
            // Find the rest manager and setup to get web requests
            m_restHandler = new RestHandler("/chat", GetHandler, PostHandler);

            // Find the world and connect to same to hear about all the avatars
            String commName = ModuleParams.ParamString(m_moduleName + ".Comm.Name");
            m_comm = (CommLLLP)LGB.ModManager.Module(commName);
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "EXCEPTION CONNECTING TO SERVICES: {0}", e);
            return false;
        }

        m_comm.GridClient.Self.ChatFromSimulator += new EventHandler<OpenMetaverse.ChatEventArgs>(Self_ChatFromSimulator);

        return true;
    }

    // IModule.Start
    public virtual void Start() {
        return;
    }

    // IModule.Stop
    public virtual void Stop() {
        return;
    }

    // IModule.PrepareForUnload
    public virtual bool PrepareForUnload() {
        return false;
    }
    #endregion IModule

    // there is chat coming our way
    void Self_ChatFromSimulator(object sender, OpenMetaverse.ChatEventArgs e) {
        m_log.Log(LogLevel.DCOMMDETAIL, "Self_ChatFromSimulator: {0} says '{1}'", e.FromName, e.Message);
        if (e.Message.Length == 0) {
            // zero length messages are typing start and end
            return;
        }
        ChatEntry ce = new ChatEntry();
        ce.fromName = e.FromName;
        ce.message = e.Message;
        ce.position = e.Position;
        ce.sourceType = e.SourceType;
        ce.chatType = e.Type;
        switch (e.Type) {
            case OMV.ChatType.Normal:      ce.chatTypeString = "Normal";      break;
            case OMV.ChatType.Shout:       ce.chatTypeString = "Shout";       break;
            case OMV.ChatType.Whisper:     ce.chatTypeString = "Whisper";     break;
            case OMV.ChatType.OwnerSay:    ce.chatTypeString = "OwnerSay";    break;
            case OMV.ChatType.RegionSay:   ce.chatTypeString = "RegionSay";   break;
            case OMV.ChatType.Debug:       ce.chatTypeString = "Debug";       break;
            case OMV.ChatType.StartTyping: ce.chatTypeString = "StartTyping"; break;
            case OMV.ChatType.StopTyping:  ce.chatTypeString = "StopTyping";  break;
            default:                       ce.chatTypeString = "Normal";      break;
        }
        ce.ownerID = e.OwnerID;
        ce.chatEntryType = ChatEntryType.Normal;
        if (e.SourceType == OMV.ChatSourceType.Agent && e.FromName.EndsWith("Linden")) {
            ce.chatEntryType = ChatEntryType.LindenChat;
        }
        if (e.SourceType == OMV.ChatSourceType.Object) {
            if (e.Type == OMV.ChatType.OwnerSay) {
                ce.chatEntryType = ChatEntryType.OwnerSay;
            }
            else {
                ce.chatEntryType = ChatEntryType.ObjectChat;
            }
        }
        lock (m_chats) m_chats.Enqueue(ce);
    }

    private OMVSD.OSD GetHandler(RestHandler handler, Uri uri, String after) {
        OMVSD.OSDMap ret = new OMVSD.OSDMap();
        string lastDate = "xx";
        lock (m_chats) {
            while (m_chats.Count > 0) {
                ChatEntry ce = m_chats.Dequeue();
                string dateString = ce.time.ToString("yyyyMMddhhmmssfff");
                OMVSD.OSDMap chat = new OMVSD.OSDMap();
                chat.Add("Time", new OMVSD.OSDString(dateString));
                chat.Add("From", new OMVSD.OSDString(ce.fromName));
                chat.Add("Message", new OMVSD.OSDString(ce.message));
                chat.Add("Type", new OMVSD.OSDString(ce.chatTypeString));
                chat.Add("EntryType", new OMVSD.OSDString(ChatEntryTypeString[(int)ce.chatEntryType]));
                chat.Add("Position", new OMVSD.OSDString(ce.position.ToString()));
                if (ce.ownerID != null) {
                    chat.Add("OwnerID", new OMVSD.OSDString(ce.ownerID.ToString()));
                }
                while (ret.ContainsKey(dateString)) {
                    dateString += "1";
                }
                ret.Add(dateString, chat);
                lastDate = dateString;
            }
        }
        return ret;
    }

    private OMVSD.OSD PostHandler(RestHandler handler, Uri uri, String after, OMVSD.OSD body) {
        try {
            OMVSD.OSDMap mapBody = (OMVSD.OSDMap)body;
            m_log.Log(LogLevel.DCOMMDETAIL, "PostHandler: received chat '{0}'", mapBody["Message"]);
            // collect parameters and send it to the simulator
            string msg = Uri.UnescapeDataString(mapBody["Message"].AsString().Replace("+", " "));
            OMVSD.OSD channelString = new OMVSD.OSDString("0");
            mapBody.TryGetValue("Channel", out channelString);
            int channel = Int32.Parse(channelString.AsString());
            OMVSD.OSD typeString = new OMVSD.OSDString("Normal");
            mapBody.TryGetValue("Type", out typeString);
            OMV.ChatType chatType = OpenMetaverse.ChatType.Normal;
            if (typeString.AsString().Equals("Whisper")) chatType = OMV.ChatType.Whisper;
            if (typeString.AsString().Equals("Shout")) chatType = OMV.ChatType.Shout;
            m_comm.GridClient.Self.Chat(msg, channel, chatType);

            // echo my own message back for the log and chat window
            /* NOTE: Don't have to do this. The simulator echos it back
            OMV.ChatEventArgs cea = new OpenMetaverse.ChatEventArgs(m_comm.GridClient.Network.CurrentSim, 
                            msg, 
                            OpenMetaverse.ChatAudibleLevel.Fully,
                            chatType, 
                            OpenMetaverse.ChatSourceType.Agent, 
                            m_comm.GridClient.Self.Name, 
                            OMV.UUID.Zero, 
                            OMV.UUID.Zero, 
                            m_comm.GridClient.Self.RelativePosition);
            this.Self_ChatFromSimulator(this, cea);
             */
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DCOMM, "ERROR PARSING CHAT MESSAGE: {0}", e);
        }
        // the return value does not matter
        return new OMVSD.OSDMap();
    }
}
}
