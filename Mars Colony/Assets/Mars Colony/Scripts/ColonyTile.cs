using UnityEngine;

namespace MarsColony
{
    [System.Serializable]
    public class ColonyTile
    {
        public string Name;
        public Vector2Int Position;
        public ColonyTileSO TileSO { get; private set; }

        public ColonyTile(Vector2Int position, ColonyTileSO tileSO)
        {
            Name = tileSO.name;
            Position = position;
            TileSO = tileSO;
        }
    }
}
