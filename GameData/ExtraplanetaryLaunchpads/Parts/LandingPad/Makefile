LANDINGPAD = \
	LandingPad.png	\
	LandingPad.cfg	\
	LandingPad.mu		\
	$e

all: ${LANDINGPAD}

install: all
	#nothing to do as GameData makefile will take care of installation

${LANDINGPAD} : LandingPad.blend LandingPad.cfg.in
	blender -noaudio --background LandingPad.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${LANDINGPAD}

.PHONY: all clean install
