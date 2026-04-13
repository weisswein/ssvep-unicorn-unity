using UnityEngine;
using UnityEngine.UI;

public class SsvepStimulus : MonoBehaviour
{
    public Image img;
    public float dimAlpha = 0.2f;
    public float brightAlpha = 1.0f;

    private float frequency = 10f;
    private bool running = false;

    private void Update()
    {
        if (img == null) return;

        if (!running)
        {
            SetAlpha(dimAlpha);
            return;
        }

        float s = Mathf.Sin(2f * Mathf.PI * frequency * Time.time);
        SetAlpha(s > 0 ? brightAlpha : dimAlpha);
    }

    public void StartStimulus(float freq)
    {
        frequency = freq;
        running = true;
    }

    public void StopStimulus()
    {
        running = false;
        SetAlpha(dimAlpha);
    }

    private void SetAlpha(float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }
}