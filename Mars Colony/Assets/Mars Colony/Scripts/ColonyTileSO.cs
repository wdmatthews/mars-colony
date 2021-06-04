using UnityEngine;
using UnityEngine.Tilemaps;

namespace MarsColony
{
    [CreateAssetMenu(fileName = "New Tile", menuName = "Mars Colony/Tile")]
    public class ColonyTileSO : ScriptableObject
    {
        public string NameOnHUD = "";
        public string Description = "";
        public Tile Tile = null;
        public ColonyTileType Type = ColonyTileType.Terrain;
        public ColonyResourceCost[] Costs = { };
        public ColonyTileSO[] CanBeReplacedBy = { };
        [Tooltip("Used for production and storage tiles.")]
        public ColonyResourceType Resource = ColonyResourceType.Wood;
        [Tooltip("Used for storage tiles.")]
        public int Capacity = 0;
        [Tooltip("Amount per time. Used for production tiles.")]
        public int Rate = 0;
    }
}
