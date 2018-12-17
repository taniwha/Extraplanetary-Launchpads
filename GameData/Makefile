MODNAME		:= ExtraplanetaryLaunchpads
KSPDIR		:= ${HOME}/ksp/KSP_linux
MANAGED		:= ${KSPDIR}/KSP_Data/Managed
GAMEDATA	:= ${KSPDIR}/GameData
MODGAMEDATA	:= ${GAMEDATA}/${MODNAME}
PLUGINDIR	:= ${MODGAMEDATA}/Plugins
APIEXTDATA	:= ${PLUGINDIR}

RESGEN2	:= resgen2
GMCS	:= gmcs
GIT		:= git
TAR		:= tar
ZIP		:= zip

SUBDIRS=\
	ExtraplanetaryLaunchpads/Parts/Auger			\
	ExtraplanetaryLaunchpads/Parts/ConstructionDrone\
	ExtraplanetaryLaunchpads/Parts/ControlReference	\
	ExtraplanetaryLaunchpads/Parts/LandingPad		\
	ExtraplanetaryLaunchpads/Parts/Mallet			\
	ExtraplanetaryLaunchpads/Parts/MicroPad			\
	ExtraplanetaryLaunchpads/Parts/RecycleBin		\
	ExtraplanetaryLaunchpads/Parts/SurveyBuoy		\
	ExtraplanetaryLaunchpads/Parts/SurveyStake		\
	ExtraplanetaryLaunchpads/Parts/Workshop			\
	ExtraplanetaryLaunchpads/Parts/launchpad2		\
	ExtraplanetaryLaunchpads/Parts/rocket_builder	\
	ExtraplanetaryLaunchpads/Parts/smelter

all clean:
	@for dir in ${SUBDIRS}; do \
		make -C $$dir $@ || exit 1; \
	done

info:
	@echo "${MODNAME} Build Information"
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
