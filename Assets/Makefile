MODNAME		:= ExtraplanetaryLaunchpads
KSPDIR		:= ${HOME}/ksp/KSP_linux
GAMEDATA	:= ${KSPDIR}/GameData
MODGAMEDATA  := ${GAMEDATA}/${MODNAME}

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

ICONS := \
	icon_button.png	\
	icon_filter_n.png \
	icon_filter_s.png

FLAGS := \
	kairyuu.png	\
	nightheron.png

MISC := \
	ELGenericCraftThumb.png \
	kairyuu_scaled.png	\
	plaque.png

UI_IMAGES := \
	ui_background.png	\
	ui_button.png		\
	ui_checkmark.png	\
	ui_dropdown.png		\
	ui_leftturn.png		\
	ui_rightturn.png	\
	ui_tabdesel.png		\
	ui_tabempty.png		\
	ui_tabsel.png		\
	ui_toggle_off.png	\
	ui_toggle_on.png	\
	$e

UI_CFG := \
	DefaultSkin.cfg		\
	$e

all: ${ICONS} ${FLAGS} ${MISC} ${UI_IMAGES}

info:
	@echo "Extraplanetary Launchpads Build Information"
	@echo "    resgen2:    ${RESGEN2}"
	@echo "    gmcs:       ${GMCS}"
	@echo "    gmcs flags: ${GMCSFLAGS}"
	@echo "    git:        ${GIT}"
	@echo "    tar:        ${TAR}"
	@echo "    zip:        ${ZIP}"
	@echo "    KSP Data:   ${KSPDIR}"

GUI_ICONS := \
	gui_background.png	\
	gui_flat.png		\
	gui_raised.png		\
	gui_recessed.png	\
	$e

${GUI_ICONS} : gui_icons.blend export-images.py
	blender -noaudio --background gui_icons.blend -P export-images.py

plaque.png: plaque.svg
	inkscape --export-width=128 --export-height=128 --export-type=png -o $@ $^

kairyuu.png: kairyuu.svg
	inkscape --export-width=256 --export-height=160 --export-type=png -o $@ $^

nightheron.png: nightheron.svg
	inkscape --export-width=256 --export-height=160 --export-type=png -o $@ $^

kairyuu_scaled.png: kairyuu.svg
	inkscape --export-width=64 --export-height=40 --export-type=png -o $@ $^

icon_button.png: EL_app.svg
	inkscape --export-width=38 --export-height=38 --export-type=png -o $@ $^

icon_filter_n.png: EL_filter.svg
	sed -e 's/\#[[:xdigit:]]\{6\}/#000000/g' -e 's/\#[[:xdigit:]]\{3\}/#000/g' $^ | inkscape --export-width=32 --export-height=32 --export-type=png -o $@ /dev/stdin

icon_filter_s.png: EL_filter.svg
	sed -e 's/\#[[:xdigit:]]\{6\}/#ffffff/g' -e 's/\#[[:xdigit:]]\{3\}/#fff/g' $^ | inkscape --export-width=32 --export-height=32 --export-type=png -o $@ /dev/stdin

ELGenericCraftThumb.png: EL_UntitledSpacecraft.svg
	inkscape --export-width=256 --export-height=256 --export-type=png -o $@ $^

ui_background.png: ui_background.svg
	inkscape --export-width=32 --export-height=32 --export-type=png -o $@ $^

ui_button.png: ui_button.svg
	inkscape --export-width=16 --export-height=16 --export-type=png -o $@ $^

ui_checkmark.png: ui_checkmark.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_dropdown.png: ui_dropdown.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_leftturn.png: ui_leftturn.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_rightturn.png: ui_rightturn.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_tabdesel.png: ui_tabdesel.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_tabempty.png: ui_tabempty.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_tabsel.png: ui_tabsel.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_toggle_off.png: ui_toggle_off.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

ui_toggle_on.png: ui_toggle_on.svg
	inkscape --export-width=24 --export-height=24 --export-type=png -o $@ $^

clean:
	rm -f ${ICONS} ${FLAGS} ${MISC} ${GUI_ICONS} ${UI_IMAGES}

install: all
	mkdir -p ${MODGAMEDATA}/Agencies ${MODGAMEDATA}/Flags
	cp kairyuu.png kairyuu_scaled.png ${MODGAMEDATA}/Agencies
	cp nightheron.png ${MODGAMEDATA}/Flags
	mkdir -p ${MODGAMEDATA}/Textures
	cp ELGenericCraftThumb.png icon_button.png icon_filter*.png plaque.png ${MODGAMEDATA}/Textures
	mkdir -p ${MODGAMEDATA}/UI
	cp ${UI_CFG} ${UI_IMAGES} ${MODGAMEDATA}/UI

.PHONY: all clean install
