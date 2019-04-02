using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class BeltsManager : MonoBehaviour
{
    public GameObject ItemPrefab;
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

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        stopwatch.Restart();

        foreach (BeltSystem beltSystem in Belts)
        {
            for (int j = 0; j < beltSystem.Items.Count; j++)
            {
                var items = beltSystem.Items;

                float progress = items[j].Progress + Time.deltaTime;
                if (progress > beltSystem.Points.Length - 1.25f) progress = beltSystem.Points.Length - 1.25f;

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
                    hand.To.Items.Add(item);
                    hand.ItemOnBelt = null;
                }
                else
                {
                    item.Progress = progress;
                    hand.ItemOnBelt = item;
                }
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

        if (Input.GetMouseButtonDown(1))
        {
            SpawnItem(mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)));
        }

        stopwatch.Stop();
        TimeUpdateText.text = stopwatch.ElapsedMilliseconds + " ms / frame (Update)";
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

            for (int j = 1; j < beltSystem.Points.Length - 1; j++)
            {
                Vector2 pos = beltSystem.Points[j];
                Graphics.DrawTexture(new Rect((pos.x - .5f) * 100, (pos.y - .5f) * 100, 100, 100), BeltTexture);
            }

            foreach (ItemOnBelt item in beltSystem.Items)
            {
                float itemProgr = item.Progress;
                Vector2 a = beltSystem.Points[(int) itemProgr];
                Vector2 b = beltSystem.Points[(int) itemProgr + 1];
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

            for (int j = 0; j < Belts[i].Points.Length; j++)
            {
                if ((worldPosition - Belts[i].Points[j]).sqrMagnitude < .5)
                {
                    ItemOnBelt itemOnBelt = new ItemOnBelt(j);
                    Debug.Log(Belts[i].Points[j]);

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