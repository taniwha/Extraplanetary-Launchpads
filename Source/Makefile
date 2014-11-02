KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
ELGAMEDATA  := ${GAMEDATA}/ExtraplanetaryLaunchpads
PLUGINDIR	:= ${ELGAMEDATA}/Plugins
TBGAMEDATA  := ${GAMEDATA}/000_Toolbar
APIEXTDATA	:= ${PLUGINDIR}

TARGETS		:= Launchpad.dll

EL_FILES := \
    AssemblyInfo.cs		\
	BuildControl.cs		\
	BuildCost.cs		\
	BuildWindow.cs		\
	Checkers.cs			\
	DropDownList.cs		\
	KerbalStatsWrapper.cs	\
	LaunchClamp.cs		\
	Launchpad.cs		\
	ProgressBar.cs		\
	Recycler.cs			\
	Resources.cs		\
	Settings.cs			\
	ShipInfo.cs			\
	StrutFixer.cs		\
	SurveyStake.cs		\
	SurveyStation.cs	\
	SurveyTracker.cs	\
	Target.cs			\
	Toolbar.cs			\
	ToolbarWrapper.cs	\
	VersionReport.cs	\
	Workshop.cs			\
	$e

RESGEN2		:= resgen2
GMCS		:= gmcs
GMCSFLAGS	:= -optimize -warnaserror
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: version ${TARGETS}

.PHONY: version
version:
	@./git-version.sh

info:
	@echo "Extraplanetary Launchpads Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

Launchpad.dll: ${EL_FILES}
	${GMCS} ${GMCSFLAGS} -t:library -lib:${APIEXTDATA},${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-r:KSPAPIExtensions \
		-out:$@ $^

clean:
	rm -f ${TARGETS} AssemblyInfo.cs

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}

.PHONY: all clean install
