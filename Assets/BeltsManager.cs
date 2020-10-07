using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class BeltsManager : MonoBehaviour
{
    public Text TimeUpdateText;
    public Text TimeRenderText;

    [HideInInspector] public List<BeltSystem> Belts = new List<BeltSystem>();
    [HideInInspector] public List<Hand> Hands = new List<Hand>();

    [HideInInspector]
    public QuadTree Objects = new QuadTree(new Bounds(new Vector3(125000, -125000), Vector2.one * 250000));

    public Texture BeltTexture;
    public Texture ItemTexture;
    public Texture HandTexture;
    public Texture Hand2Texture;

    private Camera mainCamera;
    private Stopwatch stopwatch = new Stopwatch();

    private float beltsAnimationOffset = 4;

    private Barrier updatingBarrier = new Barrier(4);
    private SemaphoreSlim startUpdating = new SemaphoreSlim(0);
    private Thread[] updatingThreads;

    private void Start()
    {
        mainCamera = Camera.main;

        updatingThreads = new Thread[3];
        for (int i = 0; i < updatingThreads.Length; i++)
        {
            updatingThreads[i] = new Thread(parameter => UpdatePackThreadStarted((int) parameter, 4));
			updatingThreads[i].IsBackground = true;
            updatingThreads[i].Start(i + 1);
        }
    }

    private float multithreadedDeltaTime;

    private void Update()
    {
        stopwatch.Restart();

        multithreadedDeltaTime = Time.deltaTime;
        startUpdating.Release(3);
        UpdatePack(0, 4);

        updatingBarrier.SignalAndWait();

        if (Input.GetMouseButtonDown(1))
        {
            SpawnItem(mainCamera.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, 10)));
        }

        beltsAnimationOffset = ((int) (Time.time * 32) % 16 * 40f + 4f) / 640f;

        stopwatch.Stop();
        TimeUpdateText.text = stopwatch.ElapsedMilliseconds + " ms / frame (Update)";
    }

    private void UpdatePackThreadStarted(int packIndex, int totalPacks)
    {
        while (true)
        {
            startUpdating.Wait();
            UpdatePack(packIndex, totalPacks);
            updatingBarrier.SignalAndWait();
        }
    }

    private void UpdatePack(int packIndex, int totalPacks)
    {
        float from = (float) packIndex / totalPacks;
        float to = (float) (packIndex + 1) / totalPacks;
        
        for (int i = (int) (Belts.Count * from); i < (int) (Belts.Count * to); i++)
        {
            BeltSystem beltSystem = Belts[i];
            for (int j = 0; j < beltSystem.Items.Count - beltSystem.StuckItems; j++)
            {
                var items = beltSystem.Items;

                float progress = items[j].Progress + multithreadedDeltaTime;
                if (progress > beltSystem.WayPoints.Length - 1.176f - beltSystem.StuckItems * .33f)
                {
                    progress = beltSystem.WayPoints.Length - 1.16f - beltSystem.StuckItems * .33f;
                    beltSystem.StuckItems++;
                }

                items[j] = new ItemOnBelt(progress);
            }
        }
        
        updatingBarrier.SignalAndWait();

        for (int i = (int) (Hands.Count * from); i < (int) (Hands.Count * to); i++)
        {
            Hand hand = Hands[i];
            if (hand.ItemOnBelt.HasValue)
            {
                var item = hand.ItemOnBelt.Value;
                float progress = item.Progress + multithreadedDeltaTime;
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

        RenderQuadTreePart(Objects, cameraBounds);

        GL.PopMatrix();
        stopwatch.Stop();
        TimeRenderText.text = stopwatch.ElapsedMilliseconds + " ms / frame (Render)";
    }

    private void RenderQuadTreePart(QuadTree tree, Bounds cameraBounds)
    {
        if (!cameraBounds.Intersects(tree.bounds)) return;

        if (tree.isDivided)
        {
            RenderQuadTreePart(tree.northEast, cameraBounds);
            RenderQuadTreePart(tree.northWest, cameraBounds);
            RenderQuadTreePart(tree.southEast, cameraBounds);
            RenderQuadTreePart(tree.southWest, cameraBounds);
        }
        else
        {
            foreach (ObjectWithBounds obj in tree.objects)
            {
                if (!cameraBounds.Intersects(obj.Bounds)) continue;

                var beltSystem = obj as BeltSystem;
                if (beltSystem != null)
                {
                    foreach (Vector2 pos in beltSystem.SpritePositions)
                    {
                        int upside = beltSystem.Down ? 1 : -1;
                        Graphics.DrawTexture(
                            new Rect((pos.x - .5f) * 100, (pos.y - .5f * upside) * 100, 100, 100 * upside),
                            BeltTexture,
                            new Rect(beltsAnimationOffset, 404f / 480f, 32f / 640f, 32f / 480f), 0, 0, 0, 0);
                    }

                    foreach (ItemOnBelt item in beltSystem.Items)
                    {
                        int itemProgr = (int) item.Progress;
                        Vector2 a = beltSystem.WayPoints[itemProgr];
                        Vector2 b = beltSystem.WayPoints[itemProgr + 1];
                        float p = item.Progress - itemProgr;

                        Vector3 pos = Vector2.LerpUnclamped(a, b, p);

                        Graphics.DrawTexture(new Rect((pos.x - .25f) * 100, (pos.y + .25f) * 100, 50, -50),
                            ItemTexture);
                    }
                }
                else
                {
                    var hand = (Hand) obj;

                    Vector2 pos = hand.Bounds.center;
                    Graphics.DrawTexture(new Rect(pos.x * 100 - 90, pos.y * 100 + 46, 181, -92), HandTexture);

                    if (hand.ItemOnBelt.HasValue)
                    {
                        float progress = hand.ItemOnBelt.Value.Progress;

                        GL.PushMatrix();
                        GL.modelview = mainCamera.worldToCameraMatrix *
                                       Matrix4x4.Translate(pos) *
                                       Matrix4x4.Rotate(Quaternion.Euler(0, 0, progress * 180)) *
                                       Matrix4x4.Translate(-pos) *
                                       Matrix4x4.Scale(Vector3.one * .01f);
                        Graphics.DrawTexture(new Rect(pos.x * 100 - 90, pos.y * 100 + 46, 181, -92), Hand2Texture);
                        GL.PopMatrix();

                        pos += (Vector2) (Quaternion.Euler(0, 0, progress * 180) * (Vector2.left * .8f));
                        Graphics.DrawTexture(new Rect(pos.x * 100 - 25, pos.y * 100 + 25, 50, -50), ItemTexture);
                    }
                    else
                    {
                        Graphics.DrawTexture(new Rect(pos.x * 100 - 90, pos.y * 100 + 46, 181, -92), Hand2Texture);
                    }
                }
            }
        }
    }

    public void SpawnItem(Vector2 worldPosition)
    {
        foreach (BeltSystem beltSystem in Belts)
        {
            if (!beltSystem.Bounds.Contains(worldPosition)) continue;

            for (int j = 0; j < beltSystem.WayPoints.Length; j++)
            {
                if ((worldPosition - beltSystem.WayPoints[j]).sqrMagnitude < .5)
                {
                    beltSystem.PushItem(new ItemOnBelt(j));
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