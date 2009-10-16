using System;
using System.Collections.Generic;
using System.Text;
using OpenMetaverse;

namespace LookingGlass.World.LL {

    public abstract class LLEntityBase : EntityBase {

        protected Primitive m_prim;
        public Primitive Prim { get { return m_prim; } set { m_prim = value; } }

        public const ulong NOREGION = 0xffffffff;
        protected ulong m_regionHandle;
        public ulong RegionHandle { get { return m_regionHandle; } set { m_regionHandle = value; } }

        protected Simulator m_simulator;
        public Simulator Sim { get { return m_simulator; } set { m_simulator = value; } }

        public const uint NOLOCALID = 0xffffffff;
        protected uint m_localID;
        // an LL localID is a per sim unique handle for the item
        public uint LocalID { get { return m_localID; } set { m_localID = value; m_LGID = m_localID;  } }

        public LLEntityBase(RegionContextBase rcontext, AssetContextBase acontext) 
                    : base(rcontext, acontext) {
            this.Prim = null;
            this.Sim = null;
            this.RegionHandle = LLEntityBase.NOREGION;
            this.LocalID = LLEntityBase.NOLOCALID;
        }

    }
}
