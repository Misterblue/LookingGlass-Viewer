#! /bin/bash
OGREDIR=/cygdrive/c/dev/ogre-1.6.4-bld
LGLIBDIR=/cygdrive/c/dev/lookingglass/trunk/lib
LGBINDIR=/cygdrive/c/dev/lookingglass/trunk/bin


echo "Copying lib files: $OGREDIR/lib => $LGLIBDIR/Ogre/lib"
cd "${OGREDIR}/lib"
cp OgreGUIRenderer.dll          "${LGLIBDIR}/Ogre/lib"
cp OgreGUIRenderer.lib          "${LGLIBDIR}/Ogre/lib"
cp OgreMain.dll                 "${LGLIBDIR}/Ogre/lib"
cp OgreMain.lib                 "${LGLIBDIR}/Ogre/lib"
cp OgreMain_d.lib               "${LGLIBDIR}/Ogre/lib"
cp Plugin_BSPSceneManager.dll   "${LGLIBDIR}/Ogre/lib"
cp Plugin_BSPSceneManager.lib   "${LGLIBDIR}/Ogre/lib"
cp Plugin_CgProgramManager.dll  "${LGLIBDIR}/Ogre/lib"
cp Plugin_CgProgramManager.lib  "${LGLIBDIR}/Ogre/lib"
cp Plugin_OctreeSceneManager.dll "${LGLIBDIR}/Ogre/lib"
cp Plugin_OctreeSceneManager.lib "${LGLIBDIR}/Ogre/lib"
cp Plugin_OctreeZone.dll        "${LGLIBDIR}/Ogre/lib"
cp Plugin_OctreeZone.lib        "${LGLIBDIR}/Ogre/lib"
cp Plugin_PCZSceneManager.dll   "${LGLIBDIR}/Ogre/lib"
cp Plugin_PCZSceneManager.lib   "${LGLIBDIR}/Ogre/lib"
cp Plugin_ParticleFX.dll        "${LGLIBDIR}/Ogre/lib"
cp Plugin_ParticleFX.lib        "${LGLIBDIR}/Ogre/lib"
cp ReferenceAppLayer.dll        "${LGLIBDIR}/Ogre/lib"
cp ReferenceAppLayer.lib        "${LGLIBDIR}/Ogre/lib"
cp RenderSystem_Direct3D9.dll   "${LGLIBDIR}/Ogre/lib"
cp RenderSystem_Direct3D9.lib   "${LGLIBDIR}/Ogre/lib"
cp RenderSystem_GL.dll          "${LGLIBDIR}/Ogre/lib"
cp RenderSystem_GL.lib          "${LGLIBDIR}/Ogre/lib"
cp ../Dependencies/lib/Release/OIS.lib "${LGLIBDIR}/Ogre/lib"

echo "Copying bin files: $OGREDIR/lib => $LGLIBDIR/Ogre/bin/Release"
cd "${OGREDIR}/lib"
cp OgreGUIRenderer.dll          "${LGLIBDIR}/Ogre/bin/Release"
cp OgreMain.dll                 "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_BSPSceneManager.dll   "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_CgProgramManager.dll  "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_OctreeSceneManager.dll "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_OctreeZone.dll        "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_PCZSceneManager.dll   "${LGLIBDIR}/Ogre/bin/Release"
cp Plugin_ParticleFX.dll        "${LGLIBDIR}/Ogre/bin/Release"
cp RenderSystem_Direct3D9.dll   "${LGLIBDIR}/Ogre/bin/Release"
cp RenderSystem_GL.dll          "${LGLIBDIR}/Ogre/bin/Release"


echo "rsyncing include: $OGREDIR/OgreMain/include => $LGLIBDIR/Ogre/include"
rsync -aqv "${OGREDIR}/OgreMain/include" "${LGLIBDIR}/Ogre"

echo "Bin files to bin: $OGREDIR/lib -> $LGBINDIR"
cd "${OGREDIR}/lib"
cp OgreGUIRenderer.dll          "${LGBINDIR}"
cp OgreMain.dll                 "${LGBINDIR}"
cp Plugin_BSPSceneManager.dll   "${LGBINDIR}"
cp Plugin_CgProgramManager.dll  "${LGBINDIR}"
cp Plugin_OctreeSceneManager.dll "${LGBINDIR}"
cp Plugin_OctreeZone.dll        "${LGBINDIR}"
cp Plugin_PCZSceneManager.dll   "${LGBINDIR}"
cp Plugin_ParticleFX.dll        "${LGBINDIR}"
cp RenderSystem_Direct3D9.dll   "${LGBINDIR}"
cp RenderSystem_GL.dll          "${LGBINDIR}"
cp ../Sample/Common/bin/Release/OIS.dll "${LGBINDIR}"
cp ../Sample/Common/bin/Release/cg.dll "${LGBINDIR}"
