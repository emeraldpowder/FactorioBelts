using UnityEngine;
using UnityEngine.UI;

public class BeltsSpawner : MonoBehaviour
{
    public BeltsManager Manager;
    public GameObject BeltSprite;
    public Text CountText;

    public int RowsCount = 10;
    public int BeltLength = 10;

    private bool firstUpdate = true;

    private void Start()
    {
        for (int i = 0; i < RowsCount; i++)
        {
            SpawnBelt(Vector2.right * 2 * i);
        }

        CountText.text = RowsCount * BeltLength + " belts";
    }

    private void SpawnBelt(Vector2 offset)
    {
        for (int i = 0; i < BeltLength; i++)
        {
            Instantiate(BeltSprite, offset + new Vector2(0, -.5f - i), Quaternion.identity);
        }

        var points = new Vector2[BeltLength + 1];
        for (int i = 0; i < BeltLength + 1; i++)
        {
            points[i] = offset + Vector2.down * i;
        }

        Manager.Belts.Add(new BeltSystem(points));
    }

    private void Update()
    {
        if (firstUpdate)
        {
            for (int x = 0; x < RowsCount; x++)
            {
                for (int y = 0; y < 4; y++)
                {
                    Manager.SpawnItem(new Vector2(x * 2, y));
                }
            }
            
            CountText.text += "," + RowsCount * 4 + " items";

            firstUpdate = false;
        }

    }
}