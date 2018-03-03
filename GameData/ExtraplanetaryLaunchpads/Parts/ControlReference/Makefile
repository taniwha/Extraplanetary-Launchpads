CTRLREF = \
	ControlReference.png	\
	ControlReference.cfg	\
	ControlReference.mu		\
	$e

all: ${CTRLREF}

install: all
	#nothing to do as GameData makefile will take care of installation

${CTRLREF} : ControlReference.blend ControlReference.cfg.in
	blender -noaudio --background ControlReference.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${CTRLREF}

.PHONY: all clean install
