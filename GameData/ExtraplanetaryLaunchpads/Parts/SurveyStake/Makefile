SURVEYSTAKE = \
	surveystake.mu		\
	Stake.png

all: ${SURVEYSTAKE}

install: all
	#nothing to do as GameData makefile will take care of installation

${SURVEYSTAKE} : surveystake.blend
	blender -noaudio --background surveystake.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${SURVEYSTAKE}

.PHONY: all clean install
