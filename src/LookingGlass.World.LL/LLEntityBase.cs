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

        public override OMV.Vector3 RelativePosition {
            get {
                if (Prim != null) {
                    return Prim.Position;
                }
                else {
                    return base.RelativePosition;
                }
            }
            set {
                if (Prim != null) {
                    Prim.Position = value;
                }
                else {
                    base.RelativePosition = value;
                }
            }
        }

        public override OMV.Vector3d GlobalPosition {
            get {
                if (Prim != null) {
                    return new OMV.Vector3d(
                        m_regionContext.WorldBase.X + (double)Prim.Position.X,
                        m_regionContext.WorldBase.Y + (double)Prim.Position.Y,
                        m_regionContext.WorldBase.Z + (double)Prim.Position.Z);
                }
                else {
                    return base.GlobalPosition;
                }
            }
            set {
                if (Prim != null) {
                    Prim.Position = new OMV.Vector3(
                        (int)(value.X - m_regionContext.WorldBase.X),
                        (int)(value.Y - m_regionContext.WorldBase.Y),
                        (int)(value.Z - m_regionContext.WorldBase.Z)
                    );
                }
                else {
                    base.GlobalPosition = value;
                }
            }
        }


    }
}
