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
using UnityEngine;

namespace ExtraplanetaryLaunchpads {

	public class ScrollView
	{
		Rect rect;
		public Vector2 scroll;
		public bool mouseOver { get; private set; }
		GUILayoutOption width;
		GUILayoutOption height;
		GUILayoutOption sbWidth;

		public ScrollView (int width, int height)
		{
			this.width = GUILayout.Width (width);
			this.height = GUILayout.Height (height);
			this.sbWidth = GUILayout.Width (15);
		}

		public void Begin ()
		{
			scroll = GUILayout.BeginScrollView (scroll, width, height);
			GUILayout.BeginHorizontal ();
			GUILayout.BeginVertical ();
		}

		public void End ()
		{
			GUILayout.EndVertical ();
			GUILayout.Label ("", ELStyles.label, sbWidth);
			GUILayout.EndHorizontal ();
			GUILayout.EndScrollView ();
			if (Event.current.type == EventType.Repaint) {
				rect = GUILayoutUtility.GetLastRect();
				mouseOver = rect.Contains(Event.current.mousePosition);
			}
		}

		public void Reset ()
		{
			scroll = Vector2.zero;
		}
	}

}
