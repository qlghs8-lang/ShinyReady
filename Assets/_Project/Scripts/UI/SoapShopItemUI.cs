using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ShinyReady.Cleaning;
using ShinyReady.Currency;
using ShinyReady.Audio;

namespace ShinyReady.UI
{
    /// <summary>
    /// 비누 상점의 개별 아이템 행. SoapShopUI에서 동적으로 생성된다.
    /// </summary>
    public class SoapShopItemUI : MonoBehaviour
    {
        [Header("UI 컴포넌트")]
        [SerializeField] private Image _iconBackground;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _statsText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _actionButton;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private Image _buttonImage;
        [SerializeField] private GameObject _equippedBadge;
        [Tooltip("해금 전(해금하기) 상태일 때 표시할 잠금 아이콘 GameObject")]
        [SerializeField] private GameObject _lockIcon;
        [Tooltip("해금 후(장착하기) 상태일 때 표시할 장착 아이콘 GameObject")]
        [SerializeField] private GameObject _equipIcon;

        [Header("버튼 색상")]
        [SerializeField] private Color _unlockColor   = new Color(0.25f, 0.78f, 0.35f);
        [SerializeField] private Color _equipColor    = new Color(0.20f, 0.60f, 1.00f);
        [SerializeField] private Color _equippedColor = new Color(0.40f, 0.55f, 0.90f);
        [SerializeField] private Color _expensiveColor = new Color(0.82f, 0.30f, 0.30f);

        private SoapData _soap;

        private void Awake()
        {
            _actionButton?.onClick.AddListener(OnActionClick);
        }

        public void Setup(SoapData soap)
        {
            _soap = soap;
            Refresh();
        }

        public void Refresh()
        {
            if (_soap == null) return;
            var mgr = SoapInventoryManager.Instance;
            if (mgr == null) return;

            bool unlocked = mgr.IsSoapUnlocked(_soap);
            bool equipped = mgr.EquippedSoap == _soap;

            if (_iconBackground != null) _iconBackground.color = _soap.boxColor;

            _nameText?.SetText(_soap.soapName);

            // 스탯 표시
            string stats = $"세차 속도 x{_soap.washSpeedMultiplier:0.##}\n수익 배율 x{_soap.incomeMultiplier:0.##}";
            if (_soap.detailingChanceBonus > 0f)
                stats += $"\n광택 확률 +{_soap.detailingChanceBonus * 100f:0.#}%";
            _statsText?.SetText(stats);

            // 비용 표시
            _costText?.SetText(unlocked ? "" : $"$ {_soap.unlockCost}");

            // 장착 중일 때: 배지만 표시, 버튼 숨김
            if (_equippedBadge != null) _equippedBadge.SetActive(equipped);
            if (_actionButton != null) _actionButton.gameObject.SetActive(!equipped);

            if (unlocked && !equipped)
            {
                _buttonText?.SetText("");
                if (_buttonImage != null) _buttonImage.color = _equipColor;
                _lockIcon?.SetActive(false);
                _equipIcon?.SetActive(true);
            }
            else
            {
                bool canAfford = CurrencyManager.Instance != null
                              && CurrencyManager.Instance.HasEnoughMoney(_soap.unlockCost);
                _buttonText?.SetText("");
                if (_buttonImage != null)
                    _buttonImage.color = canAfford ? _unlockColor : _expensiveColor;
                _lockIcon?.SetActive(true);
                _equipIcon?.SetActive(false);
            }
        }

        // ── 클릭 처리 ─────────────────────────────────────────────────

        private void OnActionClick()
        {
            if (_soap == null) return;
            var mgr = SoapInventoryManager.Instance;
            if (mgr == null) return;

            if (!mgr.IsSoapUnlocked(_soap))
            {
                bool success = mgr.TryUnlock(_soap);
                if (success)
                {
                    mgr.EquipSoap(_soap);
                    PlaySuccessEffect();
                }
                else
                {
                    PlayFailEffect();
                }
            }
            else
            {
                mgr.EquipSoap(_soap);
                PlaySuccessEffect();
            }
        }

        // ── 피드백 ────────────────────────────────────────────────────

        private void PlaySuccessEffect()
        {
            if (_actionButton != null)
            {
                _actionButton.transform.DOKill();
                _actionButton.transform.localScale = Vector3.one;
                _actionButton.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.5f);
            }
            SoundManager.Instance?.PlaySuccess();
        }

        private void PlayFailEffect()
        {
            if (_actionButton != null)
            {
                _actionButton.transform.DOKill();
                _actionButton.transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20, 0f);
            }
            if (_buttonImage != null)
            {
                Color original = _buttonImage.color;
                _buttonImage.DOKill();
                _buttonImage.DOColor(Color.red, 0.1f)
                            .SetLoops(2, LoopType.Yoyo)
                            .OnComplete(() => _buttonImage.color = original);
            }
            SoundManager.Instance?.PlayFail();
        }
    }
}
