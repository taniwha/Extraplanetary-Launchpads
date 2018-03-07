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
		CanvasRenderer[] bounds = new CanvasRenderer[6];

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

		internal static Quaternion[] BoundRotations = {
			new Quaternion (0, 0.7f, 0, 0.7f),
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
											typeof (Canvas),
											typeof (CanvasScaler),
											typeof (RectTransform));
			bounds[ind] = go.AddComponent<CanvasRenderer> ();

			RectTransform rxform = go.transform as RectTransform;
			rxform.SetParent (gameObject.transform, false);
			rxform.localPosition = new Vector3 (0, 0, 0);
			rxform.localScale = new Vector3 (0.25f, 0.25f, 0.25f);
			rxform.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, 128);
			rxform.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, 128);

			rxform.localRotation = BoundRotations[ind % 3];

			Image bg = go.AddComponent<Image> ();
			Texture2D tex = GameDatabase.Instance.GetTexture("ExtraplanetaryLaunchpads/Textures/plaque", false);
			bg.sprite = Sprite.Create (tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4 (17, 17, 17, 17));
			bg.type = Image.Type.Sliced;

			go.SetActive(false);
		}

		void Start ()
		{
			for (int i = 0; i < AxisPoints.Length; i++) {
				CreateAxis (i);
				CreateBounds (i);
			}
			gameObject.SetActive (false);	// currently for debug
		}

		IEnumerator WaitAndSetBounds ()
		{
			yield return null;

			for (int i = 0; i < 6; i++) {
				string use = ELSurveyStake.StakeUses[i + 1];
				Vector3d pos;
				if (points.bounds.TryGetValue (use, out pos)) {
					bounds[i].transform.position = points.center + pos;
					bounds[i].gameObject.SetActive (true);
					bounds[i].SetColor (AxisColors[i]);
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
			GameObject go = new GameObject ("EL Virtual Pad");
			var pad = go.AddComponent<EL_VirtualPad> ();
			pad.SetSite (site);
			return pad;
		}
	}
}
