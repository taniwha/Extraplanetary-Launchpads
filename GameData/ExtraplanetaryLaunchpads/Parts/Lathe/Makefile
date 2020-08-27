LATHE = \
	Lathe.cfg	\
	Lathe.mu	\
	Lathe.png	\
	Lathe_NRM.png	\
	$e

LATHE_DDS = \
	Lathe.dds	\
	Lathe_NRM.dds	\
	$e

all: ${LATHE} ${LATHE_DDS}

Lathe.dds: Lathe.png
	nvcompress -bc1 -alpha -color $< $@
Lathe_NRM.dds: Lathe_NRM.png
	nvcompress -bc3 -alpha -normal $< $@

install: all
	#nothing to do as GameData makefile will take care of installation

${LATHE} : Lathe.blend Lathe.cfg.in
	blender -noaudio --background Lathe.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${LATHE} ${LATHE_DDS}

.PHONY: all clean install
