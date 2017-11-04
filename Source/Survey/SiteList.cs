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
	internal class SiteList
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
							var s = sites[j];
							sites[i].Merge (sites[j]);
							sites.RemoveAt (j);
							ELSurveyTracker.onSiteRemoved.Fire (s);
							continue;
						}
						j++;
					}
					ELSurveyTracker.onSiteModified.Fire (sites[i]);
					return sites[i];
				}
			}
			sites.Add (site);
			return site;
		}

		internal void RemoveSite (SurveySite site)
		{
			for (int i = 0; i < sites.Count; i++) {
				if (sites[i] == site) {
					sites.RemoveAt (i);
				}
			}
		}

		internal SurveySite FindSite (Vessel stake)
		{
			foreach (var site in sites) {
				if (site.Contains (stake)) {
					return site;
				}
			}
			return null;
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
	}
}
