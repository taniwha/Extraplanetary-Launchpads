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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;
using ModuleWheels;

namespace ExtraplanetaryLaunchpads {

	public class ELSurveyStation : PartModule, IModuleInfo, IPartMassModifier, ELBuildControl.IBuilder, ELControlInterface, ELWorkSink, ELRenameWindow.IRenamable
	{
		[KSPField (isPersistant = true, guiActive = true, guiName = "Pad name")]
		public string StationName = "";

		[KSPField (isPersistant = true)]
		public bool Operational = true;

		[KSPField] public float EVARange = 0;

		EL_VirtualPad virtualPad;
		DropDownList site_list;
		List<SurveySite> available_sites;
		SurveySite site;
		double craft_mass;
		[KSPField (guiName = "Range", guiActive = true)]
		float range = 20;
		public static float[] site_ranges = {
			20, 50, 100, 200, 400, 800, 1600, 2000
		};

		public override string GetInfo ()
		{
			return "Survey Station";
		}

		public string GetPrimaryField ()
		{
			return null;
		}

		public string GetModuleTitle ()
		{
			return "EL Survey Station";
		}

		public Callback<Rect> GetDrawModulePanelCallback ()
		{
			return null;
		}

		public bool canBuild
		{
			get {
				if (vessel.situation == Vessel.Situations.LANDED
					|| vessel.situation == Vessel.Situations.SPLASHED
					|| vessel.situation == Vessel.Situations.PRELAUNCH) {
					return canOperate;
				}
				return false;
			}
		}

		public bool capture
		{
			get {
				return false;
			}
		}

		public ELBuildControl control
		{
			get;
			private set;
		}

		public new Vessel vessel
		{
			get {
				return base.vessel;
			}
		}

		public new Part part
		{
			get {
				return base.part;
			}
		}

		public string Name
		{
			get {
				return StationName;
			}
			set {
				StationName = value;
			}
		}

		public string LandedAt
		{
			get {
				if (site != null) {
					return site.SiteName;
				}
				return "";
			}
		}
		public string LaunchedFrom
		{
			get {
				if (site != null) {
					return site.SiteName;
				}
				return "";
			}
		}

		public bool isBusy
		{
			get {
				return control.state > ELBuildControl.State.Planning;
			}
		}

		public bool canOperate
		{
			get { return Operational; }
			set {
				Operational = value;
				DetermineRange ();
			}
		}

		public void PadSelection_start ()
		{
			if (site_list == null) {
				return;
			}
			site_list.styleListItem = ELStyles.listItem;
			site_list.styleListBox = ELStyles.listBox;
			site_list.DrawBlockingSelector ();
		}

		void SetSite (SurveySite selected_site)
		{
			if (site == selected_site) {
				if (site != null && virtualPad != null) {
					// update display
					virtualPad.SetSite (site);
				}
				return;
			}
			Highlight (false);
			site = selected_site;
			if (site == null) {
				if (virtualPad != null) {
					Destroy (virtualPad.gameObject);
					virtualPad = null;
				}
			} else {
				if (virtualPad == null) {
					//virtualPad = EL_VirtualPad.Create (site);
				} else {
					virtualPad.SetSite (site);
				}
			}
			if (site_list != null) {
				site_list.SelectItem (available_sites.IndexOf (site));
			}
			// The build window will take care of turning on highlighting
		}

		void RenameSite ()
		{
			bool en = GUI.enabled;
			GUI.enabled = en && (site != null);
			if (GUILayout.Button ("Rename Site", ELStyles.normal,
								  GUILayout.ExpandWidth (false))) {
				if (site != null) {
					ELRenameWindow.ShowGUI (site);
				}
			}
			GUI.enabled = en;
		}

		public void PadSelection ()
		{
			if (site_list == null) {
				GUILayout.BeginHorizontal ();
				if (control.state == ELBuildControl.State.Complete) {
					GUILayout.Label ("No sites found. Explosions likely.",
									 ELStyles.red);
				} else {
					GUILayout.Label ("No sites found.");
				}
				GUILayout.EndHorizontal ();
			} else {
				GUILayout.BeginHorizontal ();
				site_list.DrawButton ();
				SetSite (available_sites[site_list.SelectedIndex]);
				RenameSite ();
				GUILayout.EndHorizontal ();
			}
		}

