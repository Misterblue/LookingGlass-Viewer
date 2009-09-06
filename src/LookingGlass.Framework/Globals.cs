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
using System.IO;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Modules;
using LookingGlass.Framework.Parameters;

namespace LookingGlass {
public static class Globals {

    private static string m_applicationName = "LookingGlass";
    public static string ApplicationName { get { return m_applicationName; } set { m_applicationName = value; } }
    private static string m_applicationVersion = "V0.1.1";
    public static string ApplicationVersion { get { return m_applicationVersion; } set { m_applicationVersion = value; } }

    /// <summary>
    /// True if everything should keep running. Anything can set this to 'false'.
    /// Once set, the main control will call Stop() on everything and shut it all
    /// down.
    /// </summary>
    private static bool m_keepRunning = false;
    public static bool KeepRunning { get { return m_keepRunning; } set { m_keepRunning = value; } }

    /// <summary>
    /// Useful Constants
    /// </summary>
    public static float DEGREETORADIAN = 0.0174533f;
    public static float PI = 3.14159265f;
    public static float TWOPI = 6.28318530f;

    /// <summary>
    /// All of the parameters for the applicaiton are saved in this stucture
    /// </summary>
    private static AppParameters m_configuration = null;
    public static AppParameters Configuration { get { return m_configuration; } set { m_configuration = value; } }
    public static void ReadConfigurationFile() {
        // if anything goes wrong, just throw exception
        IParameterPersist ipp = (IParameterPersist)m_configuration;
        ipp.ReadParameterPersist();
    }
    public static void WriteConfigurationFile() {
        // if anything goes wrong, just throw exception
        IParameterPersist ipp = (IParameterPersist)m_configuration;
        ipp.WriteParameterPersist();
    }

    /// <summary>
    /// The stupid application storage function MS defined adds "corporation/application/version"
    /// to the end of the application path. This takes them off and just adds the application name.
    /// </summary>
    /// <returns></returns>
    public static string GetDefaultApplicationStorageDir(string subdir) {
        string appdir = System.Windows.Forms.Application.UserAppDataPath;
        string[] pieces = appdir.Split(Path.DirectorySeparatorChar);
        string newAppDir = pieces[0];
        if (pieces.Length > 3) {
            newAppDir = String.Join(System.Char.ToString(Path.DirectorySeparatorChar), pieces, 0, pieces.Length - 3);
        }
        newAppDir = Path.Combine(newAppDir, Globals.ApplicationName);
        if ((subdir != null) && (subdir.Length > 0)) newAppDir = Path.Combine(newAppDir, subdir);
        return newAppDir;
    }

}
}
