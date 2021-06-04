using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/Colony")]
    [DisallowMultipleComponent]
    public class Colony : MonoBehaviour
    {
        private static readonly Dictionary<ColonyResourceType, int> _startingCapacitiesByType = new Dictionary<ColonyResourceType, int>
        {
            { ColonyResourceType.Wood, 100 },
            { ColonyResourceType.Crystal, 100 },
            { ColonyResourceType.Cargo, 0 },
            { ColonyResourceType.Population, 10 },
        };
        private const int _startingExporeCost = 5;
        private const float _generationTime = 10;
        private const float _saveTime = 5;
        public static bool LoadNewColony = true;
        public static string ColonySaveData = "";

        #region Inspector Fields
        [SerializeField] private ColonyHUD _hud = null;
        [SerializeField] private Camera _camera = null;
        [SerializeField] private Grid _grid = null;
        [SerializeField] private Tilemap _tilemap = null;
        [SerializeField] private SpriteRenderer _selectedTileCursor = null;
        [SerializeField] private SpriteRenderer _tileCursor = null;
        [SerializeField] private ColonyTileSO[] _tileSOs = { };
        #endregion

        #region Runtime Fields
        private Dictionary<string, ColonyTileSO> _tileSOsByName = new Dictionary<string, ColonyTileSO>();
        private Dictionary<ColonyTileType, List<ColonyTileSO>> _tileSOsByType = new Dictionary<ColonyTileType, List<ColonyTileSO>>();
        private Dictionary<Vector2Int, ColonyTile> _tilesByPosition = new Dictionary<Vector2Int, ColonyTile>();
        private bool _mouseWasPressed = false;
        private Dictionary<ColonyResourceType, int> _resourcesByType = new Dictionary<ColonyResourceType, int>();
        private Dictionary<ColonyResourceType, int> _resourceCapacitiesByType = new Dictionary<ColonyResourceType, int>();
        private Dictionary<ColonyResourceType, int> _resourceRatesByType = new Dictionary<ColonyResourceType, int>();
        private ColonyTile _selectedTile = null;
        private Vector2Int _selectedPosition = new Vector2Int();
        private int _exploreCost = 5;
        private float _resourceGenerationTimer = 0;
        private float _saveTimer = 0;
        #endregion

        #region Unity Events
        private void Start()
        {
            ReadTileSOs();
            Load();
            _hud.StartingCapacitiesByType = _startingCapacitiesByType;
            _hud.CanExplore = () => _resourcesByType[ColonyResourceType.Crystal] > _exploreCost;
            _hud.OnExplore = Explore;
            _hud.OnReplace = Replace;
            _hud.GetResourcesByType = () => _resourcesByType;
            _hud.GetResourceCapacitiesByType = () => _resourceCapacitiesByType;
            _hud.GetResourceRatesByType = () => _resourceRatesByType;
            _selectedTileCursor.gameObject.SetActive(false);
        }

        private void Update()
        {
            Vector3Int mousePosition = ColonyMouse.GetPosition(_camera, _grid);
            Vector2Int gridPosition = new Vector2Int(mousePosition.x, mousePosition.y);

            if (!_hud.IsMovingCamera)
            {
                if (!_tileCursor.gameObject.activeSelf) _tileCursor.gameObject.SetActive(true);
                _tileCursor.transform.position = _grid.CellToWorld(mousePosition);
            } else if (_tileCursor.gameObject.activeSelf) _tileCursor.gameObject.SetActive(false);

            bool mouseIsPressed = ColonyMouse.IsPressed;
            bool mouseWasClicked = mouseIsPressed && !_mouseWasPressed && ColonyMouse.IsOnScreen
                && !_hud.MouseIsOverPanel() && !_hud.IsMovingCamera && !_hud.WindowIsOpen;

            if (mouseWasClicked && _tilesByPosition.ContainsKey(gridPosition))
            {
                _selectedPosition = gridPosition;
                SelectTile(_tilesByPosition[gridPosition]);
                _hud.HideExplorePanel();
            }
            else if (mouseWasClicked)
            {
                bool positionChanged = _selectedPosition != gridPosition;
                _selectedPosition = gridPosition;
                if (_selectedTile != null) DeselectTile();

                if (positionChanged)
                {
                    _hud.ShowExplorePanel(_exploreCost);
                    if (!_selectedTileCursor.gameObject.activeSelf) _selectedTileCursor.gameObject.SetActive(true);
                    _selectedTileCursor.transform.position = _grid.CellToWorld(mousePosition);
                }
                else
                {
                    _hud.HideExplorePanel();
                    _selectedTileCursor.gameObject.SetActive(false);
                }
            }

            _mouseWasPressed = mouseIsPressed;
            GenerateResources();

            if (Mathf.Approximately(_saveTimer, _saveTime))
            {
                _saveTimer = 0;
                Save();
            }
            else _saveTimer = Mathf.Clamp(_saveTimer + Time.deltaTime, 0, _saveTime);
        }
        #endregion

        #region Loading
        private void ReadTileSOs()
        {
            foreach (ColonyTileSO tileSO in _tileSOs)
            {
                _tileSOsByName.Add(tileSO.name, tileSO);

                if (_tileSOsByType.ContainsKey(tileSO.Type)) _tileSOsByType[tileSO.Type].Add(tileSO);
                else _tileSOsByType[tileSO.Type] = new List<ColonyTileSO> { tileSO };
            }
        }

        private ColonySave GetNewSave()
        {
            return new ColonySave
            {
                tiles = new List<ColonyTile>(),
                exploreCost = _startingExporeCost,
                resourceAmounts = new List<int>(_startingCapacitiesByType.Values),
            };
        }

        private void Load()
        {
            ColonySave saveData = null;

            #if UNITY_EDITOR
            saveData = GetNewSave();
            #elif UNITY_WEBGL
            if (LoadNewColony) saveData = GetNewSave();
            else saveData = JsonUtility.FromJson<ColonySave>(ColonySaveData);
            #endif
            LoadResources(saveData);
            LoadTiles(saveData);

            if (LoadNewColony) Save();
        }

        private void LoadResources(ColonySave saveData)
        {
            _exploreCost = saveData.exploreCost;
            int resourceIndex = 0;
            foreach (var resource in _startingCapacitiesByType)
            {
                _resourcesByType[resource.Key] = saveData.resourceAmounts[resourceIndex];
                _hud.UpdateResourceAmount(resource.Key, saveData.resourceAmounts[resourceIndex]);
                _resourceCapacitiesByType.Add(resource.Key, _startingCapacitiesByType[resource.Key]);
                _resourceRatesByType.Add(resource.Key, 0);
                resourceIndex++;
            }
        }

        private void LoadTiles(ColonySave saveData)
        {
            if (saveData.tiles.Count == 0)
            {
                PlaceTile(new Vector2Int(), _tileSOsByName["Headquarters"]);
                PlaceTileRings(3, _tileSOsByName["Dirt"]);
                GenerateInitialTerrainAndResourceTiles();
            }
            else
            {
                foreach (ColonyTile tile in saveData.tiles)
                {
                    PlaceTile(tile.Position, _tileSOsByName[tile.Name]);
                }
            }
        }

        private void GenerateInitialTerrainAndResourceTiles(int treeCount = 0, int crystalCount = 0,
            int recursiveIterations = 0)
        {
            List<Vector2Int> positionsToReplace = new List<Vector2Int>();

            foreach (Vector2Int position in _tilesByPosition.Keys)
            {
                if (position == new Vector2Int()) continue;
                if (Random.Range(0f, 1f) < 0.3f) positionsToReplace.Add(position);
            }

            foreach (Vector2Int position in _tilesByPosition.Keys)
            {
                if (position == new Vector2Int()) continue;
                if (Random.Range(0f, 1f) < 0.3f) positionsToReplace.Add(position);
            }

            int newTreeCount = treeCount;
            int newCrystalCount = crystalCount;

            foreach (Vector2Int position in positionsToReplace)
            {
                ColonyTile tile = _tilesByPosition.ContainsKey(position) ? _tilesByPosition[position] : null;
                if (tile.TileSO.Type == ColonyTileType.Resource) continue;
                ColonyTileSO tileSO = PlaceRandomTerrainOrResourceTile(position);

                if (tileSO.name.Contains("Trees")) newTreeCount++;
                if (tileSO.name.Contains("Crystals")) newCrystalCount++;
            }

            if ((newTreeCount < 2 || newCrystalCount < 2) && recursiveIterations < 10)
            {
                GenerateInitialTerrainAndResourceTiles(newTreeCount, newCrystalCount, recursiveIterations + 1);
            }
        }
        #endregion

        #region Saving
        private void Save()
        {
            List<ColonyTile> tiles = new List<ColonyTile>();

            foreach (ColonyTile tile in _tilesByPosition.Values)
            {
                tiles.Add(tile);
            }

            ColonySave saveData = new ColonySave
            {
                tiles = tiles,
                exploreCost = _exploreCost,
                resourceAmounts = new List<int>(_resourcesByType.Values)
            };

            #if UNITY_EDITOR
            _hud.ShowSaveIcon().OnComplete(_hud.HideSaveIcon);
            #elif UNITY_WEBGL
            _hud.ShowSaveIcon().OnComplete(() =>
            {
                string saveDataString = JsonUtility.ToJson(saveData);
                ColonyServer.Save(saveDataString);
            });
            #endif
        }
        #endregion

        #region Resources
        private void GenerateResources()
        {
            if (Mathf.Approximately(_resourceGenerationTimer, _generationTime))
            {
                _resourceGenerationTimer = 0;

                foreach (var pair in _resourceRatesByType)
                {
                    if (pair.Value == 0
                        || _resourcesByType[pair.Key] == _resourceCapacitiesByType[pair.Key]) continue;
                    ChangeResourceAmount(pair.Key, pair.Value, true);
                }
            }
            else _resourceGenerationTimer = Mathf.Clamp(_resourceGenerationTimer + Time.deltaTime, 0, _generationTime);
        }

        private void ChangeResourceAmount(ColonyResourceType resource, int amount, bool shouldAnimate = false)
        {
            int newAmount = Mathf.Clamp(_resourcesByType[resource] + amount, 0, _resourceCapacitiesByType[resource]);
            _resourcesByType[resource] = newAmount;
            _hud.UpdateResourceAmount(resource, newAmount, shouldAnimate);
        }
        #endregion

        #region Tile Placement
        private void PlaceTile(Vector2Int position, ColonyTileSO tileSO, bool useCargo = false)
        {
            ColonyTile replacedTile = _tilesByPosition.ContainsKey(position) ? _tilesByPosition[position] : null;
            _tilesByPosition[position] = new ColonyTile(position, tileSO);
            _tilemap.SetTile(new Vector3Int(position.x, position.y, 0), tileSO.Tile);

            if (tileSO.Type == ColonyTileType.Storage)
            {
                _resourceCapacitiesByType[tileSO.Resource] +=
                    tileSO.Capacity - (replacedTile != null ? replacedTile.TileSO.Capacity : 0);
            }
            else if (tileSO.Type == ColonyTileType.Production)
            {
                _resourceRatesByType[tileSO.Resource] +=
                    tileSO.Rate - (replacedTile != null ? replacedTile.TileSO.Rate : 0);
            }

            foreach (ColonyResourceCost cost in tileSO.Costs)
            {
                if (useCargo && cost.Resource != ColonyResourceType.Cargo
                    || !useCargo && cost.Resource == ColonyResourceType.Cargo) continue;
                ChangeResourceAmount(cost.Resource, -cost.Amount, true);
            }
        }

        private void PlaceTileLine(int start, int end, int y, ColonyTileSO tileSO)
        {
            for (int x = start; x <= end; x++)
            {
                if (x == 0 && y == 0) continue;
                PlaceTile(new Vector2Int(x, y), tileSO);
            }
        }

        private void PlaceTileRings(int size, ColonyTileSO tileSO)
        {
            for (int y = -size; y <= size; y++)
            {
                int start = -(size - Mathf.FloorToInt(Mathf.Abs(y) / 2.0f));
                int end = size - Mathf.FloorToInt((Mathf.Abs(y) + 1) / 2.0f);
                PlaceTileLine(start, end, y, tileSO);
            }
        }

        private ColonyTileSO PlaceRandomTerrainOrResourceTile(Vector2Int position)
        {
            bool isTerrainTile = Random.Range(0, 2) == 0;
            List<ColonyTileSO> tileSOs = isTerrainTile
                ? _tileSOsByType[ColonyTileType.Terrain]
                : _tileSOsByType[ColonyTileType.Resource];
            ColonyTileSO tileSO = tileSOs[Random.Range(0, tileSOs.Count)];
            PlaceTile(position, tileSO);
            return tileSO;
        }
        #endregion

        #region Tile Actions
        private void SelectTile(ColonyTile tile)
        {
            if (_selectedTile != null && _selectedTile == tile) DeselectTile();
            else
            {
                _selectedTile = tile;
                _hud.ShowTilePanel(tile);
                if (!_selectedTileCursor.gameObject.activeSelf) _selectedTileCursor.gameObject.SetActive(true);
                _selectedTileCursor.transform.position = _grid.CellToWorld(new Vector3Int(tile.Position.x, tile.Position.y, 0));
            }
        }

        private void DeselectTile()
        {
            _selectedTile = null;
            _hud.HideTilePanel();
            _selectedTileCursor.gameObject.SetActive(false);
        }

        private void Explore()
        {
            PlaceRandomTerrainOrResourceTile(_selectedPosition);
            SelectTile(_tilesByPosition[_selectedPosition]);
            ChangeResourceAmount(ColonyResourceType.Crystal, -_exploreCost, true);
            if (_exploreCost < 100) _exploreCost++;
        }

        private void Replace(ColonyTileSO tileSO, bool useCargo)
        {
            PlaceTile(_selectedPosition, tileSO, useCargo);
            _selectedTile = null;
            SelectTile(_tilesByPosition[_selectedPosition]);
        }
        #endregion
    }
}
