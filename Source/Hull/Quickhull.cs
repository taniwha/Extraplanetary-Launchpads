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
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

/*
This is an implementation of the quickhull algorithm based on

The Quickhull Algorithm for Convex Hulls
C. BRADFORD BARBER University of Minnesota
DAVID P. DOBKIN Princeton University
HANNU HUHDANPAA Configured Energy Systems, Inc.
*/

public class Quickhull
{
	public static bool dump_faces;
	public static bool dump_points;

	public RawMesh mesh;

	public Quickhull (RawMesh mesh)
	{
		if (dump_points) {
			var bw = new BinaryWriter(File.Open("/tmp/quickhull-points.bin", FileMode.Create));
			mesh.Write (bw);
			bw.Close ();
		}
		this.mesh = mesh;
	}

	// min x, y, z and max x, y, z
	int []points = new int[6];

	void FindExtremePoints ()
	{
		for (int i = 0; i < mesh.verts.Length; i++) {
			if (mesh.verts[i].x < mesh.verts[points[0]].x) {
				points[0] = i;
			}
			if (mesh.verts[i].y < mesh.verts[points[1]].y) {
				points[1] = i;
			}
			if (mesh.verts[i].z < mesh.verts[points[2]].z) {
				points[2] = i;
			}
			if (mesh.verts[i].x > mesh.verts[points[3]].x) {
				points[3] = i;
			}
			if (mesh.verts[i].y > mesh.verts[points[4]].y) {
				points[4] = i;
			}
			if (mesh.verts[i].z > mesh.verts[points[5]].z) {
				points[5] = i;
			}
		}
	}


	FaceSet FindSimplex ()
	{
		int a, b, c, d;

		a = 0;
		b = 1;
		var tEdge = new Edge (mesh, a, b);
		float bestd = tEdge.vect.sqrMagnitude;
		for (int i = 0; i < 6; i++) {
			int p = points[i];
			for (int j = 0; j < 6; j++) {
				int q = points[j];
				if (q == p
					|| (a == p && b == q)
					|| (a == q && b == p)) {
					continue;
				}
				tEdge.a = p;
				tEdge.b = q;
				float r = tEdge.vect.sqrMagnitude;
				if (r > bestd) {
					a = p;
					b = q;
					bestd = r;
				}
			}
		}

		tEdge.a = a;
		tEdge.b = b;
		bestd = 0;
		c = 0;

		for (int i = 0; i < 6; i++) {
			int p = points[i];
			if (a == p || b == p) {
				continue;
			}
			float r = tEdge.Distance (p);
			if (r > bestd) {
				c = p;
				bestd = r;
			}
		}

		var tri = new Triangle(mesh, a, b, c);
		d = 0;
		bestd = 0;
		for (int i = 0; i < mesh.verts.Length; i++) {
			int p = i;
			if (a == p || b == p || c == p) {
				continue;
			}
			float r = tri.Dist(p);
			if (r*r > bestd*bestd) {
				d = p;
				bestd = r;
			}
		}
		if (bestd > 0) {
			int t = b;
			b = c;
			c = t;
		}
		return new FaceSet(mesh,
						   new Triangle (mesh, a, b, c),
						   new Triangle (mesh, a, d, b),
						   new Triangle (mesh, a, c, d),
						   new Triangle (mesh, c, b, d));
	}

	public FaceSet GetHull ()
	{
		FindExtremePoints ();

		FaceSet faces = FindSimplex ();

		for (int i = 0; i < mesh.verts.Length; i++) {
			for (int j = 0; j < faces.Count; j++) {
				var f = faces[j];
				if (f.AddPoint (i)) {
					//break;	// process the next point
				}
			}
		}
		//for (int i = 0; i < faces.Count; i++) {
		//	var f = faces[i];
		//	Debug.Log($"[Quickhull] GetHull {i} {f.vispoints.Count} {f.highest} {f.height}");
		//}

		FaceSet finalFaces = new FaceSet (mesh);

		int iter = 0;
		BinaryWriter bw = null;

		while (faces.Count > 0) {
			if (dump_faces) {
				bw = new BinaryWriter(File.Open($"/tmp/quickhull-{iter++:D5}.bin", FileMode.Create));
				mesh.Write (bw);
				faces.Write (bw);
				finalFaces.Write (bw);
			}
			//int nvis = 0;
			//for (int i = 0; i < faces.Count; i++) {
			//	nvis += faces[i].vispoints.Count;
			//}
			var f = faces.Pop ();
			//Debug.Log($"[Quickhull] total vis {nvis} f.vis {f.vispoints.Count} faces {faces.Count}");
			if (f.vispoints.Count < 1) {
				finalFaces.Add (f);
				continue;
			}
			int point = f.vispoints[f.highest];
			var litFaces = faces.LightFaces (f, point);
			// light final faces as well so that face merging can be done
			litFaces.Extend (finalFaces.LightFaces (null, point));
			if (dump_faces) {
				litFaces.Write (bw);
			}
			//Debug.Log($"[Quickhull] final:{finalFaces.Count} faces:{faces.Count} lit:{litFaces.Count}");
			var horizonEdges = litFaces.FindOuterEdges ();
			var newFaces = new FaceSet (mesh);
			foreach (Edge e in horizonEdges) {
				newFaces.Add (new Triangle (mesh, e.a, e.b, point));
			}
			for (int i = 0; i < litFaces.Count; i++) {
				var lf = litFaces[i];
				for (int j = 0; j < lf.vispoints.Count; j++) {
					int p = lf.vispoints[j];
					for (int k = 0; k < newFaces.Count; k++) {
						if (newFaces[k].AddPoint (p)) {
							//break;
						}
					}
				}
			}
			if (dump_faces) {
				newFaces.Write (bw);
			}
			for (int i = 0; i < newFaces.Count; i++) {
				var nf = newFaces[i];
				if (nf.vispoints.Count > 0) {
					faces.Add (nf);
				} else {
					finalFaces.Add (nf);
				}
			}
			if (dump_faces) {
				bw.Close ();
			}
		}
		return finalFaces;
	}
}

}
