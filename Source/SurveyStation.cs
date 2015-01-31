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

namespace ExLP {

	public class ExSurveyStation : PartModule, ExBuildControl.IBuilder
	{
		[KSPField (isPersistant = true)]
		public string StationName = "";

		DropDownList site_list;
		List<ExSurveyTracker.SurveySite> available_sites;
		ExSurveyTracker.SurveySite site;
		float base_mass;

		public bool capture
		{
			get {
				return false;
			}
		}

		public ExBuildControl control
		{
			get;
			private set;
		}

		public new Vessel vessel
		{
			get {
				if (site != null) {
					return site[0].part.vessel;
				}
				return base.vessel;
			}
		}

		public new Part part
		{
			get {
				if (site != null) {
					return site[0].part;
				}
				return base.part;
			}
		}

		public string Name
		{
			get {
				return StationName;
			}
		}

		public void PadSelection_start ()
		{
			if (site_list == null) {
				return;
			}
			site_list.styleListItem = ExBuildWindow.Styles.listItem;
			site_list.styleListBox = ExBuildWindow.Styles.listBox;
			site_list.DrawBlockingSelector ();
		}

		void Select_Site (ExSurveyTracker.SurveySite selected_site)
		{
			if (site != selected_site) {
				Highlight (false);
			}
			site = selected_site;
			site_list.SelectItem (available_sites.IndexOf (site));
			// The build window will take care of turning on highlighting
		}

		public void PadSelection ()
		{
			if (site_list == null) {
				GUILayout.BeginHorizontal ();
				GUILayout.Label ("Where do you want it, boss?");
				GUILayout.EndHorizontal ();
			} else {
				GUILayout.BeginHorizontal ();
				site_list.DrawButton ();
				Select_Site (available_sites[site_list.SelectedIndex]);
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
			if (site != null) {
				foreach (var stake in site) {
					stake.Highlight (on);
				}
			}
		}

		[KSPEvent (guiActive=false, active = true)]
		void ExDiscoverWorkshops (BaseEventData data)
		{
			data.Get<List<ExWorkSink>> ("sinks").Add (control);
		}

		class Points
		{
			Dictionary<string, Vector3d> points;
			CelestialBody body;
			public Vector3d center;

			public Points (ExSurveyTracker.SurveySite site)
			{
				Dictionary<string, int> counts = new Dictionary<string, int> ();
				int count = 0;

				points = new Dictionary<string, Vector3d> ();

				body = site.Body;

				center = Vector3d.zero;
				foreach (var stake in site) {
					string key = ExSurveyStake.StakeUses[stake.use];
					if (stake.bound) {
						key = key + "!";
					}
					var pos = stake.vessel.GetWorldPos3D ();
					center += pos;
					count++;
					if (points.ContainsKey (key)) {
						points[key] += pos;
						counts[key] += 1;
					} else {
						points[key] = pos;
						counts[key] = 1;
					}
				}
				center /= (double) count;
				foreach (var key in ExSurveyStake.StakeUses) {
					if (points.ContainsKey (key)) {
						points[key] /= (double) counts[key];
					}
				}
				if (points.ContainsKey ("Origin")) {
					center = points["Origin"];
				}
			}

			public Vector3d Up ()
			{
				double lat = body.GetLatitude (center);
				double lon = body.GetLongitude (center);
				return body.GetSurfaceNVector (lat, lon);
			}

			public Vector3d GetDirection (string dir)
			{
				Vector3d v;
				string p = "+" + dir;
				string m = "-" + dir;

				if (points.ContainsKey (p)) {
					if (points.ContainsKey (m)) {
						v = Vector3d.Normalize (points[p] - points[m]);
					} else {
						v = Vector3d.Normalize (points[p] - center);
					}
				} else if (points.ContainsKey (m)) {
					v = Vector3d.Normalize (center - points[m]);
				} else {
					v = Vector3d.zero;
				}
				return v;
			}

			public Quaternion ChooseRotation (Vector3d r, Vector3d f)
			{
				// find a reference frame that is close to the given possibly
				// non-orthogonal frame, but where up is always up
				Vector3d u = Up ();
				r = Vector3d.Normalize (r - Vector3d.Dot (r, u) * u);
				f = Vector3d.Normalize (f - Vector3d.Dot (f, u) * u);
				f = Vector3d.Normalize (f + Vector3d.Cross (r, u));
				return Quaternion.LookRotation (f, u);
			}

			public Quaternion ChooseRotation (Vector3d r, Vector3d f, Vector3d u)
			{
				// find a reference frame that is close to the given possibly
				// non-orthogonal frame
				u = u + Vector3d.Normalize (Vector3d.Cross (f, r));
				u.Normalize ();
				r = Vector3d.Normalize (r - Vector3d.Dot (r, u) * u);
				f = Vector3d.Normalize (f - Vector3d.Dot (f, u) * u);
				f = Vector3d.Normalize (f + Vector3d.Cross (r, u));
				return Quaternion.LookRotation (f, u);
			}
		}

		Quaternion GetOrientation (Points p)
		{
			var x = p.GetDirection ("X");
			var y = p.GetDirection ("Y");
			var z = p.GetDirection ("Z");
			Quaternion rot;
			if (y.IsZero ()) {
				if (z.IsZero () && x.IsZero ()) {
					x = Vector3d.Cross (p.Up (), Vector3d.up);
					x.Normalize ();
					z = Vector3d.Cross (x, p.Up ());
				} else if (z.IsZero ()) {
					z = Vector3d.Cross (x, p.Up ());
					z.Normalize ();
				} else if (x.IsZero ()) {
					x = Vector3d.Cross (p.Up (), z);
					x.Normalize ();
				}
				rot = p.ChooseRotation (x, z);
			} else if (x.IsZero ()) {
				// y is not zero
				if (z.IsZero ()) {
					z = Vector3d.Cross (p.Up (), y);
					z.Normalize ();
				}
				rot = p.ChooseRotation (z, y);
			} else if (z.IsZero ()) {
				// neither x nor y are zero
				rot = p.ChooseRotation (y, x);
			} else {
				// no direction is 0
				rot = p.ChooseRotation (x, z, y);
			}
			return rot;
		}

		public void SetCraftMass (double mass)
		{
			base.part.mass = base_mass + (float) mass;
		}

		public Transform PlaceShip (ShipConstruct ship, ExBuildControl.Box vessel_bounds)
		{
			if (site == null) {
				return part.transform;
			}
			Transform xform;
			xform = part.FindModelTransform ("EL launch pos");
			if (xform == null) {
				var p = new Points (site);
				Transform t = part.transform;
				GameObject launchPos = new GameObject ("EL launch pos");
				launchPos.transform.parent = t;
				launchPos.transform.position = t.position;
				launchPos.transform.position += p.center - part.vessel.GetWorldPos3D ();
				launchPos.transform.rotation = GetOrientation (p);
				xform = launchPos.transform;
				Debug.Log (String.Format ("[EL] launchPos {0}", xform));
			}
			Debug.Log (String.Format ("[EL] launchPos {0} {1}", xform.position, xform.rotation));

			float angle;
			Vector3 axis;
			xform.rotation.ToAngleAxis (out angle, out axis);

			Vector3 pos = ship.parts[0].transform.position;
			Vector3 shift = new Vector3 (-pos.x, -vessel_bounds.min.y, -pos.z);
			shift += xform.position;
			ship.parts[0].transform.Translate (shift, Space.World);
			ship.parts[0].transform.RotateAround (xform.position,
												  axis, angle);
			return xform;
		}

		public override void OnSave (ConfigNode node)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			control.Save (node);
			if (base_mass != 0) {
				node.AddValue ("baseMass", base_mass);
			}
		}

