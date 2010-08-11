#! /bin/bash
# Run in the build directory, copies binary files into a temp
# directory then runs nsis to create installation file.

TEMPDIR=tempBuildRelease
FILELIST=tempFileList
DATE=$(date +%Y%m%d%H%M)

rm -rf "$TEMPDIR"
rm -f "$FILELIST"

cat > "$FILELIST" << EOLIST
LookingGlass.exe
LookingGlass.Comm.dll
LookingGlass.Comm.LLLP.dll
LookingGlass.Framework.dll
LookingGlass.Framework.dll.log4net
LookingGlass.Radegast.dll
LookingGlass.Renderer.dll
LookingGlass.Renderer.Ogre.dll
LookingGlass.Rest.dll
LookingGlass.View.dll
LookingGlass.World.OS.dll
LookingGlass.World.LL.dll
LookingGlass.World.Services.dll
LookingGlass.World.dll
LookingGlassOgre.dll
PrimMesher.dll
RadegastLookingGlass.json
RadegastModules.json
msvcr90.dll
log4net.dll
XMLRPC.dll
SkyX.dll
LookingGlass.json
Modules.json
Grids.json
OIS.dll
OgreGUIRenderer.dll
OgreMain.dll
Plugin_BSPSceneManager.dll
Plugin_CgProgramManager.dll
Plugin_OctreeSceneManager.dll
Plugin_OctreeZone.dll
Plugin_PCZSceneManager.dll
Plugin_ParticleFX.dll
cg.dll
RenderSystem_Direct3D9.dll
RenderSystem_GL.dll
Plugins.cfg
resources.cfg
FreeImage.dll
CSJ2K.dll
OpenMetaverse.StructuredData.dll
OpenMetaverse.Utilities.dll
OpenMetaverse.dll
OpenMetaverse.dll.config
OpenMetaverseTypes.dll
openjpeg-dotnet.dll
openjpeg-dotnet-x86_64.dll
EOLIST

mkdir -p "$TEMPDIR"

rsync -r --exclude Shadow0[345] --exclude .svn ../bin/LookingGlassResources "$TEMPDIR"
rsync -r --exclude .svn ../bin/LookingGlassUI "$TEMPDIR"
cat "$FILELIST" | while read filename ; do
    cp "../bin/$filename" "$TEMPDIR"
done
cp ../LICENSE.txt "$TEMPDIR"
rsync -r --exclude .svn ../THIRDPARTYLICENSES "$TEMPDIR"

find "$TEMPDIR" -type f | xargs chmod --quiet 764
find "$TEMPDIR" -type d | xargs chmod --quiet 755
find "$TEMPDIR" -type f -name \*.dll | xargs chmod --quiet +x
find "$TEMPDIR" -type f -name \*.exe | xargs chmod --quiet +x
find "$TEMPDIR" -type f -name \*.sh | xargs chmod --quiet +x

cd "$TEMPDIR"
# INSTALLATION BUILD HERE

cd ..
#rm -rf "$TEMPDIR"
rm -f "$FILELIST"


