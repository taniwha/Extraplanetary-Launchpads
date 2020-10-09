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
using UnityEngine.SceneManagement;
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

		static Vector4 angles;
		static float fovFactor = 0.9f;

		static IEnumerator capture (ShipConstruct ship, string thumbName)
		{
			yield return null;

			var savedScene = SceneManager.GetActiveScene ();
			CraftThumbnail.TakeSnaphot (ship, 256, "thumbs", thumbName,
										angles.x, angles.y, angles.z, angles.w,
										fovFactor);
			for (int i = ship.parts.Count; i-- > 0; ) {
				UnityEngine.Object.Destroy (ship.parts[i].gameObject);
			}
		}

		public static void Capture (ConfigNode craft, ELCraftType craftType, string craftFile)
		{
			var ship = new ShipConstruct ();
			ship.LoadShip (craft);

			if (ship.vesselDeltaV != null) {
				// The delta-v module is not needed. It has its own gameObject
				// for ShipConstruct.
				UnityEngine.Object.Destroy (ship.vesselDeltaV.gameObject);
				ship.vesselDeltaV = null;
			}
			// Need to keep the parts around for a few frames, but
			// Part.FixedUpdate throws if things are not set up properly
			// (which they won't be), but setting the state to PLACEMENT gets
			// around this by tricking the part into thinking its being placed
			// in the editor
			for (int i = ship.parts.Count; i-- > 0; ) {
				ship.parts[i].State = PartStates.PLACEMENT;
			}

			if (craftType == ELCraftType.SPH) {
				angles = new Vector4 (35, 135, 35, 135);
			} else {
				angles = new Vector4 (45, 45, 45, 45);
			}

			string thumbName = UserThumbName (craftType, craftFile);
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
