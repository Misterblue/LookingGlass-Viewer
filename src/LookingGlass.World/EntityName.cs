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
using System.Collections.Generic;
using System.Text;
using LookingGlass.World;

namespace LookingGlass.World {
// This class exists to hold all of the oddness of entity names.
// Names will have components and parts and conversion methods
// to convert between forms. This is where they all hide.
    // The goal is to have this class hold the details of the name
    // thus allowing recreation of the name.
    // The name is basicly an URI but it needs to be taken apart and
    // reassembled throughout it's use. This holds the pieces of the
    // URI so we don't need to reparse the name to rebuild it in
    // different forms.
// The basic name is available with the toString() method and string conversion
public class EntityName : IComparable {
    public const string HeaderSeparator = ":";
    public const string PartSeparator = "/";

    protected string m_fullName = null;
    protected string m_header = "";
    protected string m_host = "";   // host contains the terminating separator
    protected string m_entity = "";

    // The following is here so there is one place to change everything.
    // Entity names are rewritten for the underlying render system and to
    // match the cached filenames. These are rules  for converting regular
    // entity names into cached filenames and back.
    // rules for two level cache names
    // protected const string EntityNameMatch = @"^(...)(...)(..)-(.)(.*)$";
    // protected const string CachedNameReplace = @"$1/$2/$3$4/$1$2$3-$4$5";
    // protected const string CachedNameMatch = @"^(.*)/.../.../.../([^/]*)$";
    // protected const string EntityNameReplace = @"$1/$2";
    // Rules for two digit upper level cache dir
    // protected const string EntityNameMatch = @"^(..)(.*)$";
    // protected const string CachedNameReplace = @"$1/$1$2";
    // protected const string CachedNameMatch = @"^(.*)/[0-9a-f][0-9a-f]/([^/]*)$";
    // protected const string EntityNameReplace = @"$1/$2";
    // Rules for one digit upper level cache dir
    protected const string EntityNameMatch = @"^(.)(.*)$";
    protected const string CachedNameReplace = @"$1/$1$2";
    protected const string CachedNameMatch = @"^(.*)/[0-9a-f]/([^/]*)$";
    protected const string EntityNameReplace = @"$1/$2";

    // just a raw string. We don't know it's orgins so we guess at it's structure
    // presuming it is "HOST/ENTITYID"
    public EntityName(string name) {
        m_fullName = name;
        m_header = this.ExtractHeaderPartFromEntityName();
        m_host = this.ExtractHostPartFromEntityName();
        m_entity = this.ExtractEntityFromEntityName();
    }

    public EntityName(string header, string host, string entity) {
        m_header = header;
        m_host = host;
        m_entity = entity;
        m_fullName = null;
    }

    public EntityName(IEntity entityContext, string name) 
            : this(entityContext.AssetContext, name)
    {
    }

    public EntityName(EntityName entName) 
            : this(entName.Name)
    {
    }

    // created with a hosting asset server. 
    // Extract the host name handle from the asset context and use the passed name
    // as teh name of the entity in that context. 
    public EntityName(AssetContextBase acontext, string name) {
        if (acontext != null) {
            m_header = "";
            m_host = acontext.Name;
            m_entity = name;
        }
        else {
            m_header = "";
            m_host = "LOOKINGGLASS";
            m_entity = name;
        }
        // m_fullName is created when it is asked for
    }

    public string HeaderPart { get { return m_header; } }
    public string HostPart { get { return m_host; } }
    public string EntityPart { get { return m_entity; } }

    // return the complete, combined name
    public string Name {
        get {
            if (m_fullName == null) {
                m_fullName = CombineEntityName(m_header, m_host, m_entity);
            }
            return m_fullName;
        }
    }

    public static explicit operator string(EntityName name) {
        return name.Name;
    }

    public override string ToString() {
        return this.Name;
    }

    // IComparable method
    public int CompareTo(Object other) {
        if (other is EntityName) {
            return this.Name.CompareTo(((EntityName)other).Name);
        }
        if (other is String) {
            return this.Name.CompareTo((string)other);
        }
        throw new ArgumentException("EntityName.CompareTo: passed non EntityName or String");
    }

    // Raw routine for combining the parts of the name.
    // We still don't handle headers properly
    // Can be overridden for context specific changes
    public virtual string CombineEntityName(string header, string host, string ent) {
        string ret = "";
        if (header != null && header.Length != 0) {
            if (header.EndsWith(HeaderSeparator)) {
                ret = header;
            }
            else {
                ret = header + HeaderSeparator;
            }
        }
        if (host.EndsWith(PartSeparator)) {
            ret += host + ent;
        }
        else {
            ret += host + PartSeparator + ent;
        }
        return ret;
    }

    // the default way to build a cache filename.
    // Can be overridden for context specific changes
    public virtual string CacheFilename {
        get {
            return CombineEntityName(HeaderPart, HostPart, EntityPart);
        }
    }

    // we really don't do headers yet
    // Can be overridden for context specific changes
    public virtual string ExtractHeaderPartFromEntityName() {
        return "";
    }

    // default way to get the host part out of an entity name
    // The default format is HOSTPART + "/" + ENTITYPART
    // Can be overridden for context specific changes
    public virtual string ExtractHostPartFromEntityName() {
        int pos1 = this.Name.IndexOf(HeaderSeparator);
        int pos = this.Name.IndexOf(PartSeparator);
        if (pos > 0) {
            return this.Name.Substring(0, pos);
        }
        return "";
    }

    // Can be overridden for context specific changes
    // we assume it's whatever's after the first slash
    public virtual string ExtractEntityFromEntityName() {
        int pos = this.Name.IndexOf(PartSeparator);
        if (pos >= 0) {
            return this.Name.Substring(pos + 1);
        }
        return "";
    }

}
}
