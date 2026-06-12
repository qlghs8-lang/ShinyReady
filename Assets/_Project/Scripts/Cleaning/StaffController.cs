using UnityEngine;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// WashBay_02 등 자동화 구역에 고정 배치되는 아르바이트생 오브젝트에 부착.
    /// Tag: "Staff"  (인스펙터에서 직접 설정)
    /// WashingInteraction._assignedStaff 슬롯에 연결하면 세차 자동화가 활성화된다.
    /// 파티클 / 애니메이터는 선택 사항으로, 연결하지 않아도 동작에 문제없다.
    /// </summary>
    public class StaffController : MonoBehaviour
    {
        [Header("Visual Feedback")]
        [Tooltip("세차 중일 때 재생할 파티클 (없으면 무시)")]
        [SerializeField] private ParticleSystem _workingParticle;

        [Tooltip("세차 중/정지 전환에 쓸 Animator (없으면 무시)")]
        [SerializeField] private Animator _animator;

        [Tooltip("Animator의 bool 파라미터 이름 (기본: IsWorking)")]
        [SerializeField] private string _workingAnimParam = "IsWorking";

        // ── 업그레이드 확장용 공개 프로퍼티 ──────────────────────────────────
        /// <summary>현재 세차 중 여부 (WashingInteraction이 갱신)</summary>
        public bool IsWorking { get; private set; }

        // ── 외부 호출 ─────────────────────────────────────────────────────────

        /// <summary>
        /// WashingInteraction에서 세차 상태가 바뀔 때 호출.
        /// 동일 상태 중복 호출은 내부에서 무시한다.
        /// </summary>
        public void SetWorking(bool working)
        {
            if (IsWorking == working) return;
            IsWorking = working;

            UpdateParticle(working);
            UpdateAnimator(working);
        }

        // ── 내부 헬퍼 ─────────────────────────────────────────────────────────

        private void UpdateParticle(bool working)
        {
            if (_workingParticle == null) return;
            if (working) _workingParticle.Play();
            else         _workingParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        private void UpdateAnimator(bool working)
        {
            if (_animator == null || string.IsNullOrEmpty(_workingAnimParam)) return;
            _animator.SetBool(_workingAnimParam, working);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 씬 뷰에서 아르바이트생 위치 표시 (주황 구체)
            Gizmos.color = IsWorking
                ? new Color(1f, 0.6f, 0f, 0.9f)
                : new Color(1f, 0.6f, 0f, 0.35f);
            Gizmos.DrawSphere(transform.position + Vector3.up * 1.1f, 0.18f);
        }
#endif
    }
}
