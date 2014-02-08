using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class ProgressBar
	{
		GUIStyle style_back;
		GUIStyle style_bar;
		GUIStyle style_bar_thin;
		GUIStyle style_text;

		Texture2D ColorTexture (Color color)
		{
			Texture2D tex = new Texture2D (1, 1);
			tex.SetPixel (0, 0, color);
			tex.Apply ();
			return tex;
		}

		public ProgressBar (Color bar_color, Color back_color, Color text_color)
		{
			GUIStyle def = new GUIStyle (GUI.skin.box);
			def.border = new RectOffset (2, 2, 2, 2);
			def.normal.textColor = text_color;

			style_text = new GUIStyle (GUI.skin.label);
			style_text.alignment = TextAnchor.MiddleCenter;
			style_text.normal.textColor = text_color;
			style_text.wordWrap = false;

			style_bar = new GUIStyle (def);
			style_bar.normal.background = ColorTexture (bar_color);
			style_bar_thin = new GUIStyle (def);
			style_bar_thin.border = new RectOffset (0, 0, 0, 0);

			style_back = new GUIStyle (def);
			style_back.normal.background = ColorTexture (back_color);
		}

		Rect DrawBar (int width = 0)
		{
			List<GUILayoutOption> options = new List<GUILayoutOption> ();
			if (width == 0) {
				options.Add (GUILayout.ExpandWidth (true));
			}
			GUILayout.Label ("", style_back, options.ToArray ());
			return GUILayoutUtility.GetLastRect ();
		}
		void DrawBarScaled (Rect rect, float scale)
		{
			Rect r = new Rect (rect);
			r.width *= scale;
			if (r.width <= 2) {
				GUI.Label (r, "", style_bar_thin);
			} else {
				GUI.Label (r, "", style_bar);
			}
		}
		void DrawBarText (Rect rect, string text,
						  TextAnchor alignment = TextAnchor.MiddleCenter)
		{
			style_text.alignment = alignment;
			GUI.Label (rect, text, style_text);
		}
		public void Draw (float scale, string text, int width = 0)
		{
			Rect r = DrawBar (width);
			DrawBarScaled (r, scale);
			DrawBarText (r, text);
		}
	}
}
