MCSFLAGS = -optimize -warnaserror -unsafe

cstest_sources = TestHarness.cs Box.cs Connectivity.cs Edge.cs FaceSet.cs Quickhull.cs RawMesh.cs Triangle.cs

cstest = test

all: $(cstest)
$(cstest): $(cstest_sources)
	mcs $(MCSFLAGS) -out:$@ $^

clean:
	rm -rf $(cstest)
