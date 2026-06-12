using System;
using UnityEngine;
using ShinyReady.Player;
using ShinyReady.Cleaning;
using ShinyReady.Car;
using ShinyReady.Currency;

namespace ShinyReady.Upgrade
{
    /// <summary>
    /// 업그레이드 시스템 싱글톤. 씬에 하나만 배치할 것.
    /// 업그레이드 버튼 클릭 → TryUpgrade() → 재화 차감 → 수치 즉시 적용 → OnUpgradeChanged 발생
    /// </summary>
    public class UpgradeManager : MonoBehaviour
    {
        public static UpgradeManager Instance { get; private set; }

        // ── 업그레이드 데이터 ─────────────────────────────────────

        [Header("Player 업그레이드")]
        public UpgradeData playerMoveSpeed;
        public UpgradeData playerWorkSpeed;
        public UpgradeData playerStackCapacity;

        [Header("Staff 업그레이드")]
        public UpgradeData staffWorkSpeed;
        public UpgradeData staffTipChance;
        public UpgradeData staffTipAmount;

        [Header("WashBay 업그레이드")]
        public UpgradeData washBaySoapLimit;
        public UpgradeData washBayHighValueChance;

        [Header("Detailing 업그레이드 (requiredZoneLevel = 2 로 설정할 것)")]
        public UpgradeData detailingSpeed;
        public UpgradeData detailingIncome;
        public UpgradeData detailingEntryChance;

        // ── 씬 레퍼런스 ───────────────────────────────────────────

        [Header("씬 레퍼런스")]
        public PlayerController playerController;
        public PlayerSoapCarrier playerSoapCarrier;
        public WashingInteraction[] washingInteractions;
        public CarSpawner[] carSpawners;
        [Tooltip("씬의 DetailingZone 컴포넌트들 (광택 속도·수익 업그레이드 적용 대상)")]
        public ShinyReady.Cleaning.DetailingZone[] detailingZones;

        // ── 구역 해금 레벨 ────────────────────────────────────────

        private const string ZONE_LEVEL_KEY = "ZoneLevel";

        private int _zoneLevel;
        /// <summary>
        /// 현재 해금된 구역 레벨. UnlockZone이나 외부 시스템에서 증가시킨다.
        /// 변경 시 자동 저장 및 OnUpgradeChanged 발생.
        /// </summary>
        public int ZoneLevel
        {
            get => _zoneLevel;
            set
            {
                _zoneLevel = value;
                PlayerPrefs.SetInt(ZONE_LEVEL_KEY, _zoneLevel);
                PlayerPrefs.Save();
                OnUpgradeChanged?.Invoke();
            }
        }

        /// <summary>업그레이드 수치가 변경됐을 때 발생. UpgradePanel이 구독해 UI를 갱신한다.</summary>
        public event Action OnUpgradeChanged;

        // ── Unity 생명주기 ────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            LoadAll();
            ApplyAll();

            foreach (var spawner in carSpawners)
                if (spawner != null)
                    spawner.OnWashComplete += HandleWashComplete;
        }

        private void OnDestroy()
        {
            foreach (var spawner in carSpawners)
                if (spawner != null)
                    spawner.OnWashComplete -= HandleWashComplete;
        }

        // ── 로드 & 적용 ───────────────────────────────────────────

        private void LoadAll()
        {
            playerMoveSpeed?.Load();
            playerWorkSpeed?.Load();
            playerStackCapacity?.Load();
            staffWorkSpeed?.Load();
            staffTipChance?.Load();
            staffTipAmount?.Load();
            washBaySoapLimit?.Load();
            washBayHighValueChance?.Load();
            detailingSpeed?.Load();
            detailingIncome?.Load();
            detailingEntryChance?.Load();
            _zoneLevel = PlayerPrefs.GetInt(ZONE_LEVEL_KEY, 0);
        }

        public void ApplyAll()
        {
            ApplyPlayerMoveSpeed();
            ApplyPlayerWorkSpeed();
            ApplyPlayerStackCapacity();
            ApplyStaffWorkSpeed();
            ApplyWashBaySoapLimit();
            ApplyHighValueChance();
            ApplyDetailingSpeed();
            ApplyDetailingIncome();
            ApplyDetailingEntryChance();
        }

