KSPDIR		:= ${HOME}/ksp/KSP_linux
GAMEDATA	:= ${KSPDIR}/GameData
ELGAMEDATA  := ${GAMEDATA}/ExtraplanetaryLaunchpads

DOC_FILES := \
	FreeEVA.png \
	License.txt \
	README.md

RESGEN2		:= resgen2
GMCS		:= gmcs
GMCSFLAGS	:= -optimize -warnaserror
GIT			:= git
TAR			:= tar
ZIP			:= zip

all: EL_Manual.pdf

info:
	@echo "Extraplanetary Launchpads Build Information"
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
	rm -f *.pdf

veryclean: clean
	rm -f *.bak *~

install: all
	mkdir -p ${ELGAMEDATA}
	cp *.pdf ${ELGAMEDATA}

.PHONY: all clean install veryclean
