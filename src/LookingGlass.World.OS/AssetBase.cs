/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
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
using System.Xml.Serialization;
using System.Reflection;
using OpenMetaverse;

namespace OpenSim.Framework
{
    /// <summary>
    /// Asset class.   All Assets are reference by this class or a class derived from this class
    /// </summary>
    [Serializable]
    public class AssetBase
    {

        /// <summary>
        /// Data of the Asset
        /// </summary>
        private byte[] m_data;

        /// <summary>
        /// Meta Data of the Asset
        /// </summary>
        private AssetMetadata m_metadata;

        // This is needed for .NET serialization!!!
        // Do NOT "Optimize" away!
        public AssetBase()
        {
            m_metadata = new AssetMetadata();
            m_metadata.FullID = UUID.Zero;
            m_metadata.ID = UUID.Zero.ToString();
            m_metadata.Type = (sbyte)AssetType.Unknown;
        }

        public AssetBase(UUID assetID, string name, sbyte assetType)
        {
            if (assetType == (sbyte)AssetType.Unknown)
            {
                System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(true);
            }

            m_metadata = new AssetMetadata();
            m_metadata.FullID = assetID;
            m_metadata.Name = name;
            m_metadata.Type = assetType;
        }

        public AssetBase(string assetID, string name, sbyte assetType)
        {
            if (assetType == (sbyte)AssetType.Unknown)
            {
                System.Diagnostics.StackTrace trace = new System.Diagnostics.StackTrace(true);
            }

            m_metadata = new AssetMetadata();
            m_metadata.ID = assetID;
            m_metadata.Name = name;
            m_metadata.Type = assetType;
        }

        public bool ContainsReferences
        {
            get
            {
                return 
                    IsTextualAsset && (
                    Type != (sbyte)AssetType.Notecard
                    && Type != (sbyte)AssetType.CallingCard
                    && Type != (sbyte)AssetType.LSLText
                    && Type != (sbyte)AssetType.Landmark);
            }
        }

        public bool IsTextualAsset
        {
            get
            {
                return !IsBinaryAsset;
            }

        }

        /// <summary>
        /// Checks if this asset is a binary or text asset
        /// </summary>
        public bool IsBinaryAsset
        {
            get
            {
                return 
                    (Type == (sbyte) AssetType.Animation ||
                     Type == (sbyte)AssetType.Gesture ||
                     Type == (sbyte)AssetType.Simstate ||
                     Type == (sbyte)AssetType.Unknown ||
                     Type == (sbyte)AssetType.Object ||
                     Type == (sbyte)AssetType.Sound ||
                     Type == (sbyte)AssetType.SoundWAV ||
                     Type == (sbyte)AssetType.Texture ||
                     Type == (sbyte)AssetType.TextureTGA ||
                     Type == (sbyte)AssetType.Folder ||
                     Type == (sbyte)AssetType.RootFolder ||
                     Type == (sbyte)AssetType.LostAndFoundFolder ||
                     Type == (sbyte)AssetType.SnapshotFolder ||
                     Type == (sbyte)AssetType.TrashFolder ||
                     Type == (sbyte)AssetType.ImageJPEG ||
                     Type == (sbyte) AssetType.ImageTGA ||
                     Type == (sbyte) AssetType.LSLBytecode);
            }
        }

        public virtual byte[] Data
        {
            get { return m_data; }
            set { m_data = value; }
        }

        /// <summary>
        /// Asset UUID
        /// </summary>
        public UUID FullID
        {
            get { return m_metadata.FullID; }
            set { m_metadata.FullID = value; }
        }
        /// <summary>
        /// Asset MetaData ID (transferring from UUID to string ID)
        /// </summary>
        public string ID
        {
            get { return m_metadata.ID; }
            set { m_metadata.ID = value; }
        }

        public string Name
        {
            get { return m_metadata.Name; }
            set { m_metadata.Name = value; }
        }

        public string Description
        {
            get { return m_metadata.Description; }
            set { m_metadata.Description = value; }
        }

        /// <summary>
        /// (sbyte) AssetType enum
        /// </summary>
        public sbyte Type
        {
            get { return m_metadata.Type; }
            set { m_metadata.Type = value; }
        }

        /// <summary>
        /// Is this a region only asset, or does this exist on the asset server also
        /// </summary>
        public bool Local
        {
            get { return m_metadata.Local; }
            set { m_metadata.Local = value; }
        }

        /// <summary>
        /// Is this asset going to be saved to the asset database?
        /// </summary>
        public bool Temporary
        {
            get { return m_metadata.Temporary; }
            set { m_metadata.Temporary = value; }
        }

        [XmlIgnore]
        public AssetMetadata Metadata
        {
            get { return m_metadata; }
            set { m_metadata = value; }
        }

        public override string ToString()
        {
            return FullID.ToString();
        }
    }

    [Serializable]
    public class AssetMetadata
    {
        private UUID m_fullid;
        // m_id added as a dirty hack to transition from FullID to ID
        private string m_id;
        private string m_name = String.Empty;
        private string m_description = String.Empty;
        private DateTime m_creation_date;
        private sbyte m_type = (sbyte)AssetType.Unknown;
        private string m_content_type;
        private byte[] m_sha1;
        private bool m_local;
        private bool m_temporary;
        //private Dictionary<string, Uri> m_methods = new Dictionary<string, Uri>();
        //private OSDMap m_extra_data;

        public UUID FullID
        {
            get { return m_fullid; }
            set { m_fullid = value; m_id = m_fullid.ToString(); }
        }

        public string ID
        {
            //get { return m_fullid.ToString(); }
            //set { m_fullid = new UUID(value); }
            get
            {
                if (String.IsNullOrEmpty(m_id))
                    m_id = m_fullid.ToString();

                return m_id;
            }
            set
            {
                UUID uuid = UUID.Zero;
                if (UUID.TryParse(value, out uuid))
                {
                    m_fullid = uuid;
                    m_id = m_fullid.ToString();
                }
                else
                    m_id = value;
            }
        }

        public string Name
        {
            get { return m_name; }
            set { m_name = value; }
        }

        public string Description
        {
            get { return m_description; }
            set { m_description = value; }
        }

        public DateTime CreationDate
        {
            get { return m_creation_date; }
            set { m_creation_date = value; }
        }

        public sbyte Type
        {
            get { return m_type; }
            set { m_type = value; }
        }

        public string ContentType
        {
            get { return m_content_type; }
            set { m_content_type = value; }
        }

        public byte[] SHA1
        {
            get { return m_sha1; }
            set { m_sha1 = value; }
        }

        public bool Local
        {
            get { return m_local; }
            set { m_local = value; }
        }

        public bool Temporary
        {
            get { return m_temporary; }
            set { m_temporary = value; }
        }

        //public Dictionary<string, Uri> Methods
        //{
        //    get { return m_methods; }
        //    set { m_methods = value; }
        //}

        //public OSDMap ExtraData
        //{
        //    get { return m_extra_data; }
        //    set { m_extra_data = value; }
        //}
    }
}
