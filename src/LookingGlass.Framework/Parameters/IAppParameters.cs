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

namespace LookingGlass.Framework.Parameters {
/// <summary>
/// The application handles parameters with several layers of
/// parameters specification.
/// The application modules will add Default parameters. Then
/// parameters come from the configuratin file ("Ini" parameters).
/// Then the user specific parameters are added and finally
/// override, session parameters which usually come from the invocation
/// line.
/// When searching for a value for a parameter, these sets are searched
/// in the order: override, user, ini, default. The first found value is
/// used.
/// 
/// Parameters are stored in multiple lists. Default, Ini, User, and Override.
/// Default comes from the program itself (default values for every
/// possible parameter), Ini is read from the initialization file,
/// User is read from the user parameter file and Override are command
/// line parameters.
/// 
/// These lists are searched in order from Override to Default. The first
/// found value is used. The exeception is the multiple valued parameters
/// which can only occur in the Ini list.
/// 
/// The workflow for parameters is:
/// <list type="bullet">
/// <item>
/// create the instance of the class providing IParameterProvider;
/// </item>
/// <item>
/// as each extension or module is initialized, it adds the defaults
/// for its parameters with 'addDefaultParameter'. This makes all the
/// parameters, their values and documentation available;
/// </item>
/// <item>
/// read in the User file
/// </item>
/// <item>
/// read in the Ini file
/// </item>
/// <item>
/// </item>
/// read in the Modules file (into the Ini list)
/// <item>
/// add any override name/value pairs with 'addOverrideParameter';
/// use the parameters
/// <item>
/// </list>
/// </summary>
 
public interface IAppParameters : IParameters {
    /// <summary>
    /// Add a parameter to the Default store. Searched last.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does</param>
    void AddDefaultParameter(string key, string value, string desc);

    /// <summary>
    /// Add a parameter to the Ini store. Searched next to last.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does.
    /// Value may be 'null' to specify no description</param>
    void AddIniParameter(string key, string value, string desc);

    /// <summary>
    /// Add a parameter to the User store. Searched second.
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    /// <param name="desc">human readable explanation of what the parameter does.
    /// Value may be 'null' to specify no description</param>
    void AddUserParameter(string key, string value, string desc);

    /// <summary>
    /// Add an override parameter to the store. Searched first
    /// </summary>
    /// <param name="key">parameter name</param>
    /// <param name="value">string representation of value</param>
    void AddOverrideParameter(string key, string value);

}
}
