export MODNAME		:= ExtraplanetaryLaunchpads
export KSPDIR		:= ${HOME}/ksp/KSP_linux
export MANAGED		:= ${KSPDIR}/KSP_Data/Managed
export GAMEDATA		:= ${KSPDIR}/GameData
export MODGAMEDATA  := ${GAMEDATA}/${MODNAME}
export PLUGINDIR	:= ${MODGAMEDATA}/Plugins
export APIEXTDATA	:= ${PLUGINDIR}

RESGEN2	:= resgen2
CSC		:= csc
GIT		:= git
TAR		:= tar
ZIP		:= zip

.PHONY: all clean info install release

SUBDIRS=Assets Documentation GameData Source

all clean install:
	@for dir in ${SUBDIRS}; do \
		make -C $$dir $@ || exit 1; \
	done

info:
	@echo "${MODNAME} Build Information"
	@echo "    resgen2:  ${RESGEN2}"
	@echo "    gmcs:     ${CSC}"
	@echo "    git:      ${GIT}"
	@echo "    tar:      ${TAR}"
	@echo "    zip:      ${ZIP}"
	@echo "    KSP Data: ${KSPDIR}"
	@echo "    Plugin:   ${PLUGINDIR}"

release:
	tools/make-release -u
