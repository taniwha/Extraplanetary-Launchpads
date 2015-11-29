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

all: kairyuu.png kairyuu_scaled.png

info:
	@echo "Extraplanetary Launchpads Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

kairyuu.png: kairyuu.svg
	inkscape --export-width=256 --export-height=160 --export-png=$@ $^

kairyuu_scaled.png: kairyuu.svg
	inkscape --export-width=64 --export-height=40 --export-background='#000000' --export-background-opacity=0 --export-png=$@ $^

clean:
	rm -f kairyuu.png kairyuu_scaled.png

install: all
	mkdir -p ${ELGAMEDATA}/Agencies
	cp kairyuu.png kairyuu_scaled.png ${ELGAMEDATA}/Agencies

.PHONY: all clean install
