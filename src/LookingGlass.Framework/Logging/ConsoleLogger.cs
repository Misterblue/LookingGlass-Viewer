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

namespace LookingGlass.Framework.Logging {

    public sealed class ConsoleLogger : ILog {

        // there is only one filterlevel used by all instances of the logger class
        private static LogLevel filterLevel = 0;
        public LogLevel FilterLevel { set { filterLevel = value; } get { return filterLevel; } }

        private string moduleName = "";
        public string ModuleName { set { moduleName = value; } get { return moduleName; } }

        public ConsoleLogger() : this("") {
        }

        public ConsoleLogger(string modName) {
            moduleName = modName;
        }

        /// <summary>
        /// return true of a message would be logged with the specified loglevel
        /// </summary>
        /// <param name="logLevel">the loglevel to test if it's enabled now</param>
        /// <returns></returns>
        public bool WouldLog(LogLevel logLevel) {
            return IfLog(logLevel);
        }

        /// <summary>
        /// Internal routine that returns true if the passed logging flag it not filtered
        /// (it will cause some output).
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        private bool IfLog(LogLevel logLevel) {
            return ((logLevel & filterLevel) != 0);
        }

        /// <summary>
        /// Log the passed message if the loglevel is not filtered out
        /// </summary>
        /// <param name="logLevel">log level of the message</param>
        /// <param name="msg">the message to log</param>
        public void Log(LogLevel logLevel, string msg) {
            if (IfLog(logLevel)) LogIt(logLevel, msg);
        }

        public void Log(LogLevel logLevel, string msg, object p1) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1));
        }

        public void Log(LogLevel logLevel, string msg, object p1, object p2) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1, p2));
        }

        public void Log(LogLevel logLevel, string msg, object p1, object p2, object p3) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1, p2, p3));
        }

        public void Log(LogLevel logLevel, string msg, object p1, object p2, object p3, object p4) {
            if (IfLog(logLevel)) LogIt(logLevel, String.Format(msg, p1, p2, p3, p4));
        }

        private void LogIt(LogLevel logLevel, string msg) {
            StringBuilder buf = new StringBuilder(256);
            buf.Append(DateTime.Now.ToString("yyyyMMddHHmmss"));
            buf.Append(": ");
            buf.Append(LookingGlassBase.ApplicationName);
            buf.Append(": ");
            if (ModuleName.Length != 0) {
                buf.Append(ModuleName);
            }
            buf.Append(": ");
            buf.Append(msg);

            Console.WriteLine(buf.ToString());
        }
    }
}
