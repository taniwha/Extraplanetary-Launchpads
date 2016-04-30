using System.Collections.Generic;
using System.Collections;
using System;
using UnityEngine;

public class InternalFlagDecal : InternalModule
{
	[KSPField]
	public string textureQuadName = "";

	Renderer textureQuadRenderer;
	Texture2D flagTexture;

	private IEnumerator WaitForPart()
	{
		while (!part) {
			yield return null;
		}
		UpdateFlagTexture();
	}

	public override void OnAwake()
	{
		StartCoroutine(WaitForPart());
	}

	private void OnDestroy()
	{
	}

	public void UpdateFlagTexture()
	{
		if (part.flagURL != "") {
			flagTexture = GameDatabase.Instance.GetTexture(part.flagURL, false);

			if (flagTexture == null) {
				Debug.LogWarning("[FlagDecal Warning!]: Flag URL is given as " + part.flagURL + ", but no texture found in database with that name", gameObject);
			}
		}

		if (textureQuadName != "") {
			Transform quad = internalProp.FindModelTransform(textureQuadName);

			if (quad != null) {
				textureQuadRenderer = quad.GetComponent<Renderer>();

				if (textureQuadRenderer != null) {
					if (flagTexture != null) {
						textureQuadRenderer.material.mainTexture = flagTexture;
					}
				} else {
					Debug.LogWarning("[FlagDecal Warning!]: Flag quad object is given as " + textureQuadName + ", but has no renderer attached", quad.gameObject);
				}
			} else {
				Debug.LogWarning("[FlagDecal Warning!]: Flag quad object is given as " + textureQuadName + ", but no object found in model with that name", gameObject);
			}
		}
	}
}
