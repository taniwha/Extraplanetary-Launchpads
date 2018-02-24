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
using KSP.UI.Screens;

using ExtraplanetaryLaunchpads_KACWrapper;

namespace ExtraplanetaryLaunchpads {

	[KSPAddon (KSPAddon.Startup.Flight, false)]
	public class ELResourceWindow : MonoBehaviour
	{
		static ELResourceWindow instance;
		static bool hide_ui = false;
		static bool gui_enabled = true;
		static Rect windowpos;
		static bool link_lfo_sliders = true;
		static ScrollView resscroll = new ScrollView (680, 300);
		static GUILayoutOption toggleWidth = GUILayout.Width (80);

		public enum XferState {
			Hold,
			In,
			Out,
		};

		RMResourceManager resourceManager;
		bool []setSelected;
		XferState []xferState;
		Dictionary<string, RMResourceSet> dstSets;
		Dictionary<string, RMResourceSet> srcSets;
		bool canTransfer;
		List<string> resources;
		bool []resourceStates;
		bool resourceStatesChanged;
		bool transferring;

		internal void Start()
		{
		}

		public static void ToggleGUI ()
		{
			gui_enabled = !gui_enabled;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void HideGUI ()
		{
			gui_enabled = false;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void ShowGUI ()
		{
			gui_enabled = true;
			if (instance != null) {
				instance.UpdateGUIState ();
			}
		}

		public static void LoadSettings (ConfigNode node)
		{
			string val = node.GetValue ("rect");
			if (val != null) {
				Quaternion pos;
				pos = ConfigNode.ParseQuaternion (val);
				windowpos.x = pos.x;
				windowpos.y = pos.y;
				windowpos.width = pos.z;
				windowpos.height = pos.w;
			}
			val = node.GetValue ("visible");
			if (val != null) {
				bool.TryParse (val, out gui_enabled);
			}
			val = node.GetValue ("link_lfo_sliders");
			if (val != null) {
				bool.TryParse (val, out link_lfo_sliders);
			}
		}

		public static void SaveSettings (ConfigNode node)
		{
			Quaternion pos;
			pos.x = windowpos.x;
			pos.y = windowpos.y;
			pos.z = windowpos.width;
			pos.w = windowpos.height;
			node.AddValue ("rect", KSPUtil.WriteQuaternion (pos));
			node.AddValue ("visible", gui_enabled);
			node.AddValue ("link_lfo_sliders", link_lfo_sliders);
		}

		void onVesselChange (Vessel v)
		{
			resourceManager = new RMResourceManager (v.parts, true);
			resscroll.Reset ();
			var set = new HashSet<string> ();
			setSelected = null;
			xferState = null;
			dstSets.Clear ();
			srcSets.Clear ();
			foreach (var s in resourceManager.resourceSets) {
				foreach (string r in s.resources.Keys) {
					set.Add (r);
				}
			}
			resources = set.ToList ();
			resourceStates = new bool [resources.Count];

			UpdateGUIState ();
		}

		void onVesselWasModified (Vessel v)
		{
			if (FlightGlobals.ActiveVessel == v) {

			}
		}

		void UpdateGUIState ()
		{
			enabled = !hide_ui && resourceManager != null && gui_enabled;
		}

		void onHideUI ()
		{
			hide_ui = true;
			UpdateGUIState ();
		}

		void onShowUI ()
		{
			hide_ui = false;
			UpdateGUIState ();
		}

		void Awake ()
		{
			instance = this;
			GameEvents.onVesselChange.Add (onVesselChange);
			GameEvents.onVesselWasModified.Add (onVesselWasModified);
			GameEvents.onHideUI.Add (onHideUI);
			GameEvents.onShowUI.Add (onShowUI);
			enabled = false;

			dstSets = new Dictionary<string, RMResourceSet> ();
			srcSets = new Dictionary<string, RMResourceSet> ();
		}

		void OnDestroy ()
		{
			instance = null;
			GameEvents.onVesselChange.Remove (onVesselChange);
			GameEvents.onVesselWasModified.Remove (onVesselWasModified);
			GameEvents.onHideUI.Remove (onHideUI);
			GameEvents.onShowUI.Remove (onShowUI);
		}

		IEnumerator TransferResources ()
		{
			while (transferring) {
				bool didSomething = false;
				foreach (string res in dstSets.Keys) {
					var dst = dstSets[res];
					if (!srcSets.ContainsKey (res)) {
						continue;
					}
					var src = srcSets[res];
					double amount = dst.ResourceCapacity (res) / 20;
					amount *= Time.deltaTime;
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
				if (!didSomething) {
					transferring = false;
					break;
				}
				yield return new WaitForFixedUpdate ();
			}
		}

		void TransferButtons ()
		{
			bool gui_enabled = GUI.enabled;

			GUI.enabled = canTransfer;
			if (transferring) {
				if (GUILayout.Button ("Stop Transfer")) {
					transferring = false;
				}
			} else {
				if (GUILayout.Button ("Start Transfer")) {
					transferring = true;
					StartCoroutine (TransferResources ());
				}
			}
			GUI.enabled = gui_enabled;
		}

		void CloseButton ()
		{
			GUILayout.BeginHorizontal ();
			GUILayout.FlexibleSpace ();
			if (GUILayout.Button ("Close")) {
				HideGUI ();
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
		}

		bool ResourceLine (int ind)
		{
			string res = resources[ind];
			bool state = resourceStates[ind];
			GUILayout.BeginHorizontal ();
			resourceStates[ind] = GUILayout.Toggle (state, res);
			if (state != resourceStates[ind]) {
				resourceStatesChanged = true;
			}
			GUILayout.FlexibleSpace ();
			GUILayout.EndHorizontal ();
			return state;
		}

		void HighlightPart (Part part, bool on)
		{
			if (on) {
				part.SetHighlightColor (XKCDColors.LightSeaGreen);
				part.SetHighlight (true, false);
			} else {
				part.SetHighlightDefault ();
			}
		}

		void HighlightSet (RMResourceSet set, string res, bool on)
		{
			RMResourceInfo info;
			if (set.resources.TryGetValue (res, out info)) {
				for (int i = 0; i < info.containers.Count; i++) {
					var c = info.containers[i];
					if (c is PartResourceContainer) {
						HighlightPart (c.part, on);
					} else if (c is ResourceSetContainer) {
						var sc = c as ResourceSetContainer;
						HighlightSet (sc.set, res, on);
					}
				}
			}
		}

		void ToggleXferState (int ind, XferState state, string label)
		{
			bool on = false;
			if (xferState != null) {
				on = xferState[ind] == state;
			}
			if (GUILayout.Toggle (on, label, toggleWidth)) {
				if (xferState != null) {
					xferState[ind] = state;
				}
			}
		}

		void RemoveSet (Dictionary<string, RMResourceSet> dict, RMResourceSet set, string res)
		{
			var s = dict[res];
			s.RemoveSet (set);
			if (s.resources.Count < 1) {
				dict.Remove (res);
			}
		}

		void AddSet (Dictionary<string, RMResourceSet> dict, RMResourceSet set, string res)
		{
			if (!dict.ContainsKey (res)) {
				dict[res] = new RMResourceSet ();
				dict[res].balanced = true;
			}
			dict[res].AddSet (set);
		}

		void TransferState (int ind, RMResourceSet set, string res)
		{
			XferState old = XferState.Hold;
			if (xferState != null) {
				old = xferState[ind];
			}
			ToggleXferState (ind, XferState.Hold, "Hold");
			ToggleXferState (ind, XferState.In, "In");
			ToggleXferState (ind, XferState.Out, "Out");
			GUILayout.Space (40);
			if (xferState != null && xferState[ind] != old) {
				if (old == XferState.In) {
					RemoveSet (dstSets, set, res);
				} else if (old == XferState.Out) {
					RemoveSet (srcSets, set, res);
				}
				if (xferState[ind] == XferState.In) {
					AddSet (dstSets, set, res);
				} else if (xferState[ind] == XferState.Out) {
					AddSet (srcSets, set, res);
				}
				canTransfer = false;
				foreach (string r in dstSets.Keys) {
					if (srcSets.ContainsKey (r)) {
						canTransfer = true;
						break;
					}
				}
			}
		}

		void FlowState (RMResourceSet set, string res)
		{
			bool curFlow = set.GetFlowState (res);
			bool newFlow = GUILayout.Toggle (curFlow, "");
			if (newFlow != curFlow) {
				set.SetFlowState (res, newFlow);
			}
		}

		void ModuleResourceLine (int ind, RMResourceSet set, string res,
								 bool highlight)
		{
			double amount = set.ResourceAmount (res);
			double maxAmount = set.ResourceCapacity (res);
			GUILayout.BeginHorizontal ();
			GUILayout.Space (40);
			GUILayout.Label (set.name, ELStyles.label);
			GUILayout.FlexibleSpace ();
			string amountFmt = "F0";
			string maxAmountFmt = "F0";
			if (amount < 100) {
				amountFmt = "F2";
			}
			if (maxAmount < 100) {
				maxAmountFmt = "F2";
			}
			GUILayout.Label (amount.ToString(amountFmt), ELStyles.label);
			GUILayout.Label ("/", ELStyles.label);
			GUILayout.Label (maxAmount.ToString(maxAmountFmt), ELStyles.label);
			TransferState (ind, set, res);
			FlowState (set, res);
			GUILayout.EndHorizontal ();

			if (setSelected != null
				&& Event.current.type == EventType.Repaint) {
				var rect = GUILayoutUtility.GetLastRect();
				if (highlight && rect.Contains(Event.current.mousePosition)) {
					if (!setSelected[ind]) {
						setSelected[ind] = true;
						HighlightSet (set, res, true);
					}
				} else {
					if (setSelected[ind]) {
						setSelected[ind] = false;
						HighlightSet (set, res, false);
					}
				}
			}
		}

		void ResourceModules (bool highlight)
		{
			int ind = 0;
			for (int i = 0; i < resources.Count; i++) {
				string res = resources[i];
				if (ResourceLine (i)) {
					for (int j = 0; j < resourceManager.resourceSets.Count; j++) {
						var set = resourceManager.resourceSets[j];
						if (!set.resources.ContainsKey (res)) {
							continue;
						}
						ModuleResourceLine (ind++, set, res, highlight);
					}
				}
			}
			if (resourceStatesChanged) {
				resourceStatesChanged = false;
				setSelected = null;
				xferState = null;
			} else {
				if (setSelected == null && ind > 0) {
					setSelected = new bool[ind];
					xferState = new XferState[ind];
				}
			}
		}

		void WindowGUI (int windowID)
		{
			ELStyles.Init ();

			GUILayout.BeginVertical ();

			resscroll.Begin ();
			ResourceModules (resscroll.mouseOver);
			resscroll.End ();

			GUILayout.EndVertical ();

			TransferButtons ();
			CloseButton ();

			GUI.DragWindow (new Rect (0, 0, 10000, 20));

		}

		void OnGUI ()
		{
			GUI.skin = HighLogic.Skin;
			windowpos = GUILayout.Window (GetInstanceID (),
										  windowpos, WindowGUI,
										  ELVersionReport.GetVersion (),
										  GUILayout.Width (695));
		}
	}
}
