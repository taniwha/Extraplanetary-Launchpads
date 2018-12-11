AUGER = \
	Auger.mu		\
	$e

all: ${AUGER}

install: all
	#nothing to do as GameData makefile will take care of installation

${AUGER} : Auger.blend
	blender -noaudio --background Auger.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${AUGER} model000.mbm.png

.PHONY: all clean install
