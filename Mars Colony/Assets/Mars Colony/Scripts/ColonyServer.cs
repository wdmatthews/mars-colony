using System.Runtime.InteropServices;
using UnityEngine;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/Server")]
    [DisallowMultipleComponent]
    public class ColonyServer : MonoBehaviour
    {
        public static bool HasSave { get; set; }

        #region Inspector Fields
        [SerializeField] private ColonyMenu _menu = null;
        [SerializeField] private ColonyHUD _hud = null;
        #endregion

        #region Methods
        [DllImport("__Internal")]
        public static extern void Load();

        [DllImport("__Internal")]
        public static extern void Save(string saveData);

        private void Awake()
        {
            if (_menu) _menu.OnContinue = Load;
        }

        public void EnableContinueButton()
        {
            HasSave = true;
            _menu.EnableContinueButton();
        }

        public void ReceiveGameSave(string gameSave)
        {
            Colony.ColonySaveData = gameSave;
            _menu.LoadScene();
        }

        public void SavedSuccessfully() => _hud.HideSaveIcon();
        #endregion
    }
}
