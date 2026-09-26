using UnityEngine;

//builds a small placeholder sprite (top half green = deflect side, bottom half red = destroy side)
//so ChainLink can flip it to show which side currently deflects, without needing an art asset.
public static class DeflectSideVisual
{
    private static Sprite cached;

    public static Sprite GetSprite()
    {
        if (cached != null) return cached;

        Texture2D tex = new Texture2D(2, 2);
        tex.filterMode = FilterMode.Point;
        Color deflectColor = new Color(0.2f, 1f, 0.2f);
        Color destroyColor = new Color(1f, 0.2f, 0.2f);
        tex.SetPixel(0, 1, deflectColor);
        tex.SetPixel(1, 1, deflectColor);
        tex.SetPixel(0, 0, destroyColor);
        tex.SetPixel(1, 0, destroyColor);
        tex.Apply();

        cached = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);
        return cached;
    }
}
