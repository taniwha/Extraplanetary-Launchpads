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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class KISResourceContainer : IResourceContainer {
		object resource;
		Part kis_part;

		public double maxAmount
		{
			get {
				return (double) KISWrapper.maxAmount.GetValue (resource);
			}
			set {
				KISWrapper.maxAmount.SetValue (resource, value);
			}
		}

		public double amount
		{
			get {
				return (double) KISWrapper.amount.GetValue (resource);
			}
			set {
				KISWrapper.amount.SetValue (resource, value);
			}
		}

		public Part part
		{
			get {
				return kis_part;
			}
		}

		public KISResourceContainer (Part part, object res)
		{
			kis_part = part;
			resource = res;
		}

		static void GetResources (PartModule mod, Dictionary<string, ResourceInfo> resources)
		{
			var items = KISWrapper.Items (mod);
			foreach (var item in items.Values) {
				var kis_resources = (IList) KISWrapper.GetResources.Invoke (item, null);
				ResourceInfo resourceInfo;
				foreach (var res in kis_resources) {
					var resourceName = (string) KISWrapper.resourceName.GetValue (res);
					if (!resources.ContainsKey (resourceName)) {
						resourceInfo = new ResourceInfo ();
						resources[resourceName] = resourceInfo;
					}
					resourceInfo = resources[resourceName];
					resourceInfo.containers.Add (new KISResourceContainer (mod.part, res));
				}
			}
		}

		public static void GetResources (Part part, Dictionary<string, ResourceInfo> resources)
		{
			foreach (PartModule mod in part.Modules) {
				if (mod.moduleName == "ModuleKISInventory") {
					GetResources (mod, resources);
				}
			}
		}
	}
}
