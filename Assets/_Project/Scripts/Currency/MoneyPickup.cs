using System.Collections;
using UnityEngine;

namespace ShinyReady.Currency
{
    /// <summary>
    /// 세차 완료 후 생성되는 돈 아이템.
    /// 스폰 시 위로 튀어올랐다 착지하고, 이후 자석 범위의 플레이어에게 빨려든다.
    /// </summary>
    public class MoneyPickup : MonoBehaviour
    {
        [Header("Value")]
        [SerializeField] private int _value = 10;

        [Header("Idle Animation")]
        [SerializeField] private float _bobSpeed = 2.5f;
        [SerializeField] private float _bobHeight = 0.15f;
        [SerializeField] private float _rotateSpeed = 90f;

        [Header("Spawn Pop")]
        [Tooltip("스폰 시 튀어오르는 최고 높이 (m)")]
        [SerializeField] private float _launchHeight = 1.2f;
        [Tooltip("팝 애니메이션 총 소요 시간 (초)")]
        [SerializeField] private float _launchDuration = 0.45f;
        [Tooltip("착지 후 XZ 방향 랜덤 이동 최대 거리 (m)")]
        [SerializeField] private float _launchSpread = 0.3f;

        [Header("Magnet")]
        [Tooltip("플레이어 감지 반경 (m)")]
        [SerializeField] private float _magnetRadius = 1.5f;
        [Tooltip("흡수 기본 속도 (m/s). 가까울수록 자동으로 가속됨")]
        [SerializeField] private float _attractSpeed = 8f;
        [Tooltip("이 거리 이하로 접근하면 즉시 수집")]
        [SerializeField] private float _collectDistance = 0.4f;

        [Header("Car Push")]
        [Tooltip("차량 충돌 감지 반경 (m). 차량과 겹쳤을 때만 밀려나도록 작게 설정")]
        [SerializeField] private float _pushDetectRadius = 0.6f;
        [Tooltip("밀려나는 속도 (m/s)")]
        [SerializeField] private float _pushSpeed = 3f;

        public void SetValue(int value) => _value = value;

        private Vector3 _originPos;
        private Transform _attractTarget;
        private bool _launching = true;

        private void Awake()
        {
            _originPos = transform.position;

            if (TryGetComponent<BoxCollider>(out var box))
                Destroy(box);

            // 기존 SphereCollider 제거 후 재생성: _magnetRadius 인스펙터 값이 항상 반영되도록
            foreach (var sc in GetComponents<SphereCollider>())
                Destroy(sc);

            var sphere = gameObject.AddComponent<SphereCollider>();
            sphere.isTrigger = true;
            sphere.radius = _magnetRadius;
        }

        private void Start()
        {
            StartCoroutine(LaunchCoroutine());
        }

        private void Update()
        {
            if (_launching) return;

            if (_attractTarget != null)
            {
                Attract();
                return;
            }

            PushFromObstacles();
            IdleAnimation();
        }

        // ── 스폰 팝 연출 ──────────────────────────────────────────
        private IEnumerator LaunchCoroutine()
        {
            Vector3 startPos = transform.position;

            // XZ 착지 목표: 스폰 위치에서 소폭 랜덤 이동
            Vector2 spread = UnityEngine.Random.insideUnitCircle * _launchSpread;
            Vector3 endPos = startPos + new Vector3(spread.x, 0f, spread.y);

            // 포물선 제어점: 정점
            Vector3 peakPos = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * _launchHeight;

            float elapsed = 0f;
            while (elapsed < _launchDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _launchDuration);

                // 2차 베지어: startPos → peakPos → endPos
                Vector3 a = Vector3.Lerp(startPos, peakPos, t);
                Vector3 b = Vector3.Lerp(peakPos, endPos, t);
                transform.position = Vector3.Lerp(a, b, t);

                yield return null;
            }

            transform.position = endPos;
            _originPos = endPos; // 착지 위치를 bob 기준점으로 확정
            _launching = false;
        }

        // ── 자석 흡수 ─────────────────────────────────────────────
        private void Attract()
        {
            Vector3 targetPos = _attractTarget.position;
            float dist = Vector3.Distance(transform.position, targetPos);

            if (dist <= _collectDistance)
            {
                CurrencyManager.Instance?.AddMoney(_value);
                Destroy(gameObject);
                return;
            }

            float t = 1f - Mathf.Clamp01(dist / _magnetRadius);
            float speed = _attractSpeed * (1f + t * 2f);
            transform.position = Vector3.MoveTowards(transform.position, targetPos, speed * Time.deltaTime);
        }

        // ── 차량 밀림 ─────────────────────────────────────────────
        private void PushFromObstacles()
        {
            Collider[] hits = Physics.OverlapSphere(_originPos, _pushDetectRadius);
            foreach (var hit in hits)
            {
                if (hit.isTrigger) continue;
                if (hit.transform == transform) continue;
                // Car 컴포넌트가 있는 오브젝트만 밀림 대상으로 한정
                if (hit.GetComponentInParent<ShinyReady.Car.Car>() == null) continue;

                Vector3 pushDir = _originPos - hit.bounds.center;
                pushDir.y = 0f;
                if (pushDir.sqrMagnitude < 0.001f) pushDir = Vector3.right;
                _originPos += pushDir.normalized * _pushSpeed * Time.deltaTime;
            }
        }

        // ── 대기 애니메이션 ───────────────────────────────────────
        private void IdleAnimation()
        {
            transform.position = _originPos + Vector3.up * (Mathf.Sin(Time.time * _bobSpeed) * _bobHeight);
            transform.Rotate(Vector3.up, _rotateSpeed * Time.deltaTime);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                _attractTarget = other.transform;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _attractTarget = null;
            _originPos = transform.position;
        }
    }
}
