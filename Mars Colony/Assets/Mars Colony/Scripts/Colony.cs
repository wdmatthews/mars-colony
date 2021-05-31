using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/Colony")]
    [DisallowMultipleComponent]
    public class Colony : MonoBehaviour
    {
        [SerializeField] private Camera _camera = null;
        [SerializeField] private Grid _grid = null;
        [SerializeField] private Tilemap _tilemap = null;
        [SerializeField] private SpriteRenderer _tileCursor = null;
        [SerializeField] private ColonyTileSO[] _tileSOs = { };

        private Dictionary<string, ColonyTileSO> _tileSOsByName = new Dictionary<string, ColonyTileSO>();
        private Dictionary<Vector2Int, ColonyTile> _tilesByPosition = new Dictionary<Vector2Int, ColonyTile>();
        private bool _mouseWasPressed = false;

        private void Awake()
        {
            ReadTileSOs();
            LoadTiles();
        }

        private void Update()
        {
            Vector3Int mousePosition = ColonyMouse.GetPosition(_camera, _grid);
            Vector2Int gridPosition = new Vector2Int(mousePosition.x, mousePosition.y);
            _tileCursor.transform.position = _grid.CellToWorld(mousePosition);
            bool mouseIsPressed = ColonyMouse.IsPressed;
            bool mouseWasClicked = mouseIsPressed && !_mouseWasPressed;

            if (mouseWasClicked && _tilesByPosition.ContainsKey(gridPosition))
            {
                ColonyTile clickedTile = _tilesByPosition[gridPosition];
                Debug.Log($"Clicked on \"{clickedTile.Name}\" at ({gridPosition.x}, {gridPosition.y}).");
            }
            else if (mouseWasClicked)
            {
                Debug.Log("Clicked outside the tilemap.");
            }

            _mouseWasPressed = mouseIsPressed;
        }

        private void ReadTileSOs()
        {
            foreach (ColonyTileSO tileSO in _tileSOs)
            {
                _tileSOsByName.Add(tileSO.name, tileSO);
            }
        }

        private void LoadTiles()
        {
            // TODO Load from save if one exists.
            PlaceTile(new Vector2Int(), _tileSOsByName["Headquarters"]);
            PlaceTileRings(3, _tileSOsByName["Dirt"]);
        }

        private void PlaceTile(Vector2Int position, ColonyTileSO tileSO)
        {
            _tilesByPosition[position] = new ColonyTile(position, tileSO);
            _tilemap.SetTile(new Vector3Int(position.x, position.y, 0), tileSO.Tile);
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
    }
}
