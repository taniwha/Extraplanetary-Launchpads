/*
This file is part of KerbalStats.

KerbalStats is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

KerbalStats is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with KerbalStats.  If not, see <http://www.gnu.org/licenses/>.
*/
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace ExtraplanetaryLaunchpads.KerbalStats {
	public class KerbalExt
	{
		static MethodInfo GetMethod;
		static bool initialized;

		public static string Get (ProtoCrewMember kerbal, string parms)
		{
			if (!initialized) {
				initialized = true;
				System.Type KStype = AssemblyLoader.loadedAssemblies
					.Select(a => a.assembly.GetTypes())
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
