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

namespace ExtraplanetaryLaunchpads {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ELSurveyTracker : MonoBehaviour
	{
		internal static EventData<SurveySite> onSiteRemoved = new EventData<SurveySite> ("onSiteRemoved");
		internal static EventData<SurveySite> onSiteAdded = new EventData<SurveySite> ("onSiteAdded");
		internal static EventData<SurveySite> onSiteModified = new EventData<SurveySite> ("onSiteModified");
		internal static EventData<ELSurveyStake> onStakeModified = new EventData<ELSurveyStake> ("onStakeModified");
		internal static ELSurveyTracker instance;

		Dictionary<string, SiteBody> sites;

		internal List<SurveySite> FindSites (Vessel vessel, double range)
		{
			var site_list = new List<SurveySite> ();
			string bodyName = vessel.mainBody.bodyName;
			if (sites.ContainsKey (bodyName)) {
				foreach (var list in sites[bodyName]) {
					foreach (var site in list) {
						if (site.isClose (vessel, range)) {
							site_list.Add (site);
						}
					}
				}
			}
			return site_list;
		}

		internal SurveySite AddSite (SurveySite site)
		{
			if (!sites.ContainsKey (site.BodyName)) {
				sites[site.BodyName] = new SiteBody ();
			}
			var s = sites[site.BodyName].AddSite (site);
			// if a different site is returned, the given site was merged into
			// another site rather than added.
			if (s == site) {
				onSiteAdded.Fire (site);
			}
			return s;
		}

		internal void RemoveSite (SurveySite site)
		{
			if (sites.ContainsKey (site.BodyName)) {
				sites[site.BodyName].RemoveSite (site);
			}
			onSiteRemoved.Fire (site);
		}

		bool isStake (Vessel vessel)
		{
			if (vessel.loaded) {
				if (vessel.Parts.Count != 1)
					return false;

				if (vessel[0].Modules.OfType<ELSurveyStake> ().Count () < 1)
					return false;
			} else {
				var pvessel = vessel.protoVessel;
				if (pvessel.protoPartSnapshots.Count != 1)
					return false;
				var ppart = pvessel.protoPartSnapshots[0];
				var mod = ppart.FindModule ("ELSurveyStake");
				if (mod == null)
					return false;
				Debug.LogFormat ("[EL ST] stake on rails {0}",
								 vessel.vesselName);
			}
			return true;
		}

		void AddStake (Vessel vessel)
		{
			Debug.Log (String.Format ("[EL ST] AddStake {0} {1}", vessel.vesselName, vessel.mainBody.bodyName));
			SurveySite site = new SurveySite (vessel);
			AddSite (site);
		}

		internal void ModifyStake (Vessel vessel)
		{
			Debug.Log (String.Format ("[EL ST] ModifyStake {0} {1}", vessel.vesselName, vessel.mainBody.bodyName));
			string bodyName = vessel.mainBody.bodyName;
			string siteName = vessel.vesselName;
			if (!sites.ContainsKey (bodyName)
				|| !sites[bodyName].Contains (siteName)) {
				Debug.Log (String.Format ("[EL ST] stake not found"));
				return;
			}
			onSiteModified.Fire (sites[bodyName][siteName].FindSite (vessel));
		}

		internal void RemoveStake (Vessel vessel)
		{
			Debug.Log (String.Format ("[EL ST] RemoveStake {0} {1}", vessel.vesselName, vessel.mainBody.bodyName));
			string bodyName = vessel.mainBody.bodyName;
			string siteName = vessel.vesselName;
			if (!sites.ContainsKey (bodyName)
				|| !sites[bodyName].Contains (siteName)) {
				Debug.Log (String.Format ("[EL ST] stake not found"));
				return;
			}
			sites[bodyName][siteName].RemoveStake (vessel);
		}

		IEnumerator WaitAndAddStake (Vessel vessel)
		{
			while (vessel.vesselName == null || vessel.vesselName == "") {
				yield return null;
			}
			while (vessel.mainBody == null) {
				yield return null;
			}
			AddStake (vessel);
		}

		void onVesselCreate (Vessel vessel)
		{
			if (!isStake (vessel))
				return;
			StartCoroutine (WaitAndAddStake (vessel));
		}

		void onVesselDestroy (Vessel vessel)
		{
			if (!isStake (vessel))
				return;
			RemoveStake (vessel);
		}

		void onVesselRename (GameEvents.HostedFromToAction<Vessel, string> h)
		{
			Vessel vessel = h.host;
			if (!isStake (vessel))
				return;
			vessel.vesselName = h.from;
			RemoveStake (vessel);
			vessel.vesselName = h.to;
			AddStake (vessel);
		}

		void Awake ()
		{
			enabled = true;
			instance = this;
			sites = new Dictionary<string, SiteBody> ();
			GameEvents.onVesselCreate.Add (onVesselCreate);
			GameEvents.onVesselDestroy.Add (onVesselDestroy);
			GameEvents.onVesselRename.Add (onVesselRename);
		}

		void OnDestroy ()
		{
			GameEvents.onVesselCreate.Remove (onVesselCreate);
			GameEvents.onVesselDestroy.Remove (onVesselDestroy);
			GameEvents.onVesselRename.Remove (onVesselRename);
		}

		void Start ()
		{
		}
	}
}
