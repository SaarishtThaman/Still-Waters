using System;
using UnityEngine;

public class FishData : MonoBehaviour
{
    public String title;
    public String description;
    public Sprite sprite;
    public int difficulty = 1;
    public enum LOCATION
    {
        NEAR, FAR
    }
    public enum TYPE
    {
        FISH, JUNK, BODYPART
    }
    public LOCATION location;
    public int sellingPrice;
    public int rodRequirement = 0;
    public int baitRequirement = 0;
    public TYPE type;
}
