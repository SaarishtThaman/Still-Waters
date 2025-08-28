using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class FishSpawner : MonoBehaviour
{
    private float yMax = -3f, yMin = -5f, xMin = -8.6f, xMax = 14f;
    private int fishCount = 15;
    public GameObject fishPrefab;

    void Start()
    {
        for (int i = 0; i < fishCount; i++)
        {
            float x = Random.Range(xMin, xMax);
            float y = Random.Range(yMin, yMax);
            GameObject fish = Instantiate(fishPrefab, new Vector3(x, y, 0), Quaternion.identity);
            float size = Random.Range(0.3f, 0.6f);
            fish.transform.localScale = new Vector3(1, 1, 1) * size;
            SpriteRenderer sr = fish.GetComponent<SpriteRenderer>();
            float alpha = Random.Range(15f, 32f);
            sr.color = new Color(1, 1, 1, alpha / 255);
            sr.sortingOrder = 12;
            StartCoroutine(MoveAround(fish, x));
        }
    }

    IEnumerator MoveAround(GameObject fish, float xInitial)
    {
        float amplitude = Random.Range(2, 3.5f);
        float xMax = xInitial + amplitude;
        float xMin = xInitial - amplitude;
        int dir = -1;
        float speed = Random.Range(1f, 2f);
        Transform fishTransform = fish.transform;
        while (true)
        {
            fishTransform.position = new Vector3(fishTransform.position.x + dir * speed * Time.deltaTime, fishTransform.position.y, fishTransform.position.x);
            if (fishTransform.position.x <= xMin || fishTransform.position.x >= xMax)
            {
                fishTransform.localScale = new Vector3(fishTransform.localScale.x * -1, fishTransform.localScale.y, fishTransform.localScale.z);
                dir *= -1;
            }
            yield return null;
        }
    }

}
