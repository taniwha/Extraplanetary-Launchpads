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
using System.Reflection;
using System.Collections.Generic;
//using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExtraplanetaryLaunchpads {

	public class RMResourceManager
	{
		HashSet<uint> visitedParts;
		Dictionary<uint, Part> partMap;
		//Dictionary<uint, RMResourceSet> symmetryDict;
		List<RMResourceSet> symmetrySets;
		List<RMResourceSet> moduleSets;
		bool useFlightID;

		void CreatePartMap (List<Part> parts)
		{
			partMap = new Dictionary<uint, Part>();
			for (int i = parts.Count; i-- > 0; ) {
				Part p = parts[i];
				// flightID is usable when the parts are from an existing
				// vessel, and craftID is usable when the parts are from
				// a craft file (ie, the vessel does not yet exist)
				// FIXME does it matter? will this be used on an embryonic
				// vessel?
				uint id = useFlightID ? p.flightID : p.craftID;
				partMap[id] = p;
			}
		}

		Part GetOtherPart (IStageSeparator separator)
		{
			AttachNode node = null;
			if (separator is ModuleAnchoredDecoupler) {
				node = (separator as ModuleAnchoredDecoupler).ExplosiveNode;
			} else if (separator is ModuleDecouple) {
				node = (separator as ModuleDecouple).ExplosiveNode;
			} else if (separator is ModuleDockingNode) {
				// if referenceNode.attachedPart is not null, then the port
				// was attached in the editor and may be separated later,
				// otherwise, need to check for the port having been docked.
				// if referenceNode itself is null, then the port cannot be
				// docked in the editor (eg, inline docking port)
				var port = separator as ModuleDockingNode;
				node = port.referenceNode;
				if (node == null || node.attachedPart) {
					if (port.otherNode != null) {
						return port.otherNode.part;
					}
					return null;
				}
			}
			if (node == null) {
				// separators detach on both ends (and all surface attachments?)
				// and thus don't keep track of the node(s), so return the parent
				return (separator as PartModule).part.parent;
			}
			return node.attachedPart;
		}

		static FieldInfo _grappleNodeField;
		static FieldInfo grappleNodeField
		{
			get {
				if (_grappleNodeField == null) {
					var fields = typeof (ModuleGrappleNode).GetFields (BindingFlags.NonPublic | BindingFlags.Instance);
					for (int i = 0; i < fields.Length; i++) {
						if (fields[i].FieldType == typeof (AttachNode)) {
							_grappleNodeField =  fields[i];
							break;
						}
					}
				}
				return _grappleNodeField;
			}
		}
		Part GetOtherPart (ModuleGrappleNode grapple)
		{
			// The claw is a very unfriendly part. All the important fields
			// private.
			AttachNode grappleNode = (AttachNode) grappleNodeField.GetValue (grapple);
			if (grappleNode != null) {
				return grappleNode.attachedPart;
			}
			return null;
		}

		Part GetOtherPart (ELLaunchpad pad)
		{
			// no need to worry about something attached via a node as
			// hopefully that part is a decoupler of some sort, otherwise
			// the pad is probably unusable, and surface attached parts
			// don't matter too much, either.
			// if pad.control is ever null, bigger problems are afoot
			return pad.control.craftRoot;
		}

		Part GetOtherPart (Part part)
		{
			// This covers radial and stack decouplers and separators, launch
			// clamps, and docking ports.
			var separator = part.FindModuleImplementing<IStageSeparator> ();
			if (separator != null) {
				return GetOtherPart (separator);
			} else {
				// The claw is on its own as it is never staged (I guess).
				var grapple = part.FindModuleImplementing<ModuleGrappleNode> ();
				if (grapple != null) {
					return GetOtherPart (grapple);
				} else {
					// EL's launchpad module is very much on its own. No need
					// to worry about survey stations as the built vessel is
					// never attached, nor disposable pads as they self
					// destruct.
					var pad = part.FindModuleImplementing<ELLaunchpad> ();
					if (pad != null) {
						return GetOtherPart (pad);
					}
				}
				//FIXME need to add KAS connectors
			}
			return null;
		}

		RMResourceSet AddModule ()
		{
			var set = new RMResourceSet ();
			set.name = "module";
			moduleSets.Add (set);
			return set;
		}

		void ProcessParts (Part part, RMResourceSet set)
		{
			var otherPart = GetOtherPart (part);

			if (part.parent != null && part.parent == otherPart) {
				set = AddModule ();
			}

			set.AddPart(part);

			for (int i = part.children.Count; i-- > 0; ) {
				var child = part.children[i];
				if (child == otherPart) {
					ProcessParts (child, AddModule ());
				} else {
					ProcessParts (child, set);
				}
			}
		}

		static uint GetID (Part p)
		{
			return p.flightID != 0 ? p.flightID : p.craftID;
		}

		// The dictionary is indexed by part id (flight or craft) such that
		// the symmetry set can be found by the id of any part within the set.
		// However, the sets consist of only those parts that hold resources.
		void FindSymmetrySets (List<Part> parts)
		{
			var dict = new Dictionary<uint, RMResourceSet> ();
			var sets = new List<RMResourceSet> ();
			for (int i = 0; i < parts.Count; i++) {
				Part p = parts[i];
				uint id = GetID (p);
				Debug.LogFormat ("{0} {1} {2}", i, p.name, p.symmetryCounterparts.Count);
				if (p.Resources.Count < 1) {
					// no resources, so no point worrying about symmetry
					continue;
				}
				if (p.symmetryCounterparts.Count < 1) {
					// not part of a symmetry set
					continue;
				}
				if (visitedParts.Contains (id)) {
					// already added this part
					continue;
				}
				visitedParts.Add (id);
				RMResourceSet symmetrySet = new RMResourceSet ();
				symmetrySet.balanced = true;
				symmetrySet.name = "sym " + id.ToString ();
				symmetrySet.AddPart (p);
				dict[id] = symmetrySet;
				sets.Add (symmetrySet);
				for (int j = 0; j < p.symmetryCounterparts.Count; j++) {
					Part s = p.symmetryCounterparts[j];
					id = GetID (s);
					visitedParts.Add (id);
					symmetrySet.AddPart (s);
					dict[id] = symmetrySet;
				}
			}
			//symmetryDict = dict;
			symmetrySets = sets;
		}

		public RMResourceManager (List<Part> parts, bool useFlightID)
		{
			this.useFlightID = useFlightID;
			CreatePartMap (parts);
			visitedParts = new HashSet<uint> ();
			FindSymmetrySets (parts);
			foreach (var s in symmetrySets) {
				Debug.LogFormat ("[RMResourceManager] {0} {1} {2} {3}", s.name, s.parts.Count, s.sets.Count, s.resources.Count);
			}
			visitedParts.Clear ();
			moduleSets = new List<RMResourceSet> ();
			ProcessParts (parts[0].localRoot, AddModule ());
			for (int i = 0; i < moduleSets.Count; ) {
				if (moduleSets[i].parts.Count > 0) {
					i++;
				} else {
					moduleSets.RemoveAt (i);
				}
			}
			foreach (var m in moduleSets) {
				Debug.LogFormat ("[RMResourceManager] {0} {1} {2} {3}", m.name, m.parts.Count, m.sets.Count, m.resources.Count);
			}
		}
	}
}
