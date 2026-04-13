using UnityEngine;

public class SsvepLayout : MonoBehaviour
{
    public RectTransform leftTop;
    public RectTransform rightTop;
    public RectTransform leftBottom;
    public RectTransform rightBottom;

    public float marginX = 250f;
    public float marginY = 150f;

    void Start()
    {
        Place(leftTop,     new Vector2(-marginX,  marginY));
        Place(rightTop,    new Vector2( marginX,  marginY));
        Place(leftBottom,  new Vector2(-marginX, -marginY));
        Place(rightBottom, new Vector2( marginX, -marginY));
    }

    void Place(RectTransform rt, Vector2 pos)
    {
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
    }
}