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
	class FaceList : LinkedList<Triangle> { }
	FaceList faces = new FaceList ();
	public RawMesh mesh;

	public FaceSet (RawMesh mesh, params Triangle []faces)
	{
		this.mesh = mesh;
		for (int i = 0; i < faces.Length; i++) {
			faces[i].Pull ();
			this.faces.AddFirst (faces[i].Node);
		}
	}

	public int Count => faces.Count;

	public Triangle First
	{
		get {
			var first = faces.First;
			return first != null ? first.Value : null;
		}
	}

	public bool Contains (Triangle face)
	{
		return face.Node.List == faces;
	}

	public void Add (Triangle face)
	{
		face.Pull ();
		faces.AddFirst (face.Node);
	}

	public void Extend (FaceSet newFaces)
	{
		Triangle face, next;
		for (face = newFaces.First; face != null; face = next) {
			next = face.Next;
			Add (face);
		}
	}

	public Triangle Pop ()
	{
		Triangle face = First;
		if (face != null) {
			faces.Remove (face.Node);
		}
		return face;
	}

	public HashSet<Edge> FindOuterEdges ()
	{
		var edges = new HashSet<Edge> ();
		for (var f = First; f != null; f = f.Next) {
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

	Mesh MakeSubMesh (ref Triangle face, int count)
	{
		vertexMap.Clear ();

		var vertInds = new int[count * 3];
		var tris = new int[count * 3];
		int numVerts = 0;

		for (int i = 0; i < count; i++) {
			for (int j = 0; j < 3; j++) {
				int vi = face.edges[j].a;
				int vo;
				if (!vertexMap.TryGetValue (vi, out vo)) {
					vo = numVerts++;
					vertexMap[vi] = vo;
					vertInds[vo] = vi;
				}
				tris[i*3 + j] = vo;
			}
			face = face.Next;
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
		Triangle face = First;
		int count = faces.Count;
		for (int i = 0; i < numMeshes; i++) {
			int c = count;
			if (c > maxFaces) {
				c = maxFaces;
			}
			count -= c;
			meshes[i] = MakeSubMesh (ref face, c);
		}
		vertexMap = null;
		return meshes;
	}

	public Box GetBounds ()
	{
		Triangle face = First;
		if (face == null) {
			return new Box (Vector3.zero, Vector3.zero);
		}

		Vector3 min = mesh.verts[face.edges[0].a];
		Vector3 max = mesh.verts[face.edges[0].a];

		while (face != null) {
			for (int j = 0; j < 3; j++) {
				Vector3 v = mesh.verts[face.edges[j].a];
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
		for (var face = First; face != null; face = face.Next) {
			face.Write (bw);
		}
	}
}

}
