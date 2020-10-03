export MODNAME		:= ExtraplanetaryLaunchpads
export KSPDIR		:= ${HOME}/ksp/KSP_linux
export MANAGED		:= ${KSPDIR}/KSP_Data/Managed
export GAMEDATA		:= ${KSPDIR}/GameData
export MODGAMEDATA	:= ${GAMEDATA}/${MODNAME}
export PLUGINDIR	:= ${MODGAMEDATA}/Plugins

TARGETS		:= bin/Launchpad.dll

EL_FILES := \
	CommandSeat/ControlReference.cs		\
	CommandSeat/FixSeatName.cs			\
	CommandSeat/NoControlSwitch.cs		\
	Converter/Converter.cs				\
	Converter/CoreHeat.cs				\
	Converter/Extractor.cs				\
	Converter/IResourceProvider.cs		\
	Converter/KethaneResourceProvider.cs\
	Converter/StockResourceProvider.cs	\
	DisposablePad/DisposablePad.cs		\
	Hull/Box.cs							\
	Hull/Connectivity.cs				\
	Hull/CraftHull.cs					\
	Hull/Quickhull.cs					\
	Hull/Edge.cs						\
	Hull/FaceSet.cs						\
	Hull/RawMesh.cs						\
	Hull/Triangle.cs					\
	InternalParentConstraint.cs			\
	Recipes/BuildCost.cs				\
	Recipes/BuildResource.cs			\
	Recipes/BuildResourceSet.cs			\
	Recipes/ConverterRecipe.cs			\
	Recipes/CostReport.cs				\
	Recipes/Ingredient.cs				\
	Recipes/PartRecipe.cs				\
	Recipes/Recipe.cs					\
	Recipes/RecipeResourceContainer.cs	\
	Recipes/RecipeDatabase.cs			\
	Recipes/RecipeLoader.cs				\
	ResourceManager/IResourceContainer.cs		\
	ResourceManager/KISResourceContainer.cs		\
	ResourceManager/PartResourceContainer.cs	\
	ResourceManager/ResourceInfo.cs				\
	ResourceManager/ResourceManager.cs			\
	ResourceManager/ResourceSet.cs				\
	ResourceManager/ResourceSetContainer.cs		\
	Survey/Billboard.cs					\
	Survey/Points.cs					\
	Survey/SiteBody.cs					\
	Survey/SiteList.cs					\
	Survey/SurveySite.cs				\
	Survey/SurveySkill.cs				\
	Survey/SurveyStake.cs				\
	Survey/SurveyStation.cs				\
	Survey/SurveyTracker.cs				\
	Survey/VirtualPad.cs				\
    assembly/AssemblyInfo.cs			\
	assembly/Checkers.cs				\
	assembly/VersionReport.cs			\
	GUI/BuildWindow.cs					\
	GUI/CraftBrowser.cs					\
	GUI/DropDownList.cs					\
	GUI/EditorToolbar.cs				\
	GUI/ProgressBar.cs					\
	GUI/RenameWindow.cs					\
	GUI/ResourceWindow.cs				\
	GUI/ScrollView.cs					\
	GUI/ShipInfo.cs						\
	GUI/Styles.cs						\
	GUI/TextField.cs					\
	LaunchClamp/LaunchClamp.cs			\
	Pad/Launchpad.cs					\
	Recycler/Recycler.cs				\
	Recycler/StateMachine.cs			\
	Target/Target.cs					\
	UI/BuildView.cs						\
	UI/CraftView.cs						\
	UI/IResourceLine.cs					\
	UI/Localization.cs					\
	UI/MainWindow.cs					\
	UI/PadView.cs						\
	UI/ResourceDisplay.cs				\
	UI/ResourceLine.cs					\
	UI/StatusBar.cs						\
	UI/TransferView.cs					\
	Workshop/ConstructionSkill.cs		\
	Workshop/ProtoWorkSink.cs			\
	Workshop/VesselWorkNet.cs			\
	Workshop/WorkSink.cs				\
	Workshop/WorkSource.cs				\
	Workshop/Workshop.cs				\
	lib/KASWrapper.cs					\
	lib/KISWrapper.cs					\
	lib/KACWrapper.cs					\
	lib/KerbalStatsWrapper.cs			\
	lib/Utils.cs						\
	toolbar/Toolbar.cs					\
	toolbar/ToolbarWrapper.cs			\
	AirstreamShield.cs					\
	BuildControl.cs						\
	ControlInterface.cs					\
	Settings.cs							\
	$e

include KodeUI/KodeUI.inc

RESGEN2		:= resgen2
CSC			:= csc
CSCFLAGS	:= -highentropyva- -noconfig -nostdlib+ -t:library -optimize -warnaserror -debug
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: version ${TARGETS}

.PHONY: version
version:
	@../tools/git-version.sh

info:
	@echo "${MODNAME} Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    csc:        ${CSC}"
	@echo "    csc flags:  ${CSCFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

SYSTEM := \
	-lib:${MANAGED} \
	-r:${MANAGED}/mscorlib.dll \
	-r:${MANAGED}/System.dll \
	-r:${MANAGED}/System.Core.dll

KSP := \
	-r:Assembly-CSharp.dll \
	-r:Assembly-CSharp-firstpass.dll

UNITY := \
	-r:UnityEngine.dll \
	-r:UnityEngine.UI.dll \
	-r:UnityEngine.UIModule.dll \
	-r:UnityEngine.IMGUIModule.dll \
	-r:UnityEngine.TextRenderingModule.dll \
	-r:UnityEngine.AnimationModule.dll \
	-r:UnityEngine.CoreModule.dll \
	-r:UnityEngine.PhysicsModule.dll \
	-r:UnityEngine.Physics2DModule.dll \
	-r:UnityEngine.InputLegacyModule.dll \
	-r:UnityEngine.ImageConversionModule.dll \
	-r:UnityEngine.ParticleSystemModule.dll \
	-r:UnityEngine.AudioModule.dll \
	$e

bin/Launchpad.dll: ${EL_FILES} ${KodeUI}
	@mkdir -p bin
	${CSC} ${CSCFLAGS} ${SYSTEM} ${KSP} ${UNITY} -out:$@ $^

clean:
	rm -f ${TARGETS}* assembly/AssemblyInfo.cs bin/${MODNAME}.version
	test -d bin && rmdir bin || true

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} bin/${MODNAME}.version ${PLUGINDIR}

.PHONY: all clean install
