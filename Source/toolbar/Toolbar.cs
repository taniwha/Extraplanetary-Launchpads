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

	[KSPAddon (KSPAddon.Startup.EditorAny, false)]
	public class ExToolbar_ShipInfo : MonoBehaviour
	{
		private IButton ExEditorButton;
		private static ApplicationLauncherButton appEditorButton = null;

		public void Awake ()
		{
			if (ToolbarManager.Instance == null) {
				if (appEditorButton == null) {
					GameEvents.onGUIApplicationLauncherReady.Add(setupAppButton_ShipInfo);
				}
				return;
			}
			ExEditorButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExEditorButton");
			ExEditorButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExEditorButton.ToolTip = "EL Build Resources Display";
			ExEditorButton.OnClick += (e) => ExShipInfo.ToggleGUI ();
		}

		public void setupAppButton_ShipInfo() {
			if (appEditorButton == null) {
				if (ApplicationLauncher.Ready) {
					appEditorButton = ApplicationLauncher.Instance.AddModApplication(
						ExShipInfo.ToggleGUI, ExShipInfo.ToggleGUI, null, null, null, null,
						ApplicationLauncher.AppScenes.VAB | ApplicationLauncher.AppScenes.SPH,
						GameDatabase.Instance.GetTexture("ExtraplanetaryLaunchpads/Textures/icon_button", false)
					);
				}
			}
		}

		void OnDestroy()
		{
			if (ExEditorButton != null) {
				ExEditorButton.Destroy ();
			}
		}
	}

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExToolbar_BuildWindow : MonoBehaviour
	{
		private IButton ExBuildWindowButton;
		private static ApplicationLauncherButton appBuildWindowButton = null;

		public void Awake ()
		{
			if (ToolbarManager.Instance == null) {
				if (appBuildWindowButton == null) {
					GameEvents.onGUIApplicationLauncherReady.Add(setupAppButton_BuildWindow);
				}
				return;
			}
			ExBuildWindowButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExBuildWindowButton");
			ExBuildWindowButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExBuildWindowButton.ToolTip = "EL Build Window";
			ExBuildWindowButton.OnClick += (e) => ExBuildWindow.ToggleGUI ();
		}

		public void setupAppButton_BuildWindow() {
			if (appBuildWindowButton == null) {
				if (ApplicationLauncher.Ready) {
					appBuildWindowButton = ApplicationLauncher.Instance.AddModApplication(
						ExBuildWindow.ToggleGUI, ExBuildWindow.ToggleGUI, null, null, null, null,
						ApplicationLauncher.AppScenes.FLIGHT,
						GameDatabase.Instance.GetTexture("ExtraplanetaryLaunchpads/Textures/icon_button", false)
					);
				}
			}
		}

		void OnDestroy()
		{
			if (ExBuildWindowButton != null) {
				ExBuildWindowButton.Destroy ();
			}
		}
	}

	[KSPAddon (KSPAddon.Startup.SpaceCentre, false)]
	public class ExToolbar_SettingsWindow : MonoBehaviour
	{
		private IButton ExSettingsButton;
		private static ApplicationLauncherButton appSettingsButton;

		public void Awake ()
		{
			if (ToolbarManager.Instance == null) {
				if (appSettingsButton == null) {
					GameEvents.onGUIApplicationLauncherReady.Add(setupAppButton_SpaceCenter);
				}
				return;
			}
			ExSettingsButton = ToolbarManager.Instance.add ("ExtraplanetaryLaunchpads", "ExSettingsButton");
			ExSettingsButton.TexturePath = "ExtraplanetaryLaunchpads/Textures/icon_button";
			ExSettingsButton.ToolTip = "EL Settings Window";
			ExSettingsButton.OnClick += (e) => ExSettings.ToggleGUI ();
		}

		public void setupAppButton_SpaceCenter() {
			if (appSettingsButton == null) {
				if (ApplicationLauncher.Ready) {
					appSettingsButton = ApplicationLauncher.Instance.AddModApplication(
						ExSettings.ToggleGUI, ExSettings.ToggleGUI, null, null, null, null,
						ApplicationLauncher.AppScenes.SPACECENTER,
						GameDatabase.Instance.GetTexture("ExtraplanetaryLaunchpads/Textures/icon_button", false)
					);
				}
			}
		}

		void OnDestroy()
		{
			if (ExSettingsButton != null) {
				ExSettingsButton.Destroy ();
			}
		}
	}
}
