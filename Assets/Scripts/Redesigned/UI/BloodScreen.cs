using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Self-contained horror effect. No singleton — CatchUIController holds a
// direct reference and calls PlayBloodScreen() when a body part is caught.
public class BloodScreen : MonoBehaviour
{
    private Image image;

    static readonly Color BloodColor = new Color(130f / 255f, 25f / 255f, 25f / 255f);

    void Start() => image = GetComponent<Image>();

    public void PlayBloodScreen() => StartCoroutine(Animate());

    IEnumerator Animate()
    {
        for (float a = 0f; a < 0.8f; a += 0.05f)
        {
            image.color = new Color(BloodColor.r, BloodColor.g, BloodColor.b, a);
            yield return new WaitForSeconds(0.05f);
        }
        for (float a = 0.8f; a > 0f; a -= 0.05f)
        {
            image.color = new Color(BloodColor.r, BloodColor.g, BloodColor.b, a);
            yield return new WaitForSeconds(0.1f);
        }
        image.color = new Color(BloodColor.r, BloodColor.g, BloodColor.b, 0f);
    }
}
