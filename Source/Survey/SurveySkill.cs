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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;
using Experience;

namespace ExtraplanetaryLaunchpads {

using KerbalStats;

	public class ELSurveySkill : ExperienceEffect
	{
		protected override float GetDefaultValue ()
		{
			return 0;
		}

		protected override string GetDescription ()
		{
			ProtoCrewMember crew = Parent.CrewMember;
			string pronoun;
			if (crew.gender == ProtoCrewMember.Gender.Female) {
				pronoun = "She";
			} else {
				pronoun = "He";
			}
			int exp = Parent.CrewMemberExperienceLevel (6);
			return String.Format ("{0} can use survey sites out to {1}m.",
								  pronoun,
								  ELSurveyStation.default_site_ranges[exp + 2]);
		}

		public int GetValue ()
		{
			return Parent.CrewMemberExperienceLevel (6);
		}

		protected override void OnRegister (Part part)
		{
		}

		protected override void OnUnregister (Part part)
		{
		}

		public ELSurveySkill (ExperienceTrait parent) : base (parent)
		{
		}

		public ELSurveySkill (ExperienceTrait parent, float[] modifiers) : base (parent, modifiers)
		{
		}
	}
}
