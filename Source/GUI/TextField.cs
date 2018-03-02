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

namespace ExtraplanetaryLaunchpads {

	public class TextField
	{
		string controlName;

		string newText;
		string _text;
		public string text
		{
			get { return _text; }
			set { _text = value; newText = value; }
		}

		bool resetOnFocusLoss;

		public TextField (string controlName, bool reset = false)
		{
			this.controlName = controlName;
			resetOnFocusLoss = reset;
		}

		public void AcceptInput ()
		{
			text = newText;
		}

		public bool HandleInput ()
		{
			bool done = false;

			GUI.SetNextControlName (controlName);
			if (GUI.GetNameOfFocusedControl () == controlName) {
				if (Event.current.isKey) {
					switch (Event.current.keyCode) {
						case KeyCode.Return:
						case KeyCode.KeypadEnter:
							Event.current.Use ();
							done = true;
							AcceptInput ();
							break;
						case KeyCode.Escape:
							newText = text;
							done = true;
							break;
					}
				}
			} else {
				if (resetOnFocusLoss) {
					newText = text;
				}
			}
			newText = GUILayout.TextField (newText);
			return done;
		}
	}

}
