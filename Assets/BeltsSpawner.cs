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
    public int RowsCountNotInEditor = 1000;
    public int BeltLength = 10;

    private bool firstUpdate = true;

    private void Start()
    {
#if !UNITY_EDITOR
        RowsCount = RowsCountNotInEditor;
#endif

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

    private void SpawnBelt(Vector2 offset, bool down)
    {
        var points = new Vector2[BeltLength + 2];
        points[0] = offset + new Vector2(0, down ? 0 : BeltLength);
        for (int i = 1; i < BeltLength + 1; i++)
        {
            points[i] = offset + new Vector2(0, -.5f - (down ? i - 1 : (BeltLength - i)));
        }

        points[points.Length - 1] = offset + new Vector2(0, down ? -BeltLength : 0);

        Manager.Belts.Add(new BeltSystem(points) {Down = down});
    }

    private void SpawnHand(int index, bool atUp)
    {
        var position = new Vector3(2 * index + 1, -(atUp ? 1 : BeltLength - 1));
        Hand hand = new Hand(position);

        hand.From = Manager.Belts[index];
        hand.To = Manager.Belts[index + 1];
        hand.FromProgress = BeltLength - .5f;
        hand.ToProgress = 1.5f;

        Manager.Hands.Add(hand);
    }

    private void Update()
    {
        if (firstUpdate)
        {
            for (int x = 0; x < RowsCount; x++)
            {
                for (int i = 1; i < BeltLength - 1; i++)
                {
                    Manager.SpawnItem(new Vector2(x * 2, -i));
                }
            }

            CountText.text += "," + RowsCount * (BeltLength - 2) + " items";

            firstUpdate = false;
        }
    }
}