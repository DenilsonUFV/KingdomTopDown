using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortingStatic : MonoBehaviour
{
    [SerializeField] private float yOffset = 0f;
    [SerializeField] private int sortingPrecision = 100;

    private void Start()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        sr.sortingLayerName = "Dynamic";
        sr.sortingOrder = Mathf.RoundToInt(-(transform.position.y + yOffset) * sortingPrecision);
    }
}