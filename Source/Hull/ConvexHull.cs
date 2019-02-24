using UnityEngine;

public class ConvexHull
{
	public RawMesh mesh;

	public ConvexHull (RawMesh mesh)
	{
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
					break;	// process the next point
				}
			}
		}
		//for (int i = 0; i < faces.Count; i++) {
		//	var f = faces[i];
		//	Debug.Log($"[ConvexHull] GetHull {i} {f.vispoints.Count} {f.highest} {f.height}");
		//}

		FaceSet finalFaces = new FaceSet (mesh);

		while (faces.Count > 0) {
			int nvis = 0;
			for (int i = 0; i < faces.Count; i++) {
				nvis += faces[i].vispoints.Count;
			}
			var f = faces.Pop ();
			//Debug.Log($"[ConvexHull] total vis {nvis} f.vis {f.vispoints.Count} faces {faces.Count}");
			if (f.vispoints.Count < 1) {
				finalFaces.Add (f);
				continue;
			}
			int point = f.vispoints[f.highest];
			var litFaces = faces.LightFaces (f, point);
			// light final faces as well so that face merging can be done
			litFaces.Extend (finalFaces.LightFaces (null, point));
			//Debug.Log($"[ConvexHull] final:{finalFaces.Count} faces:{faces.Count} lit:{litFaces.Count}");
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
							break;
						}
					}
				}
			}
			for (int i = 0; i < newFaces.Count; i++) {
				var nf = newFaces[i];
				if (nf.vispoints.Count > 0) {
					faces.Add (nf);
				} else {
					finalFaces.Add (nf);
				}
			}
		}
		return finalFaces;
	}
}
