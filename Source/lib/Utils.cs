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
using Experience;

namespace ExtraplanetaryLaunchpads {

	public class EL_Utils {
		public static List<ProtoCrewMember> GetCrewList (Part part)
		{
			if (part.CrewCapacity > 0) {
				return part.protoModuleCrew;
			} else {
				var crew = new List<ProtoCrewMember> ();
				var seats = part.FindModulesImplementing<KerbalSeat> ();
				foreach (var s in seats) {
					if (s.Occupant != null) {
						crew.Add (s.Occupant.protoModuleCrew[0]);
					}
				}
				return crew;
			}
		}
		public static bool HasSkill<T> (ProtoCrewMember crew) where T : class
		{
			ExperienceEffect skill = crew.experienceTrait.Effects.Where (e => e is T).FirstOrDefault ();
			if (skill == null) {
				return false;
			}
			return true;
		}
	}
}
