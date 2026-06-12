using System.Collections.Generic;
using UnityEngine;
using ShinyReady.Audio;
using ShinyReady.Currency;
using CarCls = ShinyReady.Car.Car;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// 고급 광택(Detailing) 구역. BoxCollider(IsTrigger)와 함께 사용.
    /// CarSpawner에서 세차 완료 차량을 확률적으로 ReceiveCar()로 전달받는다.
    /// 대기 줄(_queueWaypoints)을 지원하며, 플레이어가 구역 내에 있을 때만 진행된다.
    /// 완료 시 재화(일반 세차보다 높음)와 Material Smoothness 광택 효과를 제공한다.
    /// </summary>
    public class DetailingZone : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WashProgressUI _progressUI;
        [Tooltip("광택 구역 진입 직전 방향 정렬용 웨이포인트. null이면 ParkPoint로 직행.")]
        [SerializeField] private Transform _entryPoint;
        [Tooltip("차량이 광택 작업 중 주차할 위치")]
        [SerializeField] private Transform _parkPoint;
        [Tooltip("광택 완료 후 차량이 이동할 출구")]
        [SerializeField] private Transform _exitPoint;
        [Tooltip("획득 금액이 $9 이하일 때 스폰되는 코인 프리팹")]
        [SerializeField] private GameObject _coinPrefab;
        [Tooltip("획득 금액이 $10~$19일 때 스폰되는 cash 프리팹")]
        [SerializeField] private GameObject _cashPrefab;
        [Tooltip("획득 금액이 $20 이상일 때 스폰되는 goldbar 프리팹")]
        [SerializeField] private GameObject _goldbarPrefab;

        [Header("Queue Settings")]
        [Tooltip("최대 대기 차량 수")]
        [SerializeField] private int _maxQueueSize = 4;
        [Tooltip("대기 슬롯 위치. 인덱스 0 = 입구에서 가장 가까운 슬롯, 마지막 = 맨 뒤")]
        [SerializeField] private Transform[] _queueWaypoints;

        [Header("Detailing Settings")]
        [Tooltip("초당 기본 진행도 (일반 세차 0.33 보다 낮게 설정)")]
        [SerializeField] private float _baseDetailingSpeed = 0.15f;
        [Tooltip("광택 완료 시 지급 금액 (일반 세차보다 훨씬 높게 설정)")]
        [SerializeField] private int _moneyPerDetailing = 50;
        [SerializeField] private int _moneySpawnCount = 8;
        [SerializeField] private float _moneySpawnHeight = 0.5f;
        [Tooltip("코인 스폰 최소 반경 (m). 차량 크기보다 크게 설정해 차량과 겹침 방지")]
        [SerializeField] private float _minScatterRadius = 2.0f;
        [Tooltip("코인 스폰 최대 반경 (m)")]
        [SerializeField] private float _moneyScatterRadius = 4.0f;

        [Header("Visual Feedback - URP Smoothness Boost")]
        [Tooltip("광택 완료 시 차량 Material에 적용할 Smoothness 값 (0~1). 높을수록 반짝임.")]
        [SerializeField] [Range(0f, 1f)] private float _smoothnessTarget = 0.9f;

        [Header("FX - Sound")]
        [Tooltip("광택 진행 중 루프 재생될 AudioSource (Loop=true, PlayOnAwake=false로 설정)")]
        [SerializeField] private AudioSource _loopSource;
        [SerializeField] private AudioClip _detailLoopClip;

        [Header("FX - Particles")]
        [Tooltip("광택 완료 시 터지는 반짝임 파티클 (자식 오브젝트로 배치, Stop Action=None)")]
        [SerializeField] private ParticleSystem _sparkleParticle;

        // Upgrade multipliers (UpgradeManager가 동적으로 조절)
        private float _incomeMultiplier = 1f;

        private CarCls _currentCar;
        private readonly List<CarCls> _queue = new List<CarCls>();
        // EntryPoint를 아직 통과하지 않은 차량 집합. 슬롯 전진 시 이 차들은 건드리지 않는다.
        private readonly HashSet<CarCls> _awaitingEntry = new HashSet<CarCls>();
        private float _progress;
        private bool _playerInside;
        private bool _isCompleting;
        private bool _wasActive;

        /// <summary>
        /// 새 차를 받을 수 있는 상태인지 여부.
        /// 작업 중인 차가 없거나, 대기 줄에 빈 슬롯이 있으면 true.
        /// </summary>
        public bool IsAvailable => (_currentCar == null && !_isCompleting)
                                || _queue.Count < _maxQueueSize;

        /// <summary>UpgradeManager에서 광택 속도 업그레이드 시 호출.</summary>
        public void SetBaseDetailingSpeed(float speed)
        {
            _baseDetailingSpeed = Mathf.Max(0.01f, speed);
        }

        /// <summary>UpgradeManager에서 광택 수익 업그레이드 시 호출.</summary>
        public void SetIncomeMultiplier(float mult)
        {
            _incomeMultiplier = Mathf.Max(0.01f, mult);
        }

        private void OnEnable()
        {
            foreach (var car in _queue)
                if (car != null) Destroy(car.gameObject);
            _queue.Clear();
            _awaitingEntry.Clear();

            if (_currentCar != null) { Destroy(_currentCar.gameObject); _currentCar = null; }
            _progress     = 0f;
            _playerInside = false;
            _isCompleting = false;
            _wasActive    = false;
        }

        private void Update()
        {
            bool active = !_isCompleting
                       && _playerInside
                       && _currentCar != null
                       && _currentCar.State == ShinyReady.Car.CarState.Washing;

            if (active != _wasActive)
            {
                _wasActive = active;
                if (active) { _progressUI?.Show(); StartDetailFX(); }
                else        { _progressUI?.Hide(); StopDetailLoop(); }
            }

            if (!active) return;

            float detailingTimeMult = _currentCar.TryGetComponent<ShinyReady.Car.CarData>(out var cd)
                ? cd.DetailingTimeMultiplier : 1f;
            _progress = Mathf.MoveTowards(_progress, 1f, _baseDetailingSpeed / detailingTimeMult * Time.deltaTime);
            _progressUI?.SetProgress(_progress);

            if (_progress >= 1f)
            {
                _isCompleting = true;
                _wasActive    = false;
                _progress     = 0f;
                _progressUI?.SetProgress(1f);
                _progressUI?.PlayCompleteEffect(OnDetailingComplete);
            }
        }

        // ── 차량 수신 & 대기열 ────────────────────────────────────

        /// <summary>
        /// CarSpawner에서 세차 완료 차량을 광택 구역으로 보낼 때 호출.
        /// 구역이 비어 있으면 즉시 작업 시작, 바쁘면 대기 줄에 추가한다.
        /// </summary>
        public void ReceiveCar(CarCls car)
        {
            if (car == null || _parkPoint == null) return;

            if (_currentCar == null && !_isCompleting)
            {
                StartDetailing(car);
            }
            else
            {
                _queue.Add(car);

                if (_entryPoint != null)
                {
                    // EntryPoint 통과 전임을 표시. 슬롯 전진 대상에서 제외된다.
                    _awaitingEntry.Add(car);
                    car.MoveTo(_entryPoint, () =>
                    {
                        _awaitingEntry.Remove(car);
                        EnterQueueSlot(car);    // 도착 시점의 슬롯을 동적으로 계산
                    });
                }
                else
                {
                    EnterQueueSlot(car);
                }
            }
        }

        // EntryPoint 도착 후, 혹은 EntryPoint 없이 큐에 진입할 때 현재 슬롯을 계산해 이동
        private void EnterQueueSlot(CarCls car)
        {
            int idx = _queue.IndexOf(car);
            if (idx < 0) return;

            // 대기 중 구역이 비어 있고 맨 앞 차라면 바로 작업 시작
            if (idx == 0 && _currentCar == null && !_isCompleting)
            {
                _queue.RemoveAt(0);
                StartDetailing(car, skipEntry: true);
                AdvanceQueue();
                return;
            }

            if (_queueWaypoints != null && idx < _queueWaypoints.Length && _queueWaypoints[idx] != null)
                car.MoveTo(_queueWaypoints[idx], () => { car.SetInQueue(); AdvanceQueue(); });
        }

        // skipEntry: 대기열에서 올라오는 차처럼 이미 구역 내부에 있을 때 true
        private void StartDetailing(CarCls car, bool skipEntry = false)
        {
            _currentCar   = car;
            _progress     = 0f;
            _isCompleting = false;

            if (_entryPoint != null && !skipEntry)
                car.MoveTo(_entryPoint, () => car.MoveTo(_parkPoint, () => car.SetWashing()));
            else
                car.MoveTo(_parkPoint, () => car.SetWashing());
        }

        private void AdvanceQueue()
        {
            if (_currentCar != null || _isCompleting) return;
            if (_queue.Count == 0) return;

            CarCls next = _queue[0];
            // 아직 이동 중인 차는 슬롯 도착 콜백에서 다시 시도한다
            if (next.State != ShinyReady.Car.CarState.InQueue) return;

            _queue.RemoveAt(0);
            StartDetailing(next, skipEntry: true);

            // 나머지 차량 한 칸씩 전진
            // _awaitingEntry 차량은 EntryPoint 미통과 상태이므로 건드리지 않음
            // → 해당 차가 EntryPoint 도착 시 EnterQueueSlot()에서 현재 슬롯을 동적 계산
            for (int i = 0; i < _queue.Count; i++)
            {
                CarCls c = _queue[i];
                if (_awaitingEntry.Contains(c)) continue;

                int targetSlot = i;
                if (_queueWaypoints != null && targetSlot < _queueWaypoints.Length && _queueWaypoints[targetSlot] != null)
                    c.MoveTo(_queueWaypoints[targetSlot], () => { c.SetInQueue(); AdvanceQueue(); });
            }
        }

        // ── 광택 완료 ────────────────────────────────────────────

        private void OnDetailingComplete()
        {
            if (_currentCar == null)
            {
                _isCompleting = false;
                AdvanceQueue();
                return;
            }

            StopDetailLoop();
            SoundManager.Instance?.PlayDetailComplete();
            ApplySmoothness(_currentCar);
            SpawnMoney();

            CarCls finishedCar = _currentCar;
            _currentCar   = null;
            _isCompleting = false;

            if (_exitPoint != null)
                finishedCar.StartExiting(_exitPoint, () => Destroy(finishedCar.gameObject));
            else
                Destroy(finishedCar.gameObject);

            AdvanceQueue();
        }

        // ── URP Smoothness 적용 ───────────────────────────────────

        private void ApplySmoothness(CarCls car)
        {
            foreach (var rend in car.GetComponentsInChildren<Renderer>())
            {
                foreach (var mat in rend.materials)
                {
                    if      (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", _smoothnessTarget);
                    else if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", _smoothnessTarget);
                }
            }
        }

        // ── 재화 스폰 ────────────────────────────────────────────

        private GameObject GetMoneyPrefab(int value)
        {
            if (value >= 20) return _goldbarPrefab != null ? _goldbarPrefab : _coinPrefab;
            if (value >= 10) return _cashPrefab    != null ? _cashPrefab    : _coinPrefab;
            return _coinPrefab;
        }

        private void SpawnMoney()
        {
            if (_coinPrefab == null || _parkPoint == null) return;

            float carMoneyMult = (_currentCar != null && _currentCar.TryGetComponent<ShinyReady.Car.CarData>(out var carData))
                ? carData.DetailingMoneyMultiplier : 1f;
            float adIncomeMult = ShinyReady.Ads.AdBuffManager.Instance != null
                ? ShinyReady.Ads.AdBuffManager.Instance.IncomeMultiplier : 1f;
            int totalMoney  = Mathf.RoundToInt(_moneyPerDetailing * _incomeMultiplier * carMoneyMult * adIncomeMult);
            int spawnCount  = Mathf.Max(1, _moneySpawnCount);
            int coinValue   = Mathf.Max(1, totalMoney / spawnCount);
            int remainder   = totalMoney % spawnCount;
            var placed      = new List<Vector3>(spawnCount);
            Vector3 basePos = _parkPoint.position + Vector3.up * _moneySpawnHeight;

            GameObject prefab = GetMoneyPrefab(coinValue);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = FindSpawnPosition(placed, i, basePos);
                placed.Add(spawnPos);

                GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                var pickup = obj.GetComponent<MoneyPickup>()
                          ?? obj.AddComponent<MoneyPickup>();
                pickup.SetValue(i == 0 ? coinValue + remainder : coinValue);
            }
        }

        private Vector3 FindSpawnPosition(List<Vector3> placed, int index, Vector3 basePos)
        {
            const int   MAX_ATTEMPTS = 12;
            const float MIN_SPACING  = 0.6f;

            for (int attempt = 0; attempt < MAX_ATTEMPTS; attempt++)
            {
                float angle  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = UnityEngine.Random.Range(_minScatterRadius, _moneyScatterRadius);
                Vector3 candidate = basePos + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                bool tooClose = false;
                foreach (var pos in placed)
                {
                    if (Vector3.Distance(candidate, pos) < MIN_SPACING)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose) return candidate;
            }

            float fallbackAngle  = (index / (float)Mathf.Max(1, _moneySpawnCount)) * Mathf.PI * 2f;
            float fallbackRadius = (_minScatterRadius + _moneyScatterRadius) * 0.5f;
            return basePos + new Vector3(
                Mathf.Cos(fallbackAngle) * fallbackRadius,
                0f,
                Mathf.Sin(fallbackAngle) * fallbackRadius);
        }

        // ── FX ───────────────────────────────────────────────────

        private void StartDetailFX()
        {
            if (_loopSource != null && _detailLoopClip != null && !_loopSource.isPlaying)
            {
                _loopSource.clip = _detailLoopClip;
                _loopSource.loop = true;
                _loopSource.Play();
            }

            if (_sparkleParticle != null && !_sparkleParticle.isPlaying)
                _sparkleParticle.Play();
        }

        private void StopDetailLoop()
        {
            if (_loopSource != null && _loopSource.isPlaying)
                _loopSource.Stop();

            if (_sparkleParticle != null && _sparkleParticle.isPlaying)
                _sparkleParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        // ── Trigger ──────────────────────────────────────────────

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerInside = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.CompareTag("Player"))
                _playerInside = false;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (TryGetComponent<BoxCollider>(out var col))
            {
                Gizmos.color  = new Color(1f, 0.84f, 0f, 0.22f);
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(col.center, col.size);
                Gizmos.color = new Color(1f, 0.84f, 0f);
                Gizmos.DrawWireCube(col.center, col.size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            if (_parkPoint != null)
            {
                Gizmos.color = new Color(1f, 0.84f, 0f);
                Gizmos.DrawWireSphere(_parkPoint.position, 0.3f);
                UnityEditor.Handles.Label(_parkPoint.position + Vector3.up * 0.5f, "DETAIL BAY");
            }

            if (_queueWaypoints == null) return;
            Gizmos.color = new Color(1f, 0.6f, 0f);
            for (int i = 0; i < _queueWaypoints.Length; i++)
            {
                if (_queueWaypoints[i] == null) continue;
                Gizmos.DrawSphere(_queueWaypoints[i].position, 0.2f);
                UnityEditor.Handles.Label(_queueWaypoints[i].position + Vector3.up * 0.5f, $"DQ{i + 1}");
                if (i > 0 && _queueWaypoints[i - 1] != null)
                    Gizmos.DrawLine(_queueWaypoints[i - 1].position, _queueWaypoints[i].position);
            }
        }
#endif
    }
}
