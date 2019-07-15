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

public class RawMesh
{
	int addindex;
	public Vector3 []verts;

	public RawMesh (Mesh mesh, Matrix4x4 xform)
	{
		var meshVerts = mesh.vertices;
		verts = new Vector3[meshVerts.Length];
		for (int i = 0; i < meshVerts.Length; i++) {
			verts[i] = xform.MultiplyPoint3x4(meshVerts[i]);
		}
	}

	public RawMesh (int count)
	{
		verts = new Vector3[count];
		addindex = 0;
	}

	public void AppendMesh (Mesh mesh, Matrix4x4 xform)
	{
		var v = mesh.vertices;
		if (addindex + v.Length > verts.Length) {
			var nv = new Vector3[addindex + v.Length];
			for (int i = 0; i < addindex; i++) {
				nv[i] = verts[i];
			}
			verts = nv;
		}
		for (int i = 0; i < v.Length; i++) {
			verts[addindex++] = xform.MultiplyPoint3x4 (v[i]);
		}
	}

	public bool AddVertex (Vector3 vert)
	{
		if (addindex >= verts.Length) {
			return false;
		}
		verts[addindex++] = vert;
		return true;
	}

	public void Write (BinaryWriter bw)
	{
		bw.Write (addindex);
		for (int i = 0; i < addindex; i++) {
			bw.Write (verts[i].x);
			bw.Write (verts[i].y);
			bw.Write (verts[i].z);
		}
	}

	public void Read (BinaryReader br)
	{
		addindex = br.ReadInt32 ();
		verts = new Vector3[addindex];
		for (int i = 0; i < addindex; i++) {
			verts[i].x = br.ReadSingle ();
			verts[i].y = br.ReadSingle ();
			verts[i].z = br.ReadSingle ();
		}
	}
}

}
