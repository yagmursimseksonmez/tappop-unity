using System;
using UnityEngine;
using UnityEngine.UI;

namespace TapPop
{
    [CreateAssetMenu(menuName = "TapPop/Tile Set", fileName = "TileSet")]
    public class TileSet : ScriptableObject
    {
        [Serializable]
        public class Tile
        {
            [Tooltip("Matches the sprite name: id \"red\" uses tile_red")]
            public string id = "red";
            public Sprite sprite;
            [Tooltip("Pop particle colour, and the placeholder tint while there is no sprite")]
            public Color color = Color.white;
        }

        public Tile[] tiles;
        [Tooltip("Shown, tinted, until the real tile art is linked")]
        public Sprite placeholder;

        public string Id(int index) => tiles[index].id;
        public Color FxColor(int index) => tiles[index].color;
        public Sprite SpriteFor(int index) => tiles[index].sprite != null ? tiles[index].sprite : placeholder;
        public Color TintFor(int index) => tiles[index].sprite != null ? Color.white : tiles[index].color;

        public void Apply(Image image, int index)
        {
            image.sprite = SpriteFor(index);
            image.color = TintFor(index);
            image.type = Image.Type.Simple;
            image.preserveAspect = true;
        }
    }
}
