using System;
using System.Runtime.InteropServices;
using System.Text;

namespace LookingGlass.Renderer.Ogr {

static class Ogr {

    public const int IOTypeKeyPressed = 1;      // p1=keyboard keycode
    public const int IOTypeKeyReleased = 2;     // p1=keyboard keycode
    public const int IOTypeMouseMove = 3;       // p2=x move since last, p3=y move since last
    public const int IOTypeMouseButtonDown = 4; // p1=button number
    public const int IOTypeMouseButtonUp = 5;   // p1=button number

    public const int IOMouseButtonLeft = 0;     // mouse button codes passed with mouse events
    public const int IOMouseButtonRight = 1;    // corresponds to OIS::MouseButtonID
    public const int IOMouseButtonMiddle = 2;
    public const int IOMouseButton3 = 3;
    public const int IOMouseButton4 = 4;
    public const int IOMouseButton5 = 5;
    public const int IOMouseButton6 = 6;
    public const int IOMouseButton7 = 7;

    public const int ResourceTypeUnknown = 0;   // unknown
    public const int ResourceTypeMesh = 1;      // mesh resource
    public const int ResourceTypeTexture = 2;   // texture
    public const int ResourceTypeMaterial = 3;  // material
    public const int ResourceTypeTransparentTexture = 4;  // texture with some transparancy

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

    // =============================================================================
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void InitializeOgre();

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void ShutdownOgre();

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool RenderingThread();

    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern bool RenderOneFrame();

    // =============================================================================
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void UpdateCamera(
        float px, float py, float pz, 
        float dw, float dx, float dy, float dz,
        float nearClip, float farClip, float aspect);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void RefreshResource(int type, [MarshalAs(UnmanagedType.LPStr)]string resourceName);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void CreateMeshResource([MarshalAs(UnmanagedType.LPStr)]string resourceName,
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
    public static extern void CreateMaterialResource6(
                            [MarshalAs(UnmanagedType.LPStr)]string matName1,
                            [MarshalAs(UnmanagedType.LPStr)]string matName2,
                            [MarshalAs(UnmanagedType.LPStr)]string matName3,
                            [MarshalAs(UnmanagedType.LPStr)]string matName4,
                            [MarshalAs(UnmanagedType.LPStr)]string matName5,
                            [MarshalAs(UnmanagedType.LPStr)]string matName6,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName1,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName2,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName3,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName4,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName5,
                            [MarshalAs(UnmanagedType.LPStr)]string textureName6,
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
    public static extern void AddOceanToRegion(System.IntPtr sceneMgr, System.IntPtr sceneNodew,
            float width, float length, float height, [MarshalAs(UnmanagedType.LPStr)]string waterName);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void GenTerrainMesh(System.IntPtr sceneMgr, System.IntPtr sceneNode,
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
    public static extern System.IntPtr CreateChildSceneNode(System.IntPtr node);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void AddEntity(System.IntPtr sceneMgr, System.IntPtr sceneNode,
                [MarshalAs(UnmanagedType.LPStr)]string entName,
                [MarshalAs(UnmanagedType.LPStr)]string meshName);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SceneNodeScale( System.IntPtr sceneNode,
                    float sX, float sY, float sZ);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SceneNodePosition( System.IntPtr sceneNode,
                    float pX, float pY, float pZ);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SceneNodePitch(System.IntPtr sceneNode, float pitch, int ts);
    [DllImport("LookingGlassOgre", CallingConvention = CallingConvention.Cdecl)]
    public static extern void SceneNodeYaw(System.IntPtr sceneNode, float yaw, int ts);

    // ======================================================================
}
}
