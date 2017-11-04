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

	public class ELConstructionSkill : ExperienceEffect
	{
		static string [] skills = new string [] {
			" can work in a fully equipped workshop.",
			" can work in any workshop.",
			" is always productive in a fully equipped workshop.",
			" is always productive in any workshop.",
			" enables skilled workers in any workshop.",
			" enables unskilled workers in a fully equipped workshop.",
		};

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
			return pronoun + skills[exp];
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

		public ELConstructionSkill (ExperienceTrait parent) : base (parent)
		{
		}

		public ELConstructionSkill (ExperienceTrait parent, float[] modifiers) : base (parent, modifiers)
		{
		}
	}
}
