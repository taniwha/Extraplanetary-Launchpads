KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
ELGAMEDATA  := ${GAMEDATA}/ExtraplanetaryLaunchpads
PLUGINDIR	:= ${MODGAMEDATA}/Plugins
APIEXTDATA	:= ${PLUGINDIR}

RESGEN2	:= resgen2
GMCS	:= gmcs
GIT		:= git
TAR		:= tar
ZIP		:= zip

SUBDIRS=\
	ExtraplanetaryLaunchpads/Parts/ControlReference	\
	ExtraplanetaryLaunchpads/Parts/MicroPad	\
	ExtraplanetaryLaunchpads/Parts/smelter	\
	$e

all clean:
	@for dir in ${SUBDIRS}; do \
		make -C $$dir $@ || exit 1; \
	done

info:
	@echo "Extraplanetary Launchpads Build Information"
	@echo "    resgen2:  ${RESGEN2}"
	@echo "    gmcs:     ${GMCS}"
	@echo "    git:      ${GIT}"
	@echo "    tar:      ${TAR}"
	@echo "    zip:      ${ZIP}"
	@echo "    KSP Data: ${KSPDIR}"
	@echo "    Plugin:   ${PLUGINDIR}"

install: all
	../tools/copy-files files ${GAMEDATA}

.PHONY: all clean info install
