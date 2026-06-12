using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// WashBay World Space Canvas에 부착.
    /// 세차 진행률을 색상 변화 + 완료 팝업 연출로 표시한다.
    /// Canvas RenderMode = World Space, Width=200 Height=30 (Scale 0.01 → 2m x 0.3m)
    /// </summary>
    public class WashProgressUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Slider _progressSlider;
        [SerializeField] private Image _fillImage;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Colors")]
        [SerializeField] private Color _colorStart = new Color(1f, 0.22f, 0.08f);   // 빨강-오렌지
        [SerializeField] private Color _colorEnd   = new Color(0.08f, 0.95f, 0.25f); // 밝은 초록

        [Header("Complete Effect")]
        [SerializeField] private float _holdDuration = 0.12f;
        [SerializeField] private float _hideDuration = 0.22f;

        private Coroutine _activeCoroutine;
        private bool _visible;

        private void Awake()
        {
            transform.rotation = Quaternion.Euler(55f, 45f, 0f);
            _canvasGroup.alpha = 0f;
        }

        public void Show()
        {
            if (_visible) return;
            _visible = true;
            StopActive();
            _canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            if (!_visible) return;
            _visible = false;
            StopActive();
            _canvasGroup.alpha = 0f;
        }

        public void SetProgress(float t)
        {
            if (_progressSlider != null) _progressSlider.value = t;
            if (_fillImage      != null) _fillImage.color = Color.Lerp(_colorStart, _colorEnd, t);
        }

        /// <summary>세차 완료 시 팝업 효과 재생. 완료 후 onComplete 콜백 호출.</summary>
        public void PlayCompleteEffect(Action onComplete)
        {
            StopActive();
            _activeCoroutine = StartCoroutine(CompletePopRoutine(onComplete));
        }

        private IEnumerator CompletePopRoutine(Action onComplete)
        {
            // 100% 상태를 잠시 유지
            yield return new WaitForSeconds(_holdDuration);

            // 알파 페이드아웃 (EaseIn)
            float elapsed = 0f;
            while (elapsed < _hideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _hideDuration);
                _canvasGroup.alpha = Mathf.Lerp(1f, 0f, t * t);
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            _visible           = false;
            onComplete?.Invoke();
        }

        private void StopActive()
        {
            if (_activeCoroutine == null) return;
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }
    }
}
