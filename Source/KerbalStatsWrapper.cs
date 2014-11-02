using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ExLP.KerbalStats {
	public class KerbalExt
	{
		static MethodInfo GetMethod;
		static bool initialized;

		public static string Get (ProtoCrewMember kerbal, string parms)
		{
			if (!initialized) {
				initialized = true;
				System.Type KStype = AssemblyLoader.loadedAssemblies
					.Select(a => a.assembly.GetExportedTypes())
					.SelectMany(t => t)
					.FirstOrDefault(t => t.FullName == "KerbalStats.KerbalExt");
				if (KStype == null) {
					Debug.LogWarning ("KerbalStats.KerbalExt class not found.");
				} else {
					GetMethod = KStype.GetMethod ("Get", BindingFlags.Public | BindingFlags.Static);
					if (GetMethod == null) {
						Debug.LogWarning ("KerbalExt.Get () not found.");
					}
				}
			}
			if (GetMethod != null) {
				return (string) GetMethod.Invoke (null, new System.Object[]{kerbal, parms});
			}
			return null;
		}
	}
}
