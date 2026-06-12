using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using ShinyReady.Upgrade;
using ShinyReady.Currency;
using ShinyReady.Audio;

namespace ShinyReady.UI
{
    /// <summary>
    /// 업그레이드 항목 하나의 UI 행. Inspector에서 _upgradeData를 연결하면 된다.
    /// UpgradePanel.RefreshAll() 호출 시 Refresh()가 실행된다.
    /// </summary>
    public class UpgradeItemUI : MonoBehaviour
    {
        [Header("연결할 업그레이드 데이터 (인스펙터에서 지정)")]
        [SerializeField] private UpgradeData _upgradeData;

        [Header("UI 컴포넌트")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _effectText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TextMeshProUGUI _buttonText;
        [SerializeField] private Image _buttonImage;

        [Header("버튼 색상")]
        [SerializeField] private Color _maxLevelColor = new Color(0.40f, 0.55f, 0.90f);
        [SerializeField] private Color _lockedColor   = new Color(0.45f, 0.45f, 0.45f);

        [Header("피드백 이펙트")]
        [Tooltip("성공 시 재생할 파티클. UpgradeItem 하위에 배치 후 연결. (없으면 생략)")]
        [SerializeField] private ParticleSystem _successParticle;

        private float _lastClickTime = float.MinValue;

        private void Awake()
        {
            _upgradeButton?.onClick.AddListener(OnUpgradeClick);
        }

        public void Refresh()
        {
            if (_upgradeData == null) return;

            // 진행 중인 애니메이션을 끝값(scale=1)으로 강제 완료해 버튼이 커진 채 굳는 문제 방지
            if (_upgradeButton != null)
            {
                _upgradeButton.transform.DOComplete();
                _upgradeButton.transform.localScale = Vector3.one;
            }

            bool isLocked = UpgradeManager.Instance != null
                         && UpgradeManager.Instance.ZoneLevel < _upgradeData.requiredZoneLevel;
            bool isMax = _upgradeData.IsMaxLevel;

            _nameText?.SetText(_upgradeData.upgradeName);
            _levelText?.SetText($"Lv. {_upgradeData.CurrentLevel} / {_upgradeData.maxLevel}");

            if (isMax)
                _effectText?.SetText($"최대 달성  {FormatValue(_upgradeData.CurrentValue)}");
            else if (isLocked)
                _effectText?.SetText("잠금 — 구역 해금 필요");
            else
                _effectText?.SetText($"{FormatValue(_upgradeData.CurrentValue)}  →  {FormatValue(_upgradeData.NextValue)}");

            if (isMax || isLocked)
                _costText?.SetText("");
            else
                _costText?.SetText($"$ {_upgradeData.UpgradeCost}");

            // max/locked일 때만 버튼 비활성화.
            // 돈 부족 시에도 클릭을 받아야 fail 피드백(흔들기·사운드)이 동작한다.
            if (_upgradeButton != null) _upgradeButton.interactable = !isMax && !isLocked;

            if (_buttonImage != null)
                _buttonImage.color = isMax    ? _maxLevelColor :
                                     isLocked ? _lockedColor   :
                                                Color.white;

            if (_buttonText != null)
            {
                if (isMax)         _buttonText.text = "MAX";
                else if (isLocked) _buttonText.text = "LOCK";
                else               _buttonText.text = "UPGRADE!!";
            }

        }

        private string FormatValue(float v)
        {
            return _upgradeData.unit == "%"
                ? $"{v * 100f:0.#}%"
                : $"{v:0.##}{_upgradeData.unit}";
        }

        // ── 클릭 처리 ─────────────────────────────────────────────

        private void OnUpgradeClick()
        {
            if (_upgradeData == null || UpgradeManager.Instance == null) return;
            if (Time.unscaledTime - _lastClickTime < 0.35f) return;
            _lastClickTime = Time.unscaledTime;

            bool success = UpgradeManager.Instance.TryUpgrade(_upgradeData);
            if (success) PlaySuccessEffects();
            else         PlayFailEffects();
        }

        // ── 성공 피드백 ───────────────────────────────────────────

        private void PlaySuccessEffects()
        {
            // 버튼 PunchScale: 커졌다가 원래 크기로
            if (_upgradeButton != null)
            {
                _upgradeButton.transform.DOComplete();
                _upgradeButton.transform.localScale = Vector3.one;
                _upgradeButton.transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.5f)
                    .OnComplete(() => { if (_upgradeButton != null) _upgradeButton.transform.localScale = Vector3.one; });
            }

            // 레벨 텍스트: 위로 튀어오르기 + 골드 반짝임
            if (_levelText != null)
            {
                Color originalColor = _levelText.color;
                _levelText.transform.DOKill();
                _levelText.transform.DOPunchPosition(Vector3.up * 10f, 0.3f, 5, 0.5f);
                DOTween.To(() => _levelText.color, x => _levelText.color = x, Color.yellow, 0.12f)
                       .SetLoops(2, LoopType.Yoyo)
                       .OnComplete(() => _levelText.color = originalColor);
            }

            if (_successParticle != null) _successParticle.Play();
            SoundManager.Instance?.PlaySuccess();
        }

        // ── 실패 피드백 ───────────────────────────────────────────

        private void PlayFailEffects()
        {
            // 버튼 가로 흔들기: 돈 부족 느낌
            if (_upgradeButton != null)
            {
                _upgradeButton.transform.DOKill();
                _upgradeButton.transform.DOShakePosition(0.3f, new Vector3(10f, 0f, 0f), 20, 0f);
            }

            // 버튼 색상 빨간 강조 후 원래 색으로 복원
            if (_buttonImage != null)
            {
                Color original = _buttonImage.color;
                _buttonImage.DOKill();
                _buttonImage.DOColor(Color.red, 0.1f)
                            .SetLoops(2, LoopType.Yoyo)
                            .OnComplete(() => _buttonImage.color = original);
            }

            SoundManager.Instance?.PlayFail();
        }
    }
}
