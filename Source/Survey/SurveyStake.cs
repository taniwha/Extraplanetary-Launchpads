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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KSP.IO;
using HighlightingSystem;

namespace ExtraplanetaryLaunchpads {

	public class ExSurveyStake : PartModule, IModuleInfo
	{
		internal static string[] StakeUses = { "Origin",
											   "+X", "+Y", "+Z",
											   "-X", "-Y", "-Z"};
		[KSPField (isPersistant = true)]
		internal bool bound = false;
		[KSPField (isPersistant = true)]
		internal int use = 0;

		internal static Color[] StakeColors = {
			XKCDColors.LightSeaGreen,
			XKCDColors.CherryRed,
			XKCDColors.FluorescentGreen,
			XKCDColors.BrightSkyBlue,
			XKCDColors.RustyOrange,
			XKCDColors.MossGreen,
			XKCDColors.DeepSkyBlue,
		};
		Highlighter highlighter;

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

		public string GetPrimaryField ()
		{
			return null;
		}

		public string GetModuleTitle ()
		{
			return "EL Survey Stake";
		}

		public Callback<Rect> GetDrawModulePanelCallback ()
		{
			return null;
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
			Events["ToggleBound"].guiName = bound ? "Bound" : "Direction";
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

		[KSPEvent(active = true, guiActiveUnfocused = true, externalToEVAOnly = true, guiActive = false, unfocusedRange = 2f, guiName = "")]
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

		public void Highlight (bool on)
		{
			if (on) {
				var color = StakeColors[use];
				var model = part.FindModelTransform("model");
				if (highlighter == null) {
					var go = model.gameObject;
					highlighter = go.GetComponent<Highlighter>();
					if (highlighter == null) {
						highlighter = go.AddComponent<Highlighter>();
					}
				}
				if (bound) {
					var color2 = XKCDColors.LightGreyBlue;
					highlighter.FlashingOn (color, color2, 1.0f);
				} else {
					highlighter.ConstantOn (color);
				}
				part.SetHighlightColor (color);
				part.SetHighlight (true, false);
			} else {
				if (highlighter != null) {
					part.SetHighlightDefault ();
					highlighter.Off ();
				}
			}
		}
	}
}
