using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The QTE minigame. Raises C# events on win/loss — knows nothing about
// PlayerController or any game-flow system.
public class FishingMinigame : MonoBehaviour
{
    [SerializeField] Sprite     greenBarSprite;
    [SerializeField] GameObject minigameUI;
    [SerializeField] GameObject movingBar;
    [SerializeField] GameObject fishGameObj;

    public event Action onWin;
    public event Action onLoss;

    private RectTransform        fishImage;
    private float                speed;
    private readonly List<GameObject> greenBars = new List<GameObject>();
    private bool                 isActive;
    private float                xMin, xMax;
    private int                  dir = 1;
    private int                  maxWidth;

    void Awake()
    {
        fishImage = fishGameObj.GetComponent<RectTransform>();
        minigameUI.SetActive(false);
    }

    public void StartMiniGame(FishDataSO fish)
    {
        minigameUI.SetActive(true);
        int difficulty = fish.difficulty;

        Vector3[] corners = new Vector3[4];
        fishImage.GetLocalCorners(corners);
        xMin = corners[0].x;
        xMax = corners[2].x;

        int barCount = difficulty == 1 ? 2 : 3;
        maxWidth     = 18 / difficulty;
        speed        = difficulty == 1 ? 70f : difficulty == 2 ? 90f : 120f;

        float slotWidth = (xMax - xMin) / barCount;
        for (int i = 0; i < barCount; i++)
        {
            float slotStart = xMin + i * slotWidth;
            float xPos      = UnityEngine.Random.Range(slotStart + maxWidth / 2f, slotStart + slotWidth - maxWidth / 2f);

            var bar     = new GameObject($"GreenBar{i}");
            var img     = bar.AddComponent<Image>();
            var barRect = img.GetComponent<RectTransform>();
            bar.transform.SetParent(fishGameObj.transform, false);
            barRect.sizeDelta     = new Vector2(maxWidth, 100);
            barRect.localPosition = new Vector2(xPos, 0);
            img.sprite            = greenBarSprite;
            greenBars.Add(bar);
        }

        dir = 1;
        StartCoroutine(ActivateAfterDelay());
    }

    IEnumerator ActivateAfterDelay()
    {
        yield return new WaitForSeconds(0.15f);
        isActive = true;
    }

    void Update()
    {
        if (!isActive) return;

        var movingRect = movingBar.GetComponent<RectTransform>();
        movingRect.localPosition = new Vector2(
            movingRect.localPosition.x + dir * speed * Time.deltaTime,
            movingRect.localPosition.y);

        if (movingRect.localPosition.x >= xMax || movingRect.localPosition.x <= xMin)
            dir *= -1;

        if (!Input.GetButtonDown("Jump")) return;

        bool hit = false;
        foreach (var bar in greenBars)
        {
            if (IsInside(movingRect, bar.GetComponent<RectTransform>()))
            {
                hit = true;
                break;
            }
        }

        isActive = false;
        DestroyGreenBars();
        minigameUI.SetActive(false);

        if (hit) onWin?.Invoke();
        else     onLoss?.Invoke();
    }

    void DestroyGreenBars()
    {
        foreach (var bar in greenBars) Destroy(bar);
        greenBars.Clear();
    }

    bool IsInside(RectTransform moving, RectTransform target)
    {
        Vector3 localPoint = target.InverseTransformPoint(moving.position);
        return target.rect.Contains(localPoint);
    }
}
