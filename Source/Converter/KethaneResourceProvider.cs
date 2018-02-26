using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ExtraplanetaryLaunchpads {
	public class KethaneResourceProvider:IResourceProvider
	{
		static bool KethaneChecked;
		static Assembly KethaneAssembly;
		static Type MapOverlay;
		static MethodInfo GetCellUnder;
		//static Type IBodyResources;
		static MethodInfo GetQuantity;
		static MethodInfo Extract;
		static Type KethaneData;
		static PropertyInfo KDCurrent;
		static PropertyInfo KDIndexer;
		static Type ResourceData;
		static PropertyInfo RDIndexer;
		static Type BodyResourceData;
		static PropertyInfo BRDResources;

		public double GetAmount (string ResourceName, RPLocation location, double rate)
		{
			var cell = GetCellUnder.Invoke (null, new object[] { location.body, location.location});
			var kd = KDCurrent.GetValue (null, null);
			var rd = KDIndexer.GetValue (kd, new object[] {ResourceName});
			var brd = RDIndexer.GetValue (rd, new object[] {location.body});
			var bodyResources = BRDResources.GetValue (brd, null);
			GetQuantity = bodyResources.GetType().GetMethod ("GetQuantity");
			var deposit = (double?) GetQuantity.Invoke (bodyResources, new object[] {cell});
			if (deposit == null) {
				return 0;
			}
			return (float) Math.Min(rate, deposit.Value);
		}

		public void ExtractResource (string ResourceName, RPLocation location, double amount)
		{
			var cell = GetCellUnder.Invoke (null, new object[] { location.body, location.location});
			var kd = KDCurrent.GetValue (null, null);
			var rd = KDIndexer.GetValue (kd, new object[] {ResourceName});
			var brd = RDIndexer.GetValue (rd, new object[] {location.body});
			var bodyResources = BRDResources.GetValue (brd, null);
			Extract = bodyResources.GetType().GetMethod ("Extract");
			Extract.Invoke (bodyResources, new object[] {cell, amount});
		}

		public static KethaneResourceProvider Create ()
		{
			if (!KethaneChecked) {
				KethaneChecked = true;
				var loaded = AssemblyLoader.loadedAssemblies.Where (a => a.assembly.GetName ().Name == "Kethane").FirstOrDefault ();
				if (loaded != null) {
					KethaneAssembly = loaded.assembly;
					MapOverlay = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("MapOverlay")).FirstOrDefault ();
					GetCellUnder = MapOverlay.GetMethod ("GetCellUnder", new Type[] {typeof (CelestialBody), typeof (Vector3)});
					KethaneData = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("KethaneData")).FirstOrDefault ();
					KDCurrent = KethaneData.GetProperty ("Current");
					KDIndexer = KethaneData.GetProperties().Single(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(string));
					ResourceData = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("ResourceData")).FirstOrDefault ();
					RDIndexer = ResourceData.GetProperties().Single(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(CelestialBody));
					BodyResourceData = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("BodyResourceData")).FirstOrDefault ();
					BRDResources = BodyResourceData.GetProperty ("Resources");
					//IBodyResources = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("IBodyResources")).FirstOrDefault ();
				}
			}
			if (KethaneAssembly != null) {
				return new KethaneResourceProvider ();
			}
			return null;
		}
	}
}
