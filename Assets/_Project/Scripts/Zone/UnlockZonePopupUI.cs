using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ShinyReady.Zone
{
    /// <summary>
    /// Screen Space - Overlay 캔버스 하위에 배치하는 해금 확인 팝업 패널.
    /// UnlockZone이 Show/Hide를 호출하며, 버튼 이벤트는 콜백으로 전달된다.
    /// </summary>
    public class UnlockZonePopupUI : MonoBehaviour
    {
        [Header("텍스트")]
        [SerializeField] private TMP_Text _costText;
        [SerializeField] private TMP_Text _insufficientText;

        [Header("버튼")]
        [SerializeField] private Button _confirmButton;
        [SerializeField] private Button _cancelButton;

        private Action _onConfirm;
        private Action _onCancel;
        private Coroutine _flashCoroutine;

        private void Awake()
        {
            _confirmButton.onClick.AddListener(() => _onConfirm?.Invoke());
            _cancelButton.onClick.AddListener(() => _onCancel?.Invoke());

            gameObject.SetActive(false);
            if (_insufficientText != null)
                _insufficientText.gameObject.SetActive(false);
        }

        public void Show(int cost, Action onConfirm, Action onCancel)
        {
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            _costText.text = $"$ {cost:N0}";
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (_flashCoroutine != null)
            {
                StopCoroutine(_flashCoroutine);
                _flashCoroutine = null;
            }
            if (_insufficientText != null)
                _insufficientText.gameObject.SetActive(false);

            _onConfirm = null;
            _onCancel = null;
            gameObject.SetActive(false);
        }

        // 돈 부족 시 텍스트를 잠깐 표시했다가 자동으로 숨긴다.
        // WaitForSecondsRealtime: timeScale=0 상태에서도 정상 동작.
        public void FlashInsufficientFunds()
        {
            if (_insufficientText == null) return;
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            _insufficientText.gameObject.SetActive(true);
            yield return new WaitForSecondsRealtime(1.5f);
            _insufficientText.gameObject.SetActive(false);
            _flashCoroutine = null;
        }
    }
}
