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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELCraftThumbManager
	{
		public class ThumbSprite
		{
			Sprite _sprite;
			public Sprite sprite
			{
				get { return _sprite; }
				set {
					_sprite = value;
					onUpdate.Invoke (this);
				}
			}

			public DateTime timestamp { get; set; }

			public class ThumbEvent : UnityEvent<ThumbSprite> { }
			public ThumbEvent onUpdate { get; private set; }

			public ThumbSprite (Sprite sprite, DateTime timestamp)
			{
				_sprite = sprite;
				onUpdate = new ThumbEvent ();
				this.timestamp = timestamp;
			}
		}

		static string GenericThumbName = "GameData/ExtraplanetaryLaunchpads/Textures/ELGenericCraftThumb.png";
		static Texture2D genericCraftThumb;

		static Dictionary<string, ThumbSprite> thumbnailCache = new Dictionary<string, ThumbSprite> ();
		public static ThumbSprite GetThumb (string thumbPath)
		{
			if (!genericCraftThumb) {
				if (EL_Utils.KSPFileExists (GenericThumbName)) {
					var tex = new Texture2D (256, 256, TextureFormat.ARGB32, false);
					EL_Utils.LoadImage (ref tex, GenericThumbName);
					genericCraftThumb = tex;
				} else {
					// ick, but better than nothing
					genericCraftThumb = AssetBase.GetTexture("craftThumbGeneric");
				}
			}

			ThumbSprite thumb = null;
			var timestamp = new DateTime (0);
			if (!String.IsNullOrEmpty (thumbPath)) {
				if (EL_Utils.KSPFileExists (thumbPath)) {
					timestamp = EL_Utils.KSPFileTimestamp (thumbPath);
				}
				if (thumbnailCache.TryGetValue (thumbPath, out thumb)
					&& thumb.sprite && thumb.timestamp == timestamp) {
					return thumb;
				}
			}

			var thumbTex = GameObject.Instantiate (genericCraftThumb) as Texture2D;
			if (!String.IsNullOrEmpty (thumbPath)) {
				EL_Utils.LoadImage (ref thumbTex, thumbPath);
			}
			var sprite = EL_Utils.MakeSprite (thumbTex);

			if (thumb == null) {
				thumb = new ThumbSprite (sprite, timestamp);
			} else {
				thumb.sprite = sprite;
			}
			if (!String.IsNullOrEmpty (thumbPath)) {
				thumbnailCache[thumbPath] = thumb;
			}
			return thumb;
		}

		public static bool UpdateThumbCache (string thumbPath, Texture2D tex)
		{
			ThumbSprite thumb;
			tex.Apply ();
			if (thumbnailCache.TryGetValue (thumbPath, out thumb)) {
				GameObject.Destroy (thumb.sprite.texture);
				GameObject.Destroy (thumb.sprite);
				thumb.sprite = EL_Utils.MakeSprite (tex);
				thumb.timestamp = EL_Utils.KSPFileTimestamp (thumbPath);
				return true;
			}
			return false;
		}
	}
}
