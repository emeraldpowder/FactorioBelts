using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class BeltsManager : MonoBehaviour
{
    public GameObject ItemPrefab;
    public Text TimeText;

    [HideInInspector] public List<BeltSystem> Belts = new List<BeltSystem>();
    [HideInInspector] public List<Hand> Hands = new List<Hand>();

    private Camera mainCamera;
    private Stopwatch stopwatch = new Stopwatch();

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        stopwatch.Restart();
        Bounds cameraBounds = new Bounds((Vector2)mainCamera.transform.position, new Vector3(20, 10));

        foreach (BeltSystem beltSystem in Belts)
        {
            for (int j = 0; j < beltSystem.Items.Count; j++)
            {
                var items = beltSystem.Items;

                float progress = items[j].Progress + Time.deltaTime;
                if (progress > beltSystem.Points.Length - 1.125f) progress = beltSystem.Points.Length - 1.125f;
                
                items[j] = new ItemOnBelt(items[j].Item, progress);
            }

            if (cameraBounds.Intersects(beltSystem.Bounds))
            {
                for (int j = 0; j < beltSystem.Items.Count; j++)
                {
                    float itemProgr = beltSystem.Items[j].Progress;
                    Vector2 a = beltSystem.Points[(int) itemProgr];
                    Vector2 b = beltSystem.Points[(int) itemProgr + 1];
                    float p = itemProgr - (int) itemProgr;
                    beltSystem.Items[j].Item.transform.localPosition = Vector2.Lerp(a, b, p);
                }
            }
        }

        foreach (Hand hand in Hands)
        {
            if (hand.ItemOnBelt.HasValue)
            {
                var item = hand.ItemOnBelt.Value; 
                float progress = item.Progress + Time.deltaTime;
                if (progress >= 1)
                {
                    item.Progress = hand.ToProgress;
                    hand.To.Items.Add(item);
                    hand.ItemOnBelt = null;
                }
                else
                {
                    item.Progress = progress;
                    hand.ItemOnBelt = item;
                }
                
                hand.Sprite.transform.localRotation = Quaternion.Euler(0,0,progress*180);
                item.Item.transform.localPosition = hand.Sprite.transform.GetChild(0).position; // TODO: might be slow
            }
            else
            {
                var item = PopClose(hand.From, hand.FromProgress);
                if (item.HasValue)
                {
                    ItemOnBelt itemValue = item.Value;
                    itemValue.Progress = 0;
                    hand.ItemOnBelt = itemValue;
                }
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            SpawnItem(mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)));
        }
        
        stopwatch.Stop();
        TimeText.text = stopwatch.ElapsedMilliseconds + " ms / frame";
    }

    private ItemOnBelt? PopClose(BeltSystem beltSystem, float targetProgress)
    {
        // TODO: very slow

        for (int i = 0; i < beltSystem.Items.Count; i++)
        {
            if (Mathf.Abs(beltSystem.Items[i].Progress - targetProgress) < 0.125f)
            {
                ItemOnBelt result = beltSystem.Items[i];
                beltSystem.Items.RemoveAt(i);
                return result;
            }
        }

        return null;
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
        if (mainCamera == null) return;
        
        foreach (BeltSystem beltSystem in Belts)
        {
            Gizmos.color = new Color(1, 0, .1f, 0.2f);
            Gizmos.DrawCube(transform.position + beltSystem.Bounds.center, beltSystem.Bounds.size);
        }

        Bounds cameraBounds = new Bounds(mainCamera.transform.position, new Vector3(20, 10, 10));
        Gizmos.color = new Color(1, 0, .5f, 0.2f);
        Gizmos.DrawCube(transform.position + cameraBounds.center, cameraBounds.size);
    }
}

public class BeltSystem
{
    public Vector2[] Points;
    public List<ItemOnBelt> Items;
    public Bounds Bounds;

    public BeltSystem(Vector2[] points)
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

public class Hand
{
    public BeltSystem From;
    public float FromProgress;
    
    public BeltSystem To;
    public float ToProgress;

    public ItemOnBelt? ItemOnBelt;
    public GameObject Sprite;
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