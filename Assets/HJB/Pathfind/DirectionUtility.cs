using UnityEngine;

public static class DirectionUtility
{
    public static Vector2[] Directions16;

    static DirectionUtility()
    {
        Directions16 = new Vector2[16];

        for (int i = 0; i < 16; i++)
        {
            float angle =
                i * Mathf.PI * 2f / 16f;

            Directions16[i] =
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                );
        }
    }
}
