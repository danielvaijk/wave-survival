using UnityEngine;
using UnityEngine.UI;

// Programmed by Vaijk.
// Thank you for supporting me! Visit my website at http://www.vaijk.net.

// Used for slightly 'animating' and fading in and out a UI text object.

public class TextFade : MonoBehaviour
{
    public float fadeTime = 1f;

    [HideInInspector]
    public string text;

    private bool isFaded = true;

    private float t = 0f;

    private Vector3 randomLeft = Vector3.zero;

    private Color inicialColor = new Color();
    private Color visibleColor = new Color();

    private Text fadeText = null;

    // Called on the frame this script is enabled, before Update().
    private void Start ()
    {
        fadeText = GetComponent<Text>();
        fadeText.text = text;

        // Set the text alpha to 0f, if it's not already.
        if (fadeText.color.a != 0f)
        {
            fadeText.color = new Color(fadeText.color.r, fadeText.color.g, fadeText.color.b, 0f);
        }

        inicialColor = fadeText.color;

        visibleColor = inicialColor;
        visibleColor.a = 1f;

        // Get the random value for which horizontal direction this text is going into.
        randomLeft = new Vector3(Random.Range(-1, 2), 0f, 0f);
    }

    // Called every frame after Start().
    private void Update()
    {
        // Move this text on the screen.
        transform.localPosition += Vector3.up * 40 * Time.deltaTime;
        transform.localPosition += randomLeft * 20 * Time.deltaTime;

        // Add to the fade value.
        t += Time.deltaTime / (fadeTime / 2f);

        if (isFaded)
        {
            // Fade the text in.

            fadeText.color = Color.Lerp(inicialColor, visibleColor, t);

            if (fadeText.color == visibleColor)
            {
                t = 0f;
                isFaded = false;
                return;
            }
        }
        else
        {
            // Fade the text out.

            fadeText.color = Color.Lerp(visibleColor, inicialColor, t);

            if (fadeText.color == inicialColor)
            {
                Destroy(gameObject);
            }
        }
    }
}