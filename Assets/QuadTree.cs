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
}