using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShinyReady.Ads;

namespace ShinyReady.UI
{
    /// <summary>
    /// 광고 버프 남은 시간 HUD.
    /// AdBuffManager의 이벤트를 구독해 버프 활성 시 표시, 만료 시 숨긴다.
    ///
    /// ─ 인스펙터 세팅 가이드 ──────────────────────────────
    /// 1. Canvas 하위에 버프 HUD 루트 GameObject를 만들고 _buffRoot에 연결
    /// 2. _timerText : "2:45" 형태의 남은 시간 텍스트
    /// 3. _fillImage : 버프 게이지 Image (Image Type = Filled, Fill Method = Radial360 또는 Horizontal)
    ///                 선택 사항 — 연결 안 해도 동작함
    /// ─────────────────────────────────────────────────────
    /// </summary>
    public class AdBuffTimerUI : MonoBehaviour
    {
        [Header("Panel Root")]
        [Tooltip("버프 HUD 전체 루트. 버프 OFF 시 숨긴다.")]
        [SerializeField] private GameObject _buffRoot;

        [Header("Text")]
        [Tooltip("남은 시간 표시 텍스트 (예: 2:45)")]
        [SerializeField] private TMP_Text _timerText;

        [Header("Fill Gauge (선택)")]
        [Tooltip("남은 시간 비율을 표시하는 Image. Image Type = Filled 로 설정.")]
        [SerializeField] private Image _fillImage;

        private void Start()
        {
            if (AdBuffManager.Instance != null)
            {
                AdBuffManager.Instance.OnBuffStateChanged += OnBuffStateChanged;
                AdBuffManager.Instance.OnBuffTimeUpdated  += OnBuffTimeUpdated;
            }

            // 초기 상태: 버프가 없으면 숨김
            SetVisible(AdBuffManager.Instance != null && AdBuffManager.Instance.IsBuffActive);
        }

        private void OnDestroy()
        {
            if (AdBuffManager.Instance != null)
            {
                AdBuffManager.Instance.OnBuffStateChanged -= OnBuffStateChanged;
                AdBuffManager.Instance.OnBuffTimeUpdated  -= OnBuffTimeUpdated;
            }
        }

        // ── 이벤트 핸들러 ────────────────────────────────────────

        private void OnBuffStateChanged(bool isActive)
        {
            SetVisible(isActive);
        }

        private void OnBuffTimeUpdated(float remaining)
        {
            if (_timerText != null)
                _timerText.text = FormatTime(remaining);

            if (_fillImage != null && AdBuffManager.Instance != null)
                _fillImage.fillAmount = remaining / AdBuffManager.Instance.BuffDuration;
        }

        // ── 유틸 ────────────────────────────────────────────────

        private void SetVisible(bool visible)
        {
            if (_buffRoot != null)
                _buffRoot.SetActive(visible);
        }

        private static string FormatTime(float seconds)
        {
            int m = Mathf.FloorToInt(seconds / 60f);
            int s = Mathf.FloorToInt(seconds % 60f);
            return $"{m}:{s:D2}";
        }
    }
}
