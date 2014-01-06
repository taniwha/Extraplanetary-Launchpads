using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {
	// Thanks to Taranis Elsu and his Fuel Balancer mod for the inspiration.
	public class ResourcePartMap {
		public PartResource resource;
		public Part part;

		public ResourcePartMap(PartResource resource, Part part)
		{
			this.resource = resource;
			this.part = part;
		}
	}

	public class ResourceInfo {
		public List<ResourcePartMap> parts = new List<ResourcePartMap>();
	}

	public class VesselResources {
		public Dictionary<string, ResourceInfo> resources;

		public void AddPart(Part part)
		{
			foreach (PartResource resource in part.Resources) {
				ResourceInfo resourceInfo;
				if (!resources.ContainsKey(resource.resourceName)) {
					resourceInfo = new ResourceInfo();
					resources[resource.resourceName] = resourceInfo;
				}
				resourceInfo = resources[resource.resourceName];
				resourceInfo.parts.Add(new ResourcePartMap(resource, part));
			}
		}

		public void RemovePart(Part part)
		{
			var remove_list = new List<string> ();
			foreach (var resinfo in resources) {
				string resource = resinfo.Key;
				ResourceInfo resourceInfo = resinfo.Value;
				foreach (var pm in resourceInfo.parts) {
					if (pm.part == part) {
						resourceInfo.parts.Remove (pm);
						break;
					}
				}
				if (resourceInfo.parts.Count == 0) {
					remove_list.Add (resource);
				}
			}
			foreach (string resource in remove_list) {
				resources.Remove (resource);
			}
		}

		public VesselResources(Part rootPart)
		{
			resources = new Dictionary<string, ResourceInfo>();
			AddPart(rootPart);
		}

		public VesselResources(Vessel vessel)
		{
			resources = new Dictionary<string, ResourceInfo>();
			foreach (Part part in vessel.parts) {
				AddPart(part);
			}
		}

		// Completely empty the vessel of any and all resources.
		public void RemoveAllResources(HashSet<string> resources_to_remove = null)
		{
			foreach (KeyValuePair<string, ResourceInfo> pair in resources) {
				string resource = pair.Key;
				if (resources_to_remove != null && !resources_to_remove.Contains(resource)) {
					continue;
				}
				ResourceInfo resourceInfo = pair.Value;
				foreach (ResourcePartMap partInfo in resourceInfo.parts) {
					partInfo.resource.amount = 0.0;
				}
			}
		}

		// Return the vessel's total capacity for the resource.
		// If the vessel has no such resource 0.0 is returned.
		public double ResourceCapacity(string resource)
		{
			if (!resources.ContainsKey(resource))
				return 0.0;
			ResourceInfo resourceInfo = resources[resource];
			double capacity = 0.0;
			foreach (ResourcePartMap partInfo in resourceInfo.parts) {
				capacity += partInfo.resource.maxAmount;
			}
			return capacity;
		}

		// Return the vessel's total available amount of the resource.
		// If the vessel has no such resource 0.0 is returned.
		public double ResourceAmount(string resource)
		{
			if (!resources.ContainsKey(resource))
				return 0.0;
			ResourceInfo resourceInfo = resources[resource];
			double amount = 0.0;
			foreach (ResourcePartMap partInfo in resourceInfo.parts) {
				amount += partInfo.resource.amount;
			}
			return amount;
		}

		// Transfer a resource into (positive amount) or out of (negative
		// amount) the vessel. No attempt is made to balance the resource
		// across parts: they are filled/emptied on a first-come-first-served
		// basis.
		// If the vessel has no such resource no action is taken.
		// Returns the amount of resource not transfered (0 = all has been
		// transfered).
		public double TransferResource(string resource, double amount)
		{
			if (!resources.ContainsKey(resource))
				return amount;
			ResourceInfo resourceInfo = resources[resource];
			foreach (ResourcePartMap partInfo in resourceInfo.parts) {
				PartResource res = partInfo.resource;
				double adjust = amount;
				if (adjust < 0  && -adjust > res.amount) {
					// Ensure the resource amount never goes negative
					adjust = -res.amount;
				} else if (adjust > 0
						   && adjust > (res.maxAmount - res.amount)) {
					// ensure the resource amount never excees the maximum
					adjust = res.maxAmount - res.amount;
				}
				partInfo.resource.amount += adjust;
				amount -= adjust;
			}
			return amount;
		}
	}
}
