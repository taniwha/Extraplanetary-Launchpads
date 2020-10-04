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

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	[KSPAddon (KSPAddon.Startup.Flight, true)]
	public class ELWindowManager : MonoBehaviour
	{
		static ELWindowManager instance;

		static bool gui_enabled = true;
		static Rect windowpos;

		static double minimum_alarm_time = 60;

		static bool KACinited = false;

		internal void Start()
		{
			if (!KACinited) {
				KACinited = true;
				KACWrapper.InitKACWrapper();
			}
			if (KACWrapper.APIReady)
			{
				//All good to go
				Debug.Log ("KACWrapper initialized");
			}
		}

		public static void LoadSettings (ConfigNode node)
		{
			string val = node.GetValue ("rect");
			if (val != null) {
				Quaternion pos;
				pos = ConfigNode.ParseQuaternion (val);
				windowpos.x = pos.x;
				windowpos.y = pos.y;
				windowpos.width = pos.z;
				windowpos.height = pos.w;
			}
			val = node.GetValue ("visible");
			if (val != null) {
				bool.TryParse (val, out gui_enabled);
			}
			val = node.GetValue ("minimum_alarm_time");
			if (val != null) {
				double.TryParse (val, out minimum_alarm_time);
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
		}

		public static void HideBuildWindow ()
		{
			if (mainWindow) {
				mainWindow.gameObject.SetActive (false);
			}
		}

		public static void ShowBuildWindow (ELBuildControl control)
		{
			if (!mainWindow) {
				mainWindow = UIKit.CreateUI<ELMainWindow> (appCanvasRect, "ELMainWindow");
			}
			mainWindow.gameObject.SetActive (true);
			if (control != null) {
				mainWindow.SetControl (control);
			} else {
				mainWindow.SetVessel (FlightGlobals.ActiveVessel);
			}
		}

		public static void ToggleBuildWindow ()
		{
			if (!mainWindow || !mainWindow.gameObject.activeSelf) {
				ShowBuildWindow (null);
			} else {
				HideBuildWindow ();
			}
		}

		static Canvas appCanvas;
		static RectTransform appCanvasRect;
		static ELMainWindow mainWindow;

		void Awake ()
		{
			instance = this;
			GameObject.DontDestroyOnLoad(this);
			appCanvas = DialogCanvasUtil.DialogCanvas;
			appCanvasRect = appCanvas.transform as RectTransform;
		}

		void OnDestroy ()
		{
			instance = null;
			if (mainWindow) {
				Destroy (mainWindow.gameObject);
				mainWindow = null;
			}
		}
	}
}
