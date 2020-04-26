MODNAME		:= ExtraplanetaryLaunchpads
KSPDIR		:= ${HOME}/ksp/KSP_linux
GAMEDATA	:= ${KSPDIR}/GameData
MODGAMEDATA	:= ${GAMEDATA}/${MODNAME}

RESGEN2		:= resgen2
GMCS		:= gmcs
GMCSFLAGS	:= -optimize -warnaserror
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: EL_Manual.pdf

info:
	@echo "${MODNAME} Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

%.pdf: %.lyx
	lyx -E pdf4 $@ $^

clean:
	rm -f *.pdf *.tex

veryclean: clean
	rm -f *.bak *~

install: all
	mkdir -p ${MODGAMEDATA}
	cp *.pdf ${MODGAMEDATA}

.PHONY: all clean install veryclean
