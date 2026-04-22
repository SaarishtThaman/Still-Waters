using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Pure catch-selection logic. No cross-system calls — it receives everything
// it needs as parameters or via injected references.
public class FishingLogic : MonoBehaviour
{
    [SerializeField] SaveManager  saveManager;
    [SerializeField] StoryManager storyManager;

    [SerializeField] FishDataSO[] fish;
    [SerializeField] FishDataSO[] junk;
    [SerializeField] FishDataSO[] bodyParts;
    [SerializeField] FishDataSO   severedHand;

    private const float FarX = 9.61f;

    public FishDataSO PickFish(float playerX)
    {
        if (storyManager.SeveredHandEventPlay)
        {
            storyManager.SeveredHandEventPlay = false;
            return severedHand;
        }

        var pool = BuildPool(playerX);
        return pool.Count > 0
            ? pool[UnityEngine.Random.Range(0, pool.Count)]
            : fish[0];
    }

    List<FishDataSO> BuildPool(float playerX)
    {
        var save = saveManager.data;
        var pool = new List<FishDataSO>();

        // Regular fish are always in the pool (doubled for higher base weight).
        pool.AddRange(fish);
        pool.AddRange(fish);

        if (!save.severedHandEventPassed)
        {
            // Before the horror event: extra fish weight + junk, no body parts.
            pool.AddRange(fish);
            pool.AddRange(junk);
        }
        else
        {
            // After the horror event: body parts enter the pool.
            pool.AddRange(bodyParts);
            pool.AddRange(bodyParts);
            if (saveManager.IsFullyUpgraded)
            {
                pool.AddRange(bodyParts);
                pool.AddRange(bodyParts);
            }
        }

        return pool.Where(f => MeetsRequirements(f, playerX, save)).ToList();
    }

    bool MeetsRequirements(FishDataSO f, float playerX, SaveData save)
    {
        if (f.rodRequirement  > save.rodLevel)  return false;
        if (f.baitRequirement > save.baitLevel) return false;
        if (playerX < FarX && f.location == FishDataSO.FishLocation.Far) return false;
        if (IsBodyPartAlreadyFound(f, save)) return false;
        return true;
    }

    bool IsBodyPartAlreadyFound(FishDataSO f, SaveData save)
    {
        return f.bodyPartId switch
        {
            FishDataSO.BodyPartId.Arm           => save.armFound,
            FishDataSO.BodyPartId.Leg           => save.legFound,
            FishDataSO.BodyPartId.OrganNecklace => save.organNecklaceFound,
            FishDataSO.BodyPartId.MaleHead      => save.maleHeadFound,
            _                                   => false
        };
    }
}
