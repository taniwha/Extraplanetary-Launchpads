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
using System.Collections.Generic;

namespace ExtraplanetaryLaunchpads {
	public class StockResourceContainer : IResourceContainer {
		ProtoPartResourceSnapshot resource;
		Part stock_part;

		public double maxAmount
		{
			get {
				return resource.maxAmount;
			}
		}

		public double amount
		{
			get {
				return resource.amount;
			}
			set { }
		}

		public bool flowState
		{
			get { return false; }
			set { }
		}

		public Part part
		{
			get {
				return stock_part;
			}
		}

		public StockResourceContainer (Part part, ProtoPartResourceSnapshot res)
		{
			stock_part = part;
			resource = res;
		}

		static double GetResources (ModuleInventoryPart inv, Dictionary<string, RMResourceInfo> resources)
		{
			double resMass = 0;
			var storedParts = inv.storedParts;
			foreach (var storedPart in storedParts.Values) {
				if (storedPart.quantity != 1) {
					// stock parts cannot be stacked if they contain resources
					// and while 0 shouldn't happen, no harm in checking that
					continue;
				}
				var part = storedPart.snapshot;
				foreach (var res in part.resources) {
					RMResourceInfo resourceInfo;
					var resourceName = res.resourceName;
					if (!resources.ContainsKey (resourceName)) {
						resourceInfo = new RMResourceInfo ();
						resources[resourceName] = resourceInfo;
					}
					resourceInfo = resources[resourceName];
					resourceInfo.containers.Add (new StockResourceContainer (inv.part, res));
					resMass += res.amount * res.definition.density;
				}
			}
			return resMass;
		}

		public static double GetResources (Part part, Dictionary<string, RMResourceInfo> resources)
		{
			double resMass = 0;
			foreach (PartModule mod in part.Modules) {
				var inv = mod as ModuleInventoryPart;
				if (inv != null) {
					resMass += GetResources (inv, resources);
				}
			}
			return resMass;
		}
	}
}
