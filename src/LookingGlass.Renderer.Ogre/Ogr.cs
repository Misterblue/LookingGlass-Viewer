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
using System.Runtime.InteropServices;
using System.Text;

namespace LookingGlass.Renderer.Ogr {

static class Ogr {

    public const int ResourceTypeUnknown = 0;   // unknown
    public const int ResourceTypeMesh = 1;      // mesh resource
    public const int ResourceTypeTexture = 2;   // texture
    public const int ResourceTypeMaterial = 3;  // material
    public const int ResourceTypeTransparentTexture = 4;  // texture with some transparancy

    // offsets into statistics block
    // Numbers coorespond to numbers in LookingGlassOgre/LookingGlassOgre.h
    // culling and visibility
    public const int StatVisibleToVisible = 2;
    public const int StatInvisibleToVisible = 3;
    public const int StatVisibleToInvisible = 4;
    public const int StatInvisibleToInvisible = 5;
    public const int StatCullMeshesLoaded = 6;
    public const int StatCullTexturesLoaded = 7;
    public const int StatCullMeshesUnloaded = 13;
    public const int StatCullTexturesUnloaded = 14;
    public const int StatCullMeshesQueuedToLoad = 15;
    // between frame work
    public const int StatBetweenFrameWorkItems = 1;
    public const int StatBetweenFrameRefreshResource = 8;
    public const int StatBetweenFrameCreateMaterialResource = 9;
    public const int StatBetweenFrameCreateMeshResource = 10;
    public const int StatBetweenFrameCreateMeshSceneNode = 11;
    public const int StatBetweenFrameAddLoadedMesh = 29;
    public const int StatBetweenFrameUpdateSceneNode = 12;
    public const int StatBetweenFrameTotalProcessed = 16;
    public const int StatBetweenFrameUnknownProcess = 17;
    public const int StatBetweenFrameDiscardedDups = 21;
    // general process work thread
    public const int StatProcessAnyTimeWorkItems = 23;
    public const int StatProcessAnyTimeTotalProcessed = 24;
    public const int StatProcessAnyTimeDiscardedDups = 22;
    // material processing queues
    public const int StatMaterialUpdatesRemaining = 0;
    // mesh processing queues
    public const int StatMeshTrackerLoadQueued = 25;
    public const int StatMeshTrackerUnloadQueued = 26;
    public const int StatMeshTrackerSerializedQueued = 27;
    public const int StatMeshTrackerTotalQueued = 28;
    // misc info
    public const int StatTotalFrames = 18;
    public const int StatFramesPerSec = 19;
    public const int StatLastFrameMs = 20;

    // the number of stat values (oversized for a fudge factor)
    public const int StatSize = 40;

    // =============================================================================
    // Call from the Ogre C++ code back into  the managed code.
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void UserIOCallback( int type, int param1, float param2, float param3 );

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void DebugLogCallback([MarshalAs(UnmanagedType.LPStr)]string msg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.LPStr)]
    public delegate string FetchParameterCallback([MarshalAs(UnmanagedType.LPStr)]string param);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public delegate bool CheckKeepRunningCallback();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void RequestResourceCallback( 
            [MarshalAs(UnmanagedType.LPStr)]string contextEntName, 
            [MarshalAs(UnmanagedType.LPStr)]string entName, int type);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate bool BetweenFramesCallback();

    // =============================================================================
    // Calls  to set the callback routine location.
    // Fetch a configuration parameter
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetFetchParameterCallback(FetchParameterCallback callback);
    
    // Log a debug message
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetDebugLogCallback(DebugLogCallback callback);

    // check if we should keep running (Obsolete: BetweenFrames returns the flag)
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetCheckKeepRunningCallback(CheckKeepRunningCallback callback);

    // the user did something with the keyboard/mouse
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetUserIOCallback(UserIOCallback callback);

    // ask that a resource (mesh, material, ...) be created by the C# code
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetRequestResourceCallback(RequestResourceCallback callback);

    // we're between frames. Is there C# stuff to do?
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetBetweenFramesCallback(BetweenFramesCallback callback);

    // A pinned statistics block that is updated by Ogre and displayed by LG
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SetStatsBlock(IntPtr block);

    // =============================================================================
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InitializeOgre();

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ShutdownOgre();

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool RenderingThread();

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool RenderOneFrame(bool pump, int len);

