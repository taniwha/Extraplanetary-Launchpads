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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ExtraplanetaryLaunchpads {

	public class ELEditor : MonoBehaviour
	{
		const BindingFlags bindFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
		/** Delegate for Update and normal Start
		 */
		delegate void UpdateDelegate ();
		/** Delegate for coroutine Start
		 */
		delegate IEnumerator StartDelegate ();

		static ELEditor editor;
		static FieldInfo rootPart;

		ShipConstruct ship;

		/** Disabled instance of the VAB/SPH editor so that part modules can
		 * get at \a ship when they need to.
		 */
		EditorLogic fakeEditor;

		List<UpdateDelegate> Updaters;
		List<UpdateDelegate> FixedUpdaters;
		List<UpdateDelegate> LateUpdaters;
		// for Part.Start and any modules that use a coroutine Start
		List<StartDelegate> CRStarters;
		// For any other modules
		List<UpdateDelegate> Starters;

		bool ready;

		void Awake ()
		{
			Updaters = new List<UpdateDelegate> ();
			FixedUpdaters = new List<UpdateDelegate> ();
			LateUpdaters = new List<UpdateDelegate> ();
			CRStarters = new List<StartDelegate> ();
			Starters = new List<UpdateDelegate> ();

			rootPart = typeof(EditorLogic).GetField ("rootPart", bindFlags);
			editor = this;
		}

		void OnDestroy ()
		{
			editor = null;
		}

		void OnDisable ()
		{
			Debug.Log ($"[ELEditor] OnDisable");

			Updaters.Clear ();
			FixedUpdaters.Clear ();
			LateUpdaters.Clear ();
			CRStarters.Clear ();
			Starters.Clear ();

			ready = false;

			// Remove the fake EditorLogic so it doesn't cause problems the
			// next time the player goes to the VAB or SPH
			if (fakeEditor) {
				Destroy (fakeEditor.gameObject);
			}
			fakeEditor = null;
		}

		/** Simple wrapper to create a delegate for update or normal Start
		 */
		UpdateDelegate CreateUpdateDelegate (MonoBehaviour module, MethodInfo method)
		{
			return (UpdateDelegate) Delegate.CreateDelegate (typeof (UpdateDelegate), module, method);
		}

		/** Simple wrapper to create a delegate for coroutine Start
		 */
		StartDelegate CreateStartDelegate (MonoBehaviour module, MethodInfo method)
		{
			return (StartDelegate) Delegate.CreateDelegate (typeof (StartDelegate), module, method);
		}

		/** Find the start method and add it to the appropriate list.
		 *
		 * Unity supports coroutine Start() (return IEnumerator instead of void)
		 * so need to check the return time.
		 */
		void FindStart (MonoBehaviour module)
		{
			var type = module.GetType ();
			var start = type.GetMethod ("Start", bindFlags);
			if (start == null) {
				// many a PartModule relies on OnStart (called indirectly by
				// Part.Start) rather than Start
				return;
			}
			if (start.ReturnType == typeof(void)) {
				// Strt is a normal (void) method
				var del = CreateUpdateDelegate (module, start);
				Starters.Add (del);
			} else {
				// Start is a coroutine
				var del = CreateStartDelegate (module, start);
				CRStarters.Add (del);
			}
		}

		/** Find the named update method and add a delegate to the list.
		 */
		void FindUpdater (MonoBehaviour module, string name, List<UpdateDelegate> updaters)
		{
			var type = module.GetType ();
			var update = type.GetMethod (name, bindFlags);
			if (update == null) {
				// Not all modules implement all updaters (some don't impement
				// any)
				return;
			}
			var del = CreateUpdateDelegate (module, update);
			updaters.Add (del);
		}

		void RemoveUpdaters (object target, List<UpdateDelegate> updaters)
		{
			for (int i = updaters.Count; i-- > 0; ) {
				if (updaters[i].Target == target) {
					updaters.RemoveAt (i);
				}
			}
		}

		void RemoveUpdaters (object target)
		{
			RemoveUpdaters (target, Updaters);
			RemoveUpdaters (target, FixedUpdaters);
			RemoveUpdaters (target, LateUpdaters);
		}

		/** Run all the collected Start methods.
		 *
		 * Both Starters and CRStarters will be empty on completion.
		 */
		void RunStarters ()
		{
			SetEditorScene ();

			// run through all the simple starters first
			for (int i = Starters.Count; i-- > 0; ) {
				var mod = Starters[i].Target as MonoBehaviour;
				mod.enabled = true;
				try {
					Starters[i] ();
				} catch (Exception e) {
					Debug.LogError ($"[ELEditor] caught Start exception: {e.Message}\n{e.StackTrace}");
					Debug.LogError ($"        on {Starters[i].Target}");
				}
				if (!mod.enabled) {
					// the module (probably PartModule, but...) disabled itself
					//Debug.Log ($"[ELEditor] {Starters[i].Target} disabled itself");
					RemoveUpdaters (Starters[i].Target);
				}
				mod.enabled = false;
				// These are one-shot methods, so remove as we go
				Starters.RemoveAt (i);
			}

			Debug.Log ($"[ELEditor] RunStarters: running coroutine Start");
			// run through all the coroutine starters
			var starters = new List<IEnumerator> ();
			for (int i = 0; i < CRStarters.Count; i++) {
				starters.Add (CRStarters[i] ());
			}
			for (int i = ship.parts.Count; i-- > 0; ) {
				Part p = ship.parts[i];
				for (int j = p.Modules.Count; j-- > 0; ) {
					p.Modules[j].enabled = true;
				}
			}
			while (CRStarters.Count > 0) {
				for (int i = CRStarters.Count; i-- > 0; ) {
					bool remove = false;
					var mod = CRStarters[i].Target as MonoBehaviour;
					mod.enabled = true;
					try {
						remove = !starters[i].MoveNext ();
					} catch (Exception e) {
						Debug.LogError ($"[ELEditor] caught Start exception: {e.Message}\n{e.StackTrace}");
						Debug.LogError ($"        on {CRStarters[i].Target}");
						Debug.LogError ($"        Disabling {starters[i]} ({CRStarters[i]})");
						remove = true;
					}
					if (!mod.enabled) {
						// the module (probably PartModule, but...) disabled
						// itself
						//Debug.Log ($"[ELEditor] {CRStarters[i].Target} disabled itself");
						RemoveUpdaters (CRStarters[i].Target);
						remove = true;
					}
					mod.enabled = false;
					if (remove) {
						starters.RemoveAt (i);
						CRStarters.RemoveAt (i);
					}
				}
			}
			for (int i = ship.parts.Count; i-- > 0; ) {
				Part p = ship.parts[i];
				for (int j = p.Modules.Count; j-- > 0; ) {
					PartModule m = p.Modules[j];

					if (!m.enabled) {
						//Debug.Log ($"[ELEditor] {m} disabled itself");
						RemoveUpdaters (m);
					}

					m.enabled = false;
				}
			}
			Debug.Log ($"[ELEditor] RunStarters: end coroutine Start");

			RestoreScene ();

			ready = true;
		}

		void DisableCoroutines ()
		{
			for (int i = ship.parts.Count; i-- > 0; ) {
				Part p = ship.parts[i];
				p.StopAllCoroutines ();
				for (int j = p.Modules.Count; j-- > 0; ) {
					PartModule m = p.Modules[j];
					m.StopAllCoroutines ();
				}
			}
		}

		IEnumerator WaitAndRunStarters ()
		{
			yield return null;
			RunStarters ();
			DisableCoroutines ();
		}

		void SetupEditor (ShipConstruct ship)
		{
			this.ship = ship;

			Debug.Log ($"[ELEditor] SetupEditor");

			var go = new GameObject ("Fake Editor");
			go.transform.SetParent (transform, false);

			fakeEditor = go.AddComponent<EditorLogic> ();
			// Do not want KSP's EditorLogic to run, it's needed only to hold
			// ShipConstruct and any other necessary references so PartModules
			// can run thinking they are in the editor.
			fakeEditor.enabled = false;

			fakeEditor.ship = ship;
			Part root = ship.parts[0].localRoot;
			rootPart.SetValue (fakeEditor, ship.parts[0].localRoot);

			var nfo = new GameObject ("FakeNameField", typeof (TMP_InputField));
			nfo.transform.SetParent (go.transform, false);
			nfo.SetActive (false);
			var fnf = nfo.GetComponent<TMP_InputField> ();
			fakeEditor.shipNameField = fnf;
			fakeEditor.shipDescriptionField = fnf;

			var bo = new GameObject ("FakeButton",
									 typeof (Button),
									 typeof (FlagBrowserButton),
									 typeof (KSP.UI.UIOnClick));
			bo.transform.SetParent (go.transform, false);
			bo.SetActive (false);
			var fb = bo.GetComponent<Button> ();
			var foc = bo.GetComponent<KSP.UI.UIOnClick> ();
			var fbb = bo.GetComponent<FlagBrowserButton> ();
			fakeEditor.loadBtn = fb;
			fakeEditor.saveBtn = fb;
			fakeEditor.steamBtn = fb;
			fakeEditor.newBtn = fb;
			fakeEditor.launchBtn = fb;
			fakeEditor.exitBtn = fb;
			fakeEditor.symmetryButton = foc;
			fakeEditor.angleSnapButton = foc;
			fakeEditor.actionPanelBtn = fb;
			fakeEditor.partPanelBtn = fb;
			fakeEditor.crewPanelBtn = fb;
			fakeEditor.switchEditorBtn = fb;

			fakeEditor.flagBrowserButton = fbb;
			fakeEditor.flagBrowserButton.button = fb;

			for (int i = 0; i < ship.parts.Count; i++) {
				Part p = ship.parts[i];
				FindStart (p);
				FindUpdater (p, "Update", Updaters);
				FindUpdater (p, "FixedUpdate", FixedUpdaters);
				FindUpdater (p, "LateUpdate", LateUpdaters);
				for (int j = 0; j < p.Modules.Count; j++) {
					PartModule m = p.Modules[j];
					FindStart (m);
					FindUpdater (m, "Update", Updaters);
					FindUpdater (m, "FixedUpdate", FixedUpdaters);
					FindUpdater (m, "LateUpdate", LateUpdaters);
				}
			}

			StartCoroutine (WaitAndRunStarters ());
		}

		bool lsie;
		bool lsif;
		GameScenes scene;

		void SetEditorScene ()
		{
			// Save true state in case this is ever used in a scene other than
			// flight (though currently only editor is even vaguely expected)
			lsie = HighLogic.LoadedSceneIsEditor;
			lsif = HighLogic.LoadedSceneIsFlight;
			scene = HighLogic.LoadedScene;

			// Trick the modules into thinking they are running in the editor
			HighLogic.LoadedSceneIsEditor = true;
			HighLogic.LoadedSceneIsFlight = false;
			HighLogic.LoadedScene = GameScenes.EDITOR;
		}

		void RestoreScene ()
		{
			HighLogic.LoadedSceneIsEditor = lsie;
			HighLogic.LoadedSceneIsFlight = lsif;
			HighLogic.LoadedScene = scene;
		}

		void RunUpdateDelegates (string updateType, List<UpdateDelegate> updaters)
		{
			SetEditorScene ();

			for (int i = updaters.Count; i-- > 0; ) {
				try {
					updaters[i] ();
				} catch (Exception e) {
					Debug.LogError ($"[ELEditor] caught {updateType} exception: {e.Message}");
					Debug.LogError ($"{e.StackTrace}");
					Debug.LogError ($"        on {updaters[i].Target}");
					Debug.LogError ($"        Disabling {updaters[i]}");
					updaters.RemoveAt (i);
				}
			}

			RestoreScene ();
		}

		void Update ()
		{
			if (ready) {
				RunUpdateDelegates ("Update", Updaters);
			}
		}

		void FixedUpdate ()
		{
			if (ready) {
				RunUpdateDelegates ("FixedUpdate", FixedUpdaters);
			}
		}

		void LateUpdate ()
		{
			if (ready) {
				RunUpdateDelegates ("LateUpdate", LateUpdaters);
			}
		}

		void SetActive (bool active)
		{
			gameObject.SetActive (active);
		}

		/** Enable (create if necessary) the editor
		 *
		 * Disables all modules (including Part) and removes all colliders
		 * from the parts in ship. This means that Unity will not call Start
		 * or any of the update routines on the parts or their part modules.
		 */
		public static void EditShip (ShipConstruct ship)
		{
			// Disable the ship's parts and part modules so they won't run
			// automatically
			for (int i = ship.parts.Count; i-- > 0; ) {
				Part p = ship.parts[i];
				p.enabled = false;
				EL_Utils.DisableModules (p.gameObject);
				EL_Utils.DisableColliders (p.gameObject);
			}
			if (!editor) {
				var go = new GameObject ("EL Editor");
				editor = go.AddComponent<ELEditor> ();
			}
			editor.SetActive (true);
			editor.SetupEditor (ship);
		}

		/** Disable the editor and destroy the ship and its parts
		 */
		public static void ClearShip ()
		{
			if (!editor) {
				return;
			}
			if (editor.ship != null) {
				for (int i = editor.ship.parts.Count; i-- > 0; ) {
					Destroy (editor.ship.parts[i].gameObject);
				}
			}
			editor.ship = null;
			editor.SetActive (false);
		}
	}
}
