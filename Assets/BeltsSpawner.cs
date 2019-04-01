using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeltsSpawner : MonoBehaviour
{
    public BeltsManager Manager;
    [FormerlySerializedAs("BeltSprite")] public SpriteRenderer BeltSpritePrefab;
    public GameObject HandPrefab;
    public Text CountText;

    public int RowsCount = 10;
    public int BeltLength = 10;

    private bool firstUpdate = true;

    private void Start()
    {
        for (int i = 0; i < RowsCount; i++)
        {
            SpawnBelt(Vector2.right * 2 * i, i % 2 != 0);
        }

        for (int i = 0; i < RowsCount - 1; i++)
        {
            SpawnHand(i, i % 2 == 0);
        }

        CountText.text = RowsCount * BeltLength + " belts";
    }

    private void SpawnBelt(Vector2 offset, bool upside)
    {
        for (int i = 0; i < BeltLength; i++)
        {
            var belt = Instantiate(BeltSpritePrefab, offset + new Vector2(0, -.5f - i), Quaternion.identity);
            belt.flipY = upside;
        }

        var points = new Vector2[BeltLength + 1];
        for (int i = 0; i < BeltLength + 1; i++)
        {
            points[i] = offset + Vector2.down * (upside ? i : (BeltLength - i));
        }

        Manager.Belts.Add(new BeltSystem(points));
    }

    private void SpawnHand(int index, bool atUp)
    {
        var position = new Vector3(2 * index + 1, -(atUp ? 1 : BeltLength - 1));
        GameObject go = Instantiate(HandPrefab, position, Quaternion.identity);
        Hand hand = new Hand();

        hand.Sprite = go.transform.GetChild(0).gameObject;
        hand.From = Manager.Belts[index];
        hand.To = Manager.Belts[index + 1];
        hand.FromProgress = BeltLength - 1;
        hand.ToProgress = 1;

        Manager.Hands.Add(hand);
    }

    private void Update()
    {
        if (firstUpdate)
        {
            for (int x = 0; x < RowsCount; x++)
            {
                Manager.SpawnItem(new Vector2(x * 2, -4));
            }

            CountText.text += "," + RowsCount + " items";

            firstUpdate = false;
        }
    }
}