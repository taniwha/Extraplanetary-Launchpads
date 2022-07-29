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
using UnityEngine;
using System.Reflection;
using KSP.Localization;

namespace ExtraplanetaryLaunchpads {

	public static class ELLocalization
	{
		public static string BuildManager { get; private set; } = "#EL_UI_BuildManager"; // "Build Manager"
		public static string ResourceManager { get; private set; } = "#EL_UI_ResourceManager"; // "Resource Manager"
		public static string Productivity { get; private set; } = "#EL_UI_Productivity"; // "Productivity:"
		public static string Select { get; private set; } = "#EL_UI_Select"; // "Select"
		public static string SelectCraft { get; private set; } = "#EL_UI_SelectCraft"; // "Select Craft"
		public static string SelectedCraft { get; private set; } = "#EL_UI_SelectedCraft"; // "Selected Craft"
		public static string SelectPart { get; private set; } = "#EL_UI_SelectPart"; // "Select Part"
		public static string Reload { get; private set; } = "#EL_UI_Reload"; // "Reload"
		public static string Clear { get; private set; } = "#EL_UI_Clear"; // "Clear"
		public static string Pad { get; private set; } = "#EL_UI_Pad"; // "pad"
		public static string NotAvailable { get; private set; } = "#EL_UI_NotAvailable"; // "N/A"
		public static string Build { get; private set; } = "#EL_UI_Build"; // "Build"
		public static string PauseBuild { get; private set; } = "#EL_UI_PauseBuild"; // "Pause Build"
		public static string ResumeBuild { get; private set; } = "#EL_UI_ResumeBuild"; // "Resume Build"
		public static string PauseTeardown { get; private set; } = "#EL_UI_PauseTeardown"; // "Pause Teardown"
		public static string ResumeTeardown { get; private set; } = "#EL_UI_ResumeTeardown"; // "Resume Teardown"
		public static string FinalizeBuild { get; private set; } = "#EL_UI_FinalizeBuild"; // "Finalize Build"
		public static string CancelBuild { get; private set; } = "#EL_UI_CancelBuild"; // "Cancel Build"
		public static string RestartBuild { get; private set; } = "#EL_UI_RestartBuild"; // "Restart Build"
		public static string Paused { get; private set; } = "#EL_UI_Paused"; // "[paused]"
		public static string Release { get; private set; } = "#EL_UI_Release"; // "Release"
		public static string DryMass { get; private set; } = "#EL_UI_DryMass"; // "Dry mass"
		public static string ResourceMass { get; private set; } = "#EL_UI_ResourceMass"; // "Resource mass"
		public static string TotalMass { get; private set; } = "#EL_UI_TotalMass"; // "Total mass"
		public static string BuildTime { get; private set; } = "#EL_UI_BuildTime"; // "Build time"
		public static string KerbalHours { get; private set; } = "#EL_UI_KerbalHours"; // "Kh"
		public static string Rename { get; private set; } = "#EL_UI_Rename"; // "Rename"
		public static string RenameMicropad { get; private set; } = "#EL_UI_RenameMicropad"; // "Rename Micro-pad"
		public static string RenameLaunchpad { get; private set; } = "#EL_UI_RenameLaunchpad"; // "Rename Launchpad"
		public static string RenameSite { get; private set; } = "#EL_UI_RenameSite"; // "Rename Site"
		public static string RenameSurveyStation { get; private set; } = "#EL_UI_RenameSurveyStation"; // "Rename Survey Station"
		public static string Name { get; private set; } = "#EL_UI_Name"; // "Name"
		public static string OK { get; private set; } = "#EL_UI_OK"; // "OK"
		public static string Load { get; private set; } = "#EL_UI_Load"; // "Load"
		public static string LoadSettings { get; private set; } = "#EL_UI_LoadSettings"; // "Load Settings"
		public static string Cancel { get; private set; } = "#EL_UI_Cancel"; // "Cancel"
		public static string StartTransfer { get; private set; } = "#EL_UI_StartTransfer"; // "Start Transfer"
		public static string StopTransfer { get; private set; } = "#EL_UI_StopTransfer"; // "Stop Transfer"
		public static string Hold { get; private set; } = "#EL_UI_Hold"; // "Hold"
		public static string In { get; private set; } = "#EL_UI_In"; // "In"
		public static string Out { get; private set; } = "#EL_UI_Out"; // "Out"
		public static string WarningNoSite { get; private set; } = "#EL_UI_WarningNoSite"; // "No sites found."
		public static string WarningNoSite2 { get; private set; } = "#EL_UI_WarningNoSite2"; // "No sites found. Explosions likely."
		public static string VAB { get; private set; } = "#autoLOC_6002108"; // "VAB" (from stock)
		public static string SPH { get; private set; } = "#autoLOC_6002119"; // "SPH" (from stock)
		public static string SubAss { get; private set; } = "#EL_UI_SubAss"; // "Sub"
		public static string Part { get; private set; } = "#autoLOC_6100048"; // "Part" (from stock)
		public static string StockVessels { get; private set; } = "#EL_UI_StockVessels"; // "Stock Vessels"
		public static string New { get; private set; } = "#EL_UI_New"; // "New"
		public static string Edit { get; private set; } = "#EL_UI_Edit"; // "Edit"
		public static string PartEditor { get; private set; } = "#EL_UI_PartEditor"; // "Part Editor"
		public static string Save { get; private set; } = "#EL_UI_Save"; // "Save"
		public static string SaveAndClose { get; private set; } = "#EL_UI_SaveAndClose"; // "Save and Close"
		public static string Close { get; private set; } = "#EL_UI_Close"; // "Close"
		public static string MissingParts { get; private set; } = "#EL_UI_MissingParts"; // "Missing Parts:"

		public static string PreferBlizzy { get; private set; } = "#EL_UI_PreferBlizzy"; // "Use Blizzy's toolbar instead of App launcher"
		public static string CreateKACAlarms { get; private set; } = "#EL_UI_CreateKACAlarms"; // "Create alarms in Kerbal Alarm Clock"
		public static string ShowCraftHull { get; private set; } = "#EL_UI_ShowCraftHull"; // "Show craft hull during construction"
		public static string DebugCraftHull { get; private set; } = "#EL_UI_DebugCraftHull"; // "[Debug] Write craft hull points file"

		public static string KillWarpMessage { get; private set; } = "#EL_UI_KillWarpMessage"; // "Kill Warp+Message"
		public static string KillWarpOnly { get; private set; } = "#EL_UI_KillWarpOnly"; // "Kill Warp only"
		public static string MessageOnly { get; private set; } = "#EL_UI_MessageOnly"; // "Message Only"
		public static string PauseGame { get; private set; } = "#EL_UI_PauseGame"; // "Pause Game"

		static BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Static;
		internal static void LoadStrings ()
		{
			var properties = typeof (ELLocalization).GetProperties (bindingFlags);
			foreach (var p in properties) {
				var s = (string) p.GetValue (null, null);
				var l = Localizer.Format(s);
				//Debug.Log ($"[ELLocalization] {s} -> {l}");
				p.SetValue (null, l);
			}
		}
	}
}
