using UnityEngine;
using UnityEngine.InputSystem;

namespace MarsColony
{
    public static class ColonyMouse
    {
        public static bool IsPressed => Mouse.current.press.isPressed;
        public static Vector2 ScreenPosition => Mouse.current.position.ReadValue();
        public static Vector2 ScreenDeltaPosition => Mouse.current.delta.ReadValue();

        public static bool IsOnScreen
        {
            get
            {
                Vector2 screenPosition = ScreenPosition;
                return screenPosition.x >= 0 && screenPosition.x <= Screen.width
                    && screenPosition.y >= 0 && screenPosition.y <= Screen.height;
            }
        }

        public static Vector3Int GetPosition(Camera camera, Grid grid)
        {
            Vector3 worldPosition = camera.ScreenToWorldPoint(ScreenPosition);
            Vector3Int cellPosition = grid.WorldToCell(worldPosition);
            cellPosition.z = 0;
            return cellPosition;
        }
    }
}
