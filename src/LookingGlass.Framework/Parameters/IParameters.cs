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
using OMVSD = OpenMetaverse.StructuredData;

namespace LookingGlass.Framework.Parameters {

/// <summary>
/// Any program has many little knobs and values that effect its operation
/// and its interaction with the environment. A class that implements
/// IParameterProvider keeps these knobs and values in name/value pairs,
/// collects all the parameters into one place and makes the values
/// easily usable by an application. This interface is usually backedup
/// with an implemetation that persists the name/value pairs into a file
/// or database.
/// 
/// Parameter names are, by convention, formatted as words separated by
/// periods: "Renderer.background.fixedImage". The words as alphanumeric
/// with no white space. Case of the alpha characters is not significant.
/// 
/// The name/value pairs are always strings.
/// 
/// Error handling: there are three ways to return errors: default, null
/// and exception. Each is suitable for different applications. 
/// <list type="bullet">
/// <item>
/// 'default' returns a default value is the parameter has no value.
/// The defaults are "" for string (empty string) and zero for integers.
/// </item>
/// <item>
/// 'null' method returns the value 'null' if an application asks for 
/// a parameter that doesn't have a value.
/// <item>
/// 'exception' throws an exception if there is no value
/// </item>
/// </list>
/// These modes are set by the method 'ParamErrorMethod'. The default
/// is 'exception'.
/// 
/// </summary>
/// 

public class ParameterException : Exception {
        public ParameterException(string message)
            : base(message) {
        }

        public ParameterException(string message, Exception innerException)
            : base(message, innerException) {
        }
}

public enum paramErrorType {
    eDefaultValue,
    eNullValue,
    eException,
};

// call when things are modified
public delegate void ParamValueModifiedCallback(IParameters collection, string key, OMVSD.OSD newValue);
// perform an action on a key,value pair
public delegate void ParamAction(string k, OMVSD.OSD v);

public interface IParameters {

    event ParamValueModifiedCallback OnModifiedCallback;

    paramErrorType ParamErrorMethod { get; set; }
    
    /// <summary>
    /// Add a parameter to the store.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does.
    /// Value may be 'null' to specify no description</param>
    void Add(string key, OMVSD.OSD value);
    void Add(string key, string value);

    /// <summary>
    /// Return TRUE if the given parameter has a value specified.
    /// </summary>
    /// <param name="key">parameter to check for</param>
    /// <returns></returns>
    bool HasParameter(string key);

    /// <summary>
    /// Update a previously added parameter. This looks for the key
    /// in the User paramters and then the INI parameters. This
    /// will cause the update callbacks to happen
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void Update(string key, OMVSD.OSD value);
    void Update(string key, string value);

    /// <summary>
    /// Update a previously added parameter. This looks for the key
    /// in the User paramters and then the INI parameters. This
    /// doesn't call the updated callbacks.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    void UpdateSilent(string key, OMVSD.OSD value);
    void UpdateSilent(string key, string value);

    /// <summary>
    /// Return a string value for a parameter
    /// </summary>
    /// <param name="key">parameter to get value for</param>
    /// <returns>parameter value</returns>
    string ParamString(string key);

    /// <summary>
    /// Return an integer value for a parameter.
    /// The underlying string must parse into an integer.
    /// </summary>
    /// <param name="key">parameter to get value</param>
    /// <returns>integer representation of parmater value</returns>
    int ParamInt(string key);

    /// <summary>
    /// Return a boolean value for a parameter.
    /// The underlying string must parse into an integer.
    /// </summary>
    /// <param name="key">parameter to get value</param>
    /// <returns>integer representation of parmater value</returns>
    bool ParamBool(string key);

    /// <summary>
    /// Return a string value for a parameter
    /// </summary>
    /// <param name="key">parameter to get value for</param>
    /// <returns>parameter value</returns>
    OMVSD.OSD ParamValue(string key);

    /// <summary>
    /// Return all the values for the specified key.
    /// Searches only the Ini list.
    /// </summary>
    /// <param name="key">parameter to get value</param>
    /// <returns>Array of string values. Could be one, could be many.</returns>
    List<string> ParamStringArray(string key);

    /// <summary>
    /// Perform an action on each value in the parameter set
    /// </summary>
    /// <param name="act"></param>
    void ForEach(ParamAction act);
}
}
