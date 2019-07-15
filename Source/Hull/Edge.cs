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
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

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

	public Edge reverse { get { return new Edge (mesh, b, a); } }

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

	public bool TouchesPoint (int point)
	{
		if (point == a || point == b) {
			return true;
		}
		Vector3 x = mesh.verts[point] - mesh.verts[a];
		Vector3 v = mesh.verts[b] - mesh.verts[a];
		float vx = Vector3.Dot (v, x);
		float vv = Vector3.Dot (v, v);
		float xx = Vector3.Dot (x, x);
		if (vx > vv || vx < 0) {
			return false;
		}
		float d = (vv * xx - vx * vx) / vv;
		return d < 1e-5f;
	}
}

}
