using UnityEngine;
using UnityEngine.UI;
using ShinyReady.Upgrade;
using ShinyReady.Currency;

namespace ShinyReady.UI
{
    /// <summary>
    /// 업그레이드 창 (사무실 진입 시 OfficeTrigger가 SetActive).
    /// 탭 방식으로 [Player / Staff / WashBay / Detailing] 카테고리 전환.
    /// </summary>
    public class UpgradePanel : MonoBehaviour
    {
        [Header("탭 버튼 (5개)")]
        [SerializeField] private Button _playerTabBtn;
        [SerializeField] private Button _staffTabBtn;
        [SerializeField] private Button _washBayTabBtn;
        [SerializeField] private Button _detailingTabBtn;
        [SerializeField] private Button _soapTabBtn;

        [Header("탭별 콘텐츠 패널")]
        [SerializeField] private GameObject _playerContent;
        [SerializeField] private GameObject _staffContent;
        [SerializeField] private GameObject _washBayContent;
        [SerializeField] private GameObject _detailingContent;
        [SerializeField] private GameObject _soapContent;

        [Header("탭 색상")]
        [SerializeField] private Color _activeTabColor   = new Color(0.20f, 0.60f, 1.00f);
        [SerializeField] private Color _inactiveTabColor = new Color(0.50f, 0.50f, 0.50f);

        [Header("업그레이드 항목 (모든 탭 포함, 인스펙터에서 드래그)")]
        [SerializeField] private UpgradeItemUI[] _allItems;

        private void Start()
        {
            _playerTabBtn?.onClick.AddListener(ShowPlayerTab);
            _staffTabBtn?.onClick.AddListener(ShowStaffTab);
            _washBayTabBtn?.onClick.AddListener(ShowWashBayTab);
            _detailingTabBtn?.onClick.AddListener(ShowDetailingTab);
            _soapTabBtn?.onClick.AddListener(ShowSoapTab);
        }

        private void OnEnable()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradeChanged += RefreshAll;
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnMoneyChanged += OnMoneyChanged;

            ShowPlayerTab();
            RefreshAll();
        }

        private void OnDisable()
        {
            if (UpgradeManager.Instance != null)
                UpgradeManager.Instance.OnUpgradeChanged -= RefreshAll;
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
        }

        // ── 탭 전환 ───────────────────────────────────────────────

        public void ShowPlayerTab()    => SwitchTab(0);
        public void ShowStaffTab()     => SwitchTab(1);
        public void ShowWashBayTab()   => SwitchTab(2);
        public void ShowDetailingTab() => SwitchTab(3);
        public void ShowSoapTab()      => SwitchTab(4);

        private void SwitchTab(int index)
        {
            _playerContent?.SetActive(index == 0);
            _staffContent?.SetActive(index == 1);
            _washBayContent?.SetActive(index == 2);
            _detailingContent?.SetActive(index == 3);
            _soapContent?.SetActive(index == 4);

            SetTabColor(_playerTabBtn,    index == 0);
            SetTabColor(_staffTabBtn,     index == 1);
            SetTabColor(_washBayTabBtn,   index == 2);
            SetTabColor(_detailingTabBtn, index == 3);
            SetTabColor(_soapTabBtn,      index == 4);
        }

        private void SetTabColor(Button btn, bool isActive)
        {
            if (btn == null) return;
            var img = btn.GetComponent<Image>();
            if (img != null) img.color = isActive ? _activeTabColor : _inactiveTabColor;
        }

        // ── 갱신 ─────────────────────────────────────────────────

        public void RefreshAll()
        {
            foreach (var item in _allItems)
                item?.Refresh();
        }

        private void OnMoneyChanged(int _) => RefreshAll();
    }
}
