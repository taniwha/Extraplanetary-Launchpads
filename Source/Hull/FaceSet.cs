using System.Collections.Generic;
using UnityEngine;

public class FaceSet
{
	public RawMesh mesh;
	public List<Triangle> faces = new List<Triangle> ();

	public FaceSet (RawMesh mesh, params Triangle []faces)
	{
		this.mesh = mesh;
		for (int i = 0; i < faces.Length; i++) {
			this.faces.Add (faces[i]);
		}
	}

	public Triangle this[int index]
	{
		get {
			return faces[index];
		}
	}

	public int Count => faces.Count;

	public void Add (Triangle face)
	{
		faces.Add (face);
	}

	public void Extend (FaceSet newFaces)
	{
		faces.AddRange (newFaces.faces);
	}

	public Triangle Pop ()
	{
		Triangle face = null;
		if (faces.Count > 0) {
			face = faces[0];
			faces.RemoveAt (0);
		}
		return face;
	}

	public FaceSet LightFaces (Triangle first_face, int point)
	{
		var lit_faces = new FaceSet (mesh);
		if (first_face != null) {
			lit_faces.Add (first_face);
		}
		for (int i = faces.Count; i-- > 0; ) {
			var lf = faces[i];
			if (lf.CanSee (point)) {
				lit_faces.Add (lf);
				faces.RemoveAt (i);
			}
		}
		return lit_faces;
	}

	public HashSet<Edge> FindOuterEdges ()
	{
		var edges = new HashSet<Edge> ();
		for (int i = 0; i < faces.Count; i++) {
			var f = faces[i];
			for (int j = 0; j < 3; j++) {
				if (edges.Contains (f.redges[j])) {
					edges.Remove (f.redges[j]);
				} else {
					edges.Add (f.edges[j]);
				}
			}
		}
		return edges;
	}

	Mesh MakeSubMesh (int start, int count)
	{
		var verts = new Vector3[count * 3];
		var tris = new int[count * 3];

		for (int i = 0; i < count; i++) {
			var f = faces[start + i];
			verts[i*3 + 0] = f.a;
			verts[i*3 + 1] = f.b;
			verts[i*3 + 2] = f.c;
		}
		for (int i = 0; i < 3*count; i++) {
			tris[i] = i;
		}
		var m = new Mesh ();
		m.vertices = verts;
		m.triangles = tris;
		return m;
	}

	// While Unity 2017.3 introduces 32-bit vertex indices, KSP is still on
	// 2017.1, so they're not yet available. The maximum vertex count seems to
	// be 65000, but the generated mesh is simplistic in that vertices are not
	// shared between triangles thus the max faces is 65000/3
	const int maxFaces = 21666;

	public Mesh[] CreateMesh ()
	{
		int numMeshes = (faces.Count + maxFaces - 1) / maxFaces;
		Debug.Log ($"[FaceSet] faces: {faces.Count} meshes: {numMeshes}");
		var meshes = new Mesh[numMeshes];
		for (int i = 0; i < numMeshes; i++) {
			int start = i * maxFaces;
			int count = faces.Count - start;
			if (count > maxFaces) {
				count = maxFaces;
			}
			meshes[i] = MakeSubMesh (start, count);
		}
		return meshes;
	}
}
