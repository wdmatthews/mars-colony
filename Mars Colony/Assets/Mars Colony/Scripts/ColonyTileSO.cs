using UnityEngine;
using UnityEngine.Tilemaps;

namespace MarsColony
{
    [CreateAssetMenu(fileName = "New Tile", menuName = "Mars Colony/Tile")]
    public class ColonyTileSO : ScriptableObject
    {
        public Tile Tile = null;
        public ColonyTileType Type = ColonyTileType.Terrain;
    }
}
