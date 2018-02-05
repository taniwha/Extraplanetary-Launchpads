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

namespace ExtraplanetaryLaunchpads {
	public class PartResourceContainer : IResourceContainer {
		PartResource partResource;

		public double maxAmount
		{
			get {
				return partResource.maxAmount;
			}
		}
		public double amount
		{
			get {
				return partResource.amount;
			}
			set {
				partResource.amount = value;
			}
		}
		public bool flowState
		{
			get {
				return partResource.flowState;
			}
			set {
				partResource.flowState = value;
			}
		}
		public Part part
		{
			get {
				return partResource.part;
			}
		}

		public PartResourceContainer (PartResource partResource)
		{
			this.partResource = partResource;
		}
	}
}
