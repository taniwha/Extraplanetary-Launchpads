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
using System.Collections;
using UnityEngine;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftThumb : UIImage
	{
		ELCraftThumbManager.ThumbSprite thumbSprite;

		public override void CreateUI ()
		{
			base.CreateUI ();

			this.SizeDelta (256, 256)
				.MinSize (256, 256)
				.PreferredSize (256, 256)
				;
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
				//Debug.Log ($"[ELCraftThumb] CreateRig light {i} {lightObj.transform.localPosition} {lightObj.transform.forward}");

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

		static IEnumerator capture (ShipConstruct ship, string thumbPath)
		{
			yield return null;
			yield return null;
			yield return null;
			yield return null;
			yield return null;

			if (thumbRig) {
				var tex = takeSnapshot (ship);
				var png = tex.EncodeToPNG ();

				string dir = KSPUtil.ApplicationRootPath;
				string path = dir + thumbPath;

				if (!ELCraftThumbManager.UpdateThumbCache (thumbPath, tex)) {
					GameObject.Destroy (tex);
				}

				if (!Directory.Exists (dir)) {
					Directory.CreateDirectory (dir);
				}
				File.WriteAllBytes (path, png);

				//Debug.Log ($"[ELCraftThumb] capture {path}");
				thumbRig.SetActive (false);
			}

			ELEditor.ClearShip ();

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
			// Need to keep the parts around for a few frames, but
			// various modules throw if things are not set up properly (which
			// they won't be), but tricking the parts and their modules into
			// thinking they're being placed in the editor fixes things
			ELEditor.EditShip (ship);
			for (int i = ship.parts.Count; i-- > 0; ) {
				// Make sure the part and main scene won't interact physcally
				// or graphically (thumbLayer is chosen to be a layer that is
				// not rendered by the scene cameras), and removing the
				// colliders ensures that the parts can't collide with anything
				//
				// ELEditor removes the colliders

				EL_Utils.SetLightMasks (ship.parts[i].gameObject, thumbMask);
				EL_Utils.SetLayer (ship.parts[i].gameObject, thumbLayer, true);
			}

			if (craftType == ELCraftType.SPH) {
				angles = new Vector2 (35, 135);
			} else {
				angles = new Vector2 (45, 45);
			}

			string thumbPath = UserPath (craftType, craftFile);
			if (!thumbRig) {
				CreateRig ();
			}
			thumbRig.SetActive (true);
			// Need to wait a frame for the parts to be renderable
			// (don't know why, but my guess is to give the various renderers
			// a chance to set themselves up)
			HighLogic.fetch.StartCoroutine (capture (ship, thumbPath));
		}

		static string CleanSeparators (string path)
		{
			return path.Replace ('/', '_');
		}

		public static string ThumbName (string craftFile)
		{
			string thumbName = CleanSeparators (Path.ChangeExtension(craftFile, null));
			Debug.Log ($"[ELCraftThumb] ThumbName: {craftFile} {thumbName}");
			return thumbName;
		}

		public static string UserThumbName (ELCraftType craftType, string craftFile)
		{
			string saveDir = HighLogic.SaveFolder;
			string type = craftType.ToString ();
			return $"{saveDir}_{type}_{ThumbName (craftFile)}";
		}

		public static string StockPath (ELCraftType craftType, string craftFile)
		{
			string type = craftType.ToString ();
			return $"Ships/@thumbs/{type}/{ThumbName (craftFile)}.png";
		}

		public static string UserPath (ELCraftType craftType, string craftFile)
		{
			return $"thumbs/{UserThumbName (craftType, craftFile)}.png";
		}

		static string applicationRoot;

		public ELCraftThumb Craft (ELCraftType craftType, string craftFile)
		{
			if (applicationRoot == null) {
				applicationRoot = KSPUtil.ApplicationRootPath.Replace("\\", "/");
			}
			string thumbPath = UserPath (craftType, craftFile);

			if (File.Exists (applicationRoot + thumbPath)) {
				return Craft (thumbPath);
			}

			string stockPath = StockPath (craftType, craftFile);
			if (File.Exists (applicationRoot + stockPath)) {
				return Craft (stockPath);
			}

			// assume it's user craft and we can generate the thumb, it will
			// be updated if the thumb is generated
			return Craft (thumbPath);
		}

		void onSpriteUpdate (ELCraftThumbManager.ThumbSprite thumbSprite)
		{
			image.sprite = thumbSprite.sprite;
		}

		public ELCraftThumb Craft (string thumbPath)
		{
			if (thumbSprite != null) {
				thumbSprite.onUpdate.RemoveListener (onSpriteUpdate);
			}
			thumbSprite = ELCraftThumbManager.GetThumb (thumbPath);
			thumbSprite.onUpdate.AddListener (onSpriteUpdate);
			image.sprite = thumbSprite.sprite;
			return this;
		}
	}
}
