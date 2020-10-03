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
// Thanks to Taranis Elsu and his Fuel Balancer mod for the inspiration.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {
	public class RMResourceInfo {
		public List<IResourceContainer> containers = new List<IResourceContainer>();

		public bool flowState
		{
			get {
				for (int i = containers.Count; i-- > 0; ) {
					if (containers[i].flowState) {
						return true;
					}
				}
				return false;
			}
			set {
				for (int i = containers.Count; i-- > 0; ) {
					containers[i].flowState = value;
				}
			}
		}

		public double amount
		{
			get {
				double amount = 0;
				for (int i = containers.Count; i-- > 0; ) {
					amount += containers[i].amount;
				}
				return amount;
			}
		}

		public double maxAmount
		{
			get {
				double maxAmount = 0;
				for (int i = containers.Count; i-- > 0; ) {
					maxAmount += containers[i].maxAmount;
				}
				return maxAmount;
			}
		}

		public void RemoveAllResources ()
		{
			for (int i = containers.Count; i-- > 0; ) {
				containers[i].amount = 0.0;
			}
		}
	}
}
