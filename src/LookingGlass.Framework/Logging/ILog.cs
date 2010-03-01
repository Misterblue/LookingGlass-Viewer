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

    public enum LogLevel {
        DINIT         = 0x00000001,
        DINITDETAIL   = 0x40000002,
        DVIEW         = 0x00000004,
        DVIEWDETAIL   = 0x40000008,
        DWORLD        = 0x00000010,
        DWORLDDETAIL  = 0x40000020,
        DCOMM         = 0x00000040,
        DCOMMDETAIL   = 0x40000080,
        DREST         = 0x00000100,
        DRESTDETAIL   = 0x40000200,
        DRENDER       = 0x00000400,
        DRENDERDETAIL = 0x40000800, // 1073743872
        DTEXTURE      = 0x00001000,
        DTEXTUREDETAIL= 0x40002000,
        DMODULE       = 0x00004000,
        DMODULEDETAIL = 0x40008000,
        DUPDATE       = 0x00010000,
        DUPDATEDETAIL = 0x40020000,
        DRADEGAST     = 0x00040000,
        DRADEGASTDETAIL=0x40080000,
        DOGRE         = 0x00100000,
        DOGREDETAIL   = 0x40200000,

        DNONDETAIL    = 0x05555555, // all  the non-detail enables
        DDETAIL       = 0x40000000, // 1073741824
        DALL          = 0x7fffffff,
        DBADERROR     = 0x7fffffff,
}

    public interface ILog {
        bool WouldLog(LogLevel logLevel);
        void Log(LogLevel logLevel, string msg);
        void Log(LogLevel logLevel, string msg, object p1);
        void Log(LogLevel logLevel, string msg, object p1, object p2);
        void Log(LogLevel logLevel, string msg, object p1, object p2, object p3);
        void Log(LogLevel logLevel, string msg, object p1, object p2, object p3, object p4);
    }
}
