using System;
using System.Collections.Generic;
using UnityEngine;
using ShinyReady.Cleaning;

namespace ShinyReady.Car
{
    public class CarSpawner : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("스폰 가능한 차량 프리팹 목록. 스폰 시 무작위 선택됨")]
        [SerializeField] private GameObject[] _carPrefabs;
        [SerializeField] private Transform _spawnPoint;

        [Header("Waypoints")]
        [Tooltip("인덱스 0 = WashBay 바로 앞, 마지막 = 맨 뒤")]
        [SerializeField] private Transform[] _queueWaypoints;
        [SerializeField] private Transform _washBayPoint;
        [SerializeField] private Transform _exitPoint;

        [Header("Settings")]
        [SerializeField] private int _maxQueueSize = 4;
        [SerializeField] private float _spawnInterval = 1.5f;
        [Tooltip("WashingInteraction 사용 시 false로 설정")]
        [SerializeField] private bool _autoWash = true;
        [Tooltip("_autoWash = true 일 때만 사용되는 자동 세차 시간")]
        [SerializeField] private float _washDuration = 3f;

        [Header("Currency - Prefabs")]
        [Tooltip("획득 금액이 $9 이하일 때 스폰되는 코인 프리팹")]
        [SerializeField] private GameObject _coinPrefab;
        [Tooltip("획득 금액이 $10~$19일 때 스폰되는 cash 프리팹")]
        [SerializeField] private GameObject _cashPrefab;
        [Tooltip("획득 금액이 $20 이상일 때 스폰되는 goldbar 프리팹")]
        [SerializeField] private GameObject _goldbarPrefab;

        [Header("Currency")]
        [SerializeField] private int _moneyPerWash = 10;
        [SerializeField] private float _moneySpawnHeight = 0.5f;
        [Tooltip("코인 스폰 최소 반경 (m). 차량 크기보다 크게 설정해 차랑과 겹침 방지")]
        [SerializeField] private float _minScatterRadius = 2.0f;
        [Tooltip("코인 스폰 최대 반경 (m)")]
        [SerializeField] private float _moneyScatterRadius = 4.0f;

        [Header("Currency - Spawn Count")]
        [Tooltip("세차 1회당 생성되는 코인 개수")]
        [SerializeField] private int _moneySpawnCount = 5;
        [Tooltip("코인 간 최소 간격 (m). 겹침 방지")]
        [SerializeField] private float _minCoinSpacing = 0.7f;

        [Header("Currency - Car Avoidance")]
        [Tooltip("Car 레이어 마스크 (Inspector에서 Car 레이어 선택)")]
        [SerializeField] private LayerMask _carLayerMask;
        [Tooltip("생성 후보 지점을 차량과 비교할 안전 반경 (m)")]
        [SerializeField] private float _carAvoidRadius = 1.5f;
        [Tooltip("좌표 재탐색 최대 시도 횟수 (코인 1개당)")]
        [SerializeField] private int _maxSpawnAttempts = 15;

        [Header("고가 차량 (VIP)")]
        [Tooltip("고가 차량 등장 확률 (0~1). 발동 시 CarData.IsVip = true 인 프리팹에서만 선택됨. UpgradeManager가 동적으로 조절.")]
        [SerializeField] [Range(0f, 1f)] private float _highValueCarChance = 0f;

        [Header("고급 광택 구역 (Detailing Zone)")]
        [Tooltip("세차 완료 차량을 확률적으로 광택 구역으로 보냅니다. null이면 비활성.")]
        [SerializeField] private ShinyReady.Cleaning.DetailingZone _detailingZone;
        [Tooltip("광택 구역 진입 확률 (0~1). UpgradeManager가 동적으로 조절.")]
        [SerializeField] [Range(0f, 1f)] private float _detailingChance = 0.1f;
        [Tooltip("차가 이 지점에 도착했을 때 Detailing/Exit 분기를 결정. null이면 세차 완료 즉시 결정.")]
        [SerializeField] private Transform _detailingDecisionPoint;

        // CleaningSystem 또는 Currency에서 구독
        public event Action<Car> OnWashComplete;

        /// <summary>UpgradeManager에서 고가 차량 등장 확률 업그레이드 시 호출.</summary>
        public void SetHighValueChance(float chance) => _highValueCarChance = Mathf.Clamp01(chance);

        /// <summary>UpgradeManager에서 광택 구역 진입 확률 업그레이드 시 호출.</summary>
        public void SetDetailingChance(float chance) => _detailingChance = Mathf.Clamp01(chance);

        public bool HasCarWashing => _bayCar != null && _bayCar.State == CarState.Washing;

        /// <summary>현재 세차 중인 차량의 WashTimeMultiplier. WashingInteraction에서 진행 속도 보정에 사용.</summary>
        public float CurrentCarWashTimeMultiplier =>
            (_bayCar != null && _bayCar.TryGetComponent<CarData>(out var d)) ? d.WashTimeMultiplier : 1f;

        private readonly List<Car> _queue = new List<Car>();
        private Car _bayCar;
        private float _spawnTimer;
        private float _washTimer;

        private bool BayOccupied => _bayCar != null;

        // 런타임 동적 활성화(UnlockZone 등) 시 상태를 깨끗하게 시작하기 위해 OnEnable에서 리셋한다.
        private void OnEnable()
        {
            foreach (var car in _queue)
                if (car != null) Destroy(car.gameObject);
            _queue.Clear();

            if (_bayCar != null) { Destroy(_bayCar.gameObject); _bayCar = null; }
            _spawnTimer = 0f;
            _washTimer = 0f;
        }

        private void Update()
        {
            TrySpawn();
            TryAdvanceQueue();
            TickWash();
        }

        // ── 스폰 ──────────────────────────────────────────────
        private void TrySpawn()
        {
            if (_queue.Count >= _maxQueueSize) return;

            _spawnTimer += Time.deltaTime;
            if (_spawnTimer < _spawnInterval) return;
            _spawnTimer = 0f;

            SpawnCar();
        }

        private void SpawnCar()
        {
            if (_carPrefabs == null || _carPrefabs.Length == 0 || _spawnPoint == null) return;
            if (_queueWaypoints == null || _queue.Count >= _queueWaypoints.Length) return;

            int slot = _queue.Count;
            if (_queueWaypoints[slot] == null)
            {
                Debug.LogWarning($"[CarSpawner] {gameObject.name}: _queueWaypoints[{slot}]이 null입니다. 인스펙터에서 Waypoint를 다시 연결해주세요.");
                return;
            }

            bool spawnVip = _highValueCarChance > 0f && UnityEngine.Random.value < _highValueCarChance;
            GameObject prefab = PickPrefab(spawnVip);
            if (prefab == null) return;

            GameObject obj = Instantiate(prefab, _spawnPoint.position, _spawnPoint.rotation);
            Car car = obj.GetComponent<Car>();
            if (car == null) { Destroy(obj); return; }

            _queue.Add(car);
            car.MoveTo(_queueWaypoints[slot], () => car.SetInQueue());
        }

        private GameObject PickPrefab(bool vip)
        {
            var pool = new List<GameObject>();
            foreach (var p in _carPrefabs)
            {
                if (p == null) continue;
                bool isVipPrefab = p.TryGetComponent<CarData>(out var cd) && cd.IsVip;
                if (isVipPrefab == vip) pool.Add(p);
            }
            // 해당 풀이 비어 있으면 전체에서 선택 (프리팹 설정 누락 방지)
            if (pool.Count == 0)
            {
                Debug.LogWarning($"[CarSpawner] {gameObject.name}: IsVip={vip} 인 프리팹이 없어 전체 풀에서 선택합니다. CarData.IsVip 설정을 확인해주세요.");
                foreach (var p in _carPrefabs) if (p != null) pool.Add(p);
            }
            return pool.Count > 0 ? pool[UnityEngine.Random.Range(0, pool.Count)] : null;
        }

        // ── 대기열 → WashBay 전진 ────────────────────────────
        private void TryAdvanceQueue()
        {
            if (BayOccupied || _queue.Count == 0) return;

            Car front = _queue[0];
            if (front.State != CarState.InQueue) return; // 아직 이동 중이면 대기

            if (_washBayPoint == null)
            {
                Debug.LogWarning($"[CarSpawner] {gameObject.name}: _washBayPoint가 null입니다. 인스펙터를 확인해주세요.");
                return;
            }

            // 앞차를 WashBay로 이동
            _queue.RemoveAt(0);
            _bayCar = front;
            _washTimer = 0f;

            front.MoveTo(_washBayPoint, () =>
            {
                front.SetWashing();
                Debug.Log("[CarSpawner] 세차 시작");
            });

            // 나머지 차량 한 칸씩 전진
            for (int i = 0; i < _queue.Count; i++)
            {
                Car c = _queue[i];
                int targetSlot = i;
                c.MoveTo(_queueWaypoints[targetSlot], () => c.SetInQueue());
            }
        }

        // ── 세차 타이머 (_autoWash = true 일 때만 동작) ────────
        private void TickWash()
        {
            if (!_autoWash) return;
            if (!BayOccupied || _bayCar.State != CarState.Washing) return;

            _washTimer += Time.deltaTime;
            float washMult = _bayCar.TryGetComponent<CarData>(out var carData) ? carData.WashTimeMultiplier : 1f;
            if (_washTimer >= _washDuration * washMult)
                CompleteWash();
        }

        // CleaningSystem에서 수동으로 세차 완료 처리 시 호출
        public void NotifyWashComplete() => CompleteWash();

        private void CompleteWash()
        {
            if (_bayCar == null) return;

            float moneyMultiplier = _bayCar.TryGetComponent<CarData>(out var carData) ? carData.MoneyMultiplier : 1f;

            _bayCar.SetDone();
            OnWashComplete?.Invoke(_bayCar);
            Debug.Log($"[CarSpawner] 세차 완료 — 수익 발생 (배율 x{moneyMultiplier})");

            Car exiting = _bayCar;
            _bayCar = null; // 베이 즉시 해제 → 다음 차 전진 가능

            RouteExitingCar(exiting);

            // 차가 베이를 벗어난 뒤 코인 스폰 (차 위에 코인 겹침 방지)
            StartCoroutine(SpawnMoneyDelayed(moneyMultiplier));
        }

        private System.Collections.IEnumerator SpawnMoneyDelayed(float multiplier)
        {
            yield return new WaitForSeconds(0.4f);
            SpawnMoney(multiplier);
        }

        private void RouteExitingCar(Car car)
        {
            float detailingBonus = SoapInventoryManager.Instance?.EquippedSoap?.detailingChanceBonus ?? 0f;
            bool canDetail = _detailingZone != null
                          && _detailingZone.gameObject.activeInHierarchy
                          && _detailingZone.IsAvailable
                          && UnityEngine.Random.value < (_detailingChance + detailingBonus);

            if (!canDetail)
            {
                car.StartExiting(_exitPoint, () => Destroy(car.gameObject));
                return;
            }

            // 분기점이 지정돼 있으면 그 지점까지 먼저 이동
            if (_detailingDecisionPoint != null)
            {
                car.MoveTo(_detailingDecisionPoint, () =>
                {
                    // 분기점 도착 시 재확인 (다른 차가 선점했을 수 있음)
                    if (_detailingZone.IsAvailable)
                        _detailingZone.ReceiveCar(car);
                    else
                        car.StartExiting(_exitPoint, () => Destroy(car.gameObject));
                });
            }
            else
            {
                _detailingZone.ReceiveCar(car);
            }
        }

        private GameObject GetMoneyPrefab(int value)
        {
            if (value >= 20) return _goldbarPrefab != null ? _goldbarPrefab : _coinPrefab;
            if (value >= 10) return _cashPrefab    != null ? _cashPrefab    : _coinPrefab;
            return _coinPrefab;
        }

        private void SpawnMoney(float multiplier = 1f)
        {
            if (_coinPrefab == null || _washBayPoint == null) return;

            float incomeMultiplier = SoapInventoryManager.Instance?.EquippedSoap?.incomeMultiplier ?? 1f;
            float adIncomeMult = ShinyReady.Ads.AdBuffManager.Instance != null
                ? ShinyReady.Ads.AdBuffManager.Instance.IncomeMultiplier : 1f;
            int totalMoney  = Mathf.RoundToInt(_moneyPerWash * Mathf.Max(1f, multiplier) * incomeMultiplier * adIncomeMult);
            int spawnCount  = Mathf.Max(1, _moneySpawnCount);
            int coinValue   = Mathf.Max(1, totalMoney / spawnCount);
            int remainder   = totalMoney % spawnCount;
            var placedPositions = new System.Collections.Generic.List<Vector3>(spawnCount);

            GameObject prefab = GetMoneyPrefab(coinValue);

            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 spawnPos = FindSafeSpawnPosition(placedPositions, i);
                placedPositions.Add(spawnPos);

                GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
                var pickup = obj.GetComponent<ShinyReady.Currency.MoneyPickup>()
                          ?? obj.AddComponent<ShinyReady.Currency.MoneyPickup>();
                pickup.SetValue(i == 0 ? coinValue + remainder : coinValue);
            }
        }

        /// <param name="placed">이미 확정된 코인 위치 목록 (코인 간격 보장용)</param>
        /// <param name="index">현재 코인 인덱스 (폴백 위치 분산용)</param>
        private Vector3 FindSafeSpawnPosition(
            System.Collections.Generic.List<Vector3> placed, int index)
        {
            Vector3 basePos = _washBayPoint.position + Vector3.up * _moneySpawnHeight;

            for (int attempt = 0; attempt < _maxSpawnAttempts; attempt++)
            {
                float angle  = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
                float radius = UnityEngine.Random.Range(_minScatterRadius, _moneyScatterRadius);
                Vector3 candidate = basePos + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                // 코인 간 최소 간격만 확인 (차량 감지는 MoneyPickup이 런타임에 부상으로 처리)
                bool tooClose = false;
                foreach (var pos in placed)
                {
                    if (Vector3.Distance(candidate, pos) < _minCoinSpacing)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (!tooClose) return candidate;
            }

            // 폴백: 최소 반경 기준 원형으로 균등 배치
            float fallbackAngle  = (index / (float)Mathf.Max(1, _moneySpawnCount)) * Mathf.PI * 2f;
            float fallbackRadius = (_minScatterRadius + _moneyScatterRadius) * 0.5f;
            return basePos + new Vector3(
                Mathf.Cos(fallbackAngle) * fallbackRadius,
                0f,
                Mathf.Sin(fallbackAngle) * fallbackRadius);
        }

        // ── 에디터 Gizmos ─────────────────────────────────────
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_spawnPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_spawnPoint.position, 0.3f);
                UnityEditor.Handles.Label(_spawnPoint.position + Vector3.up * 0.5f, "SPAWN");
            }

            if (_washBayPoint != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(_washBayPoint.position, 0.3f);
                UnityEditor.Handles.Label(_washBayPoint.position + Vector3.up * 0.5f, "WASH BAY");
            }

            if (_exitPoint != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(_exitPoint.position, 0.3f);
                UnityEditor.Handles.Label(_exitPoint.position + Vector3.up * 0.5f, "EXIT");
            }

            if (_queueWaypoints == null) return;
            Gizmos.color = Color.yellow;
            for (int i = 0; i < _queueWaypoints.Length; i++)
            {
                if (_queueWaypoints[i] == null) continue;
                Gizmos.DrawSphere(_queueWaypoints[i].position, 0.2f);
                UnityEditor.Handles.Label(_queueWaypoints[i].position + Vector3.up * 0.5f, $"Q{i + 1}");
                if (i > 0 && _queueWaypoints[i - 1] != null)
                    Gizmos.DrawLine(_queueWaypoints[i - 1].position, _queueWaypoints[i].position);
            }
        }
#endif
    }
}
