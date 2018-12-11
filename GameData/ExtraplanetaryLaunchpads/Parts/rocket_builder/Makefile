AUGER = \
	rocket_builder.mu		\
	$e

all: ${AUGER}

install: all
	#nothing to do as GameData makefile will take care of installation

${AUGER} : rocket_builder.blend
	blender -noaudio --background rocket_builder.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${AUGER} model000.mbm.png

.PHONY: all clean install