        private void ApplyPlayerMoveSpeed()
        {
            if (playerMoveSpeed == null || playerController == null) return;
            playerController.SetMoveSpeed(playerMoveSpeed.CurrentValue);
        }

        private void ApplyPlayerWorkSpeed()
        {
            if (playerWorkSpeed == null) return;
            foreach (var bay in washingInteractions)
                bay?.SetBaseWashSpeed(playerWorkSpeed.CurrentValue);
        }

        private void ApplyPlayerStackCapacity()
        {
            if (playerStackCapacity == null || playerSoapCarrier == null) return;
            playerSoapCarrier.SetMaxBoxes(Mathf.RoundToInt(playerStackCapacity.CurrentValue));
        }

        private void ApplyStaffWorkSpeed()
        {
            if (staffWorkSpeed == null) return;
            foreach (var bay in washingInteractions)
                if (bay != null) bay.StaffWashSpeedMultiplier = staffWorkSpeed.CurrentValue;
        }

        private void ApplyWashBaySoapLimit()
        {
            if (washBaySoapLimit == null) return;
            foreach (var bay in washingInteractions)
                bay?.SetMaxSoap(Mathf.RoundToInt(washBaySoapLimit.CurrentValue));
        }

        private void ApplyHighValueChance()
        {
            if (washBayHighValueChance == null) return;
            foreach (var spawner in carSpawners)
                spawner?.SetHighValueChance(washBayHighValueChance.CurrentValue);
        }

        private void ApplyDetailingSpeed()
        {
            if (detailingSpeed == null) return;
            foreach (var zone in detailingZones)
                zone?.SetBaseDetailingSpeed(detailingSpeed.CurrentValue);
        }

        private void ApplyDetailingIncome()
        {
            if (detailingIncome == null) return;
            foreach (var zone in detailingZones)
                zone?.SetIncomeMultiplier(detailingIncome.CurrentValue);
        }

        private void ApplyDetailingEntryChance()
        {
            if (detailingEntryChance == null) return;
            foreach (var spawner in carSpawners)
                spawner?.SetDetailingChance(detailingEntryChance.CurrentValue);
        }

        // ── 업그레이드 실행 ───────────────────────────────────────

        /// <summary>
        /// UI 버튼에서 호출. 조건 확인 → 재화 차감 → 레벨업 → 즉시 적용.
        /// </summary>
        /// <returns>업그레이드 성공 여부</returns>
        public bool TryUpgrade(UpgradeData data)
        {
            if (data == null || data.IsMaxLevel) return false;
            if (_zoneLevel < data.requiredZoneLevel) return false;
            if (!CurrencyManager.Instance.SpendMoney(data.UpgradeCost)) return false;

            data.TryLevelUp();

            if      (data == playerMoveSpeed)        ApplyPlayerMoveSpeed();
            else if (data == playerWorkSpeed)         ApplyPlayerWorkSpeed();
            else if (data == playerStackCapacity)     ApplyPlayerStackCapacity();
            else if (data == staffWorkSpeed)          ApplyStaffWorkSpeed();
            else if (data == washBaySoapLimit)        ApplyWashBaySoapLimit();
            else if (data == washBayHighValueChance)  ApplyHighValueChance();
            else if (data == detailingSpeed)          ApplyDetailingSpeed();
            else if (data == detailingIncome)         ApplyDetailingIncome();
            else if (data == detailingEntryChance)    ApplyDetailingEntryChance();
            // staffTipChance, staffTipAmount는 HandleWashComplete에서 런타임에 직접 읽음

            OnUpgradeChanged?.Invoke();
            return true;
        }

        // ── 팁 시스템 (Staff 업그레이드 연동) ────────────────────

        private void HandleWashComplete(ShinyReady.Car.Car car)
        {
            if (staffTipChance == null || staffTipAmount == null) return;
            if (staffTipChance.CurrentValue <= 0f) return;

            if (UnityEngine.Random.value < staffTipChance.CurrentValue)
            {
                int tip = Mathf.RoundToInt(staffTipAmount.CurrentValue);
                CurrencyManager.Instance.AddMoney(tip);
            }
        }
    }
}
