using System;
using System.Collections.Generic;
using System.Text;
using OMV = OpenMetaverse;

namespace LookingGlass.World.LL {

    public abstract class LLEntityBase : EntityBase {

        protected OMV.Primitive m_prim;
        public OMV.Primitive Prim { get { return m_prim; } set { m_prim = value; } }

        public const ulong NOREGION = 0xffffffff;
        protected ulong m_regionHandle;
        public ulong RegionHandle { get { return m_regionHandle; } set { m_regionHandle = value; } }

        protected OMV.Simulator m_simulator;
        public OMV.Simulator Sim { get { return m_simulator; } set { m_simulator = value; } }

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

        public override OMV.Quaternion Heading {
            get {
                if (Prim != null) {
                    return this.m_prim.Rotation;
                }
                else {
                    return base.Heading;
                }
            }
            set {
                if (Prim != null) {
                    this.m_prim.Rotation = value;
                }
                else {
                    base.Heading = value;
                }
            }
        }

        public override OMV.Vector3 LocalPosition {
            get {
                if (Prim != null) {
                    base.LocalPosition = Prim.Position;
                    return Prim.Position;
                }
                else {
                    return base.LocalPosition;
                }
            }
            set {
                base.LocalPosition = value;
                if (Prim != null) {
                    Prim.Position = value;
                }
            }
        }

        public override OMV.Vector3d GlobalPosition {
            get {
                OMV.Vector3 regionRelative = this.RegionPosition;
                if (Prim != null) {
                    return m_regionContext.CalculateGlobalPosition(regionRelative);
                }
                else {
                    return base.GlobalPosition;
                }
            }
        }
    }
}
