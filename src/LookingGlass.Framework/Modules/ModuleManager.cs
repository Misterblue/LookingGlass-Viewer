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
using System.Reflection;
using System.Text;
using LookingGlass.Framework.Logging;
using LookingGlass.Framework.Parameters;
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.Modules {

public class ModuleManager {
    private static Dictionary<string, IModule> m_modules;
    private static ILog m_log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.Name);

    static ModuleManager() {
        m_modules = new Dictionary<string, IModule>();
    }

    /// <summary>
    /// Load a module giving and assembly name and an interface name.
    /// </summary>
    /// <param name="assemblyFilename">Name of assembly to look in</param>
    /// <param name="interfaceType">Fullname of the interface to look for</param>
    /// <param name="modName">Name of the module to manage it under. If null, the module
    /// will not be managed by this module manager.</param>
    /// <returns></returns>
    public static Object LoadModule(string assemblyFilename, string interfaceType, string modName, IAppParameters modParams) {
        string fullfile = new System.IO.FileInfo(assemblyFilename).FullName;
        Object obj = null;
        try {
            System.Reflection.Assembly assem = System.Reflection.Assembly.LoadFile(fullfile);
            foreach (Type type in assem.GetTypes()) {
                if (!type.IsClass || type.IsNotPublic) continue;
                Type[] interfaces = type.GetInterfaces();
                for (int ii = 0; ii < interfaces.Length; ii++) {
                    if (interfaces[ii].FullName.Equals(interfaceType)) {
                        //Object[] parms = { modName , modParams } ;
                        obj = assem.CreateInstance(type.FullName,
                                true, System.Reflection.BindingFlags.Default,
                                null, null, null, null);
                        //        null, parms, null, null);
                        m_log.Log(LogLevel.DMODULE, "CreateInstance of " + type.FullName);
                        ((IModule)obj).OnLoad(modName, modParams);
                        if (modName != null) {
                            ManageModule(obj, modName);
                        }
                        break;
                    }
                }
                if (obj != null) break;
            }
        }
        catch (System.Reflection.ReflectionTypeLoadException e) {
            m_log.Log(LogLevel.DBADERROR, "Failed to load type {0}. Exceptions follow", interfaceType);
            foreach (Exception ee in e.LoaderExceptions) {
                m_log.Log(LogLevel.DBADERROR, "LoaderException:" + ee.ToString());
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failed to load type {0} from {1}: {2}", 
                    interfaceType, assemblyFilename, e.ToString());
            obj = null;
        }
        if (obj == null) {
            m_log.Log(LogLevel.DBADERROR, "Failed to load type {0} from {1}", interfaceType, assemblyFilename );
            throw new LookingGlassException("Could not load module " + interfaceType
                    + " from " + assemblyFilename );
        }
        return obj;
    }

    /// <summary>
    /// Add a module to the managed module list. The parameter can be anything
    /// and this routine only adds it to the managed list if it's a managable
    /// thingy (it implements IModule).
    /// </summary>
    /// <param name="modu">IModule to manage</param>
    /// <returns>true if added. false otherwise</returns>
    public static bool ManageModule(Object modu) {
        bool ret = false;
        if (modu is IModule) {
            m_modules.Add(((IModule)modu).ModuleName, (IModule)modu);
            ret = true;
        }
        return ret;
    }

    public static bool ManageModule(Object modu, string name) {
        bool ret = false;
        if (modu is IModule) {
            m_modules.Add(name, (IModule)modu);
            ret = true;
        }
        return ret;
    }

    public static IModule Module(string modName) {
        IModule ret = null;
        m_modules.TryGetValue(modName, out ret);
        return ret;
    }

    public static bool AfterAllModulesLoaded() {
        bool ret = true;
        foreach (KeyValuePair<string,IModule> kvp in m_modules) {
            try {
                m_log.Log(LogLevel.DMODULE, kvp.Key + ".AfterAllModulesLoaded()");
                ret = kvp.Value.AfterAllModulesLoaded();
                if (!ret) {
                    m_log.Log(LogLevel.DBADERROR, "AfterAllModulesLoaded: "
                            + kvp.Key + " returned false so we're exiting");
                    break;
                }
            }
            catch (Exception e) {
                m_log.Log(Logging.LogLevel.DBADERROR,
                         "Exception calling " + kvp.Key + ".AfterAllModulesLoaded(): " + e.ToString());
                throw e;
            }
        }
        return ret;
    }

    public static bool Start() {
        bool ret = true;
        foreach (KeyValuePair<string,IModule> kvp in m_modules) {
            try {
                m_log.Log(LogLevel.DMODULE, kvp.Key + ".Start()");
                kvp.Value.Start();
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "Exception calling " + kvp.Key + ".Start(): " + e.ToString());
                ret = false;
            }
        }
        return ret;
    }

    public static bool Stop() {
        foreach (KeyValuePair<string,IModule> kvp in m_modules) {
            try {
                m_log.Log(LogLevel.DMODULE, kvp.Key + ".Stop()");
                kvp.Value.Stop();
            }
            catch (Exception e) {
                m_log.Log(LogLevel.DBADERROR, "Exception calling " + kvp.Key + ".Stop(): " + e.ToString());
            }
        }
        return true;
    }

    public static bool PrepareForUnload() {
        foreach (KeyValuePair<string,IModule> kvp in m_modules) {
            m_log.Log(LogLevel.DMODULE, kvp.Key + ".PrepareForUnload()");
            kvp.Value.PrepareForUnload();
        }
        return true;
    }

    public static bool Unload() {
        foreach (KeyValuePair<string,IModule> kvp in m_modules) {
            m_log.Log(LogLevel.DMODULE, kvp.Key + ".Unload()");
            m_modules.Remove(kvp.Key);
        }
        return true;
    }

    /// <summary>
    /// Get the module specifications from the configuration, load all the modules,
    /// and call their post all loaded entries then start them.
    /// </summary>
    /// <returns>true if it looks like everything worked</returns>
    public static bool LoadAndStartModules() {
        bool successFlag = true;
        try {
            OMVSD.OSD modulesRaw = Globals.Configuration.ParamValue("Modules");
            // modules are specified by an array of maps
            if (modulesRaw != null && modulesRaw.Type == OMVSD.OSDType.Array) {
                OMVSD.OSDArray moduleArray = (OMVSD.OSDArray)modulesRaw;
                foreach (OMVSD.OSDMap modSpec in moduleArray) {
                    string modAssembly = modSpec["Assembly"].AsString();
                    string modInterface = modSpec["Interface"].AsString();
                    string modName = modSpec["Name"].AsString();
                    Object obj = ModuleManager.LoadModule(modAssembly, modInterface, modName, Globals.Configuration);
                    if (obj == null) {
                        m_log.Log(LogLevel.DBADERROR, "Failed to load module."
                                + " a='" + modAssembly
                                + "', i='" + modInterface
                                + "', n=" + modName);
                        successFlag = false;
                        break;
                    }
                }
            }
            else {
                m_log.Log(LogLevel.DBADERROR, "'Modules' parameter is not an array of maps. Could not load modules");
                successFlag = false;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failed starting modules: " + e.ToString());
            successFlag = false;
        }

        try {
            if (successFlag && ModuleManager.AfterAllModulesLoaded()) {
                ModuleManager.Start();
                successFlag = true;
            }
            else {
                successFlag = false;
            }
        }
        catch (Exception e) {
            m_log.Log(LogLevel.DBADERROR, "Failed starting modules: " + e.ToString());
            successFlag = false;
        }

        return successFlag;
    }
}
}
