; Script generated by the HM NIS Edit Script Wizard.

; HM NIS Edit Wizard helper defines
!define PRODUCT_NAME "LookingGlass"
!define PRODUCT_VERSION "0.4.0"
!define PRODUCT_PUBLISHER "Robert Adams"
!define PRODUCT_WEB_SITE "http://lookingglassviewer.org/"
!define PRODUCT_DIR_REGKEY "Software\Microsoft\Windows\CurrentVersion\App Paths\LookingGlass.exe"
!define PRODUCT_UNINST_KEY "Software\Microsoft\Windows\CurrentVersion\Uninstall\${PRODUCT_NAME}"
!define PRODUCT_UNINST_ROOT_KEY "HKLM"

; MUI 1.67 compatible ------
!include "MUI.nsh"

; MUI Settings
!define MUI_ABORTWARNING
!define MUI_ICON "..\..\..\..\Documents and Settings\Robert\My Documents\My Pictures\LookingGlassPictures\Looking-Glass-River-32.ico"
!define MUI_UNICON "${NSISDIR}\Contrib\Graphics\Icons\modern-uninstall.ico"

; Welcome page
!insertmacro MUI_PAGE_WELCOME
; License page
!insertmacro MUI_PAGE_LICENSE "..\LICENCE.txt"
; Directory page
!insertmacro MUI_PAGE_DIRECTORY
; Instfiles page
!insertmacro MUI_PAGE_INSTFILES
; Finish page
!insertmacro MUI_PAGE_FINISH

; Uninstaller pages
!insertmacro MUI_UNPAGE_INSTFILES

; Language files
!insertmacro MUI_LANGUAGE "English"

; MUI end ------

Name "${PRODUCT_NAME} ${PRODUCT_VERSION}"
OutFile "LookingGlass-0.4.0-Radegast-1.12-Setup.exe"
InstallDir "$PROGRAMFILES\Radegast"
InstallDirRegKey HKLM "${PRODUCT_DIR_REGKEY}" ""
ShowInstDetails show
ShowUnInstDetails show

