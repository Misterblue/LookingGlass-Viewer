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
    public partial class ViewSplash : Form {
        LookingGlassBase LGB;

        public ViewSplash(LookingGlassBase llgb) {
            LGB = llgb;

            InitializeComponent();
            Random rand = new Random();

            string[] ContentLines = new String[12];
            ContentLines[00] = LookingGlassBase.ApplicationName + "  " + LookingGlassBase.ApplicationVersion;
            ContentLines[01] = "";
            ContentLines[02] = "Copyright (c) 2008 Robert Adams (LookingGlass)";
            ContentLines[03] = "Copyright (c) 2000-2006 Torus Knot Software Ltd (Ogre)";
            ContentLines[04] = "Copyright (c) OpenSimulator Contributors http://opensimulator.org/ (OpenSimulator)";
            ContentLines[05] = "Copyright (c) 2007-2009, openmetaverse.org (OpenMetaverse)";
            ContentLines[06] = "Copyright (c) Contributors, http://idealistviewer.org/ (Idealist)";
            ContentLines[07] = "Copyright (C) 2009 Xavier Verguín González (SkyX)";
            ContentLines[08] = "Copyright (c) 2009 John Resig (jQuery)";
            ContentLines[09] = "Copyright (c) 2009, Gareth Watts (SparkLines)";
            ContentLines[10] = "Copyright (c) 2004-2008 Matthew Holmes, Dan Moorehead, Rob Loach, C.J. Adams-Collier (Prebuild v2.0.4)";
            ContentLines[11] = "Copyright (c) 2001 Peter Dimov and Multi Media Ltd. (Boost)";
            this.WindowSplash.Lines = ContentLines;
            this.WindowSplash.Refresh();
            this.WindowSplash.BringToFront();
        }

        public void Initialize() {
        }

        public void Shutdown() {
            if (this.InvokeRequired) {
                BeginInvoke((MethodInvoker)delegate() { this.Close(); });
            }
            else {
                this.Close();
            }
        }

    }
}
