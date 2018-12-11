RECYCLEBIN = \
	recycle.png	\
	recyclebin.mu

RECYCLEBINLARGE = \
	recycle.png	\
	recyclebin-large.mu

all: ${RECYCLEBIN} ${RECYCLEBINLARGE}

install: all
	#nothing to do as GameData makefile will take care of installation

${RECYCLEBIN} : recyclebin.blend
	blender -noaudio --background recyclebin.blend -P ../../../../tools/mass-export.py

${RECYCLEBINLARGE} : recyclebin-large.blend
	blender -noaudio --background recyclebin-large.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${RECYCLEBIN}

.PHONY: all clean install
