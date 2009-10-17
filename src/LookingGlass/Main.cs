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
using System.Reflection;
using System.Threading;
using System.Collections.Generic;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;

namespace LookingGlass {
class Program {
    static ILog m_log = LogManager.GetLogger("Main");
    static Dictionary<string, string> m_Parameters;

    [STAThread]
    static void Main(string[] args) {

        LookingGlassBase LGB = new LookingGlassBase();

        try {
            m_Parameters = ParseArguments(args, false);
        }
        catch (Exception e) {
            throw new Exception("Could not parse command line parameters: " + e.ToString());
        }
        foreach (KeyValuePair<string, string> kvp in m_Parameters)
        {
            switch (kvp.Key)
            {
                case FIRST_PARAM:
                    break;
                case "-first":
                case "--first":
                    LGB.AppParams.AddOverrideParameter("User.Firstname", kvp.Value);
                    break;
                case "-last":
                case "--last":
                    LGB.AppParams.AddOverrideParameter("User.Lastname", kvp.Value);
                    break;
                case "-password":
                case "--password":
                    LGB.AppParams.AddOverrideParameter("User.Password", kvp.Value);
                    break;
                case "--grid":
                    LGB.AppParams.AddOverrideParameter("User.Grid", kvp.Value);
                    break;
                case "--loginuri":
                    LGB.AppParams.AddOverrideParameter("User.LoginURI", kvp.Value);
                    break;
                case "--iniFile":
                    LGB.AppParams.AddOverrideParameter("Settings.INIFile", kvp.Value);
                    break;
                case "--modulesFile":
                    LGB.AppParams.AddOverrideParameter("Settings.Modules", kvp.Value);
                    break;
                case "--cache":
                    LGB.AppParams.AddOverrideParameter("Texture.CacheDir", kvp.Value);
                    break;
                case "--param":
                    int splitPlace = kvp.Value.IndexOf(':');
                    if (splitPlace > 0) {
                        LGB.AppParams.AddOverrideParameter(
                                kvp.Value.Substring(0, splitPlace - 1).Trim(),
                                kvp.Value.Substring(splitPlace).Trim());
                    }
                    else {
                        Console.Out.WriteLine("ERROR: Could not parse param value: " + kvp.Value);
                        Invocation();
                        return;
                    }
                    break;
                case "--debug":
                    break;
                case ERROR_PARAM:
                    // if we get here, the parser found an error
                    Console.WriteLine("Parameter error: " + kvp.Value);
                    Console.Write(Invocation());
                    return;
                default:
                    Console.WriteLine("ERROR: UNKNOWN PARAMETER: " + kvp.Key);
                    Console.Write(Invocation());
                    return;
            }
        }
        try {
            LGB.ReadConfigurationFile();
        }
        catch (Exception e) {
            throw new Exception("Could not read configuration file: " + e.ToString());
        }

        // log level after all the parameters have been set
        LogManager.CurrentLogLevel = (LogLevel)LGB.AppParams.ParamInt("Log.FilterLevel");

        if (LGB.Initialize()) {
            LGB.Start();
        }

        m_log.Log(LogLevel.DINIT, "EXIT");
    }

    private static string Invocation()
    {
        return @"Invocation:
View a virtual world as though through a looking glass.
INVOCATION:
LookingGlass 
        --first user
        --last user
        --password password
        --grid gridname
        --loginuri loginuri
        --cache cacheDirectory
        --debug
        --param parameter:value
        --renderer mogre|opengl
";
    }

    #region Parameter Processing
    // ================================================================
    /// <summary>
    /// Given the array of command line arguments, create a dictionary of the parameter
    /// keyword to values. If there is no value for a parameter keyword, the value of
    /// 'null' is stored.
    /// Command line keywords begin with "-" or "--". Anything else is presumed to be
    /// a value.
    /// </summary>
    /// <param name="args">array of command line tokens</param>
    /// <param name="firstOpFlag">if 'true' presume the first token in the parameter line
    /// is a special value that should be assigned to the keyword "--firstparam".</param>
    /// <returns></returns>
    const string FIRST_PARAM = "--firstParameter";
    const string LAST_PARAM = "--lastParameter";
    const string ERROR_PARAM = "--errorParameter";
    protected static Dictionary<string, string> ParseArguments(string[] args, bool firstOpFlag)
    {
        Dictionary<string,string> m_params = new Dictionary<string,string>();

        for (int ii = 0; ii < args.Length; ii++)
        {
            string para = args[ii];
            if (para[0] == '-')     // is this a parameter?
            {
                if (ii == (args.Length-1) || args[ii + 1][0] == '-') // is the next one a parameter?
                {
                    // two parameters in a row. this must be a toggle parameter
                    m_params.Add(para, null);
                }
                else
                {
                    // looks like a parameter followed by a value
                    m_params.Add(para, args[ii + 1]);
                    ii++;       // skip the value we just added to the dictionary
                }
            }
            else
            {
                if ((ii == 0) && firstOpFlag)
                {   // if the first thing is not a parameter, make like it's an op or something
                    m_params.Add(FIRST_PARAM, para);
                }
                else
                {
                    // This token is not a keyword. If it's the last thing, place it
                    // into the dictionary as the last parameter. Otherwise and error.
                    if (ii == args.Length - 1)
                    {
                        m_params.Add(LAST_PARAM, para);
                    }
                    else
                    {
                        // something is wrong with  the format of the parameters
                        m_params.Add(ERROR_PARAM, "Unknown parameter " + para);
                    }
                }
            }
        }
        return m_params;
    }
    #endregion Parameter Processing

}
}
