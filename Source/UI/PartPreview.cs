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
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro;

using KodeUI;

namespace ExtraplanetaryLaunchpads {

	public class ELPartPreview : UIObject,
								 IBeginDragHandler,
								 IDragHandler,
								 IEndDragHandler
	{
		GameObject partIcon;

		public override void CreateUI ()
		{
			gameObject.AddComponent<Touchable> ();

			this.Pivot (PivotPresets.MiddleCenter);
		}

		public override void Style ()
		{
		}

		protected override void OnDestroy ()
		{
			base.OnDestroy ();
			Destroy (partIcon);
		}

		protected override void OnDisable ()
		{
			base.OnDisable ();
			Destroy (partIcon);
		}

		public ELPartPreview AvailablePart (AvailablePart availablePart)
		{
			Destroy (partIcon);

			var rect = rectTransform.rect;
			float size = Mathf.Min (rect.width, rect.height) / 2;
			partIcon = GameObject.Instantiate (availablePart.iconPrefab);
			partIcon.transform.SetParent (rectTransform, false);
			partIcon.transform.localPosition = new Vector3 (0, 0, -size);
			partIcon.transform.localScale = Vector3.one * size;
			var rot = Quaternion.Euler (-15, 0, 0);
			rot = rot * Quaternion.Euler (0, -30, 0);
			partIcon.transform.rotation = rot;
			partIcon.SetActive(true);
			int layer = LayerMask.NameToLayer ("UIAdditional");
			EL_Utils.SetLayer (partIcon, layer, true);
			return this;
		}

#region dragging
		const float r = 1;
		const float t = r * r / 2;
		Vector3 TrackballVector (Vector2 xyVec)
		{
			float d = xyVec.x * xyVec.x + xyVec.y * xyVec.y;
			Vector3 vec = xyVec;

			if (d < t) {
				vec.z = -Mathf.Sqrt (r * r - d);
			} else {
				vec.z = -t / Mathf.Sqrt (d);
			}
			return vec;
		}

		Quaternion DragRotation (PointerEventData eventData)
		{
			var rect = rectTransform.rect;
			float invSize = 2 / Mathf.Min (rect.width, rect.height);

			Camera cam = eventData.pressEventCamera;
			Vector2 delta = eventData.delta;
			Vector2 endPos = eventData.position;
			Vector2 startPos = endPos - delta;

			RectTransformUtility.ScreenPointToLocalPointInRectangle (rectTransform, endPos, cam, out endPos);
			RectTransformUtility.ScreenPointToLocalPointInRectangle (rectTransform, startPos, cam, out startPos);

			// The pivot is at the center of the rect, so the converted point's
			// origin is at the center so no need to offset.
			endPos = endPos * invSize;
			startPos = startPos * invSize;

			Vector3 end = TrackballVector (endPos);
			Vector3 start = TrackballVector (startPos);

			Vector3 axis = Vector3.Cross (start, end);

			float angle = delta.magnitude * invSize * 60;

			//Debug.Log ($"[ELPartPreview] DragRotation");
			//Debug.Log ($"    s:({startPos.x}, {startPos.y})");
			//Debug.Log ($"    e:({endPos.x}, {endPos.y})");
			//Debug.Log ($"    s:({start.x}, {start.y}, {start.z})");
			//Debug.Log ($"    e:({end.x}, {end.y}, {end.z})");
			//Debug.Log ($"    a:({axis.x}, {axis.y}, {axis.z})");
			//Debug.Log ($"    {angle}");
			return Quaternion.AngleAxis (angle, axis);
		}

		public void OnBeginDrag (PointerEventData eventData)
		{
		}

		public void OnDrag (PointerEventData eventData)
		{
			Quaternion q = DragRotation (eventData);
			Quaternion rot = partIcon.transform.rotation;
			partIcon.transform.rotation = q * rot;
			//Debug.Log ($"    q:({q.x}, {q.y}, {q.z}, {q.w})");
		}

		public void OnEndDrag (PointerEventData eventData)
		{
		}
#endregion
	}
}
