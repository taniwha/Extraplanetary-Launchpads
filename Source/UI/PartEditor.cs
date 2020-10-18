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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using KSP.IO;
using KSP.UI;
using KSP.UI.Screens;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELPartEditor : Window
	{
		ELPartPreview partPreview;
		VerticalLayout tweakables;
		UIInputField descriptionInput;

		Part part;

		public override void CreateUI ()
		{
			base.CreateUI ();

			this.Title (ELLocalization.PartEditor)
				.Vertical ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false,false)
				.PreferredSizeFitter (true, true)
				.Anchor (AnchorPresets.MiddleCenter)
				.Pivot (PivotPresets.MiddleCenter)
				.Add<HorizontalLayout> ()
					.Add<VerticalLayout> ()
						.Add<HorizontalLayout> ()
							.Add<VerticalLayout> ()
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
					.Finish ()
				.Add<UIText> ()
					.Text (ELVersionReport.GetVersion ())
					.Alignment (TextAlignmentOptions.Center)
					.Size (12)
					.FlexibleLayout (true, false)
					.Finish ()
				.Finish ();
		}

		static void SetControlLock (string str = null)
		{
			InputLockManager.SetControlLock ("ELPartEditor_lock");
		}

		static void ClearControlLock (string str = null)
		{
			InputLockManager.RemoveControlLock ("ELPartEditor_lock");
		}

		void Save ()
		{
		}

		void SaveAndClose ()
		{
			Save ();
			Close ();
		}

		void Close ()
		{
			SetActive (false);
		}

		void EditPart (ELCraftItem editPart)
		{
			SetActive (true);
			descriptionInput.text = "EL constructed part";
			if (part) {
				Destroy (part.gameObject);
			}
			part = null;
			partPreview.AvailablePart (null);
			if (editPart != null) {
				var node = editPart.node;
				if (node.HasValue ("description")) {
					string description = node.GetValue ("description");
					descriptionInput.text = description.Replace ('¨', '\n');
				}
				var partNode = node.GetNode ("PART");
				string partName = partNode.GetValue ("part").Split ('_')[0];

				var partInfo = PartLoader.getPartInfoByName (partName);
				if (partInfo != null) {
					partPreview.AvailablePart (partInfo);
				}
			}
		}

		static ELPartEditor partEditor;
		public static void OpenEditor (ELCraftItem editPart)
		{
			if (!partEditor) {
				partEditor = UIKit.CreateUI<ELPartEditor> (ELWindowManager.appCanvasRect, "ELPartEditor");
			}
			partEditor.EditPart (editPart);
		}
	}
}
