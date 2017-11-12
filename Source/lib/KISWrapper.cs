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

using ExtraplanetaryLaunchpads;

namespace ExtraplanetaryLaunchpads.KIS {

	public class KIS_Item {
		static Type KIS_Item_class;
		static MethodInfo kis_GetResources;
		static MethodInfo kis_SetResource;

		object obj;

		public struct ResourceInfo {
			static Type ResourceInfo_class;
			static FieldInfo kis_resourceName;
			static FieldInfo kis_amount;
			static FieldInfo kis_maxAmount;

			object obj;

			public string resourceName
			{
				get {
					return (string) kis_resourceName.GetValue (obj);
				}
				set {
					kis_resourceName.SetValue (obj, value);
				}
			}

			public double maxAmount
			{
				get {
					return (double) kis_maxAmount.GetValue (obj);
				}
				set {
					kis_maxAmount.SetValue (obj, value);
				}
			}

			public double amount
			{
				get {
					return (double) kis_amount.GetValue (obj);
				}
				set {
					kis_amount.SetValue (obj, value);
				}
			}

			internal static void Initialize (Assembly KISasm)
			{
				ResourceInfo_class = KISasm.GetTypes().Where (t => t.Name.Equals ("ResourceInfo")).FirstOrDefault ();
				kis_resourceName = ResourceInfo_class.GetField ("resourceName");
				kis_maxAmount = ResourceInfo_class.GetField ("maxAmount");
				kis_amount = ResourceInfo_class.GetField ("amount");
			}

			public ResourceInfo (object obj)
			{
				this.obj = obj;
			}
		}

		public KIS_Item (object obj)
		{
			this.obj = obj;
		}

		public List<ResourceInfo> GetResources ()
		{
			var resources = new List<ResourceInfo> ();
			var kis_resources = (IList) kis_GetResources.Invoke (obj, null);
			foreach (var item in kis_resources) {
				resources.Add (new ResourceInfo (item));
			}
			return resources;
		}

		public void SetResource(string name, double amount)
		{
			kis_SetResource.Invoke (obj, new object[]{name, amount});
		}

		internal static void Initialize (Assembly KISasm)
		{
			KIS_Item_class = KISasm.GetTypes().Where (t => t.Name.Equals ("KIS_Item")).FirstOrDefault ();
			kis_GetResources = KIS_Item_class.GetMethod ("GetResources");
			kis_SetResource = KIS_Item_class.GetMethod ("SetResource");
		}
	}

	public class ModuleKISInventory {
		static Type ModuleKISInventory_class;
		static FieldInfo kis_items;

		object obj;

		public Dictionary<int,KIS_Item> items
		{
			get {
				var dict = new Dictionary<int,KIS_Item> ();
				var items = (IDictionary) kis_items.GetValue (obj);
				foreach (DictionaryEntry de in items) {
					dict.Add ((int) de.Key, new KIS_Item(de.Value));
				}
				return dict;
			}
		}

		public Part part
		{
			get {
				return (obj as PartModule).part;
			}
		}

		public ModuleKISInventory (object obj)
		{
			this.obj = obj;
		}

		internal static void Initialize (Assembly KISasm)
		{
			ModuleKISInventory_class = KISasm.GetTypes().Where (t => t.Name.Equals ("ModuleKISInventory")).FirstOrDefault ();
			kis_items = ModuleKISInventory_class.GetField ("items");
		}
	}

	public class KISWrapper {

		public static bool Initialize ()
		{
			var KISasm = AssemblyLoader.loadedAssemblies.Where (a => a.assembly.GetName ().Name.Equals ("KIS", StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault ();
			if (KISasm == null) {
				return false;
			}

			ModuleKISInventory.Initialize (KISasm.assembly);
			KIS_Item.Initialize (KISasm.assembly);
			KIS_Item.ResourceInfo.Initialize (KISasm.assembly);
			return true;
		}

		static void GetResources (ModuleKISInventory inv, Dictionary<string, RMResourceInfo> resources)
		{
			var items = inv.items;
			foreach (var item in items.Values) {
				var kis_resources = item.GetResources ();
				foreach (var res in kis_resources) {
					RMResourceInfo resourceInfo;
					var resourceName = res.resourceName;
					if (!resources.ContainsKey (resourceName)) {
						resourceInfo = new RMResourceInfo ();
						resources[resourceName] = resourceInfo;
					}
					resourceInfo = resources[resourceName];
					resourceInfo.containers.Add (new KISResourceContainer (inv.part, res));
				}
			}
		}

		public static void GetResources (Part part, Dictionary<string, RMResourceInfo> resources)
		{
			foreach (PartModule mod in part.Modules) {
				if (mod.moduleName == "ModuleKISInventory") {
					var inv = new ModuleKISInventory (mod);
					GetResources (inv, resources);
				}
			}
		}
	}
}
