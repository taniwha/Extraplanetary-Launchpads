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

		public static string StockPath (ELCraftType craftType, string craftFile)
		{
			string type = craftType.ToString ();
			string thumbName = Path.GetFileNameWithoutExtension(craftFile);
			return $"Ships/@thumbs/{type}/{thumbName}.png";
		}

		public static string UserPath (ELCraftType craftType, string craftFile)
		{
			string saveDir = HighLogic.SaveFolder;
			string type = craftType.ToString ();
			string thumbName = Path.GetFileNameWithoutExtension(craftFile);
			return $"thumbs/{saveDir}_{type}_{thumbName}.png";
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
