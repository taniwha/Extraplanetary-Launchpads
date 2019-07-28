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
	public static string points_path = "/tmp";

	public RawMesh mesh;
	public bool error;

	public Quickhull (RawMesh mesh)
	{
		if (dump_points) {
			Debug.Log($"[Quickhull] points_path: {points_path}");
			var file = File.Open ($"{points_path}/quickhull-points.bin",
								  FileMode.Create);
			var bw = new BinaryWriter (file);
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

		var tri = new Triangle (mesh, a, b, c);
		d = 0;
		bestd = 0;
		for (int i = 0; i < mesh.verts.Length; i++) {
			int p = i;
			if (a == p || b == p || c == p) {
				continue;
			}
			float r = tri.Dist (p);
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
		return new FaceSet (mesh,
							new Triangle (mesh, a, b, c),
							new Triangle (mesh, a, d, b),
							new Triangle (mesh, a, c, d),
							new Triangle (mesh, c, b, d));
	}

	void SplitTriangle (Triangle t, int splitEdge, int point, Connectivity connectivity)
	{
		int a = t.edges[splitEdge].a;
		int b = t.edges[splitEdge].b;
		int c = t.edges[(splitEdge + 1) % 3].b;
		var list = t.Node.List;
		t.Pull ();
		connectivity.Remove (t);
		Triangle nt1 = new Triangle (mesh, a, point, c);
		Triangle nt2 = new Triangle (mesh, point, b, c);
		nt1.vispoints = t.vispoints;
		nt1.height = t.height;
		nt1.highest = t.highest;
		nt2.vispoints = new List<int> (t.vispoints);
		nt2.height = t.height;
		nt2.highest = t.highest;

		list.AddFirst (nt1.Node);
		list.AddFirst (nt2.Node);
		connectivity.Add (nt1);
		connectivity.Add (nt2);
	}

	public FaceSet GetHull ()
	{
		FindExtremePoints ();


		FaceSet faces = FindSimplex ();

		var connectivity = new Connectivity (faces);

		var dupPoints = new HashSet<int> ();

		for (int i = 0; i < mesh.verts.Length; i++) {
			for (var f = faces.First; f != null; f = f.Next) {
				if (f.IsDup (i)) {
					dupPoints.Add (i);
					break;
				}
			}
			if (dupPoints.Contains (i)) {
				continue;
			}
			for (var f = faces.First; f != null; f = f.Next) {
				f.AddPoint (i);
			}
		}
		//Debug.Log($"[Quickhull] dupPoints: {dupPoints.Count}");
		//for (var f = faces.First; f != null; f = f.Next) {
		//	Debug.Log ($"[Quickhull] GetHull {f.vispoints.Count} {f.highest} {f.height}");
		//}

		FaceSet finalFaces = new FaceSet (mesh);

		int iter = 0;
		BinaryWriter bw = null;

		var donePoints = new HashSet<int> ();

		while (faces.Count > 0) {
			//Debug.Log ($"[Quickhull] iteration {iter}");
			if (dump_faces) {
				bw = new BinaryWriter (File.Open ($"/tmp/quickhull-{iter:D5}.bin", FileMode.Create));
				mesh.Write (bw);
				faces.Write (bw);
				finalFaces.Write (bw);
			}
			iter++;
			//int nvis = 0;
			//for (var nf = faces.First; nf != null; nf = nf.Next) {
			//	nvis += nf.vispoints.Count;
			//}
			var f = faces.Pop ();
			//Debug.Log ($"[Quickhull] total vis {nvis} f.vis {f.vispoints.Count} faces {faces.Count}");
			if (f.vispoints.Count < 1) {
				finalFaces.Add (f);
				continue;
			}
			int point = f.vispoints[f.highest];
			//Debug.Log ($"[Quickhull] height {f.height}");
			var litFaces = connectivity.LightFaces (f, point);
			if (dump_faces) {
				bw.Write (point);
				litFaces.Write (bw);
			}
			//Debug.Log ($"[Quickhull] final:{finalFaces.Count} faces:{faces.Count} lit:{litFaces.Count}");
			connectivity.Remove (litFaces);
			var horizonEdges = litFaces.FindOuterEdges ();
			var newFaces = new FaceSet (mesh);
			foreach (Edge e in horizonEdges) {
				if (e.TouchesPoint (point)) {
					var t = connectivity[e.reverse];
					int splitEdge = t.TouchedEdge (point);
					//Debug.Log ($"[Quickhull] point on edge {splitEdge} {faces.Contains (t)} {finalFaces.Contains (t)} {litFaces.Contains (t)}");
					if (splitEdge >= 0) {
						SplitTriangle (t, splitEdge, point, connectivity);
					}
				} else {
					var tri = new Triangle (mesh, e.a, e.b, point);
					newFaces.Add (tri);
					connectivity.Add (tri);
				}
			}
			donePoints.Clear ();
			for (var lf = litFaces.First; lf != null; lf = lf.Next) {
				for (int j = 0; j < lf.vispoints.Count; j++) {
					int p = lf.vispoints[j];
					if (donePoints.Contains (p)) {
						continue;
					}
					donePoints.Add (p);
					for (var nf = newFaces.First; nf != null; nf = nf.Next) {
						if (nf.IsDup (p)) {
							dupPoints.Add (p);
							p = -1;
							break;
						}
					}
					if (p < 0) {
						continue;
					}
					for (var nf = newFaces.First; nf != null; nf = nf.Next) {
						nf.AddPoint (p);
					}
				}
			}
			//Debug.Log($"[Quickhull] dupPoints: {dupPoints.Count}");
			if (dump_faces) {
				newFaces.Write (bw);
			}
			Triangle next;
			for (var nf = newFaces.First; nf != null; nf = next) {
				next = nf.Next;
				if (nf.vispoints.Count > 0) {
					faces.Add (nf);
				} else {
					finalFaces.Add (nf);
				}
			}
			if (dump_faces) {
				bw.Close ();
			}
			if (connectivity.error) {
				var vis = new HashSet<int> ();
				for (var lf = litFaces.First; lf != null; lf = lf.Next) {
					for (int i = 0; i < lf.vispoints.Count; i++) {
						vis.Add (lf.vispoints[i]);
					}
				}
				Debug.Log($"[Quickhull] {litFaces.Count} {vis.Count}");
				for (var lf = litFaces.First; lf != null; lf = lf.Next) {
					float dist1 = float.PositiveInfinity;
					float dist2 = float.PositiveInfinity;
					for (int i = 0; i < 3; i++) {
						float d = lf.edges[i].Distance (point);
						if (d < dist1) {
							dist1 = d;
						}
						d = (mesh.verts[point] - mesh.verts[lf.edges[i].a]).magnitude;
						if (d < dist2) {
							dist2 = d;
						}
					}
					Debug.Log($"    h:{lf.Dist(point)} d1:{dist1} d2:{dist2} {lf.edges[0].TouchesPoint(point)} {lf.edges[1].TouchesPoint(point)} {lf.edges[2].TouchesPoint(point)}");
				}
				break;
			}
		}
		if (dump_faces && !connectivity.error) {
			bw = new BinaryWriter (File.Open ($"/tmp/quickhull-{iter++:D5}.bin", FileMode.Create));
			mesh.Write (bw);
			faces.Write (bw);
			finalFaces.Write (bw);
			bw.Write ((int)-1);
			bw.Write ((int)0);
			bw.Write ((int)0);
			bw.Close ();
		}
		error = connectivity.error;
		return finalFaces;
	}
}

}
