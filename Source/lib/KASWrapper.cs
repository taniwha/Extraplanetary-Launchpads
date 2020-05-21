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

using ExtraplanetaryLaunchpads;

namespace ExtraplanetaryLaunchpads.KAS {

	public class ILinkPeer {
		static Type ILinkPeer_iface;
		static PropertyInfo ILP_part;
		static PropertyInfo ILP_linkState;
		static PropertyInfo ILP_otherPeer;
		static PropertyInfo ILP_linkPartId;
		static PropertyInfo ILP_isLinked;
		static PropertyInfo ILP_cfgLinkType;

		protected object obj;

		public Part part
		{
			get {
				return (Part) ILP_part.GetValue (obj, null);
			}
		}

		public int linkState
		{
			get {
				return (int) ILP_linkState.GetValue (obj, null);
			}
		}

		public ILinkPeer otherPeer
		{
			get {
				object o = ILP_otherPeer.GetValue (obj, null);
				return o != null ? new ILinkPeer (o) : null;
			}
		}

		public uint linkPartId
		{
			get {
				return (uint) ILP_linkPartId.GetValue (obj, null);
			}
		}

		public bool isLinked
		{
			get {
				return (bool) ILP_isLinked.GetValue (obj, null);
			}
		}

		public string cfgLinkType
		{
			get {
				return (string) ILP_cfgLinkType.GetValue (obj, null);
			}
		}

		public ILinkPeer (object obj)
		{
			this.obj = obj;
		}
		internal static void Initialize (Assembly KASasm)
		{
			ILinkPeer_iface = KASasm.GetTypes().Where (t => t.Name.Equals ("ILinkPeer")).FirstOrDefault ();
			ILP_part = ILinkPeer_iface.GetProperty ("part");
			ILP_linkState = ILinkPeer_iface.GetProperty ("linkState");
			ILP_otherPeer = ILinkPeer_iface.GetProperty ("otherPeer");
			ILP_linkPartId = ILinkPeer_iface.GetProperty ("linkPartId");
			ILP_isLinked = ILinkPeer_iface.GetProperty ("isLinked");
			ILP_cfgLinkType = ILinkPeer_iface.GetProperty ("cfgLinkType");
		}
	}

	public class ILinkSource: ILinkPeer {
		static Type ILinkSource_iface;
		static PropertyInfo ILS_linkTarget;
		static PropertyInfo ILS_linkJoint;

		public ILinkTarget linkTarget
		{
			get {
				object o = ILS_linkTarget.GetValue (obj, null);
				return o != null ? new ILinkTarget (o) : null;
			}
		}

		public ILinkJoint linkJoint
		{
			get {
				object o = ILS_linkJoint.GetValue (obj, null);
				return o != null ? new ILinkJoint (o) : null;
			}
		}

		public ILinkSource (object obj) : base(obj)
		{
		}
		internal static new void Initialize (Assembly KASasm)
		{
			ILinkSource_iface = KASasm.GetTypes().Where (t => t.Name.Equals ("ILinkSource")).FirstOrDefault ();
			ILS_linkTarget = ILinkSource_iface.GetProperty ("linkTarget");
			ILS_linkJoint = ILinkSource_iface.GetProperty ("linkJoint");
		}
	}

	public class ILinkTarget: ILinkPeer {
		static Type ILinkTarget_iface;
		static PropertyInfo ILT_linkSource;

		public ILinkSource linkSource
		{
			get {
				object o = ILT_linkSource.GetValue (obj, null);
				return o != null ? new ILinkSource (o) : null;
			}
		}

		public ILinkTarget (object obj) : base (obj)
		{
		}
		internal static new void Initialize (Assembly KASasm)
		{
			ILinkTarget_iface = KASasm.GetTypes().Where (t => t.Name.Equals ("ILinkTarget")).FirstOrDefault ();
			ILT_linkSource = ILinkTarget_iface.GetProperty ("linkSource");
		}
	}

	public class ILinkJoint {
		static Type ILinkJoint_iface;
		static PropertyInfo ILJ_isLinked;
		static PropertyInfo ILJ_linkSource;
		static PropertyInfo ILJ_linkTarget;

		public bool isLinked
		{
			get {
				return (bool) ILJ_isLinked.GetValue (obj, null);
			}
		}

		public ILinkSource linkSource
		{
			get {
				object o = ILJ_linkSource.GetValue (obj, null);
				return o != null ? new ILinkSource (o) : null;
			}
		}

