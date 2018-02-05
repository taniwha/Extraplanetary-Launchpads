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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class RecipeResourceContainer : IResourceContainer {
		Part recipe_part;
		Ingredient recipe_resource;

		public double maxAmount
		{
			get {
				return recipe_resource.ratio;
			}
			set {
				throw new NotSupportedException ("Recipe resources are read-only");
			}
		}
		public double amount
		{
			get {
				return recipe_resource.ratio;
			}
			set {
				throw new NotSupportedException ("Recipe resources are read-only");
			}
		}
		public bool flowState
		{
			get {
				return true;
			}
			set {
				throw new NotSupportedException ("Recipe resources are read-only");
			}
		}
		public Part part
		{
			get {
				return recipe_part;
			}
		}

		public RecipeResourceContainer (Part part, Ingredient resource)
		{
			recipe_part = part;
			recipe_resource = resource;
		}
	}
}
