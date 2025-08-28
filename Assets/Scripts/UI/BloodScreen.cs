using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BloodScreen : MonoBehaviour
{

    public static BloodScreen instance;
    Image image;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        image = GetComponent<Image>();
    }

    public void PlayBloodScreen()
    {
        StartCoroutine(BloodScreenAnim());
    }

    IEnumerator BloodScreenAnim()
    {
        //130,25,25
        float alpha = 0f;
        while (alpha < 0.8f)
        {
            alpha += 0.05f;
            image.color = new Color(130f / 255, 25f / 255, 25f / 255, alpha);
            yield return new WaitForSeconds(0.05f);
        }
        while (alpha > 0)
        {
            alpha -= 0.05f;
            image.color = new Color(130f / 255, 25f / 255, 25f / 255, alpha);
            yield return new WaitForSeconds(0.1f);
        }
    }
}
