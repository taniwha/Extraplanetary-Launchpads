ROCKETBUILDER = \
	rocket_builder.mu		\
	$e

all: ${ROCKETBUILDER}

install: all
	#nothing to do as GameData makefile will take care of installation

${ROCKETBUILDER} : rocket_builder.blend
	blender -noaudio --background rocket_builder.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${ROCKETBUILDER} model000.mbm.png

.PHONY: all clean install
