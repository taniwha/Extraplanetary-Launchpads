MICROPAD = \
	MicroPad.png	\
	MicroPad.cfg	\
	MicroPad.mu		\
	$e

all: ${MICROPAD}

install: all
	#nothing to do as GameData makefile will take care of installation

${MICROPAD} : MicroPad.blend MicroPad.cfg.in
	blender -noaudio --background MicroPad.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${MICROPAD}

.PHONY: all clean install
