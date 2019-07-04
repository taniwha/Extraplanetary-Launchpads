/*
This file is part of Extraplanetary Launchpads.

Extraplanetary Launchpads is free software: you can redistribute it and/or
modify it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Extraplanetary Launchpads is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Extraplanetary Launchpads.  If not, see
<http://www.gnu.org/licenses/>.
*/
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

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

	Dictionary<int, int> vertexMap;

	Mesh MakeSubMesh (int start, int count)
	{
		vertexMap.Clear ();

		var vertInds = new int[count * 3];
		var tris = new int[count * 3];
		int numVerts = 0;

		for (int i = 0; i < count; i++) {
			var f = faces[start + i];
			for (int j = 0; j < 3; j++) {
				int vi = f.edges[j].a;
				int vo;
				if (!vertexMap.TryGetValue (vi, out vo)) {
					vo = numVerts++;
					vertexMap[vi] = vo;
					vertInds[vo] = vi;
				}
				tris[i*3 + j] = vo;
			}
		}
		var verts = new Vector3[numVerts];
		for (int i = 0; i < numVerts; i++) {
			verts[i] = mesh.verts[vertInds[i]];
		}
		Debug.Log($"[FaceSet] MakeSubMesh v:{numVerts} t:{count}");
		var m = new Mesh ();
		m.vertices = verts;
		m.triangles = tris;
		m.RecalculateBounds();
		m.RecalculateNormals();
		m.RecalculateTangents();
		return m;
	}

	// While Unity 2017.3 introduces 32-bit vertex indices, KSP is still on
	// 2017.1, so they're not yet available. The maximum vertex count seems to
	// be 65000, but the generated mesh is simplistic in that vertices are not
	// shared between triangles thus the max faces is 65000/3
	const int maxFaces = 21666;

	public Mesh[] CreateMesh ()
	{
		vertexMap = new Dictionary<int, int> ();

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
		vertexMap = null;
		return meshes;
	}

	public Box GetBounds ()
	{
		Vector3 min = mesh.verts[faces[0].edges[0].a];
		Vector3 max = mesh.verts[faces[0].edges[0].a];

		for (int i = 0; i < faces.Count; i++) {
			for (int j = 0; j < 3; j++) {
				Vector3 v = mesh.verts[faces[i].edges[j].a];
				if (v.x < min.x) {
					min.x = v.x;
				} else if (v.x > max.x) {
					max.x = v.x;
				}
				if (v.y < min.y) {
					min.y = v.y;
				} else if (v.y > max.y) {
					max.y = v.y;
				}
				if (v.z < min.z) {
					min.z = v.z;
				} else if (v.z > max.z) {
					max.z = v.z;
				}
			}
		}
		return new Box (min, max);
	}


	public void Write(BinaryWriter bw)
	{
		bw.Write(faces.Count);
		for (int i = 0; i < faces.Count; i++) {
			faces[i].Write (bw);
		}
	}
}

}
