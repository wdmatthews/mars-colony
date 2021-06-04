using UnityEngine;
using TMPro;
using DG.Tweening;

namespace MarsColony
{
    [AddComponentMenu("Mars Colony/Resource UI")]
    [DisallowMultipleComponent]
    public class ColonyResourceUI : MonoBehaviour
    {
        private const float _animationTime = 0.15f;
        private const float _scaleAnimationSize = 1.1f;

        #region Inspector Fields
        [SerializeField] private ColonyResourceType _type = ColonyResourceType.Wood;
        public ColonyResourceType Type => _type;
        [SerializeField] private TextMeshProUGUI _amountLabel = null;
        #endregion

        #region Public Methods
        public void UpdateAmount(int amount, bool shouldAnimate = false)
        {
            float millions = Mathf.Round(100 * amount / 1e6f) / 100;
            _amountLabel.text = $"{((amount <= 1e6) ? amount.ToString() : $"{millions} mil")}";

            if (shouldAnimate)
            {
                transform.DOScale(_scaleAnimationSize, _animationTime)
                    .OnComplete(() => transform.DOScale(1, _animationTime));
            }
        }
        #endregion
    }
}
