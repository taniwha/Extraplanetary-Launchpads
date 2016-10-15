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
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {
	using Toolbar;

	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class ExAppButton : MonoBehaviour
	{
		private static ApplicationLauncherButton button = null;

		public static Callback Toggle = delegate {};

		private void onToggle ()
		{
			Toggle();
		}

		public void Start()
		{
			if (ToolbarManager.Instance != null) {
				return;
			}
			GameObject.DontDestroyOnLoad(this);
			GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
		}

		void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);
		}

		void OnGUIAppLauncherReady ()
		{
			if (ApplicationLauncher.Ready && button == null) {
				var tex = GameDatabase.Instance.GetTexture("ExtraplanetaryLaunchpads/Textures/icon_button", false);
				button = ApplicationLauncher.Instance.AddModApplication(onToggle, onToggle, null, null, null, null, ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH, tex);
			}
		}
	}

	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ExToolbar_ShipInfo : MonoBehaviour
	{
		private IButton ExEditorButton;

		public void Awake ()
		{
			ExAppButton.Toggle += ExShipInfo.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ExEditorButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExEditorButton");
			ExEditorButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExEditorButton.ToolTip = "EL Build Resources Display";
			ExEditorButton.OnClick += (e) => ExShipInfo.ToggleGUI ();
		}

		void OnDestroy()
		{
			if (ExEditorButton != null) {
				ExEditorButton.Destroy ();
			}
			ExAppButton.Toggle -= ExShipInfo.ToggleGUI;
		}
	}

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExToolbar_BuildWindow : MonoBehaviour
	{
		private IButton ExBuildWindowButton;

		public void Awake ()
		{
			ExAppButton.Toggle += ExBuildWindow.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ExBuildWindowButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExBuildWindowButton");
			ExBuildWindowButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExBuildWindowButton.ToolTip = "EL Build Window";
			ExBuildWindowButton.OnClick += (e) => ExBuildWindow.ToggleGUI ();
		}

		void OnDestroy()
		{
			if (ExBuildWindowButton != null) {
				ExBuildWindowButton.Destroy ();
			}
			ExAppButton.Toggle -= ExBuildWindow.ToggleGUI;
		}
	}

	[KSPAddon (KSPAddon.Startup.SpaceCentre, false)]
	public class ExToolbar_SettingsWindow : MonoBehaviour
	{
		private IButton ExSettingsButton;

		public void Awake ()
		{
			ExAppButton.Toggle += ExSettings.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ExSettingsButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExSettingsButton");
			ExSettingsButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExSettingsButton.ToolTip = "EL Settings Window";
			ExSettingsButton.OnClick += (e) => ExSettings.ToggleGUI ();
		}

		void OnDestroy()
		{
			if (ExSettingsButton != null) {
				ExSettingsButton.Destroy ();
			}
			ExAppButton.Toggle -= ExSettings.ToggleGUI;
		}
	}
}
