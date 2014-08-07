using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ExSurveyTracker : MonoBehaviour
	{
		bool isStake (Vessel vessel)
		{
			if (vessel.Parts.Count != 1)
				return false;

			if (vessel[0].Modules.OfType<ExSurveyStake>().Count() < 1)
				return false;
			return true;
		}

		void AddStake (Vessel vessel)
		{
			Debug.Log (String.Format ("[EL ST] AddStake {0} {1}", vessel.vesselName, vessel.mainBody.bodyName));
		}

		void RemoveStake (Vessel vessel)
		{
			Debug.Log (String.Format ("[EL ST] RemoveStake {0} {1}", vessel.vesselName, vessel.mainBody.bodyName));
		}

		IEnumerator<YieldInstruction> WaitAndAddStake (Vessel vessel)
		{
			while (vessel.vesselName == null || vessel.vesselName == "") {
				yield return null;
			}
			AddStake (vessel);
		}

		void onVesselCreate (Vessel vessel)
		{
			if (!isStake (vessel))
				return;
			StartCoroutine (WaitAndAddStake (vessel));
		}

		void onVesselDestroy (Vessel vessel)
		{
			if (!isStake (vessel))
				return;
			RemoveStake (vessel);
		}

		void onVesselRename (GameEvents.HostedFromToAction<Vessel, string> h)
		{
			Vessel vessel = h.host;
			if (!isStake (vessel))
				return;
			RemoveStake (vessel);
			AddStake (vessel);
		}

		void Awake ()
		{
			enabled = false;
			GameEvents.onVesselCreate.Add (onVesselCreate);
			GameEvents.onVesselDestroy.Add (onVesselDestroy);
			GameEvents.onVesselRename.Add (onVesselRename);
		}

		void OnDestroy ()
		{
			GameEvents.onVesselCreate.Remove (onVesselCreate);
			GameEvents.onVesselDestroy.Remove (onVesselDestroy);
			GameEvents.onVesselRename.Remove (onVesselRename);
		}
	}
}
