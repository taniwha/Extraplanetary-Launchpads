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
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExRenameWindow: MonoBehaviour
	{
		private static ExLaunchPad padInstance = null;
		private static ExRenameWindow windowInstance = null;
		private static bool gui_enabled = false;
		private static Rect windowpos = new Rect(Screen.width * 0.35f,Screen.height * 0.1f,1,1);
		private static string newPadName;

		void Awake ()
		{
			windowInstance = this;
			enabled = true;
			gui_enabled = false;
		}

		public static void HideGUI ()
		{
			gui_enabled = false;
		}

		public static void ShowGUI (ExLaunchPad pad)
		{
			padInstance = pad;
			newPadName = pad.PadName;
			gui_enabled = true;
			if (windowInstance != null) {
				windowInstance.enabled = true;
			}
		}

		void WindowGUI (int windowID)
		{
			GUILayout.BeginVertical ();

			GUILayout.BeginHorizontal ();
			GUILayout.Label ("Rename launchpad: ");
			newPadName = GUILayout.TextField (newPadName, 20);
			GUILayout.EndHorizontal ();

			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("OK")) {
				padInstance.PadName = newPadName;
				gui_enabled = false;
				ExBuildWindow.updateCurrentPads ();
			}
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Cancel")) {
				gui_enabled = false;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();

			GUILayout.EndVertical ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		void OnGUI ()
		{
			if (gui_enabled) {
				//enabled = true;
				GUI.skin = HighLogic.Skin;
				windowpos = GUILayout.Window (GetInstanceID (),
					windowpos, WindowGUI,
					"Rename Launchpad",
					GUILayout.Width(500));
			}
			if (enabled && windowpos.Contains (new Vector2 (Input.mousePosition.x, Screen.height - Input.mousePosition.y))) {
				InputLockManager.SetControlLock ("EL_Rename_window_lock");
			} else {
				InputLockManager.RemoveControlLock ("EL_Rename_window_lock");
			}
		}
	}
}
