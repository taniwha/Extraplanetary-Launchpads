LAUNCHPAD = \
	launchpad2.mu		\
	pad.png

all: ${LAUNCHPAD}

install: all
	#nothing to do as GameData makefile will take care of installation

${LAUNCHPAD} : launchpad2.blend
	blender -noaudio --background launchpad2.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${LAUNCHPAD}

.PHONY: all clean install
