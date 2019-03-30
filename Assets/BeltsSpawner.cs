using UnityEngine;

public class BeltsSpawner : MonoBehaviour
{
    public BeltsManager Manager;
    public GameObject BeltSprite;

    private bool firstUpdate = true;

    private void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            SpawnBelt(Vector2.right * 2 * i);
        }
    }

    private void SpawnBelt(Vector2 offset)
    {
        for (int i = 0; i < 10; i++)
        {
            Instantiate(BeltSprite, offset + new Vector2(0, -.5f - i), Quaternion.identity);
        }

        var points = new Vector2[11];
        for (int i = 0; i < 11; i++)
        {
            points[i] = offset + Vector2.down * i;
        }

        Manager.Belts.Add(new BeltSystem(points));
    }

    private void Update()
    {
        if (firstUpdate)
        {
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    Manager.SpawnItem(new Vector2(x * 2, y));
                }
            }

            firstUpdate = false;
        }
    }
}