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
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ResourceXferControl
	{
		class XferSet : Dictionary<string, RMResourceSet> { }
		XferSet dstSets;
		XferSet srcSets;

		public delegate void OnTransferableChanged ();
		public OnTransferableChanged onTransferableChanged = delegate { };

		bool _canTransfer;
		public bool canTransfer
		{
			get { return _canTransfer; }
			set {
				bool changed = _canTransfer != value;
				_canTransfer = value;
				if (changed) {
					onTransferableChanged ();
				}
			}
		}

		public ResourceXferControl()
		{
			dstSets = new XferSet ();
			srcSets = new XferSet ();
		}

		public void Clear ()
		{
			dstSets.Clear ();
			srcSets.Clear ();
		}

		void AddSet (XferSet xferSet, RMResourceSet set, string resourceName)
		{
			if (!xferSet.ContainsKey (resourceName)) {
				xferSet[resourceName] = new RMResourceSet ();
				xferSet[resourceName].balanced = true;
			}
			xferSet[resourceName].AddSet (set);
		}

		void RemoveSet (XferSet xferSet, RMResourceSet set, string resourceName)
		{
			if (xferSet.ContainsKey (resourceName)) {
				var s = xferSet[resourceName];
				s.RemoveSet (set);
				if (s.resources.Count < 1) {
					xferSet.Remove (resourceName);
				}
			}
		}

		public void AddDestination (RMResourceSet set, string resourceName)
		{
			Debug.Log ($"[ResourceXferControl] AddDestination {set.name} {resourceName}");
			AddSet (dstSets, set, resourceName);
		}

		public void RemoveDestination (RMResourceSet set, string resourceName)
		{
			Debug.Log ($"[ResourceXferControl] RemoveDestination {set.name} {resourceName}");
			RemoveSet (dstSets, set, resourceName);
		}

		public void AddSource (RMResourceSet set, string resourceName)
		{
			Debug.Log ($"[ResourceXferControl] AddSource {set.name} {resourceName}");
			AddSet (srcSets, set, resourceName);
		}

		public void RemoveSource (RMResourceSet set, string resourceName)
		{
			Debug.Log ($"[ResourceXferControl] RemoveSource {set.name} {resourceName}");
			RemoveSet (srcSets, set, resourceName);
		}

		public void CheckTransfer ()
		{
			bool intersect = false;
			foreach (string r in dstSets.Keys) {
				if (srcSets.ContainsKey (r)) {
					intersect = true;
					break;
				}
			}
			canTransfer = intersect;
		}

		public bool TransferResources (double deltaTime)
		{
			if (!canTransfer) {
				return false;
			}
			bool didSomething = false;
			foreach (string res in dstSets.Keys) {
				var dst = dstSets[res];
				if (!srcSets.ContainsKey (res)) {
					continue;
				}
				var src = srcSets[res];

				double amount = dst.ResourceCapacity (res) / 20;
				amount *= deltaTime;

				// FIXME heat
				double srem = src.TransferResource (res, -amount);
				// srem (amount not pulled) will be negative
				if (srem == -amount) {
					// could not pull any
					continue;
				}
				amount += srem;

				double drem = dst.TransferResource (res, amount);
				if (drem != amount) {
					didSomething = true;
				}
				// return any untransfered amount back to source
				src.TransferResource (res, drem);
			}
			return didSomething;
		}
	}
}
