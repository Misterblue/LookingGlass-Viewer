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
CSJ2K.dll
Grids.json
LookingGlass.Comm.LLLP.dll
LookingGlass.Comm.dll
LookingGlass.Framework.dll
LookingGlass.Framework.dll.log4net
LookingGlass.Radegast.dll
LookingGlass.Renderer.Ogre.dll
LookingGlass.Renderer.dll
LookingGlass.Rest.dll
LookingGlass.View.dll
LookingGlass.World.LL.dll
LookingGlass.World.dll
LookingGlass.exe
LookingGlass.json
LookingGlassOgre.dll
LookingGlassOgre.lib
OgreMain.dll
OIS.dll
OgreGUIRenderer.dll
Plugin_BSPSceneManager.dll
Plugin_CgProgramManager.dll
Plugin_OctreeSceneManager.dll
Plugin_OctreeZone.dll
Plugin_PCZSceneManager.dll
Plugin_ParticleFX.dll
Plugins.cfg
PrimMesher.dll
RadegastLookingGlass.json
RenderSystem_Direct3D9.dll
RenderSystem_GL.dll
SkyX.dll
cg.dll
msvcr90.dll
resources.cfg
EOLIST

mkdir -p "$TEMPDIR"

rsync -r --exclude .svn --exclude openmetaverse_data ../LookingGlassResources "$TEMPDIR"
rsync -r --exclude .svn ../UI "$TEMPDIR"
mkdir "$TEMPDIR/bin"
cat "$FILELIST" | while read filename ; do
    cp "../bin/$filename" "$TEMPDIR/bin"
done
find "$TEMPDIR" -type f | xargs chmod 764
find "$TEMPDIR" -type d | xargs chmod 755
find "$TEMPDIR" -type f -name \*.dll | xargs chmod +x
find "$TEMPDIR" -type f -name \*.exe | xargs chmod +x
find "$TEMPDIR" -type f -name \*.sh | xargs chmod +x

cd "$TEMPDIR"
zip -qr "../RadegastLookingGlass-$DATE.zip" *
find . -type f | bzip2 -c - > "../RadegastLookingGlass-$DATE.bz2"

cd ..
rm -rf "$TEMPDIR"
rm -f "$FILELIST"

