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
using System.IO;
using System.Collections;
using UnityEngine;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELPartEditorView : Layout
	{
		ELPartPreview partPreview;
		VerticalLayout tweakables;
		UIInputField nameInput;
		UIInputField descriptionInput;

		Part part;

		SelectPartCallback OnSelectPart;
		CancelledCallback OnEditorClose;

		public override void CreateUI ()
		{
			base.CreateUI ();

			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)
				.Add<VerticalLayout> ()
					.Add<HorizontalLayout> ()
						.Add<VerticalLayout> ()
							.Add<UIButton> ()
								.Text (ELLocalization.SelectPart)
								.OnClick (SelectPart)
								.FlexibleLayout (true, false)
								.Finish ()
							.Add<UIEmpty> ()
								.FlexibleLayout (true, true)
								.Finish ()
							.Add<UIButton> ()
								.Text (ELLocalization.Save)
								.OnClick (Save)
								.FlexibleLayout (true, false)
								.Finish ()
							.Add<UIEmpty> ()
								.FlexibleLayout (true, true)
								.Finish ()
							.Add<UIButton> ()
								.Text (ELLocalization.SaveAndClose)
								.OnClick (SaveAndClose)
								.FlexibleLayout (true, false)
								.Finish ()
							.Add<UIEmpty> ()
								.FlexibleLayout (true, true)
								.Finish ()
							.Add<UIButton> ()
								.Text (ELLocalization.Close)
								.OnClick (Close)
								.FlexibleLayout (true, false)
								.Finish ()
							.Finish ()
						.Add<ELPartPreview> (out partPreview)
							.PreferredSize (256, 256)
							.Finish ()
						.Finish ()
					.Add<UIInputField> (out nameInput)
						.OnFocusGained (SetControlLock)
						.OnFocusLost (ClearControlLock)
						.FlexibleLayout (true, false)
						.Finish ()
					.Add<UIInputField> (out descriptionInput)
						.LineType (TMP_InputField.LineType.MultiLineNewline)
						.OnFocusGained (SetControlLock)
						.OnFocusLost (ClearControlLock)
						.FlexibleLayout (true, true)
						.PreferredSize (-1, 128)
						.Finish ()
					.Finish ()
				.Add<VerticalLayout> (out tweakables)
					.FlexibleLayout (true, true)
					.PreferredSize (256, -1)
					.Finish ()
				;
		}

		static void SetControlLock (string str = null)
		{
			InputLockManager.SetControlLock ("ELPartEditor_lock");
		}

		static void ClearControlLock (string str = null)
		{
			InputLockManager.RemoveControlLock ("ELPartEditor_lock");
		}

		void SelectPart ()
		{
			OnSelectPart (part.partInfo);
		}

		void Save ()
		{
			string name = nameInput.text;
			string description = descriptionInput.text;
			var ship = new ShipConstruct (name, description, part);
			Quaternion rotation = ship.parts[0].transform.rotation;
			ship.parts[0].transform.rotation = Quaternion.identity;
			ConfigNode node = ship.SaveShip ();
			ship.parts[0].transform.rotation = rotation;
			//Debug.Log ($"[ELPartEditorView] Save {node}");

			string basePath = EL_Utils.ApplicationRootPath;
			string profile = HighLogic.SaveFolder;
			string saveDir = "Parts/";
			string craft = "autopart.craft";
			if (!String.IsNullOrEmpty (name)) {
				craft = $"{name}.craft";
			}
			string dir = $"{basePath}saves/{profile}/{saveDir}";
			string fullPath = $"{dir}{craft}";
			//Debug.Log ($"[ELPartSelector] LoadPart {part.partInfo.title} {fullPath}");
			if (!Directory.Exists (dir)) {
				Directory.CreateDirectory (dir);
			}
			node.Save (fullPath);
		}

		void SaveAndClose ()
		{
			Save ();
			Close ();
		}

		void Close ()
		{
			Destroy (part);
			part = null;
			OnEditorClose ();
		}

		public ELPartEditorView SetDelegates (SelectPartCallback onSelectPart,
											  CancelledCallback onEditorClose)
		{
			OnSelectPart = onSelectPart;
			OnEditorClose = onEditorClose;
			return this;
		}

		void BuildTweakers ()
		{
			Debug.Log ($"[ELPartEditorView] BuildTweakers");
			for (int i = 0; i < part.Fields.Count; i++) {
				var f = part.Fields[i];
				if (!f.guiActiveEditor) {
					continue;
				}
				Debug.Log ($"        {f.guiName} {f.guiActiveEditor} ucf:{f.uiControlFlight} uce:{f.uiControlEditor}");
			}
			for (int i = 0; i < part.Modules.Count; i++) {
				var m = part.Modules[i];
				Debug.Log ($"    {m.ClassName}");
				for (int j = 0; j < m.Fields.Count; j++) {
					var f = m.Fields[j];
					if (!f.guiActiveEditor) {
						continue;
					}
					Debug.Log ($"        {f.guiName} {f.guiActiveEditor} ucf:{f.uiControlFlight} uce:{f.uiControlEditor}");
				}
			}
		}

		IEnumerator WaitAndBuildTweakers ()
		{
			yield return null;

			BuildTweakers ();
		}

		public void EditPart (AvailablePart availablePart)
		{
			SetActive (true);
			if (availablePart != null) {
				part = (Part)GameObject.Instantiate (availablePart.partPrefab);
				part.enabled = false;
				EL_Utils.DisableModules (part.gameObject);
				EL_Utils.RemoveColliders (part.gameObject);
				part.gameObject.SetActive (true);
				nameInput.text = availablePart.title;
				descriptionInput.text = "EL constructed part";
				partPreview.Part (part);
				StartCoroutine (WaitAndBuildTweakers ());
			} else {
				if (!part) {
					Close ();
				}
			}
		}

		public void EditPart (ELCraftItem editPart)
		{
			nameInput.text = "Unnamed part";
			descriptionInput.text = "EL constructed part";
			if (part) {
				Destroy (part.gameObject);
			}
			part = null;
			partPreview.AvailablePart (null);
			if (editPart != null) {
				var node = editPart.node;
				var partNode = node.GetNode ("PART");
				string partName = partNode.GetValue ("part").Split ('_')[0];

				var partInfo = PartLoader.getPartInfoByName (partName);
				EditPart (partInfo);

				nameInput.text = editPart.name;
				if (node.HasValue ("description")) {
					string description = node.GetValue ("description");
					descriptionInput.text = description.Replace ('¨', '\n');
				}
			}
		}
	}
}
