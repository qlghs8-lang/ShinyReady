using System;
using UnityEngine;
using ShinyReady.Currency;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// 비누 인벤토리 싱글톤. 해금 상태와 현재 장착 비누를 관리한다.
    /// 씬에 하나만 배치할 것.
    /// </summary>
    public class SoapInventoryManager : MonoBehaviour
    {
        public static SoapInventoryManager Instance { get; private set; }

        [Tooltip("모든 비누 데이터 에셋. 인덱스 0은 Basic Soap (항상 해금).")]
        [SerializeField] private SoapData[] _allSoaps;

        public SoapData[] AllSoaps => _allSoaps;

        /// <summary>현재 장착된 비누. 구독자에게 washSpeedMultiplier, incomeMultiplier, detailingChanceBonus를 제공한다.</summary>
        public SoapData EquippedSoap { get; private set; }

        /// <summary>장착 또는 해금 변경 시 발생. SoapShopUI가 구독해 UI를 갱신한다.</summary>
        public event Action OnSoapChanged;

        private const string EQUIPPED_KEY    = "SoapInventory_Equipped";
        private const string UNLOCK_PREFIX   = "SoapInventory_Unlocked_";

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
            Load();
        }

        // ── 로드 ──────────────────────────────────────────────────────

        private void Load()
        {
            // 인덱스 0(Basic Soap)은 항상 해금 상태로 강제 저장
            if (_allSoaps != null && _allSoaps.Length > 0 && _allSoaps[0] != null)
                PlayerPrefs.SetInt(UNLOCK_PREFIX + 0, 1);

            int equippedIndex = PlayerPrefs.GetInt(EQUIPPED_KEY, 0);
            if (_allSoaps != null && equippedIndex >= 0 && equippedIndex < _allSoaps.Length)
                EquippedSoap = _allSoaps[equippedIndex];
            else if (_allSoaps != null && _allSoaps.Length > 0)
                EquippedSoap = _allSoaps[0];
        }

        // ── 공개 API ──────────────────────────────────────────────────

        public bool IsSoapUnlocked(SoapData soap)
        {
            int idx = GetIndex(soap);
            if (idx < 0) return false;
            return PlayerPrefs.GetInt(UNLOCK_PREFIX + idx, 0) == 1;
        }

        /// <summary>재화를 소모해 비누를 해금한다. 성공 시 true 반환.</summary>
        public bool TryUnlock(SoapData soap)
        {
            if (soap == null || IsSoapUnlocked(soap)) return false;
            if (CurrencyManager.Instance == null || !CurrencyManager.Instance.SpendMoney(soap.unlockCost)) return false;

            int idx = GetIndex(soap);
            if (idx < 0) return false;

            PlayerPrefs.SetInt(UNLOCK_PREFIX + idx, 1);
            PlayerPrefs.Save();
            OnSoapChanged?.Invoke();
            return true;
        }

        /// <summary>해금된 비누를 장착한다. 즉시 모든 구역에 적용된다.</summary>
        public void EquipSoap(SoapData soap)
        {
            if (soap == null || !IsSoapUnlocked(soap)) return;
            int idx = GetIndex(soap);
            if (idx < 0) return;

            EquippedSoap = soap;
            PlayerPrefs.SetInt(EQUIPPED_KEY, idx);
            PlayerPrefs.Save();
            OnSoapChanged?.Invoke();
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────

        private int GetIndex(SoapData soap)
        {
            if (_allSoaps == null || soap == null) return -1;
            for (int i = 0; i < _allSoaps.Length; i++)
                if (_allSoaps[i] == soap) return i;
            return -1;
        }
    }
}
