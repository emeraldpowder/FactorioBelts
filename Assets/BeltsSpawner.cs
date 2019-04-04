using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class BeltsSpawner : MonoBehaviour
{
    public BeltsManager Manager;
    [FormerlySerializedAs("BeltSprite")] public SpriteRenderer BeltSpritePrefab;
    public GameObject HandPrefab;
    public Text CountText;

    [FormerlySerializedAs("RowsCount")] public int Rows = 10;

    public int Columns = 10;
    public int RowsNotInEditor = 1000;
    public int ColumnsNotInEditor = 10;
    public int BeltLength = 10;

    private bool firstUpdate = true;

    private void Start()
    {
#if !UNITY_EDITOR
        Rows = RowsNotInEditor;
        Columns = ColumnsNotInEditor;
#endif

        for (int i = 0; i < Rows; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                SpawnBelt(new Vector2(2 * i, -j * (BeltLength + 5)), i % 2 != 0);
            }
        }

        for (int i = 0; i < Rows - 1; i++)
        {
            for (int j = 0; j < Columns; j++)
            {
                SpawnHand(j, i, i % 2 == 0);
            }
        }

        CountText.text = Rows *Columns* BeltLength + " belts";
    }

    private void SpawnBelt(Vector2 offset, bool down)
    {
        var wayPoints = new Vector2[BeltLength + 1];
        for (int i = 0; i < BeltLength + 1; i++)
        {
            wayPoints[i] = offset + new Vector2(0, -(down ? i : (BeltLength - i)));
        }

        var spritePositions = new Vector2[BeltLength];
        for (int i = 0; i < BeltLength; i++)
        {
            spritePositions[i] = offset + new Vector2(0, -.5f - (down ? i : (BeltLength - 1 - i)));
        }

        wayPoints[wayPoints.Length - 1] = offset + new Vector2(0, down ? -BeltLength : 0);

        var beltSystem = new ObjectWithBounds(wayPoints, spritePositions) {Down = down};
        Manager.Belts.Add(beltSystem);
        Manager.Objects.Insert(beltSystem);
    }

    private void SpawnHand(int j, int index, bool atUp)
    {
        var position = new Vector3(2 * index + 1, -j * (BeltLength + 5) - (atUp ? 1 : BeltLength - 1));
        ObjectWithBounds hand = new ObjectWithBounds(position);

        hand.From = Manager.Belts[index + j * Rows];
        hand.To = Manager.Belts[index + j * Rows + 1];
        hand.FromProgress = BeltLength - 1f;
        hand.ToProgress = 1f;

        Manager.Hands.Add(hand);
        Manager.Objects.Insert(hand);
    }

    private void Update()
    {
        if (firstUpdate)
        {
            for (int x = 0; x < Rows; x++)
            {
                for (int j = 0; j < Columns; j++)
                {
                    for (int i = 1; i < BeltLength - 1; i++)
                    {
                        Manager.SpawnItem(new Vector2(x * 2, -i - j * (BeltLength + 5)));
                    }
                }
            }

            CountText.text += "," + Rows*Columns * (BeltLength - 2) + " items";

            firstUpdate = false;
        }
    }
}