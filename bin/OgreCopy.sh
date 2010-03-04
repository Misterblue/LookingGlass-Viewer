#! /bin/bash
OGREDIR=/cygdrive/c/dev/ogre-1.7.0-bld/Builds/Cthugha/
OGRESRCDIR=/cygdrive/c/dev/ogre-1.7.0-bld/Sources/Cthugha/
DEPDIR=/cygdrive/c/dev/ogre-1.7.0-bld/Sources/Dependencies/

LGLIBDIR=/cygdrive/c/dev/lookingglass/trunk/lib
LGBINDIR=/cygdrive/c/dev/lookingglass/trunk/bin

# Set next to 'Debug' or 'Release'
REL=Release


echo "Copying lib files: $OGREDIR/lib => $LGLIBDIR/Ogre/lib"
cd "${OGREDIR}/lib/$REL"
cp OgreMain.lib                 "${LGLIBDIR}/Ogre/lib"
cp Plugin_BSPSceneManager.lib   "${LGLIBDIR}/Ogre/lib"
cp Plugin_CgProgramManager.lib  "${LGLIBDIR}/Ogre/lib"
cp Plugin_OctreeSceneManager.lib "${LGLIBDIR}/Ogre/lib"
cp Plugin_OctreeZone.lib        "${LGLIBDIR}/Ogre/lib"
cp Plugin_PCZSceneManager.lib   "${LGLIBDIR}/Ogre/lib"
cp Plugin_ParticleFX.lib        "${LGLIBDIR}/Ogre/lib"
cp RenderSystem_Direct3D9.lib   "${LGLIBDIR}/Ogre/lib"
cp RenderSystem_GL.lib          "${LGLIBDIR}/Ogre/lib"
cd "${DEPDIR}/lib/$REL"
cp OIS.lib                      "${LGLIBDIR}/Ogre/lib"
cp cg.lib                       "${LGLIBDIR}/Ogre/lib"
cp FreeImage.lib                "${LGLIBDIR}/Ogre/lib"
cp freetype2311.lib             "${LGLIBDIR}/Ogre/lib"


echo "Copying bin files: $OGREDIR/bin => $LGLIBDIR/Ogre/bin/Release"
cd "${OGREDIR}/bin/$REL"
cp OgreMain.dll                 "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_BSPSceneManager.dll   "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_CgProgramManager.dll  "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_OctreeSceneManager.dll "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_OctreeZone.dll        "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_PCZSceneManager.dll   "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_ParticleFX.dll        "${LGLIBDIR}/Ogre/bin/Release"
cp RenderSystem_Direct3D9.dll   "${LGLIBDIR}/Ogre/bin/Release"
cp RenderSystem_GL.dll          "${LGLIBDIR}/Ogre/bin/Release"
cd "${DEPDIR}/bin/$REL"
cp OIS.dll                      "${LGLIBDIR}/Ogre/bin/Release"
cp cg.dll                       "${LGLIBDIR}/Ogre/bin/Release"


echo "rsyncing include: $OGRESRCDIR/OgreMain/include => $LGLIBDIR/Ogre/include"
rsync -av "${OGRESRCDIR}/OgreMain/include" "${LGLIBDIR}/Ogre"
cp "${OGREDIR}/include/OgreBuildSettings.h" "${LGLIBDIR}/Ogre/include"

echo "Bin files to bin: $OGREDIR/bin -> $LGBINDIR"
cd "${OGREDIR}/bin/$REL"
cp OgreMain.dll                 "${LGBINDIR}"
cp Plugin_BSPSceneManager.dll   "${LGBINDIR}"
cp Plugin_CgProgramManager.dll  "${LGBINDIR}"
cp Plugin_OctreeSceneManager.dll "${LGBINDIR}"
cp Plugin_OctreeZone.dll        "${LGBINDIR}"
cp Plugin_PCZSceneManager.dll   "${LGBINDIR}"
cp Plugin_ParticleFX.dll        "${LGBINDIR}"
cp RenderSystem_Direct3D9.dll   "${LGBINDIR}"
cp RenderSystem_GL.dll          "${LGBINDIR}"
cd "${DEPDIR}/bin/$REL"
cp OIS.dll                      "${LGBINDIR}"
cp cg.dll                       "${LGBINDIR}"
