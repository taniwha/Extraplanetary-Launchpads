using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;

namespace ExLP {

	public class ExSurveyStake : PartModule
	{
		internal static string[] StakeUses = { "Origin",
											   "+X", "+Y", "+Z",
											   "-X", "-Y", "-Z"};
		[KSPField (isPersistant = true)]
		internal bool bound = false;
		[KSPField (isPersistant = true)]
		internal int use = 0;

		internal string Name
		{
			get {
				return vessel.vesselName;
			}
		}

		public override string GetInfo ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return "";
			}
			return "Survey Stake";
		}

		public override void OnLoad (ConfigNode node)
		{
		}

		public override void OnStart(StartState state)
		{
			if (CompatibilityChecker.IsWin64 ()) {
				Events["NextUse"].active = false;
				//Events["ToggleBound"].active = false;
				Events["RenameVessel"].active = false;
			}
			Events["NextUse"].guiName = StakeUses[use];
			//Events["ToggleBound"].guiName = bound ? "Bound" : "Direction";
		}

		public void OnPartDie ()
		{
			if (CompatibilityChecker.IsWin64 ()) {
				return;
			}
			ExSurveyTracker.instance.RemoveStake (vessel);
		}

		public void FixedUpdate ()
		{
		}

		[KSPEvent(active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 2f, guiName = "")]
		public void NextUse()
		{
			use = (use + 1) % StakeUses.Count();
			Events["NextUse"].guiName = StakeUses[use];
		}

		//[KSPEvent(active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 2f, guiName = "")]
		public void ToggleBound()
		{
			bound = !bound;
			Events["ToggleBound"].guiName = bound ? "Bound" : "Direction";
		}

		[KSPEvent (active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 2f, guiName = "Rename Stake")]
		public void RenameVessel ()
		{
			vessel.RenameVessel ();
		}
	}
}
