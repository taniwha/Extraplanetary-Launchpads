using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExSurveyTracker : MonoBehaviour
	{
		internal static ExSurveyTracker instance;

		internal class SurveySite
		{
			List<Vessel> stakes;

			internal ExSurveyStake this[int index]
			{
				get {
					var m = stakes[index][0].Modules.OfType<ExSurveyStake> ();
					return m.FirstOrDefault ();
				}
			}

			public int Count
			{
				get {
					return stakes.Count;
				}
			}

			public CelestialBody Body
			{
				get {
					if (stakes.Count < 1)
						return null;
					return stakes[0].mainBody;
				}
			}

			public string BodyName
			{
				get {
					if (stakes.Count < 1)
						return null;
					return stakes[0].mainBody.bodyName;
				}
			}

			public string SiteName
			{
				get {
					if (stakes.Count < 1)
						return null;
					return stakes[0].vesselName;
				}
			}

			public bool isClose (Vessel vessel, double range = 200.0)
			{
				var pos = vessel.GetWorldPos3D ();
				Debug.Log (String.Format ("[EL ST] isClose {0}", range));
				range *= range;
				foreach (Vessel stake in stakes) {
					var stake_pos = stake.GetWorldPos3D ();
					var offs = pos - stake_pos;
					Debug.Log (String.Format ("[EL ST] isClose {0} {1} {2} {3}", pos, stake_pos, Vector3d.Dot (offs, offs), range));
					if (Vector3d.Dot (offs, offs) < range) {
						return true;
					}
				}
				return false;
			}

			public bool isClose (SurveySite site)
			{
				foreach (var stake in site.stakes) {
					if (isClose (stake)) {
						return true;
					}
				}
				return false;
			}

			public void Merge (SurveySite site)
			{
				Debug.Log (String.Format ("[EL ST] merge {0} {1}", stakes.Count, site.stakes.Count));
				stakes.AddRange (site.stakes);
				site.stakes.Clear ();
			}

			public bool Contains (Vessel stake)
			{
				return stakes.Contains (stake);
			}

			public void AddStake (Vessel stake)
			{
				stakes.Add (stake);
			}

			public void RemoveStake (Vessel stake)
			{
				if (!stakes.Contains (stake)) {
					return;
				}
				stakes.Remove (stake);
				while (stakes.Count > 0) {
					List<Vessel> old_stakes = stakes;
					stakes = new List<Vessel> ();
					stakes.Add (old_stakes[0]);
					old_stakes.RemoveAt (0);
					for (int i = 0; i < old_stakes.Count; ) {
						if (isClose (old_stakes[i])) {
							stakes.Add (old_stakes[i]);
							old_stakes.RemoveAt (i);
							continue;
						}
						i++;
					}
					if (old_stakes.Count > 0) {
						Debug.Log (String.Format ("[EL ST] split {0} {1}", stakes.Count, old_stakes.Count));
						SurveySite site = new SurveySite (stakes);
						stakes = old_stakes;

						ExSurveyTracker.instance.AddSite (site);
					} else {
						break;
					}
				}
			}

			public SurveySite (List<Vessel> stakes)
			{
				this.stakes = stakes;
			}

			public SurveySite (Vessel stake)
			{
				stakes = new List<Vessel> ();
				stakes.Add (stake);
			}

			public SurveySite ()
			{
				stakes = new List<Vessel> ();
			}

			public IEnumerator<ExSurveyStake> GetEnumerator ()
			{
				foreach (var stake in stakes) {
					var m = stake[0].Modules.OfType<ExSurveyStake> ();
					yield return m.FirstOrDefault ();
				}
			}
		}

		class SiteList
		{
			List<SurveySite> sites;

			internal SurveySite this[int index]
			{
				get {
					return sites[index];
				}
			}

			public IEnumerator<SurveySite> GetEnumerator ()
			{
				return sites.GetEnumerator ();
			}

			internal int Count
			{
				get {
					return sites.Count;
				}
			}

			internal SiteList ()
			{
				sites = new List<SurveySite> ();
			}

			internal SurveySite AddSite (SurveySite site)
			{
				for (int i = 0; i < sites.Count; i++) {
					if (sites[i].isClose (site)) {
						sites[i].Merge (site);
						for (int j = i + 1; j < sites.Count; ) {
							if (sites[i].isClose (sites[j])) {
								sites[i].Merge (sites[j]);
								sites.RemoveAt (j);
								continue;
							}
							j++;
						}
						return sites[i];
					}
				}
				sites.Add (site);
				return site;
			}

			internal void RemoveStake (Vessel stake)
			{
				foreach (var site in sites) {
					if (site.Contains (stake)) {
						site.RemoveStake (stake);
						return;
					}
				}
			}

			internal void Clean ()
			{
				for (int i = 0; i < sites.Count; ) {
					if (sites[i].Count < 1) {
						sites.RemoveAt (i);
						continue;
					}
					i++;
				}
			}
		}

		class SiteBody
		{
			Dictionary<string, SiteList> sites;

			public IEnumerator<SiteList> GetEnumerator ()
			{
				return sites.Values.GetEnumerator ();
			}

			internal int Count
			{
				get {
					return sites.Count;
				}
			}

			internal bool Contains (string site)
			{
				return sites.ContainsKey (site);
			}

			internal SiteList this [string siteName]
			{
				get {
					return sites[siteName];
				}
			}

			internal SiteBody ()
			{
				sites = new Dictionary<string, SiteList> ();
			}

			internal SurveySite AddSite (SurveySite site)
			{
				if (!sites.ContainsKey (site.SiteName)) {
					sites[site.SiteName] = new SiteList ();
				}
				return sites[site.SiteName].AddSite (site);
			}

			internal void Clean ()
			{
				foreach (var site in sites.Keys) {
					if (sites[site].Count < 1) {
						sites.Remove (site);
					}
				}
			}
		}

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

		void Clean ()
		{
			foreach (var site in sites.Keys) {
				if (sites[site].Count < 1) {
					sites.Remove (site);
				}
			}
		}

		SurveySite AddSite (SurveySite site)
		{
			if (!sites.ContainsKey (site.BodyName)) {
				sites[site.BodyName] = new SiteBody ();
			}
			site = sites[site.BodyName].AddSite (site);
			Clean ();
			return site;
		}

		bool isStake (Vessel vessel)
		{
			if (vessel.Parts.Count != 1)
				return false;

			if (vessel[0].Modules.OfType<ExSurveyStake> ().Count () < 1)
				return false;
			return true;
		}

		void AddStake (Vessel vessel)
		{
			Debug.Log (String.Format ("[EL ST] AddStake {0} {1}", vessel.vesselName, vessel.mainBody.bodyName));
			SurveySite site = new SurveySite ();
			site.AddStake (vessel);
			AddSite (site);
		}

		void RemoveStake (Vessel vessel)
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
			Clean ();
		}

		IEnumerator<YieldInstruction> WaitAndAddStake (Vessel vessel)
		{
			while (vessel.vesselName == null || vessel.vesselName == "") {
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

		IEnumerator<YieldInstruction> WaitAndLoadSites ()
		{
			while (!FlightGlobals.ready) {
				yield return null;
			}
			foreach (var vessel in FlightGlobals.Vessels) {
				if (isStake (vessel)) {
					AddStake (vessel);
				}
			}
		}

		void Start ()
		{
			StartCoroutine (WaitAndLoadSites ());
		}
	}
}