		public ILinkTarget linkTarget
		{
			get {
				object o = ILJ_linkTarget.GetValue (obj, null);
				return o != null ? new ILinkTarget (o) : null;
			}
		}

		protected object obj;

		public ILinkJoint (object obj)
		{
			this.obj = obj;
		}
		internal static void Initialize (Assembly KASasm)
		{
			ILinkJoint_iface = KASasm.GetTypes().Where (t => t.Name.Equals ("ILinkJoint")).FirstOrDefault ();
			ILJ_isLinked = ILinkJoint_iface.GetProperty ("isLinked");
			ILJ_linkSource = ILinkJoint_iface.GetProperty ("linkSource");
			ILJ_linkTarget = ILinkJoint_iface.GetProperty ("linkTarget");
		}
	}

	public class IKasLinkEvent {
		static Type IKasLinkEvent_iface;
		static PropertyInfo IKLE_source;
		static PropertyInfo IKLE_target;
		static PropertyInfo IKLE_actor;

		public ILinkSource source
		{
			get {
				object o = IKLE_source.GetValue (obj, null);
				return o != null ? new ILinkSource (o) : null;
			}
		}

		public ILinkTarget target
		{
			get {
				object o = IKLE_target.GetValue (obj, null);
				return o != null ? new ILinkTarget (o) : null;
			}
		}

		public int actor
		{
			get {
				return (int) IKLE_actor.GetValue (obj, null);
			}
		}

		object obj;

		public IKasLinkEvent (object obj)
		{
			this.obj = obj;
		}
		internal static void Initialize (Assembly KASasm)
		{
			IKasLinkEvent_iface = KASasm.GetTypes().Where (t => t.Name.Equals ("IKasLinkEvent")).FirstOrDefault ();
			IKLE_source = IKasLinkEvent_iface.GetProperty ("source");
			IKLE_target = IKasLinkEvent_iface.GetProperty ("target");
			IKLE_actor = IKasLinkEvent_iface.GetProperty ("actor");
		}
	}

	public class IKasEvents {
		static Type IKasEvents_iface;
		static PropertyInfo IKE_OnLinkCreated;
		static PropertyInfo IKE_OnLinkBroken;
		static object KasEvents;
		static IKasEvents ikasEvents;

		public static EventData<IKasLinkEvent> OnLinkCreated = new EventData<IKasLinkEvent>("KASWrapper.IKasEvents.OnLinkCreated");
		public static EventData<IKasLinkEvent> OnLinkBroken = new EventData<IKasLinkEvent>("KASWrapper.IKasEvents.OnLinkBroken");

		static void onLinkCreated (object obj, object data)
		{
			OnLinkCreated.Fire(new IKasLinkEvent (data));
		}

		static void onLinkBroken (object obj, object data)
		{
			OnLinkBroken.Fire(new IKasLinkEvent (data));
		}

		static void AddEvent (object evt, MethodInfo cb)
		{
			Type eType = evt.GetType ();
			var Add = eType.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public);
			var parms = Add.GetParameters();
			var dType = parms[0].ParameterType;
			Delegate d = Delegate.CreateDelegate (dType, ikasEvents, cb);
			Add.Invoke(evt, new object[]{d});
		}

		static MethodInfo GetMethodInfo (Delegate d)
		{
			return d.Method;
		}

