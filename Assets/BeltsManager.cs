using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class BeltsManager : MonoBehaviour
{
    public Text TimeUpdateText;
    public Text TimeRenderText;

    [HideInInspector] public List<BeltSystem> Belts = new List<BeltSystem>();
    [HideInInspector] public List<Hand> Hands = new List<Hand>();

    public Texture BeltTexture;
    public Texture ItemTexture;
    public Texture HandTexture;
    public Texture Hand2Texture;

    private Camera mainCamera;
    private Stopwatch stopwatch = new Stopwatch();

    private float beltsAnimationOffset = 4;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        stopwatch.Restart();

        foreach (BeltSystem beltSystem in Belts)
        {
            for (int j = 0; j < beltSystem.Items.Count - beltSystem.StuckItems; j++)
            {
                var items = beltSystem.Items;

                float progress = items[j].Progress + Time.deltaTime;
                if (progress > beltSystem.WayPoints.Length - 1.176f - beltSystem.StuckItems * .33f)
                {
                    progress = beltSystem.WayPoints.Length - 1.16f - beltSystem.StuckItems * .33f;
                    beltSystem.StuckItems++;
                }

                items[j] = new ItemOnBelt(progress);
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
                    if (hand.To.PushItem(item))
                    {
                        hand.ItemOnBelt = null;                        
                    }
                }
                else
                {
                    item.Progress = progress;
                    hand.ItemOnBelt = item;
                }
            }
            else
            {
                var item = hand.From.PopItem(hand.FromProgress);
                if (item.HasValue)
                {
                    ItemOnBelt itemValue = item.Value;
                    itemValue.Progress = 0;
                    hand.ItemOnBelt = itemValue;
                }
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            SpawnItem(mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)));
        }

        stopwatch.Stop();
        TimeUpdateText.text = stopwatch.ElapsedMilliseconds + " ms / frame (Update)";

        beltsAnimationOffset = ((int) (Time.time * 32) % 16 * 40f + 4f) / 640f;
    }

    private void OnRenderObject()
    {
        stopwatch.Restart();
        GL.PushMatrix();
        GL.LoadProjectionMatrix(Camera.current.projectionMatrix);
        GL.modelview = Camera.current.worldToCameraMatrix * Matrix4x4.Scale(Vector3.one * .01f);
        Graphics.DrawTexture(new Rect(0, 0, .6f, 1), BeltTexture);
        Bounds cameraBounds = new Bounds((Vector2) mainCamera.transform.position,
            new Vector3(4, 2) * mainCamera.orthographicSize);

        foreach (BeltSystem beltSystem in Belts)
        {
            if (!cameraBounds.Intersects(beltSystem.Bounds)) continue;

            foreach (Vector2 pos in beltSystem.SpritePositions)
            {
                int upside = beltSystem.Down ? 1 : -1;
                Graphics.DrawTexture(new Rect((pos.x - .5f) * 100, (pos.y - .5f * upside) * 100, 100, 100 * upside),
                    BeltTexture,
                    new Rect(beltsAnimationOffset, 404f / 480f, 32f / 640f, 32f / 480f), 0, 0, 0, 0);
            }

            foreach (ItemOnBelt item in beltSystem.Items)
            {
                float itemProgr = item.Progress;
                Vector2 a = beltSystem.WayPoints[(int) itemProgr];
                Vector2 b = beltSystem.WayPoints[(int) itemProgr + 1];
                float p = itemProgr - (int) itemProgr;

                Vector3 pos = Vector2.Lerp(a, b, p);

                Graphics.DrawTexture(new Rect((pos.x - .25f) * 100, (pos.y + .25f) * 100, 50, -50), ItemTexture);
            }
        }

        foreach (Hand hand in Hands)
        {
            if (!cameraBounds.Intersects(hand.Bounds)) continue;

            Vector2 pos = hand.Bounds.center;
            Graphics.DrawTexture(new Rect(pos.x * 100 - 90, pos.y * 100 + 46, 181, -92), HandTexture);

            float progress = 0;
            if (hand.ItemOnBelt.HasValue) progress = hand.ItemOnBelt.Value.Progress;

            GL.PushMatrix();
            GL.modelview = mainCamera.worldToCameraMatrix *
                           Matrix4x4.Translate(pos) *
                           Matrix4x4.Rotate(Quaternion.Euler(0, 0, progress * 180)) *
                           Matrix4x4.Translate(-pos) *
                           Matrix4x4.Scale(Vector3.one * .01f);
            Graphics.DrawTexture(new Rect(pos.x * 100 - 90, pos.y * 100 + 46, 181, -92), Hand2Texture);
            GL.PopMatrix();

            if (hand.ItemOnBelt.HasValue)
            {
                pos += (Vector2) (Quaternion.Euler(0, 0, progress * 180) * (Vector2.left * .8f));
                Graphics.DrawTexture(new Rect(pos.x * 100 - 25, pos.y * 100 + 25, 50, -50), ItemTexture);
            }
        }

        GL.PopMatrix();
        stopwatch.Stop();
        TimeRenderText.text = stopwatch.ElapsedMilliseconds + " ms / frame (Render)";
    }

    public void SpawnItem(Vector2 worldPosition)
    {
        for (int i = 0; i < Belts.Count; i++)
        {
            if (!Belts[i].Bounds.Contains(worldPosition)) continue;

            for (int j = 0; j < Belts[i].WayPoints.Length; j++)
            {
                if ((worldPosition - Belts[i].WayPoints[j]).sqrMagnitude < .5)
                {
                    Belts[i].PushItem(new ItemOnBelt(j));
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