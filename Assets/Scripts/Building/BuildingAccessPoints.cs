using System.Collections.Generic;
using UnityEngine;

public class BuildingAccessPoints : MonoBehaviour
{
    [SerializeField] private Transform entrance;
    [SerializeField] private Transform fishingPoint;
    [SerializeField] private List<Transform> leisurePoints;
    public Transform Entrance => entrance;

    public Transform FishingPoint => fishingPoint;
    public IReadOnlyList<Transform> LeisurePoints => leisurePoints;
}
