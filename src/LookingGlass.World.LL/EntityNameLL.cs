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
using System.Text.RegularExpressions;
using LookingGlass.Framework.Logging;
using LookingGlass.World;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {
// class to hold the LLLP specific name conversion routines
public class EntityNameLL : EntityName {

    // being created with just a resource name. We extract the parts
    public EntityNameLL(string name) : base(name) {
    }

    public EntityNameLL(EntityName ent) 
            : base(ent.Name) {
    }

    public EntityNameLL(IEntity entityContext, string name) 
            : base(entityContext.AssetContext, name) {
    }

    public EntityNameLL(AssetContextBase acontext, string name) 
            : base(acontext, name) {
    }

    public static EntityNameLL ConvertTextureWorldIDToEntityName(AssetContextBase context, OMV.UUID textureWorldID) {
        return ConvertTextureWorldIDToEntityName(context, textureWorldID.ToString());
    }

    public static EntityNameLL ConvertTextureWorldIDToEntityName(AssetContextBase context, string textureWorldID) {
        return new EntityNameLL(context, textureWorldID);
    }

    // this are the same rules as in EntityNameOgre so the file ends up in the cache at the right location
    // private const string EntityNameMatch = @"^(...)(...)(..)-(.)(.*)$";
    // private const string OgreNameReplace = @"$1/$2/$3$4/$1$2$3-$4$5";
    private const string EntityNameMatch = @"^(..)(.*)$";
    private const string OgreNameReplace = @"$1/$1$2";

    // Return the cache filename for this entity. This is not based in the cache directory.
    // At the moment, closely tied to the Ogre resource storage structure
    public override string CacheFilename {
        get {
            string entReplace = Regex.Replace(EntityPart, EntityNameMatch, OgreNameReplace);
            // if the replacement didn't happen entReplace == entName
            string newName = base.CombineEntityName(HeaderPart, HostPart, entReplace);
            // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "ConvertTextureEntityNameToCacheFilename: " + entName.ToString() + " => " + newName);

            // if windows, fix all the entity separators so they become directory separators
            if (Path.DirectorySeparatorChar != '/') {
                newName.Replace('/', Path.DirectorySeparatorChar);
            }
            return newName;
        }
    }

    // This class has a little more specific knowlege of how the complete entity name
    // can be converted into its parts or override the default routines.
    private const string HostPartMatch = @"^(.*)/........-....-....-....-.*$";
    private const string HostPartReplace = @"$1";
    // the host part is embedded in the name somewhere. See if we can find it.
    public override string ExtractHostPartFromEntityName() {
        return Regex.Replace(this.Name, HostPartMatch, HostPartReplace);
    }

    private const string UUIDMatch = @"^.*/(........-....-....-....-............).*$";
    private const string UUIDReplace = @"$1";
    // LL entities have a UUID in them of the real name of the entity
    public override string ExtractEntityFromEntityName() {
        return Regex.Replace(this.Name, UUIDMatch, UUIDReplace);
    }

}
}