Section "MainSection" SEC01
  SetOutPath "$PROGRAMFILES\Radegast"
  SetOverwrite try
  File "tempBuildZip\cg.dll"
  File "tempBuildZip\Grids.json"
  File "tempBuildZip\LookingGlass.Comm.dll"
  File "tempBuildZip\LookingGlass.Comm.LLLP.dll"
  File "tempBuildZip\LookingGlass.exe"
  CreateDirectory "$SMPROGRAMS\LookingGlass"
  CreateShortCut "$SMPROGRAMS\LookingGlass\LookingGlass.lnk" "$PROGRAMFILES\Radegast\LookingGlass.exe"
  CreateShortCut "$DESKTOP\LookingGlass.lnk" "$PROGRAMFILES\Radegast\LookingGlass.exe"
  File "tempBuildZip\LookingGlass.Framework.dll"
  File "tempBuildZip\LookingGlass.Framework.dll.log4net"
  File "tempBuildZip\LookingGlass.Radegast.dll"
  File "tempBuildZip\LookingGlass.Renderer.dll"
  File "tempBuildZip\LookingGlass.Renderer.Ogre.dll"
  File "tempBuildZip\LookingGlass.Rest.dll"
  File "tempBuildZip\LookingGlass.View.dll"
  File "tempBuildZip\LookingGlass.World.dll"
  File "tempBuildZip\LookingGlass.World.LL.dll"
  File "tempBuildZip\LookingGlassOgre.dll"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources"
  File "tempBuildZip\LookingGlassResources\DefaultMaterial.material"
  File "tempBuildZip\LookingGlassResources\DefaultTerrainMaterial.material"
  File "tempBuildZip\LookingGlassResources\grass_1024.jpg"
  File "tempBuildZip\LookingGlassResources\LoadingMaterial.material"
  File "tempBuildZip\LookingGlassResources\LoadingShape.material"
  File "tempBuildZip\LookingGlassResources\LoadingShape.mesh"
  File "tempBuildZip\LookingGlassResources\LoadingShape.mesh.sphere"
  File "tempBuildZip\LookingGlassResources\LoadingTexture.png"
  File "tempBuildZip\LookingGlassResources\LookingGlassLogo128.png"
  File "tempBuildZip\LookingGlassResources\LookingGlassLogo256.material"
  File "tempBuildZip\LookingGlassResources\LookingGlassLogo256.png"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\CommonCode.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\Ocean.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\overlay.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\Shadow1Tap.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\Shadow4Tap.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\ShadowCaster.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\SolidAmbient.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\SuperShader.cg"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\programs\UnlitTextured.cg"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\BlueTransparent.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\Console.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\flare.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\Jack.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\LitTextured.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\LitTexturedAdd.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\LitTexturedHardAlpha.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\LitTexturedSoftAlpha.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\LitTexturedSoftAlphaVCol.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\LitTexturedVCol.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\Ocean.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\overlay.program"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\RexSky.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\ShadowCaster.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\ShadowCaster.program"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\smoke.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\SolidAmbient.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\SuperShader.program"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTextured.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTextured.program"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedAdd.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedHardAlpha.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedSoftAlpha.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedSoftAlphaVCol.material"
  File "tempBuildZip\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedVCol.material"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\DefaultOceanSkyCube.dds"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\DefaultOceanWaves.dds"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\flare.png"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\Jack_body_yellow1.jpg"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\Jack_face_yellow.jpg"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\smoke.png"
  File "tempBuildZip\LookingGlassResources\Naali\media\textures\TextureMissing.png"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\models"
  File "tempBuildZip\LookingGlassResources\Naali\models\Jack.mesh"
  File "tempBuildZip\LookingGlassResources\Naali\models\Jack.skeleton"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources"
  File "tempBuildZip\LookingGlassResources\NoTexture.png"
  File "tempBuildZip\LookingGlassResources\Ocean.material"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-0000-9999-000000000005"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000001"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000003"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000004"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000005"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001000"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001001"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001002"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001003"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001004"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001005"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001006"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001007"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001008"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001009"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001010"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001011"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001012"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001013"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001014"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001015"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001016"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001017"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001018"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001019"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001020"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001022"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001023"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001024"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001026"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001027"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001028"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001029"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001030"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001031"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001034"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001035"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001036"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001037"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001038"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001039"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001040"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001041"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001042"
  File "tempBuildZip\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001043"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\8"
  File "tempBuildZip\LookingGlassResources\Preloaded\8\89556747-24cb-43ed-920b-47caed15465f"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01"
  File "tempBuildZip\LookingGlassResources\Shadow01\newLighting.cg"
  File "tempBuildZip\LookingGlassResources\Shadow01\newLighting.material"
  File "tempBuildZip\LookingGlassResources\Shadow01\newLighting.program"
  File "tempBuildZip\LookingGlassResources\Shadow01\newUtils.cg"
  File "tempBuildZip\LookingGlassResources\Shadow01\notes.txt"
  File "tempBuildZip\LookingGlassResources\Shadow01\ShadowCaster.material"
  File "tempBuildZip\LookingGlassResources\Shadow01\ShadowCaster.program"
  File "tempBuildZip\LookingGlassResources\Shadow01\vsmCaster.cg"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes"
  File "tempBuildZip\LookingGlassResources\Skyboxes\clouds.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\cloudy_noon_BK.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\cloudy_noon_DN.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\cloudy_noon_FR.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\cloudy_noon_LF.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\cloudy_noon_RT.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\cloudy_noon_UP.jpg"
  File "tempBuildZip\LookingGlassResources\Skyboxes\README"
  File "tempBuildZip\LookingGlassResources\Skyboxes\Skyboxes.material"
  File "tempBuildZip\LookingGlassResources\Skyboxes\Thumbs.db"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX"
  File "tempBuildZip\LookingGlassResources\SkyX\c22.png"
  File "tempBuildZip\LookingGlassResources\SkyX\c22n.png"
  File "tempBuildZip\LookingGlassResources\SkyX\Cloud1.png"
  File "tempBuildZip\LookingGlassResources\SkyX\Cloud1_Normal.png"
  File "tempBuildZip\LookingGlassResources\SkyX\DensityClouds1.png"
  File "tempBuildZip\LookingGlassResources\SkyX\Noise.jpg"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX.material"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_Clouds.hlsl"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_Ground.hlsl"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_Moon.hlsl"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_Moon.png"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_Skydome.hlsl"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_Starfield.png"
  File "tempBuildZip\LookingGlassResources\SkyX\SkyX_VolClouds.hlsl"
  File "tempBuildZip\LookingGlassResources\SkyX\Thumbs.db"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassUI\Default"
  File "tempBuildZip\LookingGlassUI\Default\DefaultParameters.html"
  File "tempBuildZip\LookingGlassUI\Default\index.html"
  File "tempBuildZip\LookingGlassUI\Default\LookingGlass.css"
  File "tempBuildZip\LookingGlassUI\Default\test-login.html"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassUI"
  File "tempBuildZip\LookingGlassUI\favicon.gif"
  File "tempBuildZip\LookingGlassUI\favicon.ico"
  SetOutPath "$PROGRAMFILES\Radegast\LookingGlassUI\std"
  File "tempBuildZip\LookingGlassUI\std\Copyrights.html"
  File "tempBuildZip\LookingGlassUI\std\Copyrights.json"
  File "tempBuildZip\LookingGlassUI\std\CopyrightsNew.html"
  File "tempBuildZip\LookingGlassUI\std\jquery-1.3.2.min.js"
  File "tempBuildZip\LookingGlassUI\std\jquery.js"
  File "tempBuildZip\LookingGlassUI\std\jquery.sparkline.min.js"
  File "tempBuildZip\LookingGlassUI\std\LGScripts.js"
  File "tempBuildZip\LookingGlassUI\std\LookingGlassLogo256.png"
  File "tempBuildZip\LookingGlassUI\std\skeleton.html"
  SetOutPath "$PROGRAMFILES\Radegast"
  File "tempBuildZip\msvcr90.dll"
  File "tempBuildZip\OgreGUIRenderer.dll"
  File "tempBuildZip\OgreMain.dll"
  File "tempBuildZip\OIS.dll"
  File "tempBuildZip\openjpeg-dotnet.dll"
  File "tempBuildZip\Plugins.cfg"
  File "tempBuildZip\Plugin_BSPSceneManager.dll"
  File "tempBuildZip\Plugin_CgProgramManager.dll"
  File "tempBuildZip\Plugin_OctreeSceneManager.dll"
  File "tempBuildZip\Plugin_OctreeZone.dll"
  File "tempBuildZip\Plugin_ParticleFX.dll"
  File "tempBuildZip\Plugin_PCZSceneManager.dll"
  File "tempBuildZip\PrimMesher.dll"
  File "tempBuildZip\RadegastLookingGlass.json"
  File "tempBuildZip\RadegastModules.json"
  File "tempBuildZip\RenderSystem_Direct3D9.dll"
  File "tempBuildZip\RenderSystem_GL.dll"
  File "tempBuildZip\resources.cfg"
  File "tempBuildZip\SkyX.dll"
