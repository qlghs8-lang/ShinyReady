using System;
using System.Globalization;
using UnityEngine;
using ShinyReady.Currency;
using ShinyReady.Upgrade;
using ShinyReady.UI;

namespace ShinyReady.Ads
{
    /// <summary>
    /// 오프라인 방치 보상 싱글톤.
    /// 앱 종료/일시정지 시 시각을 PlayerPrefs에 저장하고,
    /// 다음 실행 시 방치 시간을 계산해 팝업으로 보상을 제시한다.
    ///
    /// 수익 계산식:
    ///   incomePerSecond = washSpeed * staffSpeedMult * baseMoneyPerWash * automatedBayCount
    ///   pendingReward = Min(offlineSeconds, maxOfflineSeconds) * incomePerSecond * offlineEfficiency
    /// </summary>
    public class OfflineRewardManager : MonoBehaviour
    {
        public static OfflineRewardManager Instance { get; private set; }

        private const string LAST_SESSION_KEY = "OfflineEarnings_LastSessionTime";

        [Header("Offline Reward Settings")]
        [Tooltip("최대 오프라인 보상 적용 시간 (초). 기본 7200 = 2시간 캡")]
        [SerializeField] private float _maxOfflineSeconds = 7200f;
        [Tooltip("온라인 대비 오프라인 효율 (0~1). 0.3 = 온라인 수익의 30%.\n" +
                 "세제 자동 보충 불가, 비효율 대기 등을 감안한 패널티.")]
        [SerializeField] [Range(0.01f, 1f)] private float _offlineEfficiency = 0.3f;
        [Tooltip("팝업 표시 최소 보상 금액. 0 이상이면 표시.\n" +
                 "개발·테스트 중에는 1로 설정해 1초 방치도 팝업이 뜨게 한다.")]
        [SerializeField] private int _minRewardToShowPopup = 1;

        [Header("Base Income Reference")]
        [Tooltip("세차 1회당 기본 수익. CarSpawner._moneyPerWash 와 동일하게 맞출 것.")]
        [SerializeField] private int _baseMoneyPerWash = 10;
        [Tooltip("초당 세차 기본 진행도. WashingInteraction._baseWashSpeed 와 동일하게 맞출 것.")]
        [SerializeField] private float _baseWashSpeed = 0.33f;
        [Tooltip("스태프 기본 속도 배율. UpgradeManager를 못 읽을 때의 폴백 값.")]
        [SerializeField] private float _fallbackStaffSpeedMult = 0.6f;

        [Header("References")]
        [SerializeField] private OfflineRewardPopup _popup;

        private int _pendingReward;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            CalculateAndShowOfflineReward();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus) SaveSessionTime();
        }

        private void OnApplicationQuit()
        {
            SaveSessionTime();
        }

        // ── 세션 시간 저장 ──────────────────────────────────────────

        private void SaveSessionTime()
        {
            PlayerPrefs.SetString(LAST_SESSION_KEY, DateTime.UtcNow.ToString("O"));
            PlayerPrefs.Save();
        }

        // ── 오프라인 보상 계산 ──────────────────────────────────────

        private void CalculateAndShowOfflineReward()
        {
            string savedTimeStr = PlayerPrefs.GetString(LAST_SESSION_KEY, string.Empty);

            // 최초 실행: 기록만 남기고 종료
            if (string.IsNullOrEmpty(savedTimeStr))
            {
                SaveSessionTime();
                return;
            }

            if (!DateTime.TryParse(savedTimeStr, null, DateTimeStyles.RoundtripKind, out DateTime lastSession))
            {
                Debug.LogWarning("[OfflineRewardManager] 저장된 세션 시간 파싱 실패. 초기화합니다.");
                SaveSessionTime();
                return;
            }

            float offlineSeconds = (float)(DateTime.UtcNow - lastSession).TotalSeconds;
            offlineSeconds = Mathf.Clamp(offlineSeconds, 0f, _maxOfflineSeconds);

            // 현재 세션 시작 시각을 바로 기록
            SaveSessionTime();

            float incomePerSec = CalculateIncomePerSecond();
            _pendingReward = Mathf.RoundToInt(offlineSeconds * incomePerSec * _offlineEfficiency);

            if (_pendingReward >= _minRewardToShowPopup && _popup != null)
                _popup.Show(_pendingReward, offlineSeconds);
        }

        /// <summary>
        /// 업그레이드 수치를 기반으로 자동화 베이 1초당 수익을 계산한다.
        /// UpgradeManager가 없으면 직렬화 필드의 기본값을 사용한다.
        /// </summary>
        private float CalculateIncomePerSecond()
        {
            float staffSpeedMult = _fallbackStaffSpeedMult;
            float washSpeed = _baseWashSpeed;
            int automatedBayCount = 1;

            if (UpgradeManager.Instance != null)
            {
                if (UpgradeManager.Instance.staffWorkSpeed != null)
                    staffSpeedMult = UpgradeManager.Instance.staffWorkSpeed.CurrentValue;

                if (UpgradeManager.Instance.playerWorkSpeed != null)
                    washSpeed = UpgradeManager.Instance.playerWorkSpeed.CurrentValue;

                // 자동화 베이 수 집계
                var bays = UpgradeManager.Instance.washingInteractions;
                if (bays != null)
                {
                    int count = 0;
                    foreach (var bay in bays)
                        if (bay != null && bay.IsAutomated) count++;
                    if (count > 0) automatedBayCount = count;
                }
            }

            // 세차 진행도/초 * 스태프 배율 = 완료 횟수/초
            // 완료 횟수/초 * 단가 * 베이 수 = 초당 수익
            float washesPerSecond = washSpeed * staffSpeedMult;
            return washesPerSecond * _baseMoneyPerWash * automatedBayCount;
        }

        // ── 수령 API (팝업 버튼에서 호출) ───────────────────────────

        /// <summary>일반 수령 (1배). 팝업 일반 버튼에서 호출.</summary>
        public void ClaimReward()
        {
            if (_pendingReward <= 0) return;
            CurrencyManager.Instance?.AddMoney(_pendingReward);
            Debug.Log($"[OfflineRewardManager] 오프라인 보상 수령: ${_pendingReward}");
            _pendingReward = 0;
        }

        /// <summary>광고 시청 후 3배 수령. 팝업 광고 버튼 콜백에서 호출.</summary>
        public void ClaimRewardWithAd()
        {
            if (_pendingReward <= 0) return;
            int tripleReward = _pendingReward * 3;
            CurrencyManager.Instance?.AddMoney(tripleReward);
            Debug.Log($"[OfflineRewardManager] 광고 3배 오프라인 보상 수령: ${tripleReward}");
            _pendingReward = 0;
        }

        /// <summary>현재 대기 중인 보상 금액 (팝업 외부에서 참조용).</summary>
        public int PendingReward => _pendingReward;
    }
}
