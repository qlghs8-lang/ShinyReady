using System.Collections;
using UnityEngine;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// 세제 전용 투입구(Kiosk). SphereCollider(IsTrigger)와 함께 사용.
    /// 플레이어가 범위 내에 머무는 동안 고속 루프로 세제를 WashBay에 주입하며,
    /// 각 주입마다 플레이어 등 뒤에서 포물선 발사체가 날아가는 연출을 재생한다.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public class SoapRefillPoint : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WashingInteraction _washBay;

        [Header("Refill Settings")]
        [Tooltip("세제 1회 주입 간격 (초). 0.05~0.1 권장")]
        [SerializeField] private float _refillInterval = 0.07f;

        [Header("Projectile Settings")]
        [Tooltip("발사체가 투입구까지 날아가는 시간 (초)")]
        [SerializeField] private float _projectileDuration = 0.35f;
        [Tooltip("포물선 최대 높이 (미터)")]
        [SerializeField] private float _arcHeight = 1.5f;
        [Tooltip("발사체 구체 지름 (미터)")]
        [SerializeField] private float _projectileSize = 0.18f;

        private PlayerSoapCarrier _playerCarrier;
        private bool _playerInside;
        private Coroutine _refillCoroutine;

        private void Awake()
        {
            // 인스펙터에서 radius를 1.75(1.5~2m)로 설정. 런타임 강제 변경은 하지 않음.
            GetComponent<SphereCollider>().isTrigger = true;

            if (_washBay == null)
                Debug.LogWarning($"[SoapRefillPoint] {gameObject.name}: _washBay가 null입니다. 인스펙터에서 같은 CarWashPoints 내 WashingInteraction을 연결해주세요.");
        }

        private void OnEnable()
        {
            // 런타임 동적 활성화 시 플레이어 감지 상태를 초기화한다.
            _playerInside = false;
            _playerCarrier = null;
            if (_refillCoroutine != null)
            {
                StopCoroutine(_refillCoroutine);
                _refillCoroutine = null;
            }
        }

        // 꽉 찬 WashBay가 세제를 소모한 뒤 루프를 재개하는 케이스 처리
        private void Update()
        {
            if (_washBay == null) return;
            if (!_playerInside || _playerCarrier == null || _refillCoroutine != null) return;
            if (_playerCarrier.HasSoap && _washBay.CurrentSoap < _washBay.MaxSoap)
                StartRefill();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerCarrier = other.GetComponent<PlayerSoapCarrier>();
            _playerInside = true;

            if (_playerCarrier != null && _playerCarrier.HasSoap)
                StartRefill();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _playerInside = false;
            _playerCarrier = null;
            StopRefill();
        }

        private void StartRefill()
        {
            if (_refillCoroutine != null)
                StopCoroutine(_refillCoroutine);
            _refillCoroutine = StartCoroutine(RefillLoop());
        }

        private void StopRefill()
        {
            if (_refillCoroutine == null) return;
            StopCoroutine(_refillCoroutine);
            _refillCoroutine = null;
        }

        private IEnumerator RefillLoop()
        {
            var wait = new WaitForSeconds(_refillInterval);

            while (_playerInside && _playerCarrier != null
                   && _playerCarrier.HasSoap
                   && _washBay.CurrentSoap < _washBay.MaxSoap)
            {
                SoapData soapData = _playerCarrier.CarriedSoapData;
                Vector3 launchPos = _playerCarrier.GetTopBoxWorldPosition();

                _playerCarrier.RemoveOne();

                int amount = soapData != null ? soapData.soapAmountPerBox : 1;
                _washBay.AddSoap(amount, soapData);

                LaunchProjectile(launchPos, soapData?.boxColor ?? new Color(0.3f, 0.7f, 1f));

                yield return wait;
            }

            _refillCoroutine = null;
        }

        private void LaunchProjectile(Vector3 from, Color color)
        {
            var proj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(proj.GetComponent<Collider>());
            proj.transform.position = from;
            proj.transform.localScale = Vector3.one * _projectileSize;

            var rend = proj.GetComponent<Renderer>();
            rend.material = new Material(rend.sharedMaterial) { color = color };

            StartCoroutine(ArcFlight(proj, from, transform.position));
        }

        private IEnumerator ArcFlight(GameObject proj, Vector3 start, Vector3 end)
        {
            float elapsed = 0f;
            while (elapsed < _projectileDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / _projectileDuration);
                float arc = Mathf.Sin(t * Mathf.PI) * _arcHeight;
                proj.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * arc;
                yield return null;
            }
            Destroy(proj);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            var col = GetComponent<SphereCollider>();
            float r = col != null ? col.radius : 1.75f;
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f);
            Gizmos.DrawSphere(transform.position, r);
            Gizmos.color = new Color(1f, 0.5f, 0f, 1f);
            Gizmos.DrawWireSphere(transform.position, r);
        }
#endif
    }
}