		internal static void Initialize (Assembly KASasm)
		{
			if (KasEvents == null) {
				ikasEvents = new IKasEvents();
				var KASAPI = KASasm.GetTypes().Where (t => t.Name.Equals ("KASAPI")).FirstOrDefault ();
				var kaf = KASAPI.GetField ("KasEvents", BindingFlags.Static | BindingFlags.Public);
				KasEvents = kaf.GetValue (null);

				IKasEvents_iface = KASasm.GetTypes().Where (t => t.Name.Equals ("IKasEvents")).FirstOrDefault ();
				IKE_OnLinkCreated = IKasEvents_iface.GetProperty ("OnLinkCreated");
				IKE_OnLinkBroken = IKasEvents_iface.GetProperty ("OnLinkBroken");

				var olc = IKE_OnLinkCreated.GetValue (KasEvents, null);
				AddEvent (olc, GetMethodInfo((Action<object, object>)onLinkCreated));
				var olb = IKE_OnLinkBroken.GetValue (KasEvents, null);
				AddEvent (olb, GetMethodInfo((Action<object, object>)onLinkBroken));
			}
		}
	}

	public class KASJointCableBase: ILinkJoint {
		static Type AbstractJoint_class;

		static FieldInfo persistedSrcVesselInfo_field;
		static FieldInfo persistedTgtVesselInfo_field;

		public DockedVesselInfo persistedSrcVesselInfo
		{
			get {
				return (DockedVesselInfo) persistedSrcVesselInfo_field.GetValue (obj);
			}
		}

		public DockedVesselInfo persistedTgtVesselInfo
		{
			get {
				return (DockedVesselInfo) persistedTgtVesselInfo_field.GetValue (obj);
			}
		}

		public KASJointCableBase (object obj) : base (obj)
		{
		}

		internal static new void Initialize (Assembly KASasm)
		{
			AbstractJoint_class = KASasm.GetTypes().Where (t => t.Name.Equals ("AbstractJoint")).FirstOrDefault ();
			persistedSrcVesselInfo_field = AbstractJoint_class.GetField ("persistedSrcVesselInfo", KASWrapper.bindingFlags);
			persistedTgtVesselInfo_field = AbstractJoint_class.GetField ("persistedTgtVesselInfo", KASWrapper.bindingFlags);
			Debug.Log($"[EL.KASJointCableBase] '{persistedSrcVesselInfo_field}' '{persistedTgtVesselInfo_field}'");
		}
	}

	public class KASModuleStrut {
		static Type KASModuleAttachCore_class;

		object obj;

		static FieldInfo vesselInfo_field;
		static FieldInfo dockedPartID_field;
		public DockedVesselInfo vesselInfo
		{
			get {
				return (DockedVesselInfo) vesselInfo_field.GetValue (obj);
			}
		}
		public uint dockedPartID
		{
			get {
				var str = (string) dockedPartID_field.GetValue (obj);
				uint id;
				uint.TryParse (str, out id);
				return id;
			}
		}

		public KASModuleStrut (object obj)
		{
			this.obj = obj;
		}
		internal static void Initialize (Assembly KASasm)
		{
			KASModuleAttachCore_class = KASasm.GetTypes().Where (t => t.Name.Equals ("KASModuleAttachCore")).FirstOrDefault ();
			vesselInfo_field = KASModuleAttachCore_class.GetField ("vesselInfo", KASWrapper.bindingFlags);
			dockedPartID_field = KASModuleAttachCore_class.GetField ("dockedPartID", KASWrapper.bindingFlags);
		}
	}

	public class KASWrapper {

		internal const BindingFlags bindingFlags = BindingFlags.NonPublic
												 | BindingFlags.Instance
												 | BindingFlags.Public;
		static bool haveKAS = false;
		static bool inited = false;

		public static bool Initialize ()
		{
			if (!inited) {
				inited = true; // do this only once, assemblies won't change
				AssemblyLoader.LoadedAssembly KASAPIasm = null;
				AssemblyLoader.LoadedAssembly KASasm = null;
				AssemblyLoader.LoadedAssembly KASLegacyasm = null;
				int apiVersion = 0;

				foreach (var la in AssemblyLoader.loadedAssemblies) {
					string asmName = la.assembly.GetName ().Name;
					if (asmName.Equals ("KAS-API-v2", StringComparison.InvariantCultureIgnoreCase)) {
						KASAPIasm = la;
						apiVersion = 2;
					} else if (asmName.Equals ("KAS-API-v1", StringComparison.InvariantCultureIgnoreCase)) {
						KASAPIasm = la;
						apiVersion = 1;
					} else if (asmName.Equals ("KAS", StringComparison.InvariantCultureIgnoreCase)) {
						if (KASLegacyasm == null) {
							KASLegacyasm = la;
						} else {
							KASasm = la;
						}
					}
				}
				haveKAS = false;
				if (apiVersion == 2) {
					// With API version 2, legacy has gone away and now there's
					// just the API dll and the main dll
					// however, the main dll gets picked up as legacy
					KASasm = KASLegacyasm;
					KASLegacyasm = null;
				}
				if (KASAPIasm != null && KASasm != null) {
					haveKAS = true;
					ILinkPeer.Initialize (KASAPIasm.assembly);
					ILinkSource.Initialize (KASAPIasm.assembly);
					ILinkTarget.Initialize (KASAPIasm.assembly);
					ILinkJoint.Initialize (KASAPIasm.assembly);
					IKasLinkEvent.Initialize (KASAPIasm.assembly);
					IKasEvents.Initialize (KASAPIasm.assembly);

					KASJointCableBase.Initialize (KASasm.assembly);
				}
				if (KASLegacyasm != null) {
					haveKAS = true;
					KASModuleStrut.Initialize (KASLegacyasm.assembly);
				}
			}

			return haveKAS;
		}
	}
}
