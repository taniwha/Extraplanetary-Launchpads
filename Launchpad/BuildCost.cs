using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ExLP {
	public class BuildCost
	{
		public class BuildResource: IComparable<BuildResource>
		{
			public string name;
			public double amount;
			public double mass;
			public bool hull;

			public int CompareTo (BuildResource other)
			{
				return name.CompareTo (other.name);
			}

			private static bool isHullResource (PartResourceDefinition res)
			{
				// FIXME need smarter resource "type" handling
				if (res.resourceTransferMode == ResourceTransferMode.NONE
					|| res.resourceFlowMode == ResourceFlowMode.NO_FLOW) {
					return true;
				}
				return false;
			}

			public BuildResource (string name, float mass)
			{
				this.name = name;
				this.mass = mass;

				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (name);
				amount = mass / res_def.density;
				hull = isHullResource (res_def);
			}

			public BuildResource (string name, double amount)
			{
				this.name = name;
				this.amount = amount;
				PartResourceDefinition res_def;
				res_def = PartResourceLibrary.Instance.GetDefinition (name);
				mass = amount * res_def.density;
				hull = isHullResource (res_def);
			}
		}

		public class CostReport
		{
			public List<BuildResource> required;
			public List<BuildResource> optional;

			public CostReport ()
			{
				required = new List<BuildResource> ();
				optional = new List<BuildResource> ();
			}
		}

		VesselResources resources;
		public double mass;

		public BuildCost ()
		{
			resources = new VesselResources ();
		}

		public void addPartMassless (Part part)
		{
			resources.AddPart (part);
		}

		public void removePartMassless (Part part)
		{
			resources.RemovePart (part);
		}

		public void addPart (Part part)
		{
			resources.AddPart (part);
			mass += part.mass;
		}

		public void removePart (Part part)
		{
			resources.RemovePart (part);
			mass -= part.mass;
		}

		public CostReport cost
		{
			get {
				var report = new CostReport ();
				double hullMass = mass;
				var reslist = resources.resources.Keys.ToList ();
				foreach (string res in reslist) {
					double amount = resources.ResourceAmount (res);
					var br = new BuildResource (res, amount);

					if (br.hull) {
						//FIXME better hull resources check
						hullMass += br.mass;
					} else {
						report.optional.Add (br);
					}
				}
				var parts = new BuildResource ("RocketParts", (float) hullMass);
				report.required.Add (parts);
				return report;
			}
		}
	}
}
