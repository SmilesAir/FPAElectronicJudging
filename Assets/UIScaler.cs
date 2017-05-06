using UnityEngine;
using System.Collections;

public class UIScaler : MonoBehaviour
{
    Vector3 OriginalPosition;
    public float SourceX = 800f;
    public float SourceY = 450f;

	// Use this for initialization
	void Start ()
    {
        OriginalPosition = GetComponent<RectTransform>().anchoredPosition;
	}
	
	// Update is called once per frame
	void Update ()
    {
        float CurrAspect = ((float)Screen.width) / Screen.height;
        Quaternion rot = GetComponent<RectTransform>().localRotation;

        Vector2 Scale = new Vector2(Screen.width / SourceX, Screen.height / SourceY);

        Vector3 AdjustedLocalPos = new Vector3(OriginalPosition.x * Scale.x, OriginalPosition.y * Scale.y, 0f);

        GetComponent<RectTransform>().anchoredPosition = AdjustedLocalPos;

        if (rot == Quaternion.identity)
        {
            GetComponent<RectTransform>().localScale = new Vector3(Scale.x, Scale.y, 1f);
        }
        else
        {
            GetComponent<RectTransform>().localScale = new Vector3(Scale.y, Scale.x, 1f);
        }
	}
}
