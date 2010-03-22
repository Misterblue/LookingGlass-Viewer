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

            string[] ContentLines = new String[30];
            int line = 0;
            ContentLines[line++] = LookingGlassBase.ApplicationName + "  " + LookingGlassBase.ApplicationVersion;
            ContentLines[line++] = "";
            ContentLines[line++] = "Copyright (c) 2008 Robert Adams (LookingGlass)";
            ContentLines[line++] = "Copyright (c) 2000-2006 Torus Knot Software Ltd (Ogre)";
            ContentLines[line++] = "Copyright (c) 2001 Peter Dimov and Multi Media Ltd. (Boost)";
            ContentLines[line++] = "Copyright (c) OpenSimulator Contributors http://opensimulator.org/ (OpenSimulator)";
            ContentLines[line++] = "Copyright (c) 2007-2009, openmetaverse.org (OpenMetaverse)";
            ContentLines[line++] = "Copyright (c) Contributors, http://idealistviewer.org/ (Idealist)";
            ContentLines[line++] = "Copyright (C) 2009 Xavier Verguín González (SkyX)";
            ContentLines[line++] = "Copyright (c) 2009 John Resig (jQuery)";
            ContentLines[line++] = "Copyright (c) 2009, Gareth Watts (SparkLines)";
            ContentLines[line++] = "Copyright (c) 2004-2008 Matthew Holmes, Dan Moorehead, Rob Loach, C.J. Adams-Collier (Prebuild v2.0.4)";
            ContentLines[line++] = "Copyright (C) 2008 Linden Research, Inc. (Avatar mesh/artwork)";
            Array.Resize(ref ContentLines, line);
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
