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
using System.Text.RegularExpressions;
using LookingGlass.Framework.Logging;
using LookingGlass.World;

namespace LookingGlass.Renderer.Ogr {
// class to hold the Ogre specific name conversion routines
    // On the C# side of the world, entities are anems with a host and an id like
    //   "OSGrid/89723897-2342-4353-983739347495-235345234". In the Ogre world,
    //   the names are made into cache filenames with directories in them. This is
    //   Ogre's convention of names pointing to where you can find the resource's
    //   contents.
    // This class also has some of the conversions to and from the Ogre specific
    //   decorations. Like calcuating the name of the material given a face number.
    // Here we have the conversion from Ogre names to proper entity names and back.
public class EntityNameOgre : EntityName {

    public EntityNameOgre(string name)
        : base(name) {
    }

    public EntityNameOgre(string header, string host, string entity)
        : base(header, host, entity) {
    }

    public EntityNameOgre(IEntity entityContext, string name) 
            : base(entityContext.AssetContext, name) {
    }

    public EntityNameOgre(AssetContextBase acontext, string name) 
            : base(acontext, name) {
    }

    public static EntityNameOgre ConvertOgreResourceToEntityName(string ogreResource) {
        return new EntityNameOgre(EntityNameOgre.ConvertOgreResourceToEntityNameX(ogreResource));
    }

    // return entity name as an OgreResourceName
    public string OgreResourceName {
        get {
            return ConvertToOgreNameX(this, null);
        }
    }

    // ================================================
    // After here are conversion routines that are sometimes needed to link between
    // the Ogre resource names and regular entity names. Use at your peril.

    // private const string EntityNameMatch = @"^(...)(...)(..)-(.)(.*)$";
    // private const string OgreNameReplace = @"$1/$2/$3$4/$1$2$3-$4$5";
    private const string EntityNameMatch = @"^(.)(.*)$";
    private const string OgreNameReplace = @"$1/$1$2";

    // Ogre presumes that entity name will be the filename in the cache. Make the
    // entity name on the Ogre side have the cache filename format
    // Used for meshes, materials
    public static string ConvertToOgreNameX(EntityName entName, string extension) {
        string entReplace = Regex.Replace(entName.EntityPart, EntityNameMatch, OgreNameReplace);
        // if the replacement didn't happen entReplace == entName
        string newName = entName.CombineEntityName(entName.HeaderPart, entName.HostPart, entReplace);
        if (extension != null) newName += extension;
        // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "ConvertToOgreName: " + entName.ToString() + " => " + newName);
        return newName;
    }

    public static EntityName ConvertToOgreNameY(EntityName entName, string extension) {
        string entReplace = Regex.Replace(entName.EntityPart, EntityNameMatch, OgreNameReplace);
        // if the replacement didn't happen entReplace == entName
        if (extension != null) entReplace += extension;
        EntityName newName = new EntityNameOgre(entName.HeaderPart, entName.HostPart, entReplace);
        return newName;
    }

    // Each entity has a scene node and this is the conversion from the name of the
    // entity to the name of the scene node that contains it.
    public static EntityName ConvertToOgreMaterialName(EntityName entName) {
        return ConvertToOgreNameY(entName, ".material");
    }

    // Each entity has a scene node and this is the conversion from the name of the
    // entity to the name of the scene node that contains it.
    public static EntityName ConvertToOgreMeshName(EntityName entName) {
        return ConvertToOgreNameY(entName, ".mesh");
    }

    // Each entity has a scene node and this is the conversion from the name of the
    // entity to the name of the scene node that contains it.
    public static string ConvertToOgreSceneNodeName(EntityName entName) {
        return "SceneNode/" + entName.Name;
    }

    // Each entity has a scene node and this is the conversion from the name of the
    // entity to the name of the scene node that contains it.
    public static string ConvertToOgreEntityName(EntityName entName) {
        return "Entity/" + entName.Name;
    }

    // private const string OgreNameMatch = @"^(.*)/.../.../.../([^/]*)$";
    private const string OgreNameMatch = @"^(.*)/[0-9a-f]/([^/]*)$";
    private const string EntityNameReplace = @"$1/$2";

    // Ogre resources have been decorated with extensions and dash numbers which we remove
    // and then undo  the ConvertToOgreName to get back the origional entity namne
    public static string ConvertOgreResourceToEntityNameX(string resName) {
        int pos;
        string oldName = resName;
        if (oldName.StartsWith("SceneNode/")) oldName = oldName.Substring(10);
        if (oldName.StartsWith("Entity/")) oldName = oldName.Substring(7);
        // Remove the ".material"
        if (oldName.EndsWith(".material")) {
            oldName = oldName.Substring(0, oldName.Length - 9);
            // if there is a face number, remove it (ie, "...2478.mesh-5.material")
            if ((pos = oldName.LastIndexOf('-')) > oldName.Length - 4) oldName = oldName.Substring(0, pos);
        }
        // remote any ".mesh" if present
        if (oldName.EndsWith(".mesh")) oldName = oldName.Substring(0, oldName.Length - 5);
        // remove media type if present
        if (oldName.EndsWith(".jp2")) oldName = oldName.Substring(0, oldName.Length - 4);
        if (oldName.EndsWith(".png")) oldName = oldName.Substring(0, oldName.Length - 4);
        if (oldName.EndsWith(".gif")) oldName = oldName.Substring(0, oldName.Length - 4);
        if (oldName.EndsWith(".bmp")) oldName = oldName.Substring(0, oldName.Length - 4);
        string newName = Regex.Replace(oldName, OgreNameMatch, EntityNameReplace);
        // if the replacement does not happen, newName == oldName
        // LogManager.Log.Log(LogLevel.DRENDERDETAIL, "ConvertOgreResourceToEntity: " + resName.ToString() + " => " + newName);
        return newName;
    }

    // One place to hide the crazy name that the face materials have
    public static string ConvertToOgreMaterialNameX(EntityName entName, int faceNum) {
        return ConvertToOgreNameX(entName, ".mesh-" + faceNum.ToString() + ".material");
    }

    private const string OgreFaceNameMatch =@"^.*-(\d+)[.]material.*$";
    private const string OgreFaceNameReplace = @"$1";
    // In Ogre, we embed the face number of the material in the name of the material.
    // This routine tries to extract same. It returns -1 if the face number was not found;
    public static int GetFaceFromOgreMaterialNameX(string matName) {
        int ret = -1;
        try {
            string faceString = Regex.Replace(matName, OgreFaceNameMatch, OgreFaceNameReplace);
            if (faceString != matName) {
                ret = Int32.Parse(faceString);
            }
            else {
                LogManager.Log.Log(LogLevel.DBADERROR, "GetFaceFromOgreMaterialName: no face found in " + matName);
            }
        }
        catch (Exception e) {
            LogManager.Log.Log(LogLevel.DBADERROR, "GetFaceFromOgreMaterialName: error parsing '" 
                        + matName + ", e=" + e.ToString());
            ret = -1;
        }
        return ret;
    }
}
}
