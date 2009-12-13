using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using LookingGlass.Framework;
using LookingGlass.Framework.Parameters;

namespace LookingGlass.View {
    public partial class FormAvatars : Form {
        LookingGlassBase LGB;

        public FormAvatars(LookingGlassBase llgb) {
            LGB = llgb;

            InitializeComponent();

            string baseURL = LGB.AppParams.ParamString("RestManager.BaseURL");
            string portNum = LGB.AppParams.ParamString("RestManager.Port");
            string avatarURL = baseURL + ":" + portNum + "/static/ViewAvatars.html";
            this.WindowAvatars.Url = new Uri(avatarURL);
            this.WindowAvatars.ScriptErrorsSuppressed = false;  // DEBUG
            this.WindowAvatars.Refresh();
            this.WindowAvatars.BringToFront();
        }
    }
}
