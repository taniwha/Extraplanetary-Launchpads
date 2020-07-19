LATHE = \
	Lathe.png	\
	Lathe.cfg	\
	Lathe.mu	\
	$e

all: ${LATHE}

install: all
	#nothing to do as GameData makefile will take care of installation

${LATHE} : Lathe.blend Lathe.cfg.in
	blender -noaudio --background Lathe.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${LATHE}

.PHONY: all clean install
