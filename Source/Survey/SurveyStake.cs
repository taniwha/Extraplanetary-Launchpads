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
using UnityEngine;
using UnityEngine.UI;

using Highlighting;

namespace ExtraplanetaryLaunchpads {

	public class ELSurveyStake : PartModule, IModuleInfo
	{
		public struct Data
		{
			public bool bound;
			public int use;
			public Vessel vessel;
			public ELSurveyStake stake;
		}

		public static void RenameStake (Vessel v, string name)
		{
			if (Vessel.IsValidVesselName (name)) {
				string oldname = v.vesselName;
				v.vesselName = name;
				GameEvents.onVesselRename.Fire (new GameEvents.HostedFromToAction<Vessel, string> (v, oldname, v.vesselName));
			}
		}

		public static Data GetData (Vessel v)
		{
			if (v.loaded) {
				var stake = v[0].FindModuleImplementing<ELSurveyStake> ();
				return stake.GetData ();
			} else {
				var ppart = v.protoVessel.protoPartSnapshots[0];
				var stake = ppart.FindModule ("ELSurveyStake");

				Data data = new Data ();
				ConfigNode node = stake.moduleValues;
				bool.TryParse (node.GetValue ("bound"), out data.bound);
				int.TryParse (node.GetValue ("use"), out data.use);
				data.vessel = v;
				data.stake = null;	// part not loaded
				return data;
			}
		}

		public Data GetData ()
		{
			Data data = new Data ();
			data.bound = bound;
			data.use = use;
			data.vessel = vessel;
			data.stake = this;
			return data;
		}

		internal static string[] StakeUses = { "Origin",
											   "+X", "+Y", "+Z",
											   "-X", "-Y", "-Z"};
		internal static string[] StakeUses_short = { " O",
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

		const float PlaqueAlpha = 0.75f;

		GameObject plaque;
		CanvasRenderer plaqueBackgroundRenderer;
		CanvasRenderer plaqueTextRenderer;
		Text plaqueText;
		//TextMeshPro plaqueText;

		internal string Name
		{
			get {
				return vessel.vesselName;
			}
		}

		public override string GetInfo ()
		{
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
			Events["NextUse"].guiName = StakeUses[use];
			Events["ToggleBound"].guiName = bound ? "Bound" : "Direction";
			if (HighLogic.LoadedSceneIsFlight) {
				CreatePlaque ();
				UpdatePlaque ();
			}
		}

		void OnDestroy ()
		{
		}

		public void OnPartDie ()
		{
			ELSurveyTracker.instance.RemoveStake (vessel);
		}

		[KSPEvent(active = true, guiActiveUnfocused = true,
				  externalToEVAOnly = false, guiActive = false,
				  unfocusedRange = 200f, guiName = "")]
		public void NextUse()
		{
			use = (use + 1) % StakeUses.Length;
			Events["NextUse"].guiName = StakeUses[use];
			UpdatePlaque ();
			ELSurveyTracker.onStakeModified.Fire (this);
		}

		[KSPEvent(active = true, guiActiveUnfocused = true,
				  externalToEVAOnly = false, guiActive = false,
				  unfocusedRange = 200f, guiName = "")]
		public void ToggleBound()
		{
			bound = !bound;
			Events["ToggleBound"].guiName = bound ? "Bound" : "Direction";
			UpdatePlaque ();
			ELSurveyTracker.onStakeModified.Fire (this);
		}

		[KSPEvent (active = true, guiActiveUnfocused = true,
				   externalToEVAOnly = false, guiActive = false,
				   unfocusedRange = 200f, guiName = "Rename Stake")]
		public void RenameVessel ()
		{
			vessel.RenameVessel ();
		}

		public void Highlight (bool on)
		{
			plaque.SetActive (on);

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

				UpdatePlaque ();
			} else {
				if (highlighter != null) {
					part.SetHighlightDefault ();
					highlighter.Off ();
				}
			}
		}

		void UpdatePlaque ()
		{
			var color = StakeColors[use];
			plaqueBackgroundRenderer.SetColor (color);
			plaqueTextRenderer.SetColor (color);
			plaqueText.text = (bound ? "B" : "D") + StakeUses_short[use];
		}

		void CreateBackground (RectTransform parent)
		{
			GameObject go = new GameObject ("Survey Plaque Background",
											typeof (RectTransform));
			plaqueBackgroundRenderer = go.AddComponent<CanvasRenderer> ();
			plaqueBackgroundRenderer.SetAlpha (PlaqueAlpha);

			go.layer = LayerMask.NameToLayer("Ignore Raycast");
			RectTransform rxform = go.transform as RectTransform;
			rxform.SetParent (parent, false);
			rxform.anchorMin = new Vector2 (0, 0);
			rxform.anchorMax = new Vector2 (1, 1);
			rxform.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, 80);
			rxform.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, 50);

			Image bg = go.AddComponent<Image> ();
			Texture2D tex = GameDatabase.Instance.GetTexture("ExtraplanetaryLaunchpads/Textures/plaque", false);
			bg.sprite = Sprite.Create (tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f, 0, SpriteMeshType.FullRect, new Vector4 (17, 17, 17, 17));
			bg.type = Image.Type.Sliced;
		}

		void CreateText (RectTransform parent)
		{
			GameObject go = new GameObject ("Survey Plaque Text",
											typeof (RectTransform));
			plaqueTextRenderer = go.AddComponent<CanvasRenderer> ();
			plaqueTextRenderer.SetAlpha (PlaqueAlpha);

			go.layer = LayerMask.NameToLayer("Ignore Raycast");
			RectTransform rxform = go.transform as RectTransform;
			rxform.SetParent (parent, false);
			rxform.anchorMin = new Vector2 (0, 0);
			rxform.anchorMax = new Vector2 (1, 1);

			plaqueText = go.AddComponent<Text> ();
			plaqueText.alignment = TextAnchor.MiddleCenter;
			Font ArialFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
			plaqueText.font = ArialFont;
			plaqueText.material = ArialFont.material;
			plaqueText.fontSize = 35;

			//plaqueText = go.AddComponent<TextMeshPro> ();
			//plaqueText.alignment = TextAlignmentOptions.Center;
			//plaqueText.font = UISkinManager.TMPFont;
			//plaqueText.outlineWidth = 0.15f;
		}

		void CreatePlaque ()
		{
			plaque = new GameObject ("Survey Plaque");
			plaque.transform.SetParent (transform, false);

			EL_Billboard billboard = plaque.AddComponent<EL_Billboard>();
			billboard.LocalUp = LocalUp;

			plaque.SetActive (false);

			GameObject go = new GameObject ("Survey Plaque Canvas",
											typeof (RectTransform),
											typeof (Canvas),
											typeof (CanvasScaler));
			RectTransform rxform = go.transform as RectTransform;
			rxform.SetParent (plaque.transform, false);
			rxform.localPosition = new Vector3 (0, 0.5f, 0);
			rxform.localScale = new Vector3 (0.01f, 0.01f, 0.01f);
			rxform.SetSizeWithCurrentAnchors (RectTransform.Axis.Horizontal, 80);
			rxform.SetSizeWithCurrentAnchors (RectTransform.Axis.Vertical, 50);

			CreateBackground (rxform);
			CreateText (rxform);
		}

		Vector3 LocalUp ()
		{
			return vessel.mainBody.LocalUp (transform.position);
		}
	}
}
