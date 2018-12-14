MALLET = \
	Mallet.mu \
	Mallet-Handle-diff.png \
	Mallet-Head-diff.png \
	Mallet-Head-nrm.png

all: ${MALLET}

install: all
	#nothing to do as GameData makefile will take care of installation

${MALLET} : Mallet.blend
	blender -noaudio --background Mallet.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${MALLET}

.PHONY: all clean install
