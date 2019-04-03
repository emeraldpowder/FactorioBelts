using System.Collections.Generic;
using UnityEngine;

public class BeltSystem
{
    public Vector2[] WayPoints;
    public Vector2[] SpritePositions;
    public List<ItemOnBelt> Items;
    public Bounds Bounds;

    public int StuckItems;

    public bool Down;

    public BeltSystem(Vector2[] wayPoints, Vector2[] spritePositions)
    {
        WayPoints = wayPoints;
        SpritePositions = spritePositions;
        Items = new List<ItemOnBelt>();
        Bounds = new Bounds(wayPoints[0], Vector3.zero);
        foreach (var point in wayPoints)
        {
            Bounds.Encapsulate(point);
        }

        Bounds.Expand(0.5f);
    }

    public bool PushItem(ItemOnBelt item)
    {
        int start = 0;
        int end = Items.Count;
        while (start != end)
        {
            int median = (start + end) / 2;
            if (Items[median].Progress > item.Progress)
            {
                end = median;
            }
            else
            {
                start = median + 1;
            }
        }

        float prevEdge = .17f;
        if (start > 0) prevEdge = Items[start - 1].Progress + .33f;

        float nextEdge;
        if (start < Items.Count) nextEdge = Items[start].Progress - .33f;
        else nextEdge = WayPoints.Length - .17f;

        if (nextEdge - prevEdge < 0) return false; // No space to fit item

        if (item.Progress < prevEdge) item.Progress = prevEdge;
        else if (item.Progress > nextEdge) item.Progress = nextEdge; 

        Items.Insert(start, item);
        
#if UNITY_EDITOR
        Check();
#endif
        
        return true;
    }

    private void Check()
    {
        // TODO: For debug only, remove in production 
        float prev = -100;
        foreach (ItemOnBelt item in Items)
        {
            if (item.Progress < prev) Debug.LogError("UNSORTED");
            prev = item.Progress;
        }
    }

    public ItemOnBelt? PopItem(float targetProgress)
    {
        int start = 0;
        int end = Items.Count;
        while (start != end)
        {
            int median = (start + end) / 2;
            if (Items[median].Progress > targetProgress)
            {
                end = median;
            }
            else
            {
                start = median + 1;
            }
        }

        // ... |    start - 1     |        start         | ...
        // ... | [prog < tarProg] | [prog >= targetProg] | ...

        if (start > 0 && Mathf.Abs(Items[start - 1].Progress - targetProgress) < 0.17f)
        {
            return PopItem(start - 1);
        }

        if (start < Items.Count && Mathf.Abs(Items[start].Progress - targetProgress) < 0.17f)
        {
            return PopItem(start);
        }

        return null;
    }

    private ItemOnBelt PopItem(int index)
    {
        int indexFromEnd = Items.Count - 1 - index;
        if (indexFromEnd < StuckItems) StuckItems = indexFromEnd;

        ItemOnBelt result = Items[index];
        Items.RemoveAt(index);
        return result;
    }
}

public class Hand
{
    public BeltSystem From;
    public float FromProgress;

    public BeltSystem To;
    public float ToProgress;

    public ItemOnBelt? ItemOnBelt;

    public Bounds Bounds;

    public Hand(Vector2 worldPosition)
    {
        Bounds = new Bounds(worldPosition, new Vector3(2, 1));
    }
}

public struct ItemOnBelt
{
    public float Progress;

    public ItemOnBelt(float progress)
    {
        Progress = progress;
    }
}