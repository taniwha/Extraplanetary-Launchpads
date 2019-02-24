using UnityEngine;

public struct Edge
{
	public int a, b;
	public RawMesh mesh;

	public override int GetHashCode ()
	{
		return (a > b) ? (a * a + a + b) : (a + b * b);
	}

	public override bool Equals(object obj)
	{
		if (obj is Edge) {
			var e = (Edge) obj;
			return a == e.a && b == e.b;
		}
		return false;
	}

	public Edge (RawMesh mesh, int a, int b)
	{
		this.mesh = mesh;
		this.a = a;
		this.b = b;
	}

	public Vector3 vect { get { return mesh.verts[b] - mesh.verts[a]; } }
	public Vector3 rvect { get { return mesh.verts[a] - mesh.verts[b]; } }
	public float Distance (int point)
	{
		var p = mesh.verts[a];
		var v = mesh.verts[b] - p;
		var x = mesh.verts[point] - p;
		var vv = Vector3.Dot (v, v);
		var xv = Vector3.Dot (x, v);
		return (vv * Vector3.Dot (x, x) - xv * xv) / vv;
	}
}