    // =============================================================================
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateCamera(
        double px, double py, double pz, 
        float dw, float dx, float dy, float dz,
        float nearClip, float farClip, float aspect);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateCameraBF(
        double px, double py, double pz, 
        float dw, float dx, float dy, float dz,
        float nearClip, float farClip, float aspect);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool AttachCamera([MarshalAs(UnmanagedType.LPStr)]string parentNodeName,
       float offsetX, float offsetY, float offsetZ, float ow, float ox, float oy, float oz);

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RefreshResourceBF(float pri, int type, [MarshalAs(UnmanagedType.LPStr)]string resourceName);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CreateMeshResourceBF(float pri, 
                            [MarshalAs(UnmanagedType.LPStr)]string resourceName,
                            [MarshalAs(UnmanagedType.LPStr)]string contextSceneNode,
                            [MarshalAs(UnmanagedType.LPArray)] int[] faceCounts, 
                            [MarshalAs(UnmanagedType.LPArray)] float[] faceVertices);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CreateMaterialResource([MarshalAs(UnmanagedType.LPStr)]string resourceName,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName,
                            float colorR, float colorG, float colorB, float colorA,
                            float glow, bool fullBright, int shiny, int bump);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CreateMaterialResource2([MarshalAs(UnmanagedType.LPStr)]string resourceName,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName,
                            [MarshalAs(UnmanagedType.LPArray)] float[] parms);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    // queue and do between frame version of above
    public static extern void CreateMaterialResource2BF(float pri,
                            [MarshalAs(UnmanagedType.LPStr)]string resourceName,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName,
                            [MarshalAs(UnmanagedType.LPArray)] float[] parms);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CreateMaterialResource7BF(
                            float prio,
                            [MarshalAs(UnmanagedType.LPStr)]string uniq,
                            [MarshalAs(UnmanagedType.LPStr)]string matName1,
                            [MarshalAs(UnmanagedType.LPStr)]string matName2,
                            [MarshalAs(UnmanagedType.LPStr)]string matName3,
                            [MarshalAs(UnmanagedType.LPStr)]string matName4,
                            [MarshalAs(UnmanagedType.LPStr)]string matName5,
                            [MarshalAs(UnmanagedType.LPStr)]string matName6,
                            [MarshalAs(UnmanagedType.LPStr)]string matName7,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName1,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName2,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName3,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName4,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName5,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName6,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName7,
                            [MarshalAs(UnmanagedType.LPArray)] float[] parms);
    // pass an array of parameters to create the material
    public enum CreateMaterialParam {
        colorR = 0, colorG, colorB, colorA,
        glow, fullBright, shiny, bump,
        scrollU, scrollV, scaleU, scaleV, rotate,
        mappingType, mediaFlags,
        textureHasTransparent,
        maxParam
    };
    // unload all resources associated with our resource groups
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void DiagnosticAction(int flag);

    // ======================================================================
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddRegionBF(float prio,
            [MarshalAs(UnmanagedType.LPStr)]string regionNodeName,
            double globalX, double globalY, double globalZ,
            float sizeX, float sizeY,
            float waterHeight);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateTerrainBF(float prio, 
            [MarshalAs(UnmanagedType.LPStr)]string regionNodeName,
            int width, int length, [MarshalAs(UnmanagedType.LPArray)] float[] heightmap);
    // ======================================================================
    // SceneNode wrapper
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern System.IntPtr GetSceneMgr();
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern System.IntPtr RootNode(System.IntPtr sceneMgr);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern System.IntPtr CreateSceneNode(
                System.IntPtr sceneMgr,
                [MarshalAs(UnmanagedType.LPStr)]string nodeName,
                System.IntPtr parentNode,
                bool scale, bool orientation,
                float px, float py, float pz, float sx, float sy, float sz,
                float rw, float rx, float ry, float rz
        );
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateSceneNodeBF(float pri,
                [MarshalAs(UnmanagedType.LPStr)]string entityName,
                bool updatePosition, float px, float py, float pz,
                bool updateScale, float sx, float sy, float sz,
                bool updateRotation, float rw, float rx, float ry, float rz
        );
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool CreateMeshSceneNodeBF(float pri,
                System.IntPtr sceneMgr,
                [MarshalAs(UnmanagedType.LPStr)]string sceneNodeName,
                [MarshalAs(UnmanagedType.LPStr)]string parentNodeName,
                [MarshalAs(UnmanagedType.LPStr)]string entityName,
                [MarshalAs(UnmanagedType.LPStr)]string meshName,
                bool scale, bool orientation,
                float px, float py, float pz, float sx, float sy, float sz,
                float rw, float rx, float ry, float rz
        );

    // ======================================================================
}
}
