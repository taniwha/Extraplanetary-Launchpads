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
	public class ELAppButton : MonoBehaviour
	{
		const ApplicationLauncher.AppScenes buttonScenes = ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH;
		private static ApplicationLauncherButton button = null;

		public static Callback Toggle = delegate {};
		public static Callback RightToggle = delegate {};

		static bool buttonVisible
		{
			get {
				if (ToolbarManager.Instance == null) {
					return true;
				}
				return !ELSettings.PreferBlizzy;
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

		private void onRightClick ()
		{
			RightToggle();
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
				button.onRightClick += onRightClick;
				UpdateVisibility ();
			}
		}
	}

	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ELToolbar_ShipInfo : MonoBehaviour
	{
		private IButton ELEditorButton;

		public void Awake ()
		{
			ELAppButton.Toggle += ELShipInfo.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ELEditorButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ELEditorButton");
			ELEditorButton.Visible = ELSettings.PreferBlizzy;
			ELEditorButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ELEditorButton.ToolTip = "EL Build Resources Display";
			ELEditorButton.OnClick += (e) => ELShipInfo.ToggleGUI ();
		}

		void OnDestroy()
		{
			if (ELEditorButton != null) {
				ELEditorButton.Destroy ();
			}
			ELAppButton.Toggle -= ELShipInfo.ToggleGUI;
		}
	}

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ELToolbar_BuildWindow : MonoBehaviour
	{
		private IButton ELBuildWindowButton;

		public void Awake ()
		{
			ELAppButton.Toggle += ELBuildWindow.ToggleGUI;
			ELAppButton.RightToggle += ELResourceWindow.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ELBuildWindowButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ELBuildWindowButton");
			ELBuildWindowButton.Visible = ELSettings.PreferBlizzy;
			ELBuildWindowButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ELBuildWindowButton.ToolTip = "EL Build Window";
			ELBuildWindowButton.OnClick += (e) => ELBuildWindow.ToggleGUI ();
		}

		void OnDestroy()
		{
			if (ELBuildWindowButton != null) {
				ELBuildWindowButton.Destroy ();
			}
			ELAppButton.Toggle -= ELBuildWindow.ToggleGUI;
			ELAppButton.RightToggle -= ELResourceWindow.ToggleGUI;
		}
	}

	[KSPAddon (KSPAddon.Startup.SpaceCentre, false)]
	public class ELToolbar_SettingsWindow : MonoBehaviour
	{
		private IButton ELSettingsButton;

		public static ELToolbar_SettingsWindow Instance { get; private set; }

		public void UpdateVisibility ()
		{
			if (ELSettingsButton != null) {
				ELSettingsButton.Visible = ELSettings.PreferBlizzy;
			}
		}

		public void Awake ()
		{
			Instance = this;
			ELAppButton.Toggle += ELSettings.ToggleGUI;

			if (ToolbarManager.Instance == null) {
				return;
			}
			ELSettingsButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ELSettingsButton");
			ELSettingsButton.Visible = ELSettings.PreferBlizzy;
			ELSettingsButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ELSettingsButton.ToolTip = "EL Settings Window";
			ELSettingsButton.OnClick += (e) => ELSettings.ToggleGUI ();
		}

		void OnDestroy()
		{
			Instance = null;
			if (ELSettingsButton != null) {
				ELSettingsButton.Destroy ();
			}
			ELAppButton.Toggle -= ELSettings.ToggleGUI;
		}
	}
}