		public void PadSelection_end ()
		{
			if (site_list != null) {
				site_list.DrawDropDown();
				site_list.CloseOnOutsideClick();
			}
		}

		public void Highlight (bool on)
		{
			if (on) {
				part.SetHighlightColor (XKCDColors.LightSeaGreen);
				part.SetHighlight (true, false);
			} else {
				part.SetHighlightDefault ();
			}
			if (site != null) {
				foreach (var stake in site) {
					if (stake.stake != null) {
						stake.stake.Highlight (on);
					}
				}
			}
		}

		public void SetCraftMass (double mass)
		{
			craft_mass = mass;
		}

		public float GetModuleMass (float defaultMass, ModifierStagingSituation sit)
		{
			return (float) craft_mass;
		}

		public ModifierChangeWhen GetModuleMassChangeWhen ()
		{
			return ModifierChangeWhen.CONSTANTLY;
		}

		public void SetShipTransform (Transform shipTransform, Part rootPart)
		{
		}

		public Transform PlaceShip (Transform shipTransform, Box vessel_bounds)
		{
			if (site == null) {
				return part.transform;
			}
			Transform xform;
			xform = part.FindModelTransform ("EL launch pos");

			if (xform == null) {
				GameObject launchPos = new GameObject ("EL launch pos");
				xform = launchPos.transform;
				Transform t = part.partTransform.Find("model");
				xform.SetParent (t, false);
			}
			var points = new Points (site);
			xform.transform.position = points.center;
			xform.transform.rotation = points.GetOrientation ();
			Debug.Log ($"[EL SurveyStation] launchPos {xform.position} {xform.rotation}");

			Vector3 pos = shipTransform.position;
			pos += points.ShiftBounds (xform, pos, vessel_bounds);
			Quaternion rot = shipTransform.rotation;
			shipTransform.rotation = xform.rotation * rot;
			shipTransform.position = xform.TransformPoint (pos);
			return xform;
		}

		public void PostBuild (Vessel craftVessel)
		{
			var brakes = craftVessel.FindPartModulesImplementing<ModuleWheelBrakes> ();
			for (int i = brakes.Count; i-- > 0; ) {
				brakes[i].brakeInput = 1;
			}
			if (brakes.Count > 0) {
				var group = KSPActionGroup.Brakes;
				var actionGroups = craftVessel.ActionGroups;
				actionGroups.SetGroup (group, true);
			}
		}

		public override void OnSave (ConfigNode node)
		{
			control.Save (node);
		}

		public override void OnLoad (ConfigNode node)
		{
			if (HighLogic.LoadedScene == GameScenes.FLIGHT) {
				Debug.Log (String.Format ("[EL SurveyStation] {0} cap: {1} seats: {2}",
						  part, part.CrewCapacity,
						  part.FindModulesImplementing<KerbalSeat> ().Count));
			}
			control.Load (node);
		}

		public override void OnAwake ()
		{
			control = new ELBuildControl (this);
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (state == PartModule.StartState.None
				|| state == PartModule.StartState.Editor) {
				return;
			}
			control.OnStart ();
			if (EVARange > 0) {
				EL_Utils.SetupEVAEvent (Events["ShowRenameUI"], EVARange);
			}
			GameEvents.onVesselSituationChange.Add (onVesselSituationChange);
			GameEvents.onCrewTransferred.Add (onCrewTransferred);
			StartCoroutine (WaitAndDetermineRange ());
			ELSurveyTracker.onSiteAdded.Add (onSiteAdded);
			ELSurveyTracker.onSiteRemoved.Add (onSiteRemoved);
			ELSurveyTracker.onSiteModified.Add (onSiteModified);
		}

		void OnDestroy ()
		{
			if (control != null) {
				control.OnDestroy ();
				GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
				GameEvents.onCrewTransferred.Remove (onCrewTransferred);
				ELSurveyTracker.onSiteAdded.Remove (onSiteAdded);
				ELSurveyTracker.onSiteRemoved.Remove (onSiteRemoved);
				ELSurveyTracker.onSiteModified.Remove (onSiteModified);
			}
		}

