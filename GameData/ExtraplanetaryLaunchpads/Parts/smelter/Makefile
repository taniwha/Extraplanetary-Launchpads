SMELTER = \
	Smelter_Diffuse.png	\
	Smelter_NRM.png	\
	Smelter_Emit.png	\
	Smelter.cfg	\
	Smelter.mu		\
	$e

all: ${SMELTER}

install: all
	#nothing to do as GameData makefile will take care of installation

${SMELTER} : Smelter.blend Smelter.cfg.in
	blender -noaudio --background Smelter.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${SMELTER}

.PHONY: all clean install
