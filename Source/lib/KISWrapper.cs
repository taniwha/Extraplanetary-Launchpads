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
		static FieldInfo kis_partNode;
		static PropertyInfo kis_itemResourceMass;

		object obj;

		public ConfigNode partNode { get { return (ConfigNode)kis_partNode.GetValue (obj); } }
		public double itemResourceMass { get { return (double)kis_itemResourceMass.GetValue (obj, null); } }

		public struct ResourceInfo {
			public string resourceName { get; private set; }
			public double maxAmount { get; private set; }
			public double amount { get; private set; }

			public ResourceInfo (ConfigNode node)
			{
				double val;
				resourceName = node.GetValue ("name");
				double.TryParse (node.GetValue ("maxAmount"), out val);
				maxAmount = val;
				double.TryParse (node.GetValue ("amount"), out val);
				amount = val;
			}
		}

		public KIS_Item (object obj)
		{
			this.obj = obj;
		}

		public List<ResourceInfo> GetResources ()
		{
			var resources = new List<ResourceInfo> ();
			var kis_resources = partNode.GetNodes ("RESOURCE");
			foreach (var res in kis_resources) {
				resources.Add (new ResourceInfo (res));
			}
			return resources;
		}

		internal static void Initialize (Assembly KISasm)
		{
			KIS_Item_class = KISasm.GetTypes().Where (t => t.Name.Equals ("KIS_Item")).FirstOrDefault ();
			kis_partNode = KIS_Item_class.GetField ("partNode");
			kis_itemResourceMass = KIS_Item_class.GetProperty ("itemResourceMass");
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

		internal static bool Initialize (Assembly KISasm)
		{
			var types = KISasm.GetTypes ();

			foreach (var t in types) {
				if (t.Name == "ModuleKISInventory") {
					ModuleKISInventory_class = t;
					kis_items = ModuleKISInventory_class.GetField ("items");
					Debug.Log ($"[KISWrapper] ModuleKISInventory {kis_items}");
					return true;
				}
			}
			return false;
		}
	}

	public class KISWrapper {
		static bool haveKIS = false;
		static bool inited = false;

		public static bool Initialize ()
		{
			if (!inited) {
				inited = true; // do this only once, assemblies won't change
				AssemblyLoader.LoadedAssembly KISasm = null;

				foreach (var la in AssemblyLoader.loadedAssemblies) {
					if (la.assembly.GetName ().Name.Equals ("KIS", StringComparison.InvariantCultureIgnoreCase)) {
						KISasm = la;
					}
				}
				if (KISasm != null) {
					Debug.Log ($"[KISWrapper] found KIS {KISasm}");
					ModuleKISInventory.Initialize (KISasm.assembly);
					KIS_Item.Initialize (KISasm.assembly);
					haveKIS = true;
				}
			}
			return haveKIS;
		}

		static double GetResources (ModuleKISInventory inv, Dictionary<string, RMResourceInfo> resources)
		{
			double resMass = 0;
			var items = inv.items;
			foreach (var item in items.Values) {
				resMass += item.itemResourceMass;
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
			return resMass;
		}

		public static double GetResources (Part part, Dictionary<string, RMResourceInfo> resources)
		{
			double resMass = 0;
			foreach (PartModule mod in part.Modules) {
				if (mod.moduleName == "ModuleKISInventory") {
					var inv = new ModuleKISInventory (mod);
					resMass += GetResources (inv, resources);
				}
			}
			return resMass;
		}
	}
}
