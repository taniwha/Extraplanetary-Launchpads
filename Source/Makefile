KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
ELGAMEDATA  := ${GAMEDATA}/ExtraplanetaryLaunchpads
PLUGINDIR	:= ${ELGAMEDATA}/Plugins
TBGAMEDATA  := ${GAMEDATA}/000_Toolbar

TARGETS		:= Launchpad.dll LaunchpadToolbar.dll

EL_FILES := \
    AssemblyInfo.cs	\
	BuildCost.cs	\
	ExLaunchPad.cs	\
	Recycler.cs		\
	Resources.cs	\
	Settings.cs		\
	ShipInfo.cs		\
	StrutFixer.cs	\
	Target.cs		\
	Workshop.cs		\
	$e

ELTB_FILES := \
	AssemblyInfoToolbar.cs	\
	Toolbar.cs				\
	$e

RESGEN2	:= resgen2
GMCS	:= gmcs
GIT		:= git
TAR		:= tar
ZIP		:= zip

all: ${TARGETS}

info:
	@echo "Extraplanetary Launchpads Build Information"
	@echo "    resgen2:  ${RESGEN2}"
	@echo "    gmcs:     ${GMCS}"
	@echo "    git:      ${GIT}"
	@echo "    tar:      ${TAR}"
	@echo "    zip:      ${ZIP}"
	@echo "    KSP Data: ${KSPDIR}"

Launchpad.dll: ${EL_FILES}
	${GMCS} -t:library -lib:${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-out:$@ $^

LaunchpadToolbar.dll: ${ELTB_FILES} Launchpad.dll
	${GMCS} -t:library -lib:${TBGAMEDATA},${MANAGED} \
		-r:Assembly-CSharp,Assembly-CSharp-firstpass,UnityEngine \
		-r:Toolbar \
		-r:Launchpad \
		-out:$@ ${ELTB_FILES}

clean:
	rm -f ${TARGETS}

install: all
	mkdir -p ${PLUGINDIR}
	cp ${TARGETS} ${PLUGINDIR}

.PHONY: all clean install
