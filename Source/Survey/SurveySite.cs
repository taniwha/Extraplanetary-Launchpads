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

namespace ExtraplanetaryLaunchpads {
	internal class SurveySite
	{
		List<Vessel> stakes;

		internal ELSurveyStake.Data this[int index]
		{
			get {
				Vessel v = stakes[index];
				if (v.loaded) {
					var stake = v[0].FindModuleImplementing<ELSurveyStake> ();
					return stake.GetData ();
				} else {
					var ppart = v.protoVessel.protoPartSnapshots[0];
					var stake = ppart.FindModule ("ELSurveyStake");
					return ELSurveyStake.GetData (stake, v);
				}
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
			get;
			private set;
		}

		public string BodyName
		{
			get {
				return Body.bodyName;
			}
		}

		public string SiteName
		{
			get;
			private set;
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
			ELSurveyTracker.onSiteModified.Fire (this);
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

					ELSurveyTracker.instance.AddSite (site);
				} else {
					break;
				}
			}
			if (stakes.Count == 0) {
				ELSurveyTracker.instance.RemoveSite (this);
			} else {
				ELSurveyTracker.onSiteModified.Fire (this);
			}
		}

		public SurveySite (List<Vessel> stakes)
		{
			this.stakes = stakes;
			Body = stakes[0].mainBody;
			SiteName = stakes[0].vesselName;
		}

		public SurveySite (Vessel stake)
		{
			stakes = new List<Vessel> ();
			stakes.Add (stake);
			Body = stakes[0].mainBody;
			SiteName = stakes[0].vesselName;
		}

		public IEnumerator<ELSurveyStake.Data> GetEnumerator ()
		{
			for (int i = 0; i < stakes.Count; i++) {
				yield return this[i];
			}
		}
	}
}
