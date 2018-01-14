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
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;
using KSP.UI.Screens;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	public class ELStyles {
		public static GUIStyle normal;
		public static GUIStyle red;
		public static GUIStyle yellow;
		public static GUIStyle green;
		public static GUIStyle white;
		public static GUIStyle label;
		public static GUIStyle slider;
		public static GUIStyle sliderText;

		public static GUIStyle listItem;
		public static GUIStyle listBox;

		public static ProgressBar bar;

		private static bool initialized;

		public static void Init ()
		{
			if (initialized)
				return;
			initialized = true;

			normal = new GUIStyle (GUI.skin.button);
			normal.normal.textColor = normal.focused.textColor = Color.white;
			normal.hover.textColor = normal.active.textColor = Color.yellow;
			normal.onNormal.textColor = normal.onFocused.textColor = normal.onHover.textColor = normal.onActive.textColor = Color.green;
			normal.padding = new RectOffset (8, 8, 8, 8);

			red = new GUIStyle (GUI.skin.box);
			red.padding = new RectOffset (8, 8, 8, 8);
			red.normal.textColor = red.focused.textColor = Color.red;

			yellow = new GUIStyle (GUI.skin.box);
			yellow.padding = new RectOffset (8, 8, 8, 8);
			yellow.normal.textColor = yellow.focused.textColor = Color.yellow;

			green = new GUIStyle (GUI.skin.box);
			green.padding = new RectOffset (8, 8, 8, 8);
			green.normal.textColor = green.focused.textColor = Color.green;
			green.wordWrap = false;

			white = new GUIStyle (GUI.skin.box);
			white.padding = new RectOffset (8, 8, 8, 8);
			white.normal.textColor = white.focused.textColor = Color.white;
			white.wordWrap = false;

			label = new GUIStyle (GUI.skin.label);
			label.normal.textColor = label.focused.textColor = Color.white;
			label.alignment = TextAnchor.MiddleCenter;
			label.wordWrap = false;

			slider = new GUIStyle (GUI.skin.horizontalSlider);
			slider.margin = new RectOffset (0, 0, 0, 0);

			sliderText = new GUIStyle (GUI.skin.label);
			sliderText.alignment = TextAnchor.MiddleCenter;
			sliderText.margin = new RectOffset (0, 0, 0, 0);

			listItem = new GUIStyle ();
			listItem.normal.textColor = Color.white;
			Texture2D texInit = new Texture2D(1, 1);
			texInit.SetPixel(0, 0, Color.white);
			texInit.Apply();
			listItem.hover.background = texInit;
			listItem.onHover.background = texInit;
			listItem.hover.textColor = Color.black;
			listItem.onHover.textColor = Color.black;
			listItem.padding = new RectOffset(4, 4, 4, 4);

			listBox = new GUIStyle(GUI.skin.box);

			bar = new ProgressBar (XKCDColors.Azure,
								   XKCDColors.ElectricBlue,
								   new Color(255, 255, 255, 0.8f));
		}
	}
}
