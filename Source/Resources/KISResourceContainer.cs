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

namespace ExtraplanetaryLaunchpads {
	public class KISResourceContainer : IResourceContainer {
		object resource;
		Part kis_part;

		public double maxAmount
		{
			get {
				return (double) KISWrapper.kis_maxAmount.GetValue (resource);
			}
			set {
				KISWrapper.kis_maxAmount.SetValue (resource, value);
			}
		}

		public double amount
		{
			get {
				return (double) KISWrapper.kis_amount.GetValue (resource);
			}
			set {
				KISWrapper.kis_amount.SetValue (resource, value);
			}
		}

		public Part part
		{
			get {
				return kis_part;
			}
		}

		public KISResourceContainer (Part part, object res)
		{
			kis_part = part;
			resource = res;
		}
	}
}
