using UnityEngine;

public class BuildingAccessPoints : MonoBehaviour
{
    [SerializeField] private Transform entrance;
    [SerializeField] private Transform fishingPoint;
    public Transform Entrance => entrance;

    public Transform FishingPoint => fishingPoint;
}