		[KSPEvent (guiActive = true, guiName = "Hide UI", active = false)]
		public void HideUI ()
		{
			ELBuildWindow.HideGUI ();
		}

		[KSPEvent (guiActive = true, guiName = "Show UI", active = false)]
		public void ShowUI ()
		{
			ELBuildWindow.ShowGUI ();
			ELBuildWindow.SelectPad (control);
		}

		[KSPEvent (guiActive = true, guiActiveEditor = true,
				   guiName = "Rename", active = true)]
		public void ShowRenameUI ()
		{
			ELRenameWindow.ShowGUI (this);
		}

		public void UpdateMenus (bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}

		void FindSites ()
		{
			available_sites = ELSurveyTracker.instance.FindSites (vessel, range);
			if (available_sites == null || available_sites.Count < 1) {
				Highlight (false);
				site_list = null;
				SetSite (null);
			} else {
				var slist = new List<string> ();
				for (int ind = 0; ind < available_sites.Count; ind++) {
					slist.Add (available_sites[ind].SiteName);
				}
				if (!available_sites.Contains (site)) {
					Highlight (false);
					SetSite (available_sites[0]);
				}
				site_list = new DropDownList (slist);
			}
		}

		IEnumerator WaitAndFindSites ()
		{
			while (!FlightGlobals.ready) {
				yield return null;
			}
			for (int i = 0; i < 10; i++) {
				yield return null;
			}
			FindSites ();
		}

		IEnumerator WaitAndDetermineRange ()
		{
			yield return null;
			DetermineRange ();
		}

		void DetermineRange ()
		{
			var crewList = EL_Utils.GetCrewList (part);
			int bestLevel = -2;
			foreach (var crew in crewList) {
				int level = -1;
				if (crew.GetEffect<ELSurveySkill> () != null) {
					level = crew.experienceLevel;
				}
				if (level > bestLevel) {
					bestLevel = level;
				}
				Debug.LogFormat ("[EL SurveyStation] Kerbal: {0} {1} {2} {3}",
								 crew.name,
								 crew.GetEffect<ELSurveySkill> () != null,
								 crew.experienceLevel, level);
			}
			if (bestLevel > 5) {
				bestLevel = 5;
			}
			range = site_ranges[bestLevel + 2];
			Debug.LogFormat ("[EL SurveyStation] best level: {0}, range: {1}",
							 bestLevel, range);
			if (canBuild) {
				StartCoroutine (WaitAndFindSites ());
			}
		}

		private void onVesselSituationChange (GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vs)
		{
			if (vs.host != vessel) {
				return;
			}
			DetermineRange ();
		}

		void onCrewTransferred (GameEvents.HostedFromToAction<ProtoCrewMember,Part> hft)
		{
			if (hft.from != part && hft.to != part) {
				return;
			}
			Debug.LogFormat ("[EL SurveyStation] transfer: {0} {1} {2}",
							 hft.host, hft.from, hft.to);
			StartCoroutine (WaitAndDetermineRange ());
		}

		void onSiteAdded (SurveySite s)
		{
			Debug.LogFormat ("[ELSurveyStation] onSiteAdded");
			FindSites ();
			SetSite (site);
		}

		void onSiteRemoved (SurveySite s)
		{
			Debug.LogFormat ("[ELSurveyStation] onSiteRemoved");
			if (s == site) {
				site = null;
			}
			FindSites ();
		}

		void onSiteModified (SurveySite s)
		{
			Debug.LogFormat ("[ELSurveyStation] onSiteModified");
			FindSites ();
			SetSite (site);
		}

		public void DoWork (double kerbalHours)
		{
			control.DoWork (kerbalHours);
		}

		public bool isActive
		{
			get {
				return control.isActive;
			}
		}

		public ELVesselWorkNet workNet
		{
			get {
				return control.workNet;
			}
			set {
				control.workNet = value;
			}
		}

		public double CalculateWork ()
		{
			return control.CalculateWork();
		}

		public void OnRename ()
		{
			ELBuildWindow.updateCurrentPads ();
		}
	}
}
