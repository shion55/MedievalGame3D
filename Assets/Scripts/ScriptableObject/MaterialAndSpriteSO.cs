using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MaterialAndSprite",menuName ="ScriptableObjects/MaterialAndSprite")]
public class MaterialAndSpriteSO : ScriptableObject
{
    [System.Serializable]
    private class SpriteEntry
    {
        public MaterialType type;
        public Sprite sprite;
    }
    [SerializeField]
    private List<SpriteEntry> entries;

    private Dictionary<MaterialType, Sprite> _dict;

    public Sprite GetSprite(MaterialType type)
    {
        if (_dict == null)
        {
            _dict = new Dictionary<MaterialType, Sprite>();
            foreach (var entry in entries)
            {
                _dict[entry.type] = entry.sprite;
            }
        }

        return _dict.TryGetValue(type, out var sprite) ? sprite : null;
    }
}
