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
using System.Text;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

	public class CraftHull
	{
		string md5sum;
		Box bounds;
		Vector3 boundsOrigin;
		Vector3 position;
		Quaternion rotation = Quaternion.identity;
		// It's not actually expected that there will be more than one mesh,
		// but if the quickhull algorithm breaks down, it can produce a nasty
		// hedgehog with more vertices than can fit in a single mesh.
		Mesh []hullMeshes;
		bool hullError;

		const int magic = 0x31337001;

		public CraftHull ()
		{
		}

		public CraftHull (string craftFile)
		{
			HashCraft (craftFile);
		}

		public void SetBox (Box b, Vector3 o)
		{
			bounds = new Box(b);
			boundsOrigin = o;
		}

		public void SetTransform (Transform transform)
		{
			position = transform.position;
			rotation = transform.rotation;
		}

		public void RestoreTransform (Transform transform)
		{
			transform.position = position;
			transform.rotation = rotation;
		}

		public void HashCraft (string craftFile)
		{
			var md5Hash = MD5.Create ();
			byte[] data = md5Hash.ComputeHash (Encoding.UTF8.GetBytes (craftFile));

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < data.Length; i++) {
				sb.Append (data[i].ToString("x2"));
			}
			md5sum = sb.ToString();
		}

		public void Load (ConfigNode node)
		{
			md5sum = node.GetValue ("CraftHullSum");
			node.TryGetValue ("position", ref position);
			node.TryGetValue ("rotation", ref rotation);
			node.TryGetValue ("hullError", ref hullError);
			if (node.HasNode ("bounds")) {
				bounds = new Box (node.GetNode ("bounds"));
				node.TryGetValue ("boundsOrigin", ref boundsOrigin);
			}
		}

		public void Save (ConfigNode node)
		{
			if (!string.IsNullOrEmpty(md5sum)) {
				node.AddValue ("CraftHullSum", md5sum);
			}
			node.AddValue ("position", KSPUtil.WriteVector (position));
			node.AddValue ("rotation", KSPUtil.WriteQuaternion (rotation));
			node.AddValue ("hullError", hullError);
			if (bounds != null) {
				bounds.Save (node.AddNode ("bounds"));
				node.AddValue ("boundsOrigin", boundsOrigin);
			}
		}

		Mesh ReadMesh (BinaryReader br)
		{
			int numVerts = br.ReadInt32 ();
			int numTris = br.ReadInt32 ();

			if (numTris != (numVerts * 2 - 4) * 3) {
				Debug.LogWarning ($"[CraftHull] mis-match between verts and tris: {numVerts} {numTris}");
			}

			var verts = new Vector3[numVerts];
			var tris = new int[numTris];
			for (int i = 0; i < numVerts; i++) {
				verts[i].x = br.ReadSingle();
				verts[i].y = br.ReadSingle();
				verts[i].z = br.ReadSingle();
			}
			for (int i = 0; i < numTris; i++) {
				tris[i] = br.ReadInt32 ();
			}

			var mesh = new Mesh ();
			mesh.vertices = verts;
			mesh.triangles = tris;
			mesh.RecalculateBounds();
			mesh.RecalculateNormals();
			mesh.RecalculateTangents();
			return mesh;
		}

		public bool LoadHull (string path)
		{
			path = $"{path}/CraftHull-{md5sum}.dat";

			if (!File.Exists (path)) {
				Debug.LogError ($"[CraftHull] {path} does not exist");
				hullError = true;
				return false;
			}

			var br = new BinaryReader (File.Open(path, FileMode.Open));
			if (br == null) {
				Debug.LogError ($"[CraftHull] could not open {path}");
				hullError = true;
				return false;
			}

			int m = br.ReadInt32 ();
			if (m != magic) {
				Debug.LogError ($"[CraftHull] {path} incorrect magic number");
				br.Close ();
				hullError = true;
				return false;
			}

			int numMeshes = br.ReadInt32 ();
			hullMeshes = new Mesh[numMeshes];
			for (int i = 0; i < numMeshes; i++) {
				hullMeshes[i] = ReadMesh (br);
			}

			br.Close ();
			return true;
		}

		public void MakeBoxHull ()
		{
			var timer = System.Diagnostics.Stopwatch.StartNew ();
			Debug.Log($"[CraftHull] MakeBoxHull 8 verts to process");
			var min = bounds.min - boundsOrigin;
			var max = bounds.max - boundsOrigin;
			var rawMesh = new RawMesh (8);
			rawMesh.AddVertex (min);
			rawMesh.AddVertex (new Vector3(min.x, min.y, max.z));
			rawMesh.AddVertex (new Vector3(min.x, max.y, min.z));
			rawMesh.AddVertex (new Vector3(min.x, max.y, max.z));
			rawMesh.AddVertex (new Vector3(max.x, min.y, min.z));
			rawMesh.AddVertex (new Vector3(max.x, min.y, max.z));
			rawMesh.AddVertex (new Vector3(max.x, max.y, min.z));
			rawMesh.AddVertex (max);

			var hull = new Quickhull (rawMesh);
			var hullFaces = hull.GetHull ();
			Debug.Log($"[CraftHull] MakeBoxHull {hullFaces.Count} hull faces");
			hullMeshes = hullFaces.CreateMesh ();

			timer.Stop();
			Debug.Log($"[CraftHull] MakeBoxHull {timer.ElapsedMilliseconds}ms");
		}

		void WriteMesh (BinaryWriter bw, Mesh mesh)
		{
			var verts = mesh.vertices;
			var tris = mesh.triangles;

			bw.Write (verts.Length);
			bw.Write (tris.Length);
			for (int i = 0; i < verts.Length; i++) {
				bw.Write (verts[i].x);
				bw.Write (verts[i].y);
				bw.Write (verts[i].z);
			}
			for (int i = 0; i < tris.Length; i++) {
				bw.Write (tris[i]);
			}
		}

		public bool SaveHull (string path)
		{
			path = $"{path}/CraftHull-{md5sum}.dat";

			var bw = new BinaryWriter (File.Open (path, FileMode.Create));
			if (bw == null) {
				Debug.LogError ($"[CraftHull] could not open {path} for writing");
				return false;
			}
			bw.Write (magic);
			bw.Write (hullMeshes.Length);
			for (int i = 0; i < hullMeshes.Length; i++) {
				WriteMesh (bw, hullMeshes[i]);
			}
			bw.Close ();
			return true;
		}

		Vector3 BakeVert (Vector3 vert, Matrix4x4 []xforms, int bone, float weight)
		{
			return xforms[bone].MultiplyPoint3x4 (vert) * weight;
		}

		void BakeMesh (SkinnedMeshRenderer smr, Mesh mesh)
		{
			var xforms = new Matrix4x4[smr.bones.Length];
			var root = smr.transform.worldToLocalMatrix;

			for (int i = 0; i < smr.bones.Length; i++) {
				var bone = smr.bones[i].localToWorldMatrix;
				var bp = smr.sharedMesh.bindposes[i];
				xforms[i] = root * bone * bp;
			}

			var mv = smr.sharedMesh.vertices;
			var verts = new Vector3[mv.Length];
			for (int i = 0; i < mv.Length; i++) {
				var bw = smr.sharedMesh.boneWeights[i];
				verts[i] += BakeVert(mv[i], xforms, bw.boneIndex0, bw.weight0);
				verts[i] += BakeVert(mv[i], xforms, bw.boneIndex1, bw.weight1);
				verts[i] += BakeVert(mv[i], xforms, bw.boneIndex2, bw.weight2);
				verts[i] += BakeVert(mv[i], xforms, bw.boneIndex3, bw.weight3);
			}
			mesh.vertices = verts;
		}

		bool IsActive(MeshFilter mf)
		{
			var go = mf.gameObject;
			var renderer = go.GetComponent<Renderer>();
			if (renderer != null) {
				return renderer.enabled;
			}
			return false;
		}

		public void BuildConvexHull (Vessel craftVessel)
		{
			var timer = System.Diagnostics.Stopwatch.StartNew ();

			var meshFilters = craftVessel.GetComponentsInChildren<MeshFilter> (false);
			var skinnedMeshRenderers = craftVessel.GetComponentsInChildren<SkinnedMeshRenderer> (false);

			int numVerts = 0;

			for (int i = 0; i < meshFilters.Length; i++) {
				if (!IsActive(meshFilters[i])) {
					continue;
				}
				numVerts += meshFilters[i].sharedMesh.vertices.Length;
			}
			for (int i = 0; i < skinnedMeshRenderers.Length; i++) {
				if (!skinnedMeshRenderers[i].enabled) {
					continue;
				}
				numVerts += skinnedMeshRenderers[i].sharedMesh.vertices.Length;
			}

			Debug.Log($"[CraftHull] BuildConvexHull {numVerts} verts to process");
			var rawMesh = new RawMesh (numVerts);
			var rootXform = craftVessel.parts[0].localRoot.transform.worldToLocalMatrix;

			for (int i = 0; i < meshFilters.Length; i++) {
				var mf = meshFilters[i];
				if (!IsActive(mf)) {
					continue;
				}
				var xform = rootXform * mf.transform.localToWorldMatrix;
				rawMesh.AppendMesh (mf.sharedMesh, xform);
			}

			var m = new Mesh ();
			for (int i = 0; i < skinnedMeshRenderers.Length; i++) {
				var smr = skinnedMeshRenderers[i];
				if (!smr.enabled) {
					continue;
				}
				var xform = rootXform * smr.transform.localToWorldMatrix;
				BakeMesh (smr, m);
				rawMesh.AppendMesh (m, xform);
			}
			UnityEngine.Object.Destroy (m);

			var hull = new Quickhull (rawMesh);
			var hullFaces = hull.GetHull ();
			hullError = hull.error;
			Debug.Log($"[CraftHull] BuildConvexHull hullError:{hullError}");
			Debug.Log($"[CraftHull] BuildConvexHull {hullFaces.Count} hull faces");
			hullMeshes = hullFaces.CreateMesh ();

			timer.Stop();
			Debug.Log($"[CraftHull] BuildConvexHull {timer.ElapsedMilliseconds}ms");
		}

		public GameObject CreateHull(string name)
		{
			var hullObject = new GameObject (name);
			for (int i = 0; i < hullMeshes.Length; i++) {
				var go = new GameObject ($"{name}:hull mesh.{i}",
										 typeof (MeshRenderer),
										 typeof (MeshFilter));
				go.transform.SetParent (hullObject.transform, false);

				var meshFilter = go.GetComponent<MeshFilter> ();
				meshFilter.mesh = hullMeshes[i];

				var renderer = go.GetComponent<Renderer> ();
				renderer.material = new Material (Shader.Find("Diffuse"));
				var color = XKCDColors.MossGreen;
				if (hullError) {
					color = XKCDColors.DustyRed;
				}
				renderer.material.color = color;
			}
			hullObject.transform.position = position;
			hullObject.transform.rotation = rotation;
			return hullObject;
		}

		public void PlaceHull (ELBuildControl.IBuilder builder, GameObject hullObject)
		{
			builder.PlaceShip (hullObject.transform, bounds);
			hullObject.transform.SetParent (builder.part.transform, true);
		}

		public void Destroy ()
		{
			if (hullMeshes != null) {
				for (int i = 0; i < hullMeshes.Length; i++) {
					UnityEngine.Object.Destroy (hullMeshes[i]);
				}
			}
		}
	}
}
