#! /bin/bash
# Run in the build directory, copies binary files into a temp
# directory then creates zip files of the binary files.
# The zip files can be unzipped into Radegast and thus
# add the LG plugin.

TEMPDIR=tempBuildZip
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
LookingGlass.World.LL.dll
LookingGlass.World.dll
LookingGlassOgre.dll
PrimMesher.dll
RadegastLookingGlass.json
RadegastModules.json
msvcr90.dll
SkyX.dll
openjpeg-dotnet.dll
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
EOLIST

mkdir -p "$TEMPDIR"

rsync -r --exclude .svn --exclude openmetaverse_data ../bin/LookingGlassResources "$TEMPDIR"
rsync -r --exclude .svn ../bin/LookingGlassUI "$TEMPDIR"
cat "$FILELIST" | while read filename ; do
    cp "../bin/$filename" "$TEMPDIR"
done
find "$TEMPDIR" -type f | xargs chmod --quiet 764
find "$TEMPDIR" -type d | xargs chmod --quiet 755
find "$TEMPDIR" -type f -name \*.dll | xargs chmod --quiet +x
find "$TEMPDIR" -type f -name \*.exe | xargs chmod --quiet +x
find "$TEMPDIR" -type f -name \*.sh | xargs chmod --quiet +x

cd "$TEMPDIR"
zip -qr "../RadegastLookingGlass-$DATE.zip" *
#find . -type f | bzip2 -c - > "../RadegastLookingGlass-$DATE.bz2"

cd ..
#rm -rf "$TEMPDIR"
rm -f "$FILELIST"


