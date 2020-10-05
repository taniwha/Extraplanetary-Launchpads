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

	[KSPAddon (KSPAddon.Startup.MainMenu, true)]
	public class ELWindowManager : MonoBehaviour
	{
		static ELWindowManager instance;

		static double minimum_alarm_time = 60;

		static bool KACinited = false;

		struct WindowInfo {
			public bool visible;
			public Vector2 position;

			public void Load (ConfigNode node)
			{
				string val = node.GetValue ("position");
				if (val != null) {
					ParseExtensions.TryParseVector2 (val, out position);
				}
				val = node.GetValue ("visible");
				if (val != null) {
					bool.TryParse (val, out visible);
				}
			}

			public void Save (ConfigNode node)
			{
				node.AddValue ("position", position);
				node.AddValue ("visible", visible);
			}
		}

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
			string val = node.GetValue ("minimum_alarm_time");
			if (val != null) {
				double.TryParse (val, out minimum_alarm_time);
			}
			if (node.HasNode ("MainWindow")) {
				mainWindowInfo.Load (node.GetNode ("MainWindow"));
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
			if (mainWindow) {
				mainWindowInfo.position = mainWindow.transform.localPosition;
			}
			mainWindowInfo.Save (node.AddNode ("MainWindow"));

			if (shipInfo) {
				shipInfoInfo.position = shipInfo.transform.localPosition;
			}
			shipInfoInfo.Save (node.AddNode ("ShipInfo"));
		}

		public static void HideBuildWindow ()
		{
			if (mainWindow) {
				mainWindow.SetVisible (false);
			}
			mainWindowInfo.visible = false;
		}

		public static void ShowBuildWindow (ELBuildControl control)
		{
			if (!mainWindow) {
				mainWindow = UIKit.CreateUI<ELMainWindow> (appCanvasRect, "ELMainWindow");
				mainWindow.transform.position = mainWindowInfo.position;
			}
			mainWindowInfo.visible = true;
			mainWindow.SetVisible (true);
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

		public static void HideShipInfo ()
		{
			if (shipInfo) {
				shipInfo.SetVisible (false);
			}
			shipInfoInfo.visible = false;
		}

		public static void ShowShipInfo (ELBuildControl control)
		{
			if (!shipInfo) {
				shipInfo = UIKit.CreateUI<ELShipInfoWindow> (appCanvasRect, "ELShipInfo");
				shipInfo.transform.position = shipInfoInfo.position;
			}
			shipInfoInfo.visible = true;
			shipInfo.SetVisible (true);
		}

		public static void ToggleShipInfo ()
		{
			if (!shipInfo || !shipInfo.gameObject.activeSelf) {
				ShowShipInfo (null);
			} else {
				HideShipInfo ();
			}
		}

		static Canvas appCanvas;
		public static RectTransform appCanvasRect { get; private set; }

		static WindowInfo mainWindowInfo = new WindowInfo ();
		static ELMainWindow mainWindow;

		static WindowInfo shipInfoInfo = new WindowInfo ();
		static ELShipInfoWindow shipInfo;

		void Awake ()
		{
			instance = this;
			GameObject.DontDestroyOnLoad(this);
			appCanvas = DialogCanvasUtil.DialogCanvas;
			appCanvasRect = appCanvas.transform as RectTransform;

			GameEvents.onGameSceneSwitchRequested.Add (onGameSceneSwitchRequested);
			GameEvents.onLevelWasLoadedGUIReady.Add (onLevelWasLoadedGUIReady);
		}

		void OnDestroy ()
		{
			instance = null;
			if (mainWindow) {
				Destroy (mainWindow.gameObject);
				mainWindow = null;
			}
			if (shipInfo) {
				Destroy (shipInfo.gameObject);
				shipInfo = null;
			}
			GameEvents.onGameSceneSwitchRequested.Remove (onGameSceneSwitchRequested);
			GameEvents.onLevelWasLoadedGUIReady.Remove (onLevelWasLoadedGUIReady);
		}

		void onGameSceneSwitchRequested(GameEvents.FromToAction<GameScenes, GameScenes> data)
		{
			if (mainWindow) {
				mainWindow.SetVisible (false);
			}
			if (shipInfo) {
				shipInfo.SetVisible (false);
			}
		}

		void onLevelWasLoadedGUIReady(GameScenes scene)
		{
			if (scene == GameScenes.FLIGHT) {
				if (mainWindowInfo.visible) {
					ShowBuildWindow (null);
				}
			}
			if (scene == GameScenes.EDITOR) {
				if (shipInfoInfo.visible) {
					ShowShipInfo (null);
				}
			}
		}
	}
}
