using A;
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
		static Type IBodyResources;
		static MethodInfo GetQuantity;
		static MethodInfo Extract;
		static Type KethaneData;
		static PropertyInfo KDCurrent;
		static PropertyInfo KDIndexer;

		public float GetAbundance (string ResourceName, Vessel vessel, Vector3 location)
		{
			var cell = GetCellUnder.Invoke (null, new object[] { vessel.mainBody, location});
			var kd = KDCurrent.GetValue (null, null);
			var bodyResources = KDIndexer.GetValue (kd, new object[] {ResourceName});
			var deposit = (double?) GetQuantity.Invoke (bodyResources, new object[] {cell});
			if (deposit == null) {
				return 0;
			}
			return deposit.Value > 0 ? 1 : 0;
		}

		public void ExtractResource (string ResourceName, Vessel vessel, Vector3 location, float amount)
		{
			var cell = GetCellUnder.Invoke (null, new object[] { vessel.mainBody, location});
			var kd = KDCurrent.GetValue (null, null);
			var bodyResources = KDIndexer.GetValue (kd, new object[] {ResourceName});
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
					GetCellUnder = MapOverlay.GetMethod ("GetCellUnder");
					IBodyResources = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("IBodyResources")).FirstOrDefault ();
					GetQuantity = IBodyResources.GetMethod ("GetQuantity");
					Extract = IBodyResources.GetMethod ("Extract");
					KethaneData = KethaneAssembly.GetTypes().Where (t => t.Name.Equals ("KethaneData")).FirstOrDefault ();
					KDCurrent = KethaneData.GetProperty ("Current");
					KDIndexer = KethaneData.GetProperties().Single(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(string));
				}
			}
			if (KethaneAssembly != null) {
				return new KethaneResourceProvider ();
			}
			return null;
		}
	}
}
