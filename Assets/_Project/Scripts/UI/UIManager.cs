using UnityEngine;
using TMPro;
using ShinyReady.Currency;

namespace ShinyReady.UI
{
    public class UIManager : MonoBehaviour
    {
        [SerializeField] private TMP_Text _moneyText;

        // Start()에서 구독 — Awake() 순서 문제로 OnEnable() 시점엔 Instance가 null일 수 있음
        private void Start()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnMoneyChanged += RefreshDisplay;

            RefreshDisplay(CurrencyManager.Instance != null ? CurrencyManager.Instance.totalMoney : 0);
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
                CurrencyManager.Instance.OnMoneyChanged -= RefreshDisplay;
        }

        private void RefreshDisplay(int amount)
        {
            if (_moneyText == null) return;
            _moneyText.text = $"{amount:N0}";
        }
    }
}
