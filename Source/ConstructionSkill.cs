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

namespace ExLP {

	public class ExConstructionSkill : ExperienceEffect
	{
		static string [] skills = new string [] {
			"Can work in a fully equipped workshop.",
			"Can work in any workshop.",
			"Is always productive in a fully equipped workshop.",
			"Is always productive in any workshop.",
			"Enable unskilled workers in a fully equipped workshop.",
		};

		protected override float GetDefaultValue ()
		{
			return 0;
		}

		protected override string GetDescription ()
		{
			int exp = Parent.CrewMemberExperienceLevel (5);
			return skills[exp];
		}

		public int GetValue ()
		{
			return Parent.CrewMemberExperienceLevel (5);
		}

		protected override void OnRegister (Part part)
		{
		}

		protected override void OnUnregister (Part part)
		{
		}

		public ExConstructionSkill (ExperienceTrait parent) : base (parent)
		{
		}

		public ExConstructionSkill (ExperienceTrait parent, float[] modifiers) : base (parent, modifiers)
		{
		}
	}
}
