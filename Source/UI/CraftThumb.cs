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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftThumb : UIImage
	{
		static Texture2D genericCraftThumb;

		public override void CreateUI ()
		{
			if (!genericCraftThumb) {
				genericCraftThumb = AssetBase.GetTexture("craftThumbGeneric");
			}

			base.CreateUI ();

			this.SizeDelta (256, 256)
				.MinSize (256, 256)
				.PreferredSize (256, 256)
				;
		}

		protected override void OnDestroy ()
		{
			Destroy (image.sprite);
			base.OnDestroy ();
		}

		const int thumbLayer = 8;
		const int thumbMask = 1 << thumbLayer;
		const float thumbLightDist = 40;
		const float thumbFov = 30;
		const int thumbResolution = 256;
		const int thumbBits = 24;

		static Vector2 angles;
		static float fovFactor = 0.9f;

		static GameObject thumbRig;
		static GameObject thumbPivot;
		static Camera thumbCamera;

		static Vector3 []tetraHedron = {
			new Vector3 ( 1, 1, 1),
			new Vector3 (-1, 1,-1),
			new Vector3 (-1,-1, 1),
			new Vector3 ( 1,-1,-1),
		};

		static void CreateRig ()
		{
			thumbRig = new GameObject ("Thumb Rig");

			thumbPivot = new GameObject ("Thumb Pivot");
			thumbPivot.transform.SetParent (thumbRig.transform, false);

			var camObj = new GameObject ("Thumb Camera");
			camObj.transform.SetParent (thumbPivot.transform, false);

			thumbCamera = camObj.AddComponent<Camera> ();
			thumbCamera.enabled = false;	// disable automatic rendering
			thumbCamera.cullingMask = thumbMask;
			thumbCamera.clearFlags = CameraClearFlags.SolidColor;
			thumbCamera.backgroundColor = UnityEngine.Color.clear;
			thumbCamera.fieldOfView = thumbFov;

			for (int i = 0; i < tetraHedron.Length; i++) {
				var lightObj = new GameObject ("Thumb Light");
				lightObj.transform.SetParent (thumbRig.transform, false);
				lightObj.transform.localPosition = tetraHedron[i] * thumbLightDist;
				lightObj.transform.localRotation = Quaternion.LookRotation (-tetraHedron[i], Vector3.up);
				Debug.Log ($"[ELCraftThumb] CreateRig light {i} {lightObj.transform.localPosition} {lightObj.transform.forward}");

				var light = lightObj.AddComponent<Light> ();
				light.cullingMask = thumbMask;
				light.colorTemperature = 6570;
				light.type = LightType.Spot;
				light.spotAngle = 55;
				light.range = thumbLightDist * 10;
				light.intensity = 1;
			}

			EL_Utils.SetLayer (thumbRig, thumbLayer, true);
		}

		static Texture2D takeSnapshot (ShipConstruct ship)
		{
			Vector3 size = ShipConstruction.CalculateCraftSize (ship);
			float dist = KSPCameraUtil.GetDistanceToFit (size, thumbFov * fovFactor);
			thumbRig.transform.position = ShipConstruction.FindCraftCenter (ship, true);
			thumbCamera.transform.localPosition = new Vector3 (0, 0, -dist);
			thumbPivot.transform.localRotation = Quaternion.AngleAxis (angles.y, Vector3.up) * Quaternion.AngleAxis (angles.x, Vector3.right);

			var rect = new Rect (0, 0, thumbResolution, thumbResolution);
			var buffer = new RenderTexture ((int) rect.width, (int) rect.height, thumbBits, RenderTextureFormat.Default);
			thumbCamera.targetTexture = buffer;
			thumbCamera.Render ();

			var saveActive = RenderTexture.active;
			RenderTexture.active = buffer;
			var tex = new Texture2D ((int) rect.width, (int) rect.height, TextureFormat.ARGB32, false);
			tex.ReadPixels (rect, 0, 0);

			RenderTexture.active = saveActive;
			buffer.Release ();
			GameObject.Destroy (buffer);

			return tex;
		}

		static IEnumerator capture (ShipConstruct ship, string thumbName)
		{
			yield return null;

			if (!thumbRig) {
				// it died
				yield break;
			}

			var tex = takeSnapshot (ship);
			var png = tex.EncodeToPNG ();
			GameObject.Destroy (tex);

			string dir = KSPUtil.ApplicationRootPath + "thumbs/";
			string path = dir + thumbName + ".png";

			if (!Directory.Exists (dir)) {
				Directory.CreateDirectory (dir);
			}
			File.WriteAllBytes (path, png);

			Debug.Log ($"[ELCraftThumb] capture {path}");

			for (int i = ship.parts.Count; i-- > 0; ) {
				GameObject.Destroy (ship.parts[i].gameObject);
			}

			thumbRig.SetActive (false);
		}

		public static void Capture (ConfigNode craft, ELCraftType craftType, string craftFile)
		{
			var ship = new ShipConstruct ();
			ship.LoadShip (craft);

			if (ship.vesselDeltaV != null) {
				// The delta-v module is not needed. It has its own gameObject
				// for ShipConstruct.
				GameObject.Destroy (ship.vesselDeltaV.gameObject);
				ship.vesselDeltaV = null;
			}
			for (int i = ship.parts.Count; i-- > 0; ) {
				// Need to keep the parts around for a few frames, but
				// Part.FixedUpdate throws if things are not set up properly
				// (which they won't be), but setting the state to PLACEMENT
				// gets around this by tricking the part into thinking its
				// being placed in the editor
				ship.parts[i].State = PartStates.PLACEMENT;

				// Make sure the part and main scene won't interact physcally
				// or graphically (thumbLayer is chosen to be a layer that is
				// not rendered by the scene cameras), and removing the
				// colliders ensures that the parts can't collide with anything
				EL_Utils.RemoveColliders (ship.parts[i].gameObject);
				EL_Utils.SetLightMasks (ship.parts[i].gameObject, thumbMask);
				EL_Utils.SetLayer (ship.parts[i].gameObject, thumbLayer, true);
			}

			if (craftType == ELCraftType.SPH) {
				angles = new Vector2 (35, 135);
			} else {
				angles = new Vector2 (45, 45);
			}

			string thumbName = UserThumbName (craftType, craftFile);
			if (!thumbRig) {
				CreateRig ();
			}
			thumbRig.SetActive (true);
			// Need to wait a frame for the parts to be renderable
			// (don't know why, but my guess is to give the various renderers
			// a chance to set themselves up)
			HighLogic.fetch.StartCoroutine (capture (ship, thumbName));
		}

		public static string UserThumbName (ELCraftType craftType, string craftFile)
		{
			string saveDir = HighLogic.SaveFolder;
			string type = craftType.ToString ();
			string thumbName = Path.GetFileNameWithoutExtension(craftFile);
			return $"{saveDir}_{type}_{thumbName}";
		}

		public static string StockPath (ELCraftType craftType, string craftFile)
		{
			string type = craftType.ToString ();
			string thumbName = Path.GetFileNameWithoutExtension(craftFile);
			return $"Ships/@thumbs/{type}/{thumbName}.png";
		}

		public static string UserPath (ELCraftType craftType, string craftFile)
		{
			return $"thumbs/{UserThumbName (craftType, craftFile)}.png";
		}

		public ELCraftThumb Craft (ELCraftType craftType, string craftFile)
		{
			string saveDir = HighLogic.SaveFolder;
			string type = craftType.ToString ();
			string thumbName = Path.GetFileNameWithoutExtension(craftFile);
			string thumbPath = $"thumbs/{saveDir}_{type}_{thumbName}.png";

			var thumbTex = GameObject.Instantiate (genericCraftThumb) as Texture2D;
			if (!EL_Utils.LoadImage (ref thumbTex, thumbPath)
				&& (craftType == ELCraftType.VAB
					|| craftType == ELCraftType.SPH)) {
				thumbPath = $"Ships/@thumbs/{type}/{thumbName}.png";
				EL_Utils.LoadImage (ref thumbTex, thumbPath);
			}
			Destroy (image.sprite);
			image.sprite = EL_Utils.MakeSprite (thumbTex);

			return this;
		}

		public ELCraftThumb Craft (string thumbPath)
		{
			var thumbTex = GameObject.Instantiate (genericCraftThumb) as Texture2D;
			bool ok = EL_Utils.LoadImage (ref thumbTex, thumbPath);
			Debug.Log ($"[ELCraftThumb] Craft {thumbPath} {ok}");
			Destroy (image.sprite);
			image.sprite = EL_Utils.MakeSprite (thumbTex);

			return this;
		}
	}
}
