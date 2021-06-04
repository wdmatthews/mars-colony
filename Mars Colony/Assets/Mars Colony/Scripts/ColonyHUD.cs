using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/HUD")]
    [DisallowMultipleComponent]
    public class ColonyHUD : MonoBehaviour
    {
        private const float _animationTime = 0.15f;
        private const float _saveTime = 1f;
        private const float _panelMargin = 16;
        private const float _cameraMoveSpeed = 2;
        private const float _zoomAmount = 0.5f;
        private const float _minZoom = 2;
        private const float _maxZoom = 4;

        #region Inspector Fields
        [SerializeField] private RectTransform _menuButtonPanel = null;
        [SerializeField] private Button _menuButton = null;
        [SerializeField] private Image _saveIcon = null;
        [SerializeField] private Image _loadingTransition = null;

        [Space]
        [Header("Tile Panel")]
        [SerializeField] private RectTransform _tilePanel = null;
        [SerializeField] private TextMeshProUGUI _tilePanelName = null;
        [SerializeField] private Button _infoButton = null;
        [SerializeField] private Button _replaceButton = null;

        [Space]
        [Header("Tile Info Window")]
        [SerializeField] private RectTransform _tileInfoWindow = null;
        [SerializeField] private TextMeshProUGUI _tileInfoWindowHeader = null;
        [SerializeField] private TextMeshProUGUI _tileInfoWindowDescription = null;
        [SerializeField] private Button _closeInfoButton = null;

        [Space]
        [Header("Replace Tile Window")]
        [SerializeField] private RectTransform _replaceTileWindow = null;
        [SerializeField] private TextMeshProUGUI _replaceTileWindowHeader = null;
        [SerializeField] private RectTransform _replaceChoiceContainer = null;
        [SerializeField] private ColonyTileReplacementChoice _replacementChoicePrefab = null;
        [SerializeField] private Button _confirmReplaceButton = null;
        [SerializeField] private Button _cancelReplaceButton = null;

        [Space]
        [Header("Camera Panel")]
        [SerializeField] private Camera _camera = null;
        [SerializeField] private RectTransform _cameraPanel = null;
        [SerializeField] private Button _toggleMoveButton = null;
        [SerializeField] private RectTransform _toggleMoveButtonOutline = null;
        [SerializeField] private Button _zoomInButton = null;
        [SerializeField] private Button _zoomOutButton = null;

        [Space]
        [Header("Resource Panel")]
        [SerializeField] private RectTransform _resourcePanel = null;
        [SerializeField] private ColonyResourceUI[] _resourceAmounts = { };

        private Dictionary<ColonyResourceType, ColonyResourceUI> _resourceAmountsByType = new Dictionary<ColonyResourceType, ColonyResourceUI>();

        [Space]
        [Header("Explore Panel")]
        [SerializeField] private RectTransform _explorePanel = null;
        [SerializeField] private Button _exploreButton = null;
        [SerializeField] private TextMeshProUGUI _exploreCostLabel = null;
        #endregion

        #region Runtime Fields
        private ColonyTile _selectedTile = null;
        public bool IsMovingCamera { get; private set; } = false;
        [HideInInspector] public System.Func<bool> CanExplore = null;
        [HideInInspector] public System.Action OnExplore = null;
        private List<ColonyTileReplacementChoice> _replacementChoices = new List<ColonyTileReplacementChoice>();
        private ColonyTileSO _selectedReplacement = null;
        private bool _isUsingCargo = false;
        [HideInInspector] public System.Action<ColonyTileSO, bool> OnReplace = null;
        [HideInInspector] public System.Func<Dictionary<ColonyResourceType, int>> GetResourcesByType = null;
        public bool WindowIsOpen { get; private set; } = false;
        [HideInInspector] public Dictionary<ColonyResourceType, int> StartingCapacitiesByType = new Dictionary<ColonyResourceType, int>();
        [HideInInspector] public System.Func<Dictionary<ColonyResourceType, int>> GetResourceCapacitiesByType = null;
        [HideInInspector] public System.Func<Dictionary<ColonyResourceType, int>> GetResourceRatesByType = null;
        #endregion

        #region Unity Events
        private void Awake()
        {
            SetInitialState();
            AssignEventListeners();
        }

        private void Start()
        {
            _loadingTransition.gameObject.SetActive(true);
            _loadingTransition.DOFade(0, _animationTime * 5).From(1)
                .OnComplete(() => _loadingTransition.gameObject.SetActive(false));
        }

        private void Update()
        {
            if (ColonyMouse.IsPressed && IsMovingCamera)
            {
                Vector2 mouseDeltaPosition = ColonyMouse.ScreenDeltaPosition;

                if (!Mathf.Approximately(mouseDeltaPosition.x, 0)
                    && !Mathf.Approximately(mouseDeltaPosition.y, 0))
                {
                    _camera.transform.Translate(-Time.deltaTime * _cameraMoveSpeed * mouseDeltaPosition);
                }
            }
        }
        #endregion

        #region Initialization Methods
        private void SetInitialState()
        {
            _saveIcon.gameObject.SetActive(false);
            _saveIcon.color = new Color(1, 1, 1, 0);

            _tilePanel.gameObject.SetActive(false);
            _tilePanel.anchoredPosition = new Vector2(0, 0);

            _tileInfoWindow.gameObject.SetActive(false);
            _replaceTileWindow.gameObject.SetActive(false);

            _cameraPanel.gameObject.SetActive(true);
            _cameraPanel.anchoredPosition = new Vector2(0, -_panelMargin - _cameraPanel.rect.height);

            _toggleMoveButtonOutline.gameObject.SetActive(false);
            _zoomInButton.interactable = false;
            _zoomOutButton.interactable = true;

            _resourcePanel.gameObject.SetActive(true);
            _resourcePanel.anchoredPosition = new Vector2(-_panelMargin, -_panelMargin);

            foreach (ColonyResourceUI resourceAmount in _resourceAmounts)
            {
                _resourceAmountsByType.Add(resourceAmount.Type, resourceAmount);
            }

            _explorePanel.gameObject.SetActive(false);
            _explorePanel.anchoredPosition = new Vector2(0, 0);
        }

        private void AssignEventListeners()
        {
            _menuButton.onClick.AddListener(ReturnToMenu);
            _infoButton.onClick.AddListener(ShowTileInfoWindow);
            _replaceButton.onClick.AddListener(ShowReplaceTileWindow);
            _toggleMoveButton.onClick.AddListener(ToggleMove);
            _zoomInButton.onClick.AddListener(() => Zoom(-1));
            _zoomOutButton.onClick.AddListener(() => Zoom(1));
            _exploreButton.onClick.AddListener(Explore);
            _closeInfoButton.onClick.AddListener(HideTileInfoWindow);
            _confirmReplaceButton.onClick.AddListener(Replace);
            _cancelReplaceButton.onClick.AddListener(HideReplaceTileWindow);
        }
        #endregion

        public bool MouseIsOverPanel()
        {
            Vector2 mousePosition = ColonyMouse.ScreenPosition;
            return RectTransformUtility.RectangleContainsScreenPoint(_menuButtonPanel, mousePosition)
                || RectTransformUtility.RectangleContainsScreenPoint(_tilePanel, mousePosition)
                || RectTransformUtility.RectangleContainsScreenPoint(_cameraPanel, mousePosition)
                || RectTransformUtility.RectangleContainsScreenPoint(_resourcePanel, mousePosition)
                || RectTransformUtility.RectangleContainsScreenPoint(_explorePanel, mousePosition);
        }

        private void ReturnToMenu()
        {
            DOTween.Clear(true);
            SceneManager.LoadScene("Menu");
        }

        public DG.Tweening.Core.TweenerCore<Color, Color, DG.Tweening.Plugins.Options.ColorOptions> ShowSaveIcon()
        {
            _saveIcon.gameObject.SetActive(true);
            return _saveIcon.DOFade(1, _saveTime / 2);
        }

        public void HideSaveIcon()
        {
            _saveIcon.DOFade(0, _saveTime / 2).OnComplete(() => _saveIcon.gameObject.SetActive(false));
        }

        #region Tile Panel Methods
        public void ShowTilePanel(ColonyTile tile = null)
        {
            if (!_tilePanel.gameObject.activeSelf)
            {
                _tilePanel.gameObject.SetActive(true);
                _tilePanel.DOAnchorPosY(_panelMargin + _tilePanel.rect.height, _animationTime);
            }

            if (tile != null)
            {
                _selectedTile = tile;
                UpdateTilePanel();
            }
        }

        private void UpdateTilePanel()
        {
            _tilePanelName.text = _selectedTile.TileSO.NameOnHUD.Length > 0
                ? _selectedTile.TileSO.NameOnHUD
                : _selectedTile.Name;
            _replaceButton.interactable = _selectedTile.TileSO.CanBeReplacedBy.Length > 0;
        }

        public void HideTilePanel(bool keepSelected = false)
        {
            if (!keepSelected) _selectedTile = null;
            _tilePanel.DOAnchorPosY(0, _animationTime).OnComplete(() => _tilePanel.gameObject.SetActive(false));
        }
        #endregion

        #region Tile Info Window Methods
        private void ShowTileInfoWindow()
        {
            WindowIsOpen = true;
            _tileInfoWindowHeader.text = GetSelectedTileName();
            _tileInfoWindowDescription.text = GetSelectedTileDescription();
            _tileInfoWindow.gameObject.SetActive(true);
        }

        private void HideTileInfoWindow()
        {
            WindowIsOpen = false;
            _tileInfoWindow.gameObject.SetActive(false);
        }

        private string GetSelectedTileName()
        {
            return _selectedTile.TileSO.NameOnHUD.Length > 0
                ? _selectedTile.TileSO.NameOnHUD
                : _selectedTile.Name;
        }

        private string GetSelectedTileDescription()
        {
            ColonyTileSO tileSO = _selectedTile.TileSO;
            string description = tileSO.Description;
            Dictionary<ColonyResourceType, int> resourceCapacities = GetResourceCapacitiesByType();
            Dictionary<ColonyResourceType, int> resourceRates = GetResourceRatesByType();

            if (tileSO.Type == ColonyTileType.Headquarters)
            {
                foreach (var pair in StartingCapacitiesByType)
                {
                    description += $"\n\nHeadquarters {pair.Key} Capacity: {pair.Value}";
                    description += $"\nTotal {pair.Key} Capacity: {resourceCapacities[pair.Key]}";
                    description += $"\nTotal {pair.Key} Rate: {resourceRates[pair.Key]}";
                }
            }
            else if (tileSO.Type == ColonyTileType.Storage)
            {
                description += $"\n\n{tileSO.Resource} Capacity: {tileSO.Capacity}";
            }
            else if (tileSO.Type == ColonyTileType.Production)
            {
                description += $"\n\n{tileSO.Resource} Rate: {tileSO.Rate}";
            }

            return description;
        }
        #endregion

        #region Replace Tile Window Methods
        private void ShowReplaceTileWindow()
        {
            WindowIsOpen = true;
            Dictionary<ColonyResourceType, int> resourcesByType = GetResourcesByType();
            int replacementChoiceCount = _selectedTile.TileSO.CanBeReplacedBy.Length;
            int initializedCount = 0;

            foreach (ColonyTileReplacementChoice replacementChoice in _replacementChoices)
            {
                replacementChoice.Initialize(_selectedTile.TileSO.CanBeReplacedBy[initializedCount],
                    SelectReplacement, resourcesByType);
                replacementChoice.gameObject.SetActive(true);
                initializedCount++;

                if (initializedCount == replacementChoiceCount) break;
            }

            for (int i = replacementChoiceCount - initializedCount - 1; i >= 0; i--)
            {
                ColonyTileReplacementChoice replacementChoice = Instantiate(_replacementChoicePrefab, _replaceChoiceContainer);
                replacementChoice.Initialize(_selectedTile.TileSO.CanBeReplacedBy[initializedCount],
                    SelectReplacement, resourcesByType);
                _replacementChoices.Add(replacementChoice);
                initializedCount++;
            }

            _replaceTileWindowHeader.text = $"Replace {GetSelectedTileName()}";
            _confirmReplaceButton.interactable = false;
            _replaceTileWindow.gameObject.SetActive(true);
        }

        private void HideReplaceTileWindow()
        {
            WindowIsOpen = false;

            foreach (ColonyTileReplacementChoice replacementChoice in _replacementChoices)
            {
                replacementChoice.gameObject.SetActive(false);
            }

            _replaceTileWindow.gameObject.SetActive(false);
        }

        private void SelectReplacement(ColonyTileSO tileSO, bool useCargo, ColonyTileReplacementChoice replacementChoice)
        {
            _selectedReplacement = tileSO;
            _confirmReplaceButton.interactable = true;
            _isUsingCargo = useCargo;

            foreach (ColonyTileReplacementChoice choice in _replacementChoices)
            {
                if (choice != replacementChoice) choice.ResetSelect();
            }
        }

        private void Replace()
        {
            HideReplaceTileWindow();
            OnReplace.Invoke(_selectedReplacement, _isUsingCargo);
        }
        #endregion

        #region Camera Panel Methods
        public void ShowCameraPanel()
        {
            if (!_cameraPanel.gameObject.activeSelf)
            {
                _cameraPanel.gameObject.SetActive(true);
                _cameraPanel.DOAnchorPosY(-_panelMargin - _cameraPanel.rect.height, _animationTime);
            }
        }

        public void HideCameraPanel()
        {
            IsMovingCamera = false;
            _cameraPanel.DOAnchorPosY(0, _animationTime).OnComplete(() => _cameraPanel.gameObject.SetActive(false));
        }

        private void ToggleMove()
        {
            IsMovingCamera = !IsMovingCamera;
            _toggleMoveButtonOutline.gameObject.SetActive(IsMovingCamera);
            if (!IsMovingCamera && _selectedTile != null) ShowTilePanel();
            else if (IsMovingCamera) HideTilePanel(true);
        }

        private void Zoom(int direction)
        {
            float newZoom = Mathf.Clamp(_camera.orthographicSize + direction * _zoomAmount,
                _minZoom, _maxZoom);
            _camera.DOOrthoSize(newZoom, _animationTime);
            _zoomInButton.interactable = !Mathf.Approximately(newZoom, _minZoom);
            _zoomOutButton.interactable = !Mathf.Approximately(newZoom, _maxZoom);
        }
        #endregion

        #region Resource Panel Methods
        public void UpdateResourceAmount(ColonyResourceType resource, int amount, bool shouldAnimate = false)
        {
            _resourceAmountsByType[resource].UpdateAmount(amount, shouldAnimate);
        }
        #endregion

        #region Explore Panel Methods
        public void ShowExplorePanel(int cost)
        {
            _explorePanel.gameObject.SetActive(true);
            _explorePanel.DOAnchorPosY(_panelMargin + _explorePanel.rect.height, _animationTime);
            _exploreButton.interactable = CanExplore();
            _exploreCostLabel.text = $"{cost}";
        }

        public void HideExplorePanel()
        {
            _explorePanel.DOAnchorPosY(0, _animationTime).OnComplete(() => _explorePanel.gameObject.SetActive(false));
        }

        private void Explore()
        {
            OnExplore();
            HideExplorePanel();
        }
        #endregion
    }
}
