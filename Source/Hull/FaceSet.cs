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
		int ind = faces.Count - 1;
		if (ind >= 0) {
			face = faces[ind];
			faces.RemoveAt (ind);
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
				if (edges.Contains (f.redges[i])) {
					edges.Remove (f.redges[i]);
				} else {
					edges.Add (f.edges[i]);
				}
			}
		}
		return edges;
	}

	public Mesh CreateMesh ()
	{
		var verts = new Vector3[faces.Count * 3];
		var tris = new int[faces.Count * 3];

		for (int i = 0; i < faces.Count; i++) {
			var f = faces[i];
			verts[i*3 + 0] = f.a;
			verts[i*3 + 1] = f.b;
			verts[i*3 + 2] = f.c;
		}
		for (int i = 0; i < 3*faces.Count; i++) {
			tris[i] = i;
		}
		var m = new Mesh ();
		m.vertices = verts;
		m.triangles = tris;
		return m;
	}
}
