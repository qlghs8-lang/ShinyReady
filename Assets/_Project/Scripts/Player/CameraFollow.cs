using UnityEngine;

namespace ShinyReady.Player
{
    /// <summary>
    /// 메인 카메라에 부착. SmoothDamp로 플레이어를 부드럽게 추적한다.
    /// Rotation은 인스펙터에서 설정한 값을 Start에서 고정한다.
    /// </summary>
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;

        [Header("Offset (표준 스케일 기준)")]
        // 황금 수치: Y=15m 위, Z=8m 뒤
        [SerializeField] private Vector3 _offset = new Vector3(0f, 15f, -8f);

        [Header("Smoothing")]
        [Tooltip("값이 클수록 느리게 따라옴. 권장: 0.08 ~ 0.20")]
        [SerializeField] private float _smoothTime = 0.12f;

        private Vector3 _velocity;

        // 사무실 진입 시 OfficeTrigger가 일시적으로 덮어쓰는 오프셋
        private Vector3? _overrideOffset;
        private float?   _overrideSmoothTime;

        /// <summary>사무실 진입 시 OfficeTrigger에서 호출. 카메라를 오피스 뷰로 전환.</summary>
        public void SetOffsetOverride(Vector3 offset, float smoothTime = 0.25f)
        {
            _overrideOffset     = offset;
            _overrideSmoothTime = smoothTime;
        }

        /// <summary>사무실 이탈 시 OfficeTrigger에서 호출. 기본 카메라 뷰로 복귀.</summary>
        public void ClearOffsetOverride()
        {
            _overrideOffset     = null;
            _overrideSmoothTime = null;
        }

        private void Start()
        {
            if (_target == null)
            {
                Debug.LogWarning("[CameraFollow] Target이 설정되지 않았습니다.");
                return;
            }

            // 시작 시 카메라를 즉시 목표 위치로 스냅
            transform.position = _target.position + _offset;
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            Vector3 activeOffset     = _overrideOffset     ?? _offset;
            float   activeSmoothTime = _overrideSmoothTime ?? _smoothTime;

            Vector3 desiredPos = _target.position + activeOffset;
            transform.position = Vector3.SmoothDamp(
                transform.position,
                desiredPos,
                ref _velocity,
                activeSmoothTime
            );
        }
    }
}
