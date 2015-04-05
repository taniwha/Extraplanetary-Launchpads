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
		public static MethodInfo GetResources;
		public static MethodInfo SetResource;
		public static FieldInfo items;
		public static FieldInfo resourceName;
		public static FieldInfo amount;
		public static FieldInfo maxAmount;

		public static Dictionary<int,object> Items (PartModule mod)
		{
			if (mod.moduleName != "ModuleKISInventory") {
				return null;
			}
			var dict = new Dictionary<int,object> ();
			var kis_items = (IDictionary) items.GetValue (mod);
			foreach (DictionaryEntry de in kis_items) {
				dict.Add ((int) de.Key, de.Value);
			}
			return dict;
		}

		public static void Initialize ()
		{
			var KISasm = AssemblyLoader.loadedAssemblies.Where (a => a.assembly.GetName ().Name.Equals ("KIS", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault ().assembly;
			var InventoryModule = KISasm.GetTypes().Where (t => t.Name.Equals ("ModuleKISInventory")).FirstOrDefault ();
			items = InventoryModule.GetField ("items");
			var ItemClass = KISasm.GetTypes().Where (t => t.Name.Equals ("KIS_Item")).FirstOrDefault ();
			GetResources = ItemClass.GetMethod ("GetResources");
			SetResource = ItemClass.GetMethod ("SetResource");

			var ResourceInfoClass = KISasm.GetTypes().Where (t => t.Name.Equals ("ResourceInfo")).FirstOrDefault ();
			resourceName = ResourceInfoClass.GetField ("resourceName");
			maxAmount = ResourceInfoClass.GetField ("maxAmount");
			amount = ResourceInfoClass.GetField ("amount");
		}
	}
}
