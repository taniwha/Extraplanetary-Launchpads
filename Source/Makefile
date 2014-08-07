KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
ELGAMEDATA  := ${GAMEDATA}/ExtraplanetaryLaunchpads
PLUGINDIR	:= ${ELGAMEDATA}/Plugins
TBGAMEDATA  := ${GAMEDATA}/000_Toolbar
APIEXTDATA	:= ${PLUGINDIR}

TARGETS		:= Launchpad.dll LaunchpadToolbar.dll

EL_FILES := \
    AssemblyInfo.cs	\
	BuildCost.cs	\
	BuildWindow.cs	\
	DropDownList.cs	\
	ExLaunchPad.cs	\
	ProgressBar.cs	\
	Recycler.cs		\
	Resources.cs	\
	Settings.cs		\
	ShipInfo.cs		\
	StrutFixer.cs	\
	SurveyStake.cs	\
	SurveyTracker.cs\
	Target.cs		\
	VersionReport.cs\
	Workshop.cs		\
	$e

ELTB_FILES := \
	AssemblyInfoToolbar.cs	\
	Toolbar.cs				\
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

LaunchpadToolbar.dll: ${ELTB_FILES} Launchpad.dll
	${GMCS} ${GMCSFLAGS} -t:library -lib:${TBGAMEDATA},${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-r:Toolbar \
		-r:Launchpad \
		-out:$@ ${ELTB_FILES}

clean:
	rm -f ${TARGETS} AssemblyInfoToolbar.cs AssemblyInfo.cs

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}

.PHONY: all clean install
