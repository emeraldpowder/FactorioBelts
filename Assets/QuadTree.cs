using System.Collections.Generic;
using UnityEngine;

public class QuadTree
{
    public List<ObjectWithBounds> objects;

    public Bounds bounds;
    private const int capacity = 512;
    public bool isDivided;

    public QuadTree northEast;
    public QuadTree northWest;
    public QuadTree southEast;
    public QuadTree southWest;

    public QuadTree(Bounds initialBounds)
    {
        bounds = initialBounds;
        objects = new List<ObjectWithBounds>(capacity);
    }

    public bool Insert(ObjectWithBounds obj)
    {
        if (!bounds.Intersects(obj.Bounds))
        {
            return false;
        }

        if (!isDivided && objects.Count < capacity)
        {
            objects.Add(obj);
            return true;
        }

        // Need to divide, if not already
        if (!isDivided)
        {
            Subdivide();
        }

        northEast.Insert(obj);
        northWest.Insert(obj);
        southEast.Insert(obj);
        southWest.Insert(obj);

        return true;
    }

    public bool Remove(ObjectWithBounds obj)
    {
        if (!bounds.Intersects(obj.Bounds))
        {
            return false;
        }
        
        if (objects.Remove(obj)) return true;
        
        if (isDivided)
        {
            if (northEast.Remove(obj)) return true;
            if (northWest.Remove(obj)) return true;
            if (southEast.Remove(obj)) return true;
            if (southWest.Remove(obj)) return true;
        }

        return false;
    }

    private void Subdivide()
    {
        var x = bounds.center.x;
        var y = bounds.center.y;
        var newSize = bounds.extents;

        var ne = new Bounds(new Vector3(x + newSize.x / 2, y + newSize.y / 2), newSize);
        northEast = new QuadTree(ne);
        var nw = new Bounds(new Vector3(x - newSize.x / 2, y + newSize.y / 2), newSize);
        northWest = new QuadTree(nw);
        var se = new Bounds(new Vector3(x + newSize.x / 2, y - newSize.y / 2), newSize);
        southEast = new QuadTree(se);
        var sw = new Bounds(new Vector3(x - newSize.x / 2, y - newSize.y / 2), newSize);
        southWest = new QuadTree(sw);

        foreach (var obj in objects)
        {
            northEast.Insert(obj);
            northWest.Insert(obj);
            southEast.Insert(obj);
            southWest.Insert(obj);
        }

        objects = null;

        isDivided = true;
    }
}

public class ObjectWithBounds
{
    public Bounds Bounds;
    public bool isBeltSystem; 
        
    
    //BeltSystem:
    public Vector2[] WayPoints;
    public Vector2[] SpritePositions;
    public List<ItemOnBelt> Items;

    public int StuckItems;

    public bool Down;
    

    public ObjectWithBounds(Vector2[] wayPoints, Vector2[] spritePositions)
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
        isBeltSystem = true;
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
    
    //Hand:
    public ObjectWithBounds From;
    public float FromProgress;

    public ObjectWithBounds To;
    public float ToProgress;

    public ItemOnBelt? ItemOnBelt;

    public ObjectWithBounds(Vector2 worldPosition)
    {
        Bounds = new Bounds(worldPosition, new Vector3(2, 1));
        isBeltSystem = false;
    }
}