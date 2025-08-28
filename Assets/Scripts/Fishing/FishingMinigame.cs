using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FishingMinigame : MonoBehaviour
{

    public static FishingMinigame instance;

    public Sprite greenBarSprite;
    public GameObject minigameUI;
    public GameObject movingBar;
    public GameObject fishGameObj;
    private RectTransform fishImage;
    private float speed = 30f;
    List<GameObject> greenBars;
    int maxWidth = 0;

    private bool isMinigameActive = false;
    float xMin, xMax;
    int difficulty = 0;
    int dir = 1;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        fishImage = fishGameObj.GetComponent<RectTransform>();
    }

    // difficulty 1-3
    public void StartMiniGame()
    {
        FishData fishData = FishingManager.instance.PickFish();
        Debug.Log(fishData.name);
        difficulty = fishData.difficulty;
        minigameUI.SetActive(true);

        Vector3[] corners = new Vector3[4];
        fishImage.GetLocalCorners(corners);
        xMax = corners[2].x;
        xMin = corners[0].x;

        int bars = difficulty == 1 ? 2 : 3;
        maxWidth = 18 / difficulty;

        greenBars = new List<GameObject>();
        float totalWidth = xMax - xMin;
        float slotWidth = totalWidth / bars;

        for (int i = 0; i < bars; i++)
        {
            float slotStart = xMin + i * slotWidth;
            float slotEnd = slotStart + slotWidth;

            float xPosition = Random.Range(slotStart + maxWidth / 2, slotEnd - maxWidth / 2);

            GameObject bar = new GameObject("Bar" + i);
            Image barImg = bar.AddComponent<Image>();
            RectTransform barRect = barImg.GetComponent<RectTransform>();
            bar.transform.SetParent(fishGameObj.transform, false);
            barRect.sizeDelta = new Vector2(maxWidth, 100);
            barRect.localPosition = new Vector2(xPosition, 0);
            barImg.sprite = greenBarSprite;

            greenBars.Add(bar);
        }

        movingBar.transform.SetParent(null, false);
        movingBar.transform.SetParent(fishGameObj.transform, false);

        // Oscillate bar
        dir = 1;
        if (difficulty == 1)
        {
            speed = 70f;
        }
        else if (difficulty == 2)
        {
            speed = 90f;
        }
        else
        {
            speed = 120f;
        }
        StartCoroutine(startMinigameAfterDelay());
    }

    IEnumerator startMinigameAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        isMinigameActive = true;
    }

    void Update()
    {
        if (isMinigameActive)
        {

            Image movingBarImg = movingBar.GetComponent<Image>();
            RectTransform movingBarRect = movingBarImg.GetComponent<RectTransform>();

            if (Input.GetAxisRaw("Jump") != 0)
            {
                bool won = false;
                greenBars.ForEach(greenBar =>
                {
                    RectTransform greenBarRect = greenBar.GetComponent<RectTransform>();
                    float greenBarMin = greenBarRect.localPosition.x - maxWidth / 2;
                    float greenBarMax = greenBarRect.localPosition.x + maxWidth / 2;
                    if (IsInside(movingBarRect, greenBarRect))
                    {
                        won = true;
                    }
                });
                if (won)
                {
                    DestroyGreenBars();
                    PlayerManager.instance.StopAllCoroutines();
                    isMinigameActive = false;
                    minigameUI.SetActive(false);
                    PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.FISH_CAUGHT);
                }
                else
                {
                    DestroyGreenBars();
                    PlayerManager.instance.StopAllCoroutines();
                    isMinigameActive = false;
                    // Play some animation
                    minigameUI.SetActive(false);
                    PlayerManager.instance.LostFishThought();
                    PlayerManager.instance.SetState(PlayerManager.PLAYER_STATES.REELING_IN);
                }
            }

            movingBarRect.localPosition = new Vector2(movingBarRect.localPosition.x + dir * speed * Time.deltaTime, movingBarRect.localPosition.y);
            if (movingBarRect.localPosition.x >= xMax || movingBarRect.localPosition.x <= xMin)
            {
                dir *= -1;
            }
        }
    }

    void DestroyGreenBars()
    {
        greenBars.ForEach(x => Destroy(x));
    }

    bool IsInside(RectTransform moving, RectTransform target)
    {
        Vector3 localPoint = target.InverseTransformPoint(moving.position);
        return target.rect.Contains(localPoint);
    }
}
