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
using log4net;
using log4net.Config;

[assembly: log4net.Config.XmlConfigurator(ConfigFileExtension = "log4net", Watch = true)]

namespace LookingGlass.Framework.Logging {

    public sealed class Log4NetLogger : ILog {

        private string moduleName = "";
        public string ModuleName { set { moduleName = value; } get { return moduleName; } }

        private log4net.ILog logger = null;
        private static Object lockObject = new Object();

        public Log4NetLogger() : this("") {
        }

        public Log4NetLogger(string modName) {
            moduleName = modName;
            // logger = log4net.LogManager.GetLogger(modName);
            lock (lockObject) {
                if (logger == null) {
                    // logger = log4net.LogManager.GetLogger(LookingGlassBase.ApplicationName);
                    logger = log4net.LogManager.GetLogger(moduleName);
                    // logger = log4net.LogManager.GetLogger(Assembly.GetExecutingAssembly().FullName);
                    // logger = log4net.LogManager.GetLogger(ModuleName);
                    // If error level reporting isn't enabled we assume no logger is configured 
                    // and initialize a default ConsoleAppender
                    if (!logger.IsErrorEnabled) {
                        log4net.Appender.ConsoleAppender appender = new log4net.Appender.ConsoleAppender();
                        appender.Layout = new log4net.Layout.PatternLayout("%timestamp[%thread]%-5level: %message%newline");
                        BasicConfigurator.Configure(appender);
                    }
                }
            }
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
            if (logLevel == LogLevel.DBADERROR) return true;
            bool ret = false;
            if ((logLevel & LogManager.CurrentLogLevel) == logLevel) {
                if ((logLevel & LogLevel.DDETAIL) != 0) {
                    // must also have log4net configed for "DEBUG" to get detailed output
                    ret = logger.IsDebugEnabled;    // DETAIL is only output if in debug mode
                }
                else {
                    ret = logger.IsInfoEnabled;     // if INFO, output it
                }
            }
            return ret;
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
            if ((logLevel & LogLevel.DDETAIL) != 0) {
                logger.Debug(buf.ToString());
            }
            else {
                logger.Info(buf.ToString());
            }
        }
    }
}
