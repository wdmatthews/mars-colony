using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/Tile Replacement Choice")]
    [DisallowMultipleComponent]
    public class ColonyTileReplacementChoice : MonoBehaviour
    {
        #region Fields
        [SerializeField] private Image _tileIcon = null;
        [SerializeField] private TextMeshProUGUI _tileNameLabel = null;
        [SerializeField] private ColonyResourceUI[] _costLabels = { };
        private Dictionary<ColonyResourceType, ColonyResourceUI> _costLabelsByType = new Dictionary<ColonyResourceType, ColonyResourceUI>();
        [SerializeField] private Button _selectButton = null;
        public Button SelectButton => _selectButton;
        [SerializeField] private Button _selectUsingCargoButton = null;
        public Button SelectUsingCargoButton => _selectUsingCargoButton;
        private System.Action<ColonyTileSO, bool, ColonyTileReplacementChoice> _onSelect = null;
        private ColonyTileSO _tileSO = null;
        private bool _canAfford = false;
        private bool _canAffordUsingCargo = false;
        #endregion

        #region Methods
        public void Initialize(ColonyTileSO tileSO, System.Action<ColonyTileSO, bool, ColonyTileReplacementChoice> onSelect, Dictionary<ColonyResourceType, int> resourcesByType)
        {
            if (_costLabelsByType.Count == 0) InitializeUI();
            _tileSO = tileSO;
            _tileIcon.sprite = tileSO.Tile.sprite;
            _tileNameLabel.text = tileSO.NameOnHUD.Length > 0
                ? tileSO.NameOnHUD
                : tileSO.name;
            _onSelect = onSelect;
            _selectButton.interactable = true;
            _selectUsingCargoButton.interactable = true;
            _canAfford = true;
            _canAffordUsingCargo = true;

            foreach (ColonyResourceUI costLabel in _costLabels)
            {
                costLabel.gameObject.SetActive(false);
            }

            foreach (ColonyResourceCost cost in _tileSO.Costs)
            {
                _costLabelsByType[cost.Resource].gameObject.SetActive(true);
                _costLabelsByType[cost.Resource].UpdateAmount(cost.Amount);

                if (resourcesByType[cost.Resource] < cost.Amount
                    && cost.Resource != ColonyResourceType.Cargo
                    && _selectButton.interactable)
                {
                    _canAfford = false;
                    _selectButton.interactable = false;
                }

                if (resourcesByType[cost.Resource] < cost.Amount
                    && cost.Resource == ColonyResourceType.Cargo
                    && _selectUsingCargoButton.interactable)
                {
                    _canAffordUsingCargo = false;
                    _selectUsingCargoButton.interactable = false;
                }
            }
        }

        private void InitializeUI()
        {
            foreach (ColonyResourceUI label in _costLabels)
            {
                _costLabelsByType.Add(label.Type, label);
            }

            _selectButton.onClick.AddListener(() => Select(false));
            _selectUsingCargoButton.onClick.AddListener(() => Select(true));
        }

        private void Select(bool useCargo)
        {
            _selectButton.interactable = useCargo && _canAfford;
            _selectUsingCargoButton.interactable = !useCargo && _canAffordUsingCargo;
            _onSelect.Invoke(_tileSO, useCargo, this);
        }

        public void ResetSelect()
        {
            _selectButton.interactable = _canAfford;
            _selectUsingCargoButton.interactable = _canAffordUsingCargo;
        }
        #endregion
    }
}
