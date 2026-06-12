using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShinyReady.Ads;

namespace ShinyReady.UI
{
    /// <summary>
    /// 오프라인 방치 보상 팝업 UI.
    ///
    /// ─ 인스펙터 세팅 가이드 ─────────────────────────────
    /// 1. Canvas 하위에 팝업 루트 GameObject를 만들고 _popupRoot에 연결한다.
    /// 2. 텍스트 필드: _offlineTimeText / _rewardAmountText / _tripleRewardText
    /// 3. 버튼: _claimButton(일반 수령) / _watchAdButton(광고 3배) / _closeButton(닫기)
    /// 4. OfflineRewardManager GameObject의 _popup 필드에 이 컴포넌트를 연결한다.
    /// ────────────────────────────────────────────────────
    /// </summary>
    public class OfflineRewardPopup : MonoBehaviour
    {
        [Header("Panel Root")]
        [Tooltip("팝업 전체 루트 GameObject. Show/Hide 시 SetActive로 제어된다.")]
        [SerializeField] private GameObject _popupRoot;

        [Header("Text References")]
        [Tooltip("방치 시간 표시 (예: '1시간 30분')")]
        [SerializeField] private TextMeshProUGUI _offlineTimeText;
        [Tooltip("일반 수령 금액 표시 (예: '$1,200')")]
        [SerializeField] private TextMeshProUGUI _rewardAmountText;
        [Tooltip("광고 3배 수령 금액 표시 (예: '$3,600')")]
        [SerializeField] private TextMeshProUGUI _tripleRewardText;

        [Header("Button References")]
        [Tooltip("일반 수령 버튼")]
        [SerializeField] private Button _claimButton;
        [Tooltip("광고 시청 후 3배 수령 버튼")]
        [SerializeField] private Button _watchAdButton;
        [Tooltip("팝업 닫기(보상 포기) 버튼. 필요 없으면 연결하지 않아도 된다.")]
        [SerializeField] private Button _closeButton;

        private void Awake()
        {
            _claimButton?.onClick.AddListener(OnClaimClicked);
            _watchAdButton?.onClick.AddListener(OnWatchAdClicked);
            _closeButton?.onClick.AddListener(Hide);

            if (_popupRoot != null)
                _popupRoot.SetActive(false);
        }

        // ── 공개 API ─────────────────────────────────────────────

        /// <summary>
        /// OfflineRewardManager에서 호출. 팝업을 열고 보상 정보를 표시한다.
        /// </summary>
        /// <param name="baseReward">일반 수령 금액</param>
        /// <param name="offlineSeconds">방치 시간 (초)</param>
        public void Show(int baseReward, float offlineSeconds)
        {
            if (_offlineTimeText != null)
                _offlineTimeText.text = FormatTime(offlineSeconds);

            if (_rewardAmountText != null)
                _rewardAmountText.text = $"${baseReward:N0}";

            if (_tripleRewardText != null)
                _tripleRewardText.text = $"${baseReward * 3:N0}";

            if (_popupRoot != null)
                _popupRoot.SetActive(true);
        }

        // ── 버튼 핸들러 ──────────────────────────────────────────

        private void OnClaimClicked()
        {
            OfflineRewardManager.Instance?.ClaimReward();
            Hide();
        }

        private void OnWatchAdClicked()
        {
            // ── 광고 SDK 연동 포인트 ──────────────────────────────
            // 실제 서비스 시에는 아래 직접 호출을 제거하고,
            // 광고 SDK의 성공 콜백에서 OfflineRewardManager.Instance?.ClaimRewardWithAd()를 호출한다.
            // 개발 단계에서는 광고 없이 3배 지급.
            OfflineRewardManager.Instance?.ClaimRewardWithAd();
            Hide();
        }

        private void Hide()
        {
            if (_popupRoot != null)
                _popupRoot.SetActive(false);
        }

        // ── 유틸 ────────────────────────────────────────────────

        private static string FormatTime(float seconds)
        {
            int h = Mathf.FloorToInt(seconds / 3600f);
            int m = Mathf.FloorToInt((seconds % 3600f) / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);

            if (h > 0) return $"{h}시간 {m}분";
            if (m > 0) return $"{m}분 {s}초";
            return $"{s}초";
        }
    }
}
