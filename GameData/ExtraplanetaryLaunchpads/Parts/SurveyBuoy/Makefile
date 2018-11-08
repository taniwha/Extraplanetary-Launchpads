SURVEYBUOY = \
	SurveyBuoy.png	\
	SurveyBuoy.cfg	\
	SurveyBuoy.mu		\
	$e

all: ${SURVEYBUOY}

install: all
	#nothing to do as GameData makefile will take care of installation

${SURVEYBUOY} : SurveyBuoy.blend
	blender -noaudio --background SurveyBuoy.blend -P ../../../../tools/mass-export.py

clean:
	rm -f ${SURVEYBUOY}

.PHONY: all clean install
