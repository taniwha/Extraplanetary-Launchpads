using System.IO;
using UnityEngine;

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

	public void Write (BinaryWriter bw)
	{
		bw.Write(addindex);
		for (int i = 0; i < addindex; i++) {
			bw.Write(verts[i].x);
			bw.Write(verts[i].y);
			bw.Write(verts[i].z);
		}
	}
}
