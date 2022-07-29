CONSTRUCTIONDRONE = \
	constructionDrone.mu	\
	part.cfg

all: ${CONSTRUCTIONDRONE}

install: all
	#nothing to do as GameData makefile will take care of installation

${CONSTRUCTIONDRONE} : ConstructionDrone.blend constructionDrone.cfg.in
	blender -noaudio --background ConstructionDrone.blend -P ../../../../tools/mass-export.py
	mv constructionDrone.cfg part.cfg

clean:
	rm -f ${CONSTRUCTIONDRONE}

.PHONY: all clean install
