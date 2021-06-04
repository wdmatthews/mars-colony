using System.Collections.Generic;

namespace MarsColony
{
    [System.Serializable]
    public class ColonySave
    {
        public List<ColonyTile> tiles = new List<ColonyTile>();
        public int exploreCost = 0;
        public List<int> resourceAmounts = new List<int>();
    }
}
