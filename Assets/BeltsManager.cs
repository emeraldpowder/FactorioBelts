using System.Collections.Generic;
using UnityEngine;

public class BeltsManager : MonoBehaviour
{
    public GameObject ItemPrefab;

    [HideInInspector] public List<BeltSystem> Belts = new List<BeltSystem>();

    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        Bounds cameraBounds = new Bounds(mainCamera.transform.position, new Vector3(20, 10));

        for (int i = 0; i < Belts.Count; i++)
        {
            for (int j = 0; j < Belts[i].Items.Count; j++)
            {
                var items = Belts[i].Items;

                float progress = items[j].Progress + Time.deltaTime;
                if (progress > Belts[i].Points.Length - 1) progress = Belts[i].Points.Length - 1.01f;

                items[j] = new ItemOnBelt(items[j].Item, progress);
            }

            for (int j = 0; j < Belts[i].Items.Count; j++)
            {
                float itemProgr = Belts[i].Items[j].Progress;
                Vector2 a = Belts[i].Points[(int) itemProgr];
                Vector2 b = Belts[i].Points[(int) itemProgr + 1];
                float p = itemProgr - (int) itemProgr;
                Belts[i].Items[j].Item.transform.localPosition = Vector2.Lerp(a, b, p);
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            SpawnItem(mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)));
        }
    }

    public void SpawnItem(Vector2 worldPosition)
    {
        for (int i = 0; i < Belts.Count; i++)
        {
            if (!Belts[i].Bounds.Contains(worldPosition)) continue;

            Debug.Log("asd");
            for (int j = 0; j < Belts[i].Points.Length; j++)
            {
                if ((worldPosition - Belts[i].Points[j]).sqrMagnitude < .25)
                {
                    ItemOnBelt itemOnBelt =
                        new ItemOnBelt(Instantiate(ItemPrefab, Belts[i].Points[j], Quaternion.identity), j);
                    Belts[i].Items.Add(itemOnBelt);
                    return;
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        foreach (BeltSystem beltSystem in Belts)
        {
            Gizmos.color = new Color(1, 0, Random.value, 0.2f);
            Gizmos.DrawCube(transform.position + beltSystem.Bounds.center, beltSystem.Bounds.size);
        }
    }
}

public struct BeltSystem
{
    public Vector2[] Points;
    public List<ItemOnBelt> Items;
    public Bounds Bounds;

    public BeltSystem(Vector2[] points) : this()
    {
        Points = points;
        Items = new List<ItemOnBelt>();
        Bounds = new Bounds(points[0], Vector3.zero);
        foreach (var point in points)
        {
            Bounds.Encapsulate(point);
        }

        Bounds.Expand(0.5f);
    }
}

public struct ItemOnBelt
{
    public GameObject Item;
    public float Progress;

    public ItemOnBelt(GameObject item, float progress)
    {
        Item = item;
        Progress = progress;
    }
}