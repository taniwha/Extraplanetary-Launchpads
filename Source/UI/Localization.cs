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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using KodeUI;

using KSP.IO;
using KSP.UI.Screens;

namespace ExtraplanetaryLaunchpads {

	public static class ELLocalization
	{
		public static string Productivity { get; } = "Productivity:";
		public static string SelectCraft { get; } = "Select Craft";
		public static string SelectedCraft { get; } = "Selected Craft";
		public static string Reload { get; } = "Reload";
		public static string Clear { get; } = "Clear";
		public static string Pad { get; } = "pad";
		public static string NotAvailable { get; } = "N/A";
	}
}
