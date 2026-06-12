using System;
using UnityEngine;

namespace ShinyReady.Currency
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        [Tooltip("현재 보유 금액 (인스펙터 실시간 확인용)")]
        public int totalMoney;

        public event Action<int> OnMoneyChanged;

        private const string MONEY_SAVE_KEY = "Currency_Money";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            totalMoney = PlayerPrefs.GetInt(MONEY_SAVE_KEY, 0);
        }

        public void AddMoney(int amount)
        {
            if (amount <= 0) return;
            totalMoney += amount;
            PlayerPrefs.SetInt(MONEY_SAVE_KEY, totalMoney);
            PlayerPrefs.Save();
            OnMoneyChanged?.Invoke(totalMoney);
        }

        public bool HasEnoughMoney(int amount) => totalMoney >= amount;

        public bool SpendMoney(int amount)
        {
            if (!HasEnoughMoney(amount)) return false;
            totalMoney -= amount;
            PlayerPrefs.SetInt(MONEY_SAVE_KEY, totalMoney);
            PlayerPrefs.Save();
            OnMoneyChanged?.Invoke(totalMoney);
            return true;
        }
    }
}
