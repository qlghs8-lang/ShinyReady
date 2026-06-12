using UnityEngine;
using ShinyReady.Cleaning;
using ShinyReady.Currency;

namespace ShinyReady.UI
{
    /// <summary>
    /// 비누 상점 패널. SetActive(true)로 열고 닫는다.
    /// SoapInventoryManager의 AllSoaps를 기반으로 SoapShopItemUI를 동적 생성한다.
    /// </summary>
    public class SoapShopUI : MonoBehaviour
    {
        [Header("레이아웃")]
        [SerializeField] private Transform _itemContainer;
        [SerializeField] private SoapShopItemUI _itemPrefab;

        private SoapShopItemUI[] _items;

        private void OnEnable()
        {
            Build();

            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnMoneyChanged += OnMoneyChanged;
            if (SoapInventoryManager.Instance != null)
                SoapInventoryManager.Instance.OnSoapChanged += RefreshAll;
        }

        private void OnDisable()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
            if (SoapInventoryManager.Instance != null)
                SoapInventoryManager.Instance.OnSoapChanged -= RefreshAll;
        }

        // ── 빌드 & 갱신 ──────────────────────────────────────────────

        private void Build()
        {
            var mgr = SoapInventoryManager.Instance;
            if (mgr == null || _itemPrefab == null || _itemContainer == null) return;

            // 기존 아이템 전부 제거
            foreach (Transform child in _itemContainer)
                Destroy(child.gameObject);

            var soaps = mgr.AllSoaps;
            if (soaps == null || soaps.Length == 0) return;

            _items = new SoapShopItemUI[soaps.Length];
            for (int i = 0; i < soaps.Length; i++)
            {
                if (soaps[i] == null) continue;
                var item = Instantiate(_itemPrefab, _itemContainer);
                item.Setup(soaps[i]);
                _items[i] = item;
            }
        }

        private void RefreshAll()
        {
            if (_items == null) return;
            foreach (var item in _items)
                item?.Refresh();
        }

        private void OnMoneyChanged(int _) => RefreshAll();
    }
}
