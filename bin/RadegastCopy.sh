#! /bin/bash
SRCDIR=/cygdrive/c/dev/lookingglass/trunk/bin
DSTDIR=/cygdrive/c/dev/radegast/bin
#DSTDIR="/cygdrive/c/Program Files/Radegast"
RADSRCDIR=/cygdrive/c/dev/lookingglass

copyLookingGlassSrc=no
copyLookingGlassBin=yes
copyOgre=yes
copyOpenMetaverse=no

if [[ "$copyLookingGlassSrc" == "yes" ]] ; then
    echo "Copying LookingGlass sources to special dir that's linked to Radegast"
    rsync -r --exclude .svn "$SRCDIR/../src"  "$RADSRCDIR/trunk"
fi

if [[ "$copyLookingGlassBin" == "yes" ]] ; then
    echo "Copying LookingGlass binaries into Radegast bin"
    echo "  $SRCDIR -> $DSTDIR"
    cp "$SRCDIR/LookingGlass.exe"             "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Comm.dll"        "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Comm.LLLP.dll"   "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Framework.dll"   "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Framework.dll.log4net"   "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Radegast.dll"    "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Renderer.dll"    "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Renderer.Ogre.dll" "$DSTDIR"
    cp "$SRCDIR/LookingGlass.Rest.dll"        "$DSTDIR"
    cp "$SRCDIR/LookingGlass.View.dll"        "$DSTDIR"
    cp "$SRCDIR/LookingGlass.World.OS.dll"    "$DSTDIR"
    cp "$SRCDIR/LookingGlass.World.LL.dll"    "$DSTDIR"
    cp "$SRCDIR/LookingGlass.World.Services.dll" "$DSTDIR"
    cp "$SRCDIR/LookingGlass.World.dll"       "$DSTDIR"
    cp "$SRCDIR/LookingGlassOgre.dll"         "$DSTDIR"
    cp "$SRCDIR/PrimMesher.dll"               "$DSTDIR"
    cp "$SRCDIR/RadegastLookingGlass.json"    "$DSTDIR"
    cp "$SRCDIR/RadegastModules.json"         "$DSTDIR"
    cp "$SRCDIR/msvcr90.dll"                  "$DSTDIR"
    cp "$SRCDIR/SkyX.dll"                     "$DSTDIR"
    cp "$SRCDIR/openjpeg-dotnet.dll"          "$DSTDIR"
    cp "$SRCDIR/Grids.json"                   "$DSTDIR"
    rsync -ra --exclude .svn "$SRCDIR/LookingGlassResources" "$DSTDIR"
    rsync -ra --exclude .svn "$SRCDIR/LookingGlassUI"        "$DSTDIR"
fi

if [[ "$copyOgre" == "yes" ]] ; then
    echo "Copying Ogre into Radegast bin"
    echo "  $SRCDIR -> $DSTDIR"
    cp "$SRCDIR/OIS.dll"                      "$DSTDIR"
    cp "$SRCDIR/OgreGUIRenderer.dll"          "$DSTDIR"
    cp "$SRCDIR/OgreMain.dll"                 "$DSTDIR"
    cp "$SRCDIR/Plugin_BSPSceneManager.dll"   "$DSTDIR"
    cp "$SRCDIR/Plugin_CgProgramManager.dll"  "$DSTDIR"
    cp "$SRCDIR/Plugin_OctreeSceneManager.dll" "$DSTDIR"
    cp "$SRCDIR/Plugin_OctreeZone.dll"        "$DSTDIR"
    cp "$SRCDIR/Plugin_PCZSceneManager.dll"   "$DSTDIR"
    cp "$SRCDIR/Plugin_ParticleFX.dll"        "$DSTDIR"
    cp "$SRCDIR/cg.dll"                       "$DSTDIR"
    cp "$SRCDIR/RenderSystem_Direct3D9.dll"   "$DSTDIR"
    cp "$SRCDIR/RenderSystem_GL.dll"          "$DSTDIR"
    cp "$SRCDIR/Plugins.cfg"                  "$DSTDIR"
    cp "$SRCDIR/resources.cfg"                "$DSTDIR"
    cp "$SRCDIR/FreeImage.dll"                "$DSTDIR"
fi

if [[ "$copyOpenMetaverse" == "yes" ]] ; then
    echo "Copying OpenMetaverase into Radegast bin"
    echo "  $SRCDIR -> $DSTDIR"
    cp "$SRCDIR/CSJ2K.dll"                    "$DSTDIR"
    cp "$SRCDIR/OpenMetaverse.Http.dll"       "$DSTDIR"
    cp "$SRCDIR/OpenMetaverse.StructuredData.dll" "$DSTDIR"
    cp "$SRCDIR/OpenMetaverse.Utilities.dll"  "$DSTDIR"
    cp "$SRCDIR/OpenMetaverse.dll"            "$DSTDIR"
    cp "$SRCDIR/OpenMetaverse.dll.config"     "$DSTDIR"
    cp "$SRCDIR/OpenMetaverseTypes.dll"       "$DSTDIR"
    cp "$SRCDIR/openjpeg-dotnet.dll"          "$DSTDIR"
    cp "$SRCDIR/openjpeg-dotnet-x86_64.dll"   "$DSTDIR"
fi