SectionEnd

Section -Post
  WriteUninstaller "$INSTDIR\uninst.exe"
  WriteRegStr HKLM "${PRODUCT_DIR_REGKEY}" "" "$PROGRAMFILES\Radegast\LookingGlass.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayName" "$(^Name)"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "UninstallString" "$INSTDIR\uninst.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayIcon" "$PROGRAMFILES\Radegast\LookingGlass.exe"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "DisplayVersion" "${PRODUCT_VERSION}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "URLInfoAbout" "${PRODUCT_WEB_SITE}"
  WriteRegStr ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}" "Publisher" "${PRODUCT_PUBLISHER}"
SectionEnd


Function un.onUninstSuccess
  HideWindow
  MessageBox MB_ICONINFORMATION|MB_OK "$(^Name) was successfully removed from your computer."
FunctionEnd

Function un.onInit
  MessageBox MB_ICONQUESTION|MB_YESNO|MB_DEFBUTTON2 "Are you sure you want to completely remove $(^Name) and all of its components?" IDYES +2
  Abort
FunctionEnd

Section Uninstall
  Delete "$INSTDIR\uninst.exe"
  Delete "$PROGRAMFILES\Radegast\SkyX.dll"
  Delete "$PROGRAMFILES\Radegast\resources.cfg"
  Delete "$PROGRAMFILES\Radegast\RenderSystem_GL.dll"
  Delete "$PROGRAMFILES\Radegast\RenderSystem_Direct3D9.dll"
  Delete "$PROGRAMFILES\Radegast\RadegastModules.json"
  Delete "$PROGRAMFILES\Radegast\RadegastLookingGlass.json"
  Delete "$PROGRAMFILES\Radegast\PrimMesher.dll"
  Delete "$PROGRAMFILES\Radegast\Plugin_PCZSceneManager.dll"
  Delete "$PROGRAMFILES\Radegast\Plugin_ParticleFX.dll"
  Delete "$PROGRAMFILES\Radegast\Plugin_OctreeZone.dll"
  Delete "$PROGRAMFILES\Radegast\Plugin_OctreeSceneManager.dll"
  Delete "$PROGRAMFILES\Radegast\Plugin_CgProgramManager.dll"
  Delete "$PROGRAMFILES\Radegast\Plugin_BSPSceneManager.dll"
  Delete "$PROGRAMFILES\Radegast\Plugins.cfg"
  Delete "$PROGRAMFILES\Radegast\openjpeg-dotnet.dll"
  Delete "$PROGRAMFILES\Radegast\OIS.dll"
  Delete "$PROGRAMFILES\Radegast\OgreMain.dll"
  Delete "$PROGRAMFILES\Radegast\OgreGUIRenderer.dll"
  Delete "$PROGRAMFILES\Radegast\msvcr90.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\skeleton.html"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\LookingGlassLogo256.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\LGScripts.js"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\jquery.sparkline.min.js"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\jquery.js"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\jquery-1.3.2.min.js"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\CopyrightsNew.html"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\Copyrights.json"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\std\Copyrights.html"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\favicon.ico"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\favicon.gif"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\Default\test-login.html"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\Default\LookingGlass.css"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\Default\index.html"
  Delete "$PROGRAMFILES\Radegast\LookingGlassUI\Default\DefaultParameters.html"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\Thumbs.db"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_VolClouds.hlsl"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_Starfield.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_Skydome.hlsl"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_Moon.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_Moon.hlsl"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_Ground.hlsl"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX_Clouds.hlsl"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\SkyX.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\Noise.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\DensityClouds1.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\Cloud1_Normal.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\Cloud1.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\c22n.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX\c22.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\Thumbs.db"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\Skyboxes.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\README"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\cloudy_noon_UP.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\cloudy_noon_RT.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\cloudy_noon_LF.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\cloudy_noon_FR.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\cloudy_noon_DN.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\cloudy_noon_BK.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes\clouds.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\vsmCaster.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\ShadowCaster.program"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\ShadowCaster.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\notes.txt"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\newUtils.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\newLighting.program"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\newLighting.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01\newLighting.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\8\89556747-24cb-43ed-920b-47caed15465f"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001043"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001042"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001041"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001040"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001039"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001038"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001037"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001036"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001035"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001034"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001031"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001030"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001029"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001028"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001027"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001026"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001024"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001023"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001022"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001020"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001019"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001018"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001017"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001016"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001015"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001014"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001013"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001012"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001011"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001010"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001009"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001008"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001007"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001006"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001005"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001004"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001003"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001002"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001001"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-2222-3333-100000001000"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000005"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000004"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000003"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-1111-9999-000000000001"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0\00000000-0000-0000-9999-000000000005"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Ocean.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\NoTexture.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\models\Jack.skeleton"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\models\Jack.mesh"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\TextureMissing.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\smoke.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\Jack_face_yellow.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\Jack_body_yellow1.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\flare.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\DefaultOceanWaves.dds"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures\DefaultOceanSkyCube.dds"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedVCol.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedSoftAlphaVCol.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedSoftAlpha.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedHardAlpha.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTexturedAdd.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTextured.program"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\UnlitTextured.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\SuperShader.program"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\SolidAmbient.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\smoke.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\ShadowCaster.program"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\ShadowCaster.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\RexSky.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\overlay.program"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\Ocean.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\LitTexturedVCol.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\LitTexturedSoftAlphaVCol.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\LitTexturedSoftAlpha.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\LitTexturedHardAlpha.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\LitTexturedAdd.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\LitTextured.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\Jack.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\flare.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\Console.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts\BlueTransparent.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\UnlitTextured.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\SuperShader.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\SolidAmbient.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\ShadowCaster.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\Shadow4Tap.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\Shadow1Tap.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\overlay.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\Ocean.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs\CommonCode.cg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LookingGlassLogo256.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LookingGlassLogo256.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LookingGlassLogo128.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LoadingTexture.png"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LoadingShape.mesh.sphere"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LoadingShape.mesh"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LoadingShape.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\LoadingMaterial.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\grass_1024.jpg"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\DefaultTerrainMaterial.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassResources\DefaultMaterial.material"
  Delete "$PROGRAMFILES\Radegast\LookingGlassOgre.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.World.LL.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.World.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.View.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Rest.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Renderer.Ogre.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Renderer.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Radegast.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Framework.dll.log4net"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Framework.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.exe"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Comm.LLLP.dll"
  Delete "$PROGRAMFILES\Radegast\LookingGlass.Comm.dll"
  Delete "$PROGRAMFILES\Radegast\Grids.json"
  Delete "$PROGRAMFILES\Radegast\cg.dll"

  Delete "$DESKTOP\LookingGlass.lnk"
  Delete "$SMPROGRAMS\LookingGlass\LookingGlass.lnk"

  RMDir "$SMPROGRAMS\LookingGlass"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassUI\std"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassUI\Default"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassUI"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\SkyX"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Skyboxes"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Shadow01"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\8"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Preloaded\0"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\models"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\textures"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\scripts"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources\Naali\media\materials\programs"
  RMDir "$PROGRAMFILES\Radegast\LookingGlassResources"
  RMDir "$PROGRAMFILES\Radegast"

  DeleteRegKey ${PRODUCT_UNINST_ROOT_KEY} "${PRODUCT_UNINST_KEY}"
  DeleteRegKey HKLM "${PRODUCT_DIR_REGKEY}"
  SetAutoClose true
SectionEnd
