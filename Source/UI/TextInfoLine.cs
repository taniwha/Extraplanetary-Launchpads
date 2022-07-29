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
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELTextInfoLine : Layout
	{
		UIText label;
		UIImage spacer;
		UIText info;

		public override void CreateUI()
		{
			base.CreateUI ();

			var textMargins = new Vector4 (5, 5, 2, 2);

			this.Horizontal ()
				.ControlChildSize (true, true)
				.ChildForceExpand (false, false)
				.Anchor (AnchorPresets.TopLeft)
				.Pivot (PivotPresets.TopLeft)
				.SizeDelta (0, 0)
				.FlexibleLayout (true, false)

				.Add<UIText> (out label, "InfoLabel")
					.Margin (textMargins)
					.Alignment (TextAlignmentOptions.Left)
					.Anchor (AnchorPresets.TopLeft)
					.SizeDelta (0, 0)
					.FlexibleLayout (true, false)
					.Finish ()
				.Add<UIImage> (out spacer, "InfoSpacer")
					.Type (Image.Type.Sliced)
					.Anchor (AnchorPresets.TopLeft)
					.SizeDelta (0, 0)
					.Finish ()
				.Add<UIText> (out info, "InfoLabel")
					.Margin (textMargins)
					.Alignment (TextAlignmentOptions.Right)
					.Anchor (AnchorPresets.TopLeft)
					.SizeDelta (0, 0)
					.FlexibleLayout (true, false)
					.Finish ()
				;
		}

		public override void Style ()
		{
			base.Style ();
		}

		public ELTextInfoLine Label (string text)
		{
			label.Text (text + ":");
			return this;
		}

		public ELTextInfoLine Info (string text)
		{
			info.Text (text);
			return this;
		}
	}
}
