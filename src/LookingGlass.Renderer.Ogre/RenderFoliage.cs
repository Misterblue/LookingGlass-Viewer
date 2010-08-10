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

using LookingGlass.Framework;
using LookingGlass.Framework.Logging;
using LookingGlass.Renderer;
using LookingGlass.World;

namespace LookingGlass.Renderer.Ogr {
class RenderFoliage : IRenderEntity {
    RendererOgre m_renderer;
    IEntity m_ent;

    public RenderFoliage(RendererOgre contextRenderer, IEntity contextEntity) {
        m_renderer = contextRenderer;
        m_ent = contextEntity;
    }

    public bool Create(ref RenderableInfo ri, ref bool m_hasMesh, float priority, int retries) {
        m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderFoliage: Create");
        return true;
    }
    public void Update(UpdateCodes what) {
        m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderFoliage: Update");
        float priority = m_renderer.CalculateInterestOrder(m_ent);
        bool fullUpdate = false;    // true if a full update was done on this entity
        if ((what & UpdateCodes.New) != 0) {
            // new entity. Gets the full treatment
            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderFoliage.Update: New entity: {0}", m_ent.Name.Name);
            m_renderer.DoRenderQueued(m_ent);
            fullUpdate = true;
        }
        if (!fullUpdate && (what & (UpdateCodes.Scale | UpdateCodes.Position | UpdateCodes.Rotation)) != 0) {
            // world position has changed. Tell Ogre they have changed
            string entitySceneNodeName = EntityNameOgre.ConvertToOgreSceneNodeName(m_ent.Name);
            m_renderer.m_log.Log(LogLevel.DRENDERDETAIL, "RenderFoliage.Update: Updating position/rotation for {0}", entitySceneNodeName);
            Ogr.UpdateSceneNodeBF(priority, entitySceneNodeName,
                ((what & UpdateCodes.Position) != 0),
                m_ent.RegionPosition.X, m_ent.RegionPosition.Y, m_ent.RegionPosition.Z, 0.25f,
                false, 1f, 1f, 1f, 1f,  // don't pass scale yet
                ((what & UpdateCodes.Rotation) != 0),
                m_ent.Heading.W, m_ent.Heading.X, m_ent.Heading.Y, m_ent.Heading.Z, 0.25f);
        }
        return;
    }
}
}