		public override void OnLoad (ConfigNode node)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			control.Load (node);
			if (node.HasValue ("baseMass")) {
				float.TryParse (node.GetValue ("baseMass"), out base_mass);
			} else {
				base_mass = base.part.mass;
			}
		}

		public override void OnAwake ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			control = new ExBuildControl (this);
		}

		public override void OnStart (PartModule.StartState state)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				Events["HideUI"].active = false;
				Events["ShowUI"].active = false;
				return;
			}
			if (state == PartModule.StartState.None
				|| state == PartModule.StartState.Editor) {
				return;
			}
			control.OnStart ();
			GameEvents.onVesselSituationChange.Add (onVesselSituationChange);
			if (vessel.situation == Vessel.Situations.LANDED
				|| vessel.situation == Vessel.Situations.SPLASHED
				|| vessel.situation == Vessel.Situations.PRELAUNCH) {
				StartCoroutine (WaitAndFindSites ());
			}
			ExSurveyTracker.onSiteAdded.Add (onSiteAdded);
			ExSurveyTracker.onSiteRemoved.Add (onSiteRemoved);
			ExSurveyTracker.onSiteModified.Add (onSiteModified);
		}

		void OnDestroy ()
		{
			control.OnDestroy ();
			GameEvents.onVesselSituationChange.Remove (onVesselSituationChange);
			ExSurveyTracker.onSiteAdded.Remove (onSiteAdded);
			ExSurveyTracker.onSiteRemoved.Remove (onSiteRemoved);
			ExSurveyTracker.onSiteModified.Remove (onSiteModified);
		}

		[KSPEvent (guiActive = true, guiName = "Hide UI", active = false)]
		public void HideUI ()
		{
			ExBuildWindow.HideGUI ();
		}

		[KSPEvent (guiActive = true, guiName = "Show UI", active = false)]
		public void ShowUI ()
		{
			ExBuildWindow.ShowGUI ();
			ExBuildWindow.SelectPad (control);
		}

		public void UpdateMenus (bool visible)
		{
			Events["HideUI"].active = visible;
			Events["ShowUI"].active = !visible;
		}

		void FindSites ()
		{
			available_sites = ExSurveyTracker.instance.FindSites (vessel, 100.0);
			if (available_sites == null || available_sites.Count < 1) {
				Highlight (false);
				site = null;
				site_list = null;
			} else {
				var slist = new List<string> ();
				for (int ind = 0; ind < available_sites.Count; ind++) {
					slist.Add (available_sites[ind].SiteName);
				}
				if (!available_sites.Contains (site)) {
					Highlight (false);
					site = available_sites[0];
				}
				site_list = new DropDownList (slist);
			}
			Debug.Log (String.Format ("[EL SS] {0}", site));
		}

		IEnumerator<YieldInstruction> WaitAndFindSites ()
		{
			while (!FlightGlobals.ready) {
				yield return null;
			}
			for (int i = 0; i < 10; i++) {
				yield return null;
			}
			FindSites ();
		}

		private void onVesselSituationChange (GameEvents.HostedFromToAction<Vessel, Vessel.Situations> vs)
		{
			if (vs.host != vessel) {
				return;
			}
			if (vessel.situation == Vessel.Situations.LANDED
				|| vessel.situation == Vessel.Situations.PRELAUNCH) {
				StartCoroutine (WaitAndFindSites ());
			}
		}

		void onSiteAdded (ExSurveyTracker.SurveySite s)
		{
			FindSites ();
		}

		void onSiteRemoved (ExSurveyTracker.SurveySite s)
		{
			if (s == site) {
				site = null;
			}
		}

		void onSiteModified (ExSurveyTracker.SurveySite s)
		{
		}
	}
}
