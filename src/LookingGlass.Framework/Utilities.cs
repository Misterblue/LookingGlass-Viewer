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
using System.IO;
using System.Text;

namespace LookingGlass {
    /// <summary>
    /// Every program has a place to put general, useful, tool routines.
    /// </summary>
public class Utilities {
    /// <summary>
    /// Combine two strings into one longer url. We made sure there is only one
    /// slash between the two joined halves. This means we check for and remove
    /// any extra slash at the end of the first string or the beginning of the last
    /// string.
    /// </summary>
    /// <param name="first"></param>
    /// <param name="last"></param>
    /// <returns></returns>
    public static string JoinURLPieces(string first, string last) {
        string f = first.EndsWith("/") ? first.Substring(first.Length-1) : first;
        string l = last.StartsWith("/") ? last.Substring(1, last.Length-1) : last;
        return f + "/" + l;
    }

    /// <summary>
    /// Combine two filename pieces so there is one directory separator between.
    /// This replaces System.IO.Path.Combine which has the nasty feature that it
    /// ignores the first string if the second begins with a separator.
    /// It assumes that it's root and you don't want to join. Wish they asked
    /// me.
    /// </summary>
    /// <param name="first"></param>
    /// <param name="last"></param>
    /// <returns></returns>
    public static string JoinFilePieces(string first, string last) {
        // string separator = "" + Path.DirectorySeparatorChar;
        string separator = "/";     // .NET and mono just use the forward slash
        string f = first.EndsWith(separator) ? first.Substring(first.Length-1) : first;
        string l = last.StartsWith(separator) ? last.Substring(1, last.Length-1) : last;
        return f + separator + l;
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
        newAppDir = Path.Combine(newAppDir, LookingGlassBase.ApplicationName);
        if ((subdir != null) && (subdir.Length > 0)) newAppDir = Path.Combine(newAppDir, subdir);
        return newAppDir;
    }

    public static int TickCountMask = 0x3fffffff;
    public static int TickCount() {
        return System.Environment.TickCount & TickCountMask;
    }
    public static int TickCountSubtract(int prev) {
        int ret = TickCount() - prev;
        if (ret < 0) ret += TickCountMask + 1;
        return ret;
    }

}
}
