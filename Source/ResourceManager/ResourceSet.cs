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
// Thanks to Taranis Elsu and his Fuel Balancer mod for the inspiration.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	public delegate void RMResourceProcessor (RMResourceSet vr, string resource);

	public class RMResourceSet {
		public Dictionary<string, RMResourceInfo> resources;
		public List<Part> parts;
		public List<RMResourceSet> sets;
		public bool balanced;
		public string name;
		public uint id;	// from part definining the module if relevant

		public RMResourceInfo this[string res]
		{
			get {
				RMResourceInfo info;
				resources.TryGetValue (res, out info);
				return info;
			}
		}

		public bool GetFlowState (string res)
		{
			if (resources.ContainsKey (res)) {
				return resources[res].flowState;
			}
			return false;
		}

		public void SetFlowState (string res, bool state)
		{
			if (resources.ContainsKey (res)) {
				resources[res].flowState = state;
			}
		}

		public void AddPart (Part part)
		{
			if (part.Resources.Count > 0) {
				parts.Add (part);
			}
			foreach (PartResource resource in part.Resources) {
				RMResourceInfo resourceInfo;
				if (!resources.ContainsKey (resource.resourceName)) {
					resourceInfo = new RMResourceInfo ();
					resources[resource.resourceName] = resourceInfo;
				}
				resourceInfo = resources[resource.resourceName];
				resourceInfo.containers.Add (new PartResourceContainer (resource));
			}
		}

		public void AddSet (RMResourceSet set)
		{
			sets.Add (set);
			foreach (var resource in set.resources.Keys) {
				RMResourceInfo resourceInfo;
				if (!resources.ContainsKey (resource)) {
					resourceInfo = new RMResourceInfo ();
					resources[resource] = resourceInfo;
				}
				resourceInfo = resources[resource];
				resourceInfo.containers.Add (new ResourceSetContainer (resource, set));
			}
		}

		public void RemovePart (Part part)
		{
			parts.Remove (part);
			var remove_list = new List<string> ();
			foreach (var resinfo in resources) {
				string resource = resinfo.Key;
				RMResourceInfo resourceInfo = resinfo.Value;
				for (int i = resourceInfo.containers.Count - 1; i >= 0; i--) {
					var container = resourceInfo.containers[i];
					if (container.part == part) {
						resourceInfo.containers.Remove (container);
					}
				}
				if (resourceInfo.containers.Count == 0) {
					remove_list.Add (resource);
				}
			}
			foreach (string resource in remove_list) {
				resources.Remove (resource);
			}
		}

		public void RemoveSet (RMResourceSet set)
		{
			sets.Remove (set);
			var remove_list = new List<string> ();
			foreach (var resinfo in resources) {
				string resource = resinfo.Key;
				RMResourceInfo resourceInfo = resinfo.Value;
				for (int i = resourceInfo.containers.Count - 1; i >= 0; i--) {
					var container = resourceInfo.containers[i] as ResourceSetContainer;
					if (container != null && container.set == set) {
						resourceInfo.containers.Remove (container);
					}
				}
				if (resourceInfo.containers.Count == 0) {
					remove_list.Add (resource);
				}
			}
			foreach (string resource in remove_list) {
				resources.Remove (resource);
			}
		}

		public RMResourceSet ()
		{
			resources = new Dictionary<string, RMResourceInfo>();
			parts = new List<Part> ();
			sets = new List<RMResourceSet> ();
		}

		public RMResourceSet (Part rootPart) : this ()
		{
			AddPart (rootPart);
		}

		public RMResourceSet (Vessel vessel) : this ()
		{
			foreach (Part part in vessel.parts) {
				AddPart (part);
			}
		}

		public RMResourceSet (Vessel vessel, HashSet<uint> blacklist) : this ()
		{
			foreach (Part part in vessel.parts) {
				if (!blacklist.Contains (part.flightID)) {
					AddPart (part);
				}
			}
		}

		// Completely empty the vessel of any and all resources.
		// However, if resources_to_remove is not null, only those resources
		// specified will be removed.
		public void RemoveAllResources (HashSet<string> resources_to_remove = null)
		{
			foreach (KeyValuePair<string, RMResourceInfo> pair in resources) {
				string resource = pair.Key;
				if (resources_to_remove != null && !resources_to_remove.Contains (resource)) {
					continue;
				}
				pair.Value.RemoveAllResources ();
			}
		}

		// Return the vessel's total capacity for the resource.
		// If the vessel has no such resource 0.0 is returned.
		public double ResourceCapacity (string resource)
		{
			if (!resources.ContainsKey (resource))
				return 0.0;
			RMResourceInfo resourceInfo = resources[resource];
			return resourceInfo.maxAmount;
		}

		// Return the vessel's total available amount of the resource.
		// If the vessel has no such resource 0.0 is returned.
		public double ResourceAmount (string resource)
		{
			if (!resources.ContainsKey (resource))
				return 0.0;
			RMResourceInfo resourceInfo = resources[resource];
			return resourceInfo.amount;
		}

		// Transfer a resource into (positive amount) or out of (negative
		// amount) the vessel. No attempt is made to balance the resource
		// across parts: they are filled/emptied on a first-come-first-served
		// basis.
		// If the vessel has no such resource no action is taken.
		// Returns the amount of resource not transfered (0 = all has been
		// transfered).
		public double TransferResource (string resource, double amount)
		{
			if (!resources.ContainsKey (resource))
				return amount;
			RMResourceInfo resourceInfo = resources[resource];
			if (balanced) {
				return BalancedTransfer (resourceInfo, amount);
			} else {
				return UnbalancedTransfer (resourceInfo, amount);
			}
		}

		double UnbalancedTransfer (RMResourceInfo resourceInfo, double amount)
		{
			foreach (var container in resourceInfo.containers) {
				double adjust = amount;
				double space = container.maxAmount - container.amount;
				if (adjust < 0  && -adjust > container.amount) {
					// Ensure the resource amount never goes negative
					adjust = -container.amount;
				} else if (adjust > 0 && adjust > space) {
					// ensure the resource amount never excees the maximum
					adjust = space;
				}
				container.amount += adjust;
				amount -= adjust;
			}
			return amount;
		}

		double BalancedTransfer (RMResourceInfo resourceInfo, double amount)
		{
			double setTotal = 0;
			if (amount < 0) {
				for (int i = 0; i < resourceInfo.containers.Count; i++) {
					var container = resourceInfo.containers[i];
					double avail = container.amount;
					setTotal += avail;
				}
			} else {
				for (int i = 0; i < resourceInfo.containers.Count; i++) {
					var container = resourceInfo.containers[i];
					double space = container.maxAmount - container.amount;
					setTotal += space;
				}
			}
			double adjust = amount;
			if (adjust < 0  && -adjust > setTotal) {
				// Ensure the resource amount never goes negative
				adjust = -setTotal;
			} else if (adjust > 0 && adjust > setTotal) {
				// ensure the resource amount never excees the maximum
				adjust = setTotal;
			}
			amount -= adjust;
			if (setTotal > 0) {
				if (adjust < 0) {
					for (int i = 0; i < resourceInfo.containers.Count; i++) {
						var container = resourceInfo.containers[i];
						double avail = container.amount;
						double adj = avail * adjust / setTotal;
						if (-adj > avail) {
							adj = -avail;
						}
						container.amount += adj;
					}
				} else {
					for (int i = 0; i < resourceInfo.containers.Count; i++) {
						var container = resourceInfo.containers[i];
						double space = container.maxAmount - container.amount;
						double adj = space * adjust / setTotal;
						if (adj > space) {
							adj = space;
						}
						container.amount += adj;
					}
				}
			}
			return amount;
		}

		public double ResourceMass ()
		{
			double mass = 0;
			foreach (KeyValuePair<string, RMResourceInfo> pair in resources) {
				string resource = pair.Key;
				var def = PartResourceLibrary.Instance.GetDefinition (resource);
				float density = def.density;
				RMResourceInfo resourceInfo = pair.Value;
				foreach (var container in resourceInfo.containers) {
					mass += density * container.amount;
				}
			}
			return mass;
		}

		public void Process (RMResourceProcessor resProc)
		{
			foreach (var resource in resources.Keys) {
				resProc (this, resource);
			}
		}
	}
}
