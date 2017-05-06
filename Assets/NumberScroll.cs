using UnityEngine;
using System.Collections;

public class NumberScroll
{
	public float NumberValue = 0;
	public Rect DrawRect = new Rect();
	public float Scale = 1f;
	public float NumberMax = 10f;
	float SnapIncrement = .5f;
	public bool bEditing = false;
	Vector3 LastMousePos = new Vector3();
	public bool bAnimating = false;
	float AnimateScale = 1f;
	Rect AdjustedDrawRect = new Rect();
	public bool IsPicking { get { return bEditing; } }

	public void Update()
	{
		if (bAnimating)
		{
			AnimateScale = 1f - Mathf.Abs(Mathf.Cos(Time.time * 5f) * .1f);
		}
		else
			AnimateScale = 1f;

		AdjustedDrawRect = new Rect(DrawRect.center.x - DrawRect.width / 2f * AnimateScale, DrawRect.center.y - DrawRect.height / 2f * AnimateScale,
			DrawRect.width * AnimateScale, DrawRect.height * AnimateScale);

		Vector3 MousePos = Input.mousePosition;
		MousePos.y = Screen.height - MousePos.y;
		if (Input.GetMouseButtonDown(0) && AdjustedDrawRect.Contains(MousePos))
		{
			bEditing = true;
			LastMousePos = MousePos;
		}
		else if (bEditing && Input.GetMouseButton(0))
		{
			float DeltaY = MousePos.y - LastMousePos.y;
			NumberValue -= DeltaY / (Screen.height / 15f);

			LastMousePos = MousePos;
		}
		else
		{
			bEditing = false;

			// Snap
			NumberValue = Mathf.Round(NumberValue / SnapIncrement) * SnapIncrement;
		}

		NumberValue = Mathf.Clamp(NumberValue, 0f, NumberMax);
	}

	public float GetRoundedNumber()
	{
		return Mathf.Round(NumberValue / SnapIncrement) * SnapIncrement;
	}

	public void Draw()
	{
		float StartY = 1f - NumberValue * 1f / 11f - 1f / 11f;
		float StartScaleX = ((1f / Scale) - 1f) * .5f;
		float StartScaleY = (1f / 11f) * (1f / Scale) * .5f;

		GUI.DrawTextureWithTexCoords(AdjustedDrawRect, Global.NumberTex, new Rect(-StartScaleX, StartY - StartScaleY, 1f / Scale, 1f / 11f + (1f / 11f) * (1f / Scale)));
		GUI.DrawTexture(AdjustedDrawRect, bAnimating ? Global.NumberBorderGreenTex : Global.NumberBorderTex);
	}
}
