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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	public class EL_VirtualPad : MonoBehaviour
	{
		Points points;
		Renderer[] bounds = new Renderer[6];
		Mesh[] boundMeshes = new Mesh[6];

		internal static Color[] AxisColors = {
			XKCDColors.CherryRed,			// +X
			XKCDColors.FluorescentGreen,	// +Y
			XKCDColors.BrightSkyBlue,		// +Z
			XKCDColors.RustyOrange,			// -X
			XKCDColors.MossGreen,			// -Y
			XKCDColors.DeepSkyBlue,			// -Z
		};

		internal static Vector3[] AxisPoints = {
			Vector3.right, Vector3.up, Vector3.forward,
			Vector3.left, Vector3.down, Vector3.back,
		};

		internal static Vector3[] BoundVertices = {
			new Vector3 (-1, -1, 0),
			new Vector3 (-1,  1, 0),
			new Vector3 ( 1,  1, 0),
			new Vector3 ( 1, -1, 0),
		};
		// working set of vertices to avoid generating garbage
		static Vector3[] boundVertices = new Vector3[4];

		internal static int[] BoundTriangles = {
			0, 1, 2,	// front face
			0, 2, 3,	// front face
			0, 3, 2,	// back face
			0, 2, 1,	// back face
		};
		internal static Quaternion[] BoundRotations = {
			new Quaternion (0, -0.7f, 0, 0.7f),
			new Quaternion (0.7f, 0, 0, 0.7f),
			new Quaternion (0, 0, 0, 1),
		};

		void Awake ()
		{
		}

		void OnDestroy ()
		{
		}

		void CreateAxis (int ind)
		{
			Color color = AxisColors[ind];
			Vector3 end = AxisPoints[ind];

			GameObject go = new GameObject ("EL Virtual Pad axis");
			go.transform.SetParent (gameObject.transform, false);
			LineRenderer line = go.AddComponent<LineRenderer> ();
			Renderer rend = go.GetComponent<Renderer> ();
			rend.material = new Material (Shader.Find("Particles/Additive"));
			line.useWorldSpace = false;
			line.startWidth = 0.5f;
			line.endWidth = 0.5f;
			line.positionCount = 2;
			line.startColor = color;
			line.endColor = color;
			line.SetPosition (0, Vector3.zero);
			line.SetPosition (1, end * 5);
		}

		void CreateBounds (int ind)
		{
			GameObject go = new GameObject ("EL Virtual Pad bounds",
											typeof (MeshRenderer),
											typeof (MeshFilter));
			bounds[ind] = go.GetComponent<Renderer> ();
			bounds[ind].material = new Material (Shader.Find("Transparent/Diffuse"));

			var meshFilter = go.GetComponent<MeshFilter> ();
			var mesh = new Mesh();
			mesh.vertices = BoundVertices;
			mesh.triangles = BoundTriangles;
			meshFilter.mesh = mesh;
			boundMeshes[ind] = mesh;

			var xform = go.transform as Transform;
			xform.SetParent (gameObject.transform, false);
			xform.localPosition = new Vector3 (0, 0, 0);
			xform.localScale = new Vector3 (1, 1, 1);

			xform.localRotation = BoundRotations[ind % 3];

			go.SetActive(false);
		}

		void Start ()
		{
			for (int i = 0; i < AxisPoints.Length; i++) {
				CreateAxis (i);
				CreateBounds (i);
			}
			gameObject.SetActive (true);	// currently for debug
		}

		static int[] TopMap = { 1, 2, 1 };
		static int[] BotMap = { 4, 5, 4 };
		static int[] LeftMap = { 5, 3, 3 };
		static int[] RightMap = { 2, 0, 0 };

		void ShapeBounds (int ind, Vector3 center, Quaternion orientation)
		{
			int ti = TopMap[ind % 3];
			int bi = BotMap[ind % 3];
			int li = LeftMap[ind % 3];
			int ri = RightMap[ind % 3];
			string tn = ELSurveyStake.StakeUses[ti + 1];
			string bn = ELSurveyStake.StakeUses[bi + 1];
			string ln = ELSurveyStake.StakeUses[li + 1];
			string rn = ELSurveyStake.StakeUses[ri + 1];
			Debug.Log ($"[EL_VirtualPad] ShapeBounds {ind} {ti}:{tn} {bi}:{bn} {li}:{ln} {ri}:{rn}");
			Vector2 min, max;
			Vector3d pos, dir;
			if (points.bounds.TryGetValue (tn, out pos)) {
				dir = orientation * (pos - center);
				Debug.Log ($"    {tn} {pos} {dir}");
				max.y = (float) (dir)[ti % 3];
			} else {
				max.y = 5;
			}
			if (points.bounds.TryGetValue (bn, out pos)) {
				dir = orientation * (pos - center);
				Debug.Log ($"    {bn} {pos} {dir}");
				min.y = (float) (dir)[bi % 3];
			} else {
				min.y = -5;
			}
			if (points.bounds.TryGetValue (ln, out pos)) {
				dir = orientation * (pos - center);
				Debug.Log ($"    {ln} {pos} {dir}");
				min.x = (float) (dir)[li % 3];
			} else {
				min.x = -5;
			}
			if (points.bounds.TryGetValue (rn, out pos)) {
				dir = orientation * (pos - center);
				Debug.Log ($"    {rn} {pos} {dir}");
				max.x = (float) (dir)[ri % 3];
			} else {
				max.x = 5;
			}
			Debug.Log ($"    {min} {max}");
			boundVertices[0].x = min.x;
			boundVertices[0].y = min.y;
			boundVertices[1].x = min.x;
			boundVertices[1].y = max.y;
			boundVertices[2].x = max.x;
			boundVertices[2].y = max.y;
			boundVertices[3].x = max.x;
			boundVertices[3].y = min.y;
			boundMeshes[ind].vertices = boundVertices;
			boundMeshes[ind].triangles = BoundTriangles;
		}

		IEnumerator WaitAndSetBounds ()
		{
			yield return null;

			var orientation = Quaternion.Inverse (points.GetOrientation ());
			for (int i = 0; i < 6; i++) {
				string use = ELSurveyStake.StakeUses[i + 1];
				Vector3d pos;
				Debug.Log ($"[EL_VirtualPad] WaitAndSetBounds {i} {use}");
				if (points.bounds.TryGetValue (use, out pos)) {
					ShapeBounds(i, pos, orientation);
					bounds[i].transform.position = points.center + pos;
					bounds[i].gameObject.SetActive (true);
					var color = AxisColors[i];
					color.a = 0.5f;
					bounds[i].material.color = color;
				} else {
					bounds[i].gameObject.SetActive (false);
				}
			}
		}

		internal void SetSite (SurveySite site)
		{
			points = new Points (site);
			transform.position = points.center;
			transform.rotation = points.GetOrientation ();
			if (gameObject.activeInHierarchy) {
				StartCoroutine (WaitAndSetBounds ());
			}
		}

		internal static EL_VirtualPad Create (SurveySite site)
		{
			GameObject go = GameObject.Find ("EL Virtual Pad");
			if (go == null) {
				go = new GameObject ("EL Virtual Pad");
			} else {
				Debug.Log ("[EL_VirtualPad] oi, clean up!");
			}
			var pad = go.AddComponent<EL_VirtualPad> ();
			pad.SetSite (site);
			return pad;
		}
	}
}
