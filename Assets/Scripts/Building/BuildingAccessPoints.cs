using UnityEngine;

public class BuildingAccessPoints : MonoBehaviour
{
    [SerializeField] private Transform entrance;

    public Transform Entrance => entrance;
}
