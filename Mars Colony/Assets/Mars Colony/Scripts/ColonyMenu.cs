using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/Menu")]
    [DisallowMultipleComponent]
    public class ColonyMenu : MonoBehaviour
    {
        private const float _animationTime = 0.15f;

        #region Inspector Fields
        [SerializeField] private Button _continueButton = null;
        [SerializeField] private Button _newButton = null;
        [SerializeField] private Image _loadingScreen = null;
        #endregion

        #region Runtime Fields
        [HideInInspector] public System.Action OnContinue = null;
        #endregion

        #region Methods
        private void Awake()
        {
            _continueButton.onClick.AddListener(Continue);
            _newButton.onClick.AddListener(NewColony);
            if (ColonyServer.HasSave) EnableContinueButton();
        }

        private void Continue()
        {
            Colony.LoadNewColony = false;
            ShowLoadingScreen();
            OnContinue.Invoke();
        }

        private void NewColony()
        {
            Colony.LoadNewColony = true;
            ColonyServer.HasSave = true;
            ShowLoadingScreen().OnComplete(LoadScene);
            LoadScene();
        }

        private DG.Tweening.Core.TweenerCore<Color, Color, DG.Tweening.Plugins.Options.ColorOptions> ShowLoadingScreen()
        {
            _loadingScreen.gameObject.SetActive(true);
            return _loadingScreen.DOFade(1, _animationTime * 5);
        }

        public void LoadScene()
        {
            DOTween.Clear(true);
            SceneManager.LoadScene("Colony");
        }

        public void EnableContinueButton()
        {
            _continueButton.interactable = true;
        }
        #endregion
    }
}
