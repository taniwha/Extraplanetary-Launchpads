using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class ExTarget : PartModule, ITargetable
	{
		[KSPField]
		public float ProductivityFactor = 1.0f;

		[KSPField]
		public string TargetName = "Target";
		[KSPField]
		public string TargetTransform;
		public Transform targetTransform;

		private FieldInfo targetMode;

		public override string GetInfo ()
		{
			return "Targetable";
		}

		public override void OnStart (PartModule.StartState state)
		{
			var mask = BindingFlags.NonPublic | BindingFlags.Instance;
			FieldInfo[] fields = typeof (FlightGlobals).GetFields(mask);
			foreach (var f in fields) {
				if (f.FieldType == typeof (FlightGlobals.VesselTargetModes)) {
					targetMode = f;
					break;
				}
			}
		}

		// ITargetable interface
		public Vector3 GetFwdVector ()
		{
			return targetTransform.forward;
		}
		public string GetName ()
		{
			return vessel.vesselName + " " + TargetName;
		}
		public Vector3 GetObtVelocity ()
		{
			return vessel.obt_velocity;
		}
		public Orbit GetOrbit ()
		{
			return vessel.orbit;
		}
		public OrbitDriver GetOrbitDriver ()
		{
			return vessel.orbitDriver;
		}
		public Vector3 GetSrfVelocity ()
		{
			return vessel.srf_velocity;
		}
		public Transform GetTransform ()
		{
			return targetTransform;
		}
		public Vessel GetVessel ()
		{
			return vessel;
		}

		public override void OnLoad (ConfigNode node)
		{
			targetTransform = part.FindModelTransform (TargetTransform);
			if (targetTransform == null) {
				targetTransform = transform;
			}
		}

		public void FixedUpdate ()
		{
			if (FlightGlobals.fetch.VesselTarget == this as ITargetable) {
				Events["SetAsTarget"].active = false;
				Events["UnsetTarget"].active = true;
			} else {
				Events["SetAsTarget"].active = true;
				Events["UnsetTarget"].active = false;
			}
		}

		[KSPEvent (guiName = "Set as Target", guiActiveUnfocused = true,
				   externalToEVAOnly = false, guiActive = false,
				   unfocusedRange = 200f, active = true)]
		public void SetAsTarget ()
		{
			FlightGlobals.fetch.SetVesselTarget (this);
			if (targetMode != null) {
				targetMode.SetValue (FlightGlobals.fetch,
									 FlightGlobals.VesselTargetModes.DirectionVelocityAndOrientation);
			}
		}

		[KSPEvent (guiName = "Unset Target", guiActiveUnfocused = true,
				   externalToEVAOnly = false, guiActive = false,
				   unfocusedRange = 200f, active = false)]
		public void UnsetTarget ()
		{
			FlightGlobals.fetch.SetVesselTarget (null);
		}
	}
}
