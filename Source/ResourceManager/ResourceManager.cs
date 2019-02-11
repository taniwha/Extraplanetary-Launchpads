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

	using KAS;

	public class RMResourceManager
	{
		class ConnectedPartSet {
			Dictionary<uint, string> parts;
			public ConnectedPartSet ()
			{
				parts = new Dictionary<uint, string>();
			}
			public void Add (Part part, string name)
			{
				uint id = RMResourceManager.GetID (part);
				parts[id] = name;
			}
			public bool Has (Part part)
			{
				uint id = RMResourceManager.GetID (part);
				return parts.ContainsKey (id);
			}
			public string Name (Part part)
			{
				uint id = RMResourceManager.GetID (part);
				return parts[id];
			}
			public void Clear ()
			{
				parts.Clear ();
			}
		}

		HashSet<uint> visitedParts = new HashSet<uint> ();
		HashSet<Guid> visitedVessels = new HashSet<Guid> ();
		HashSet<Part> excludeParts;
		Dictionary<uint, Part> partMap = new Dictionary<uint, Part>();
		Dictionary<uint, RMResourceSet> symmetryDict;

		public List<RMResourceSet> symmetrySets;
		public List<RMResourceSet> moduleSets = new List<RMResourceSet> ();
		public List<RMResourceSet> resourceSets;
		public RMResourceSet masterSet;
		bool useFlightID;
		bool addVessels;

		void ExpandPartMap (IEnumerable<Part> parts)
		{
			foreach (Part p in parts) {
				if (excludeParts.Contains (p)) {
					continue;
				}
				// flightID is usable when the parts are from an existing
				// vessel, and craftID is usable when the parts are from
				// a craft file (ie, the vessel does not yet exist)
				// FIXME does it matter? will this be used on an embryonic
				// vessel?
				uint id = useFlightID ? p.flightID : p.craftID;
				partMap[id] = p;
			}
		}

		void GetOtherPart (IStageSeparator separator, ConnectedPartSet cp)
		{
			AttachNode node = null;
			if (separator is ModuleAnchoredDecoupler) {
				if (!(separator as ModuleAnchoredDecoupler).stagingEnabled) {
					return;
				}
				node = (separator as ModuleAnchoredDecoupler).ExplosiveNode;
			} else if (separator is ModuleDecouple) {
				if (!(separator as ModuleDecouple).stagingEnabled) {
					return;
				}
				node = (separator as ModuleDecouple).ExplosiveNode;
			} else if (separator is ModuleDockingNode) {
				// if referenceNode.attachedPart is not null, then the port
				// was attached in the editor and may be separated later,
				// otherwise, need to check for the port having been docked.
				// if referenceNode itself is null, then the port cannot be
				// docked in the editor (eg, inline docking port)
				var port = separator as ModuleDockingNode;
				Part otherPart = null;
				if (partMap.TryGetValue (port.dockedPartUId, out otherPart)) {
					if (port.vesselInfo != null) {
						var vi = port.vesselInfo;
						cp.Add (otherPart, vi.name);
						return;
					}
				}
				node = port.referenceNode;
				if (node == null) {
					//Debug.LogFormat ("[RMResourceManager] docking port null");
					return;
				}
			}
			if (node == null) {
				// separators detach on both ends (and all surface attachments?)
				// and thus don't keep track of the node(s), so return the parent
				Part p = (separator as PartModule).part;
				if (p.parent != null) {
					cp.Add (p.parent, "separator");
				}
				return;
			}
			if (node.attachedPart != null) {
				cp.Add (node.attachedPart, "decoupler");
			}
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
		void GetOtherPart (ModuleGrappleNode grapple, ConnectedPartSet cp)
		{
			// The claw is a very unfriendly part. All the important fields
			// private.
			AttachNode grappleNode = (AttachNode) grappleNodeField.GetValue (grapple);
			if (grappleNode != null && grappleNode.attachedPart != null) {
				var vi = grapple.vesselInfo;
				cp.Add (grappleNode.attachedPart, vi.name);
			}
		}

		void GetOtherPart (ELLaunchpad pad, ConnectedPartSet cp)
		{
			// no need to worry about something attached via a node as
			// hopefully that part is a decoupler of some sort, otherwise
			// the pad is probably unusable, and surface attached parts
			// don't matter too much, either.
			// if pad.control is ever null, bigger problems are afoot
			if (pad.control.craftRoot != null) {
				var vi = pad.control.vesselInfo;
				cp.Add (pad.control.craftRoot, vi.name);
			}
		}

		void GetOtherPart (KASModuleStrut strut, ConnectedPartSet cp)
		{
			Part otherPart = null;
			if (partMap.TryGetValue (strut.dockedPartID, out otherPart)) {
				if (strut.vesselInfo != null) {
					var vi = strut.vesselInfo;
					cp.Add (otherPart, vi.name);
				}
			}
		}

		void GetOtherPart (KASJointCableBase joint, ConnectedPartSet cp)
		{
			if (joint.isLinked) {
				var srcVessel = joint.linkSource.part.vessel;
				var dstVessel = joint.linkTarget.part.vessel;
				if (srcVessel == dstVessel
					&& joint.persistedTgtVesselInfo != null) {
					cp.Add (joint.linkTarget.part,
							joint.persistedTgtVesselInfo.name);
				}
			}
		}

		bool CheckKASLink (ILinkPeer peer)
		{
			if (addVessels && peer.isLinked && peer.otherPeer != null) {
				var thisVessel = peer.part.vessel;
				var otherVessel = peer.otherPeer.part.vessel;
				if (!excludeParts.Contains(peer.otherPeer.part)
					&& thisVessel != otherVessel
					&& !visitedVessels.Contains (otherVessel.id)) {
					visitedVessels.Add (otherVessel.id);
					AddVessel (otherVessel.vesselName, otherVessel.parts,
							   otherVessel.parts[0].localRoot);
					return true;
				}
			}
			return false;
		}

		//FIXME rework for multiple connections
		ConnectedPartSet ConnectedParts (Part part)
		{
			var connectedParts = new ConnectedPartSet ();

			for (int i = part.Modules.Count; i-- > 0; ) {
				var module = part.Modules[i];
				// This covers radial and stack decouplers and separators,
				// launch clamps, and docking ports.
				var separator = module as IStageSeparator;
				if (separator != null) {
					GetOtherPart (separator, connectedParts);
					continue;
				}

				// The claw is on its own as it is never staged (I guess).
				var grapple = module as ModuleGrappleNode;
				if (grapple != null) {
					GetOtherPart (grapple, connectedParts);
					continue;
				}

				// EL's launchpad module is very much on its own. No need
				// to worry about survey stations as the built vessel is
				// never attached, nor disposable pads as they self
				// destruct.
				var pad = module as ELLaunchpad;
				if (pad != null) {
					GetOtherPart (pad, connectedParts);
					continue;
				}

				if (module.moduleName == "KASModuleStrut") {
					// legacy pipe connector
					var kasStrut = new KASModuleStrut (module);
					GetOtherPart (kasStrut, connectedParts);
					continue;
				}

				if (module.moduleName == "KASLinkResourceConnector") {
					// new resource connector. works when undocked!
					// however, can be used to dock the vessel, in which
					// case KASJointCableBase is to be checked, or even
					// just connected on the same vessel (just ignore)
					var peer = new ILinkPeer (module);
					if (CheckKASLink (peer)) {
						continue;
					}
				}

				if (module.moduleName == "KASLinkTargetBase") {
					// new resource connector. works when undocked!
					// however, can be used to dock the vessel, in which
					// case KASJointCableBase is to be checked, or even
					// just connected on the same vessel (just ignore)
					var peer = new ILinkPeer (module);
					if (peer.cfgLinkType == "MdHose" && CheckKASLink (peer)) {
						continue;
					}
				}

				if (module.moduleName == "KASJointCableBase") {
					var kasJoint = new KASJointCableBase (module);
					GetOtherPart (kasJoint, connectedParts);
				}
			}
			return connectedParts;
		}

		RMResourceSet AddModule (string name)
		{
			var set = new RMResourceSet ();
			set.name = name;
			moduleSets.Add (set);
			return set;
		}

		void ProcessParts (Part part, RMResourceSet set)
		{
			var cp = ConnectedParts (part);

			if (part.parent != null && cp.Has (part.parent)) {
				//Debug.LogFormat("[RMResourceSet] ProcessParts: parent {0}", part.parent);
				set = AddModule (cp.Name (part.parent));
			}

			set.AddPart(part);

			for (int i = part.children.Count; i-- > 0; ) {
				var child = part.children[i];
				if (excludeParts.Contains (child)) {
					continue;
				}
				if (cp.Has (child)) {
					//Debug.LogFormat("[RMResourceSet] ProcessParts: child {0}", child);
					ProcessParts (child, AddModule (cp.Name (child)));
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
		void FindSymmetrySets (IEnumerable<Part> parts)
		{
			visitedParts.Clear ();
			var dict = new Dictionary<uint, RMResourceSet> ();
			var sets = new List<RMResourceSet> ();
			foreach (Part p in parts) {
				if (excludeParts.Contains (p)) {
					continue;
				}
				uint id = GetID (p);
				//Debug.LogFormat ("{0} {1} {2}", i, p.name, p.symmetryCounterparts.Count);
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
			symmetryDict = dict;
			symmetrySets = sets;
		}

		void FinalizeResourceSets ()
		{
			visitedParts.Clear ();
			resourceSets = new List<RMResourceSet> ();
			masterSet = new RMResourceSet ();
			RMResourceSet set = null;
			foreach (var m in moduleSets) {
				if (set == null) {
					set = new RMResourceSet ();
					set.name = m.name;
				}
				foreach (var p in m.parts) {
					uint id = GetID (p);
					if (visitedParts.Contains (id)) {
						continue;
					}
					if (p.symmetryCounterparts.Count > 0
						&& symmetryDict.ContainsKey (id)) {
						RMResourceSet sym = symmetryDict[id];
						foreach (var s in sym.parts) {
							uint sid = GetID (s);
							visitedParts.Add (sid);
						}
						set.AddSet (sym);
						masterSet.AddSet (sym);
					} else {
						visitedParts.Contains (id);
						set.AddPart (p);
						masterSet.AddPart (p);
					}
				}
				if (set.parts.Count > 0 || set.sets.Count > 0) {
					resourceSets.Add (set);
					set = null;
				}
			}
		}

		void AddVessel (string vesselName, IEnumerable<Part> parts,
						Part rootPart)
		{
			ExpandPartMap (parts);
			FindSymmetrySets (parts);
			ProcessParts (rootPart, AddModule (vesselName));
			for (int i = 0; i < moduleSets.Count; ) {
				if (moduleSets[i].parts.Count > 0) {
					i++;
				} else {
					moduleSets.RemoveAt (i);
				}
			}
		}

		void DumpResourceSets ()
		{
			foreach (var s in symmetrySets) {
				Debug.LogFormat ("[RMResourceManager]  s {0} {1} {2} {3}", s.name, s.parts.Count, s.sets.Count, s.resources.Count);
			}
			foreach (var m in moduleSets) {
				Debug.LogFormat ("[RMResourceManager]  m {0} {1} {2} {3}", m.name, m.parts.Count, m.sets.Count, m.resources.Count);
			}
			foreach (var r in resourceSets) {
				Debug.LogFormat ("[RMResourceManager]  r {0} {1} {2} {3}", r.name, r.parts.Count, r.sets.Count, r.resources.Count);
				foreach (var s in r.sets) {
					Debug.LogFormat ("[RMResourceManager] rs  {0} {1} {2} {3}", s.name, s.parts.Count, s.sets.Count, s.resources.Count);
				}
				foreach (var res in r.resources.Keys) {
					Debug.LogFormat ("[RMResourceManager] rr {0} {1} {2}", res, r.ResourceAmount (res), r.ResourceCapacity (res));
				}
			}
		}

		public RMResourceManager (string vesselName,
								  IEnumerable<Part> parts, Part rootPart,
								  HashSet<Part> excludeParts,
								  bool addVessels, bool useFlightID)
		{
			this.useFlightID = useFlightID;
			this.addVessels = addVessels;
			if (excludeParts == null) {
				excludeParts = new HashSet<Part> ();
			}
			this.excludeParts = excludeParts;

			AddVessel (vesselName, parts, rootPart);
			FinalizeResourceSets ();

			//DumpResourceSets ();
		}

		public RMResourceManager (IEnumerable<Part> parts, Part rootPart,
								  bool addVessels, bool useFlightID)
		{
			this.useFlightID = useFlightID;
			this.addVessels = addVessels;
			this.excludeParts = new HashSet<Part> ();

			string vesselName = "root";
			if (rootPart.vessel != null) {
				visitedVessels.Add (rootPart.vessel.id);
				vesselName = rootPart.vessel.vesselName;
			}

			AddVessel (vesselName, parts, rootPart);
			FinalizeResourceSets ();

			//DumpResourceSets ();
		}
	}
}
