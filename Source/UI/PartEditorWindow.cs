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

	public class ELPartEditorWindow : Window
	{
		ELPartEditorView partEditor;
		ELPartSelector partSelector;

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
				.Add<ELPartEditorView> (out partEditor)
					.SetDelegates (onSelectPart, onEditorClose)
					.Finish ()
				.Add<ELPartSelector> (out partSelector)
					.SetDelegates (onPartSelected, onPartSelectCancelled)
					.Finish ()
				.Add<UIText> ()
					.Text (ELVersionReport.GetVersion ())
					.Alignment (TextAlignmentOptions.Center)
					.Size (12)
					.FlexibleLayout (true, false)
					.Finish ()
				.Finish ();
		}

		void onSelectPart (AvailablePart availablePart)
		{
			partSelector.SetVisible (true);
			partEditor.SetActive (false);
			partSelector.SetSelectedPart (availablePart);
		}

		void onEditorClose ()
		{
			SetActive (false);
		}

		void EditPart (ELCraftItem editPart)
		{
			SetActive (true);
			if (editPart == null) {
				partSelector.SetVisible (true);
				partEditor.SetActive (false);
			} else {
				partSelector.SetVisible (false);
				partEditor.EditPart (editPart);
			}
		}

		void onPartSelected (AvailablePart availablePart)
		{
			partSelector.SetVisible (false);
			partEditor.EditPart (availablePart);
		}

		void onPartSelectCancelled ()
		{
			partSelector.SetVisible (false);
			partEditor.EditPart ((AvailablePart) null);
		}

		static ELPartEditorWindow partEditorWindow;
		public static void OpenEditor (ELCraftItem editPart)
		{
			if (!partEditorWindow) {
				partEditorWindow = UIKit.CreateUI<ELPartEditorWindow> (ELWindowManager.appCanvasRect, "ELPartEditorWindow");
			}
			partEditorWindow.EditPart (editPart);
		}
	}
}
