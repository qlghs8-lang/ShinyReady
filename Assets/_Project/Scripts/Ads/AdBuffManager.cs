using System;
using UnityEngine;

namespace ShinyReady.Ads
{
    /// <summary>
    /// 광고 시청 보상 버프 싱글톤.
    /// ActivateBuff()를 광고 SDK 완료 콜백에서 호출하면 일정 시간 동안
    /// 세차 속도(WashSpeedMultiplier)와 수익(IncomeMultiplier)이 상승한다.
    /// WashingInteraction, CarSpawner, DetailingZone에서 읽어 간다.
    /// </summary>
    public class AdBuffManager : MonoBehaviour
    {
        public static AdBuffManager Instance { get; private set; }

        [Header("Buff Configuration")]
        [Tooltip("광고 시청 후 버프 지속 시간 (초)")]
        [SerializeField] private float _buffDuration = 180f;
        [Tooltip("버프 활성 시 세차 속도 배율")]
        [SerializeField] private float _washSpeedMultiplier = 1.5f;
        [Tooltip("버프 활성 시 수익 배율 (세차 + 광택)")]
        [SerializeField] private float _incomeMultiplier = 2f;

        private float _buffRemainingTime;

        public bool IsBuffActive => _buffRemainingTime > 0f;
        public float RemainingTime => _buffRemainingTime;
        public float BuffDuration => _buffDuration;

        /// <summary>버프가 꺼져 있으면 1f를 반환해 기존 로직에 영향을 주지 않는다.</summary>
        public float WashSpeedMultiplier => IsBuffActive ? _washSpeedMultiplier : 1f;

        /// <summary>버프가 꺼져 있으면 1f를 반환해 기존 로직에 영향을 주지 않는다.</summary>
        public float IncomeMultiplier => IsBuffActive ? _incomeMultiplier : 1f;

        /// <summary>버프 ON/OFF 전환 시 발생. UI가 구독해 아이콘·타이머를 갱신한다.</summary>
        public event Action<bool> OnBuffStateChanged;

        /// <summary>버프가 활성 상태인 매 프레임 발생. 잔여 시간을 UI에 표시할 때 사용.</summary>
        public event Action<float> OnBuffTimeUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Update()
        {
            if (!IsBuffActive) return;

            _buffRemainingTime -= Time.deltaTime;
            if (_buffRemainingTime <= 0f)
            {
                _buffRemainingTime = 0f;
                OnBuffStateChanged?.Invoke(false);
            }
            OnBuffTimeUpdated?.Invoke(_buffRemainingTime);
        }

        /// <summary>
        /// 광고 시청 완료 콜백에서 호출.
        /// 이미 버프가 활성 중이라면 지속 시간을 최대치로 갱신(중첩 아님).
        /// </summary>
        [ContextMenu("테스트: 광고 버프 활성화")]
        public void ActivateBuff()
        {
            bool wasActive = IsBuffActive;
            _buffRemainingTime = _buffDuration;
            if (!wasActive)
                OnBuffStateChanged?.Invoke(true);
        }
    }
}
