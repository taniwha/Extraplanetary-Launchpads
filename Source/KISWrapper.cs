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
	public class KISWrapper {
		public static MethodInfo kis_GetResources;
		public static MethodInfo kis_SetResource;
		public static FieldInfo kis_items;
		public static FieldInfo kis_resourceName;
		public static FieldInfo kis_amount;
		public static FieldInfo kis_maxAmount;

		public static Dictionary<int,object> Items (PartModule mod)
		{
			if (mod.moduleName != "ModuleKISInventory") {
				return null;
			}
			var dict = new Dictionary<int,object> ();
			var items = (IDictionary) kis_items.GetValue (mod);
			foreach (DictionaryEntry de in items) {
				dict.Add ((int) de.Key, de.Value);
			}
			return dict;
		}

		public static void Initialize ()
		{
			var KISasm = AssemblyLoader.loadedAssemblies.Where (a => a.assembly.GetName ().Name.Equals ("KIS", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault ().assembly;
			var InventoryModule = KISasm.GetTypes().Where (t => t.Name.Equals ("ModuleKISInventory")).FirstOrDefault ();
			kis_items = InventoryModule.GetField ("items");
			var ItemClass = KISasm.GetTypes().Where (t => t.Name.Equals ("KIS_Item")).FirstOrDefault ();
			kis_GetResources = ItemClass.GetMethod ("GetResources");
			kis_SetResource = ItemClass.GetMethod ("SetResource");

			var ResourceInfoClass = KISasm.GetTypes().Where (t => t.Name.Equals ("ResourceInfo")).FirstOrDefault ();
			kis_resourceName = ResourceInfoClass.GetField ("resourceName");
			kis_maxAmount = ResourceInfoClass.GetField ("maxAmount");
			kis_amount = ResourceInfoClass.GetField ("amount");
		}

		static void GetResources (PartModule mod, Dictionary<string, ResourceInfo> resources)
		{
			var items = KISWrapper.Items (mod);
			foreach (var item in items.Values) {
				var kis_resources = (IList) kis_GetResources.Invoke (item, null);
				ResourceInfo resourceInfo;
				foreach (var res in kis_resources) {
					var resourceName = (string) kis_resourceName.GetValue (res);
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
