WORKSHOP = \
	Hatch.png \
	Hull.png \
	Ladder.png \
	Workshop-Internal.png \
	Workshop.Internal.mu \
	Workshop.mu \
	Workstation.mu \
	Workstation.png \
	Workstation_Screen-16x9.png \
	Workstation_Screen-4x3.png

all: ${WORKSHOP}

install: all
	#nothing to do as GameData makefile will take care of installation

${WORKSHOP} : Workshop.blend
	blender -noaudio --background Workshop.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${WORKSHOP}

.PHONY: all clean install
