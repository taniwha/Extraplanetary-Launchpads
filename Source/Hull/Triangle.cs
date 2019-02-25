using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class Triangle
{
	public const float epsilon = 1e-3f;

	public RawMesh mesh;
	public Vector3 a, b, c, n;
	public Edge[] edges = new Edge[3];
	public Edge[] redges = new Edge[3];
	public List<int> vispoints = new List<int> ();
	public float height;
	public int highest;

	public Triangle (RawMesh mesh, int a, int b, int c)
	{
		this.mesh = mesh;
		edges[0] = new Edge (mesh, a, b);
		edges[1] = new Edge (mesh, b, c);
		edges[2] = new Edge (mesh, c, a);
		redges[0] = new Edge (mesh, b, a);
		redges[1] = new Edge (mesh, c, b);
		redges[2] = new Edge (mesh, a, c);
		this.a = mesh.verts[a];
		this.b = mesh.verts[b];
		this.c = mesh.verts[c];
		n = Vector3.Cross (edges[2].vect, edges[0].vect).normalized;
	}

	public float Dist (int point)
	{
		Vector3 p = mesh.verts[point];
		return Vector3.Dot (p - a, n);
	}

	public bool CanSee (int point)
	{
		return Dist (point) >= 0;
	}

	public bool AddPoint (int point)
	{
		// CanSee is not used here because CanSee includes points on the
		// triangle's plane (not a problem, but suboptimal) and the height
		// is needed anyway as in the end, the highest point is desirned.
		float d = Dist (point);
		if (d > epsilon) {
			if (d > height) {
				height = d;
				highest = vispoints.Count;
			}
			vispoints.Add (point);
			return true;
		}
		return false;
	}

	public void Write (BinaryWriter bw)
	{
		bw.Write (edges[0].a);
		bw.Write (edges[1].a);
		bw.Write (edges[2].a);
		bw.Write (highest);
		bw.Write (vispoints.Count);
		for (int i = 0; i < vispoints.Count; i++) {
			bw.Write (vispoints[i]);
		}
	}
}
