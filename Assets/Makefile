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

all: kairyuu.png kairyuu_scaled.png icon_button.png icon_filter.png plaque.png
#	${GUI_ICONS}

info:
	@echo "Extraplanetary Launchpads Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

GUI_ICONS = \
	gui_background.png	\
	gui_flat.png		\
	gui_raised.png		\
	gui_recessed.png	\
	$e

${GUI_ICONS} : gui_icons.blend export-images.py
	blender -noaudio --background gui_icons.blend -P export-images.py

plaque.png: plaque.svg
	inkscape --export-width=128 --export-height=128 --export-png=$@ $^

kairyuu.png: kairyuu.svg
	inkscape --export-width=256 --export-height=160 --export-png=$@ $^

kairyuu_scaled.png: kairyuu.svg
	inkscape --export-width=64 --export-height=40 --export-background='#000000' --export-background-opacity=0 --export-png=$@ $^

icon_button.png: icon_button.svg
	inkscape --export-width=24 --export-height=24 --export-background='#000000' --export-background-opacity=0 --export-png=$@ $^

icon_filter.png: icon_button.svg
	sed -e 's/\#....../#ffffff/g' $^ | inkscape --export-width=32 --export-height=32 --export-background='#000000' --export-background-opacity=0 --export-png=$@ /dev/stdin

clean:
	rm -f kairyuu.png kairyuu_scaled.png icon_button.png icon_filter.png ${GUI_ICONS}

install: all
	mkdir -p ${ELGAMEDATA}/Agencies
	cp kairyuu.png kairyuu_scaled.png ${ELGAMEDATA}/Agencies
	mkdir -p ${ELGAMEDATA}/Textures
	cp icon_button.png icon_filter.png plaque.png ${ELGAMEDATA}/Textures
	#cp ${GUI_ICONS} ${ELGAMEDATA}/Textures

.PHONY: all clean install
