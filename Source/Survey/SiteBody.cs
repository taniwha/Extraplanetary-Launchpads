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
	internal class SiteBody
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

		internal void RemoveSite (SurveySite site)
		{
			if (sites.ContainsKey (site.SiteName)) {
				sites[site.SiteName].RemoveSite (site);
			}
		}
	}
}
