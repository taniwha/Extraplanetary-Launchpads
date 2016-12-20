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
		const ApplicationLauncher.AppScenes buttonScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
		private static ApplicationLauncherButton button = null;

		public static Callback Toggle = delegate {};

		static bool buttonVisible
		{
			get {
				if (ToolbarManager.Instance == null) {
					return true;
				}
				return !ExSettings.PreferBlizzy;
			}
		}

		public static void UpdateVisibility ()
		{
			if (button != null) {
				button.VisibleInScenes = buttonVisible ? buttonScenes : 0;
			}
		}

		private void onToggle ()
		{
			Toggle();
		}

		public void Start()
		{
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
				button = ApplicationLauncher.Instance.AddModApplication(onToggle, onToggle, null, null, null, null, buttonScenes, tex);
				UpdateVisibility ();
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
			ExEditorButton.Visible = ExSettings.PreferBlizzy;
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
			ExBuildWindowButton.Visible = ExSettings.PreferBlizzy;
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

		public static ExToolbar_SettingsWindow Instance { get; private set; }

		public void UpdateVisibility ()
		{
			if (ExSettingsButton != null) {
				ExSettingsButton.Visible = ExSettings.PreferBlizzy;
			}
		}

		public void Awake ()
		{
			Instance = this;
			ExAppButton.Toggle += ExSettings.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ExSettingsButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExSettingsButton");
			ExSettingsButton.Visible = ExSettings.PreferBlizzy;
			ExSettingsButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExSettingsButton.ToolTip = "EL Settings Window";
			ExSettingsButton.OnClick += (e) => ExSettings.ToggleGUI ();
		}

		void OnDestroy()
		{
			Instance = null;
			if (ExSettingsButton != null) {
				ExSettingsButton.Destroy ();
			}
			ExAppButton.Toggle -= ExSettings.ToggleGUI;
		}
	}
}
