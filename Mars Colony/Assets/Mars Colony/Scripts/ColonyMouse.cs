using UnityEngine;
using UnityEngine.InputSystem;

namespace MarsColony
{
    public static class ColonyMouse
    {
        public static bool IsPressed => Mouse.current.press.isPressed;

        public static Vector3Int GetPosition(Camera camera, Grid grid)
        {
            Vector3 screenPosition = Mouse.current.position.ReadValue();
            Vector3 worldPosition = camera.ScreenToWorldPoint(screenPosition);
            Vector3Int cellPosition = grid.WorldToCell(worldPosition);
            cellPosition.z = 0;
            return cellPosition;
        }
    }
}
