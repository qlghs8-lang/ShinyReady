using UnityEngine;
using ShinyReady.Audio;
using ShinyReady.Car;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// WashBay GameObject에 부착. BoxCollider(IsTrigger)와 함께 사용.
    /// 플레이어 또는 아르바이트생(Staff)이 구역 내에 있고 세제가 있을 때 세차가 진행된다.
    /// isAutomated=true 베이는 플레이어 없이 아르바이트생만으로도 자동 운영된다.
    /// 세제 보충은 SoapRefillPoint(Kiosk)를 통해 AddSoap()으로 외부에서 주입한다.
    /// </summary>
    public class WashingInteraction : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CarSpawner _carSpawner;
        [SerializeField] private WashBaySoapUI _soapUI;
        [SerializeField] private WashProgressUI _progressUI;

        [Header("Wash Settings")]
        [Tooltip("초당 기본 진행도 (SoapData.washSpeedMultiplier 가 곱해짐)")]
        [SerializeField] private float _baseWashSpeed = 0.33f;

        [Header("Soap Settings")]
        [SerializeField] private int _maxSoap = 10;
        [Tooltip("차 한 대 세차 완료 시 소모되는 세제량")]
        [SerializeField] private int _soapPerWash = 1;

        [Header("FX - Sound")]
        [Tooltip("세차 진행 중 루프 재생될 AudioSource (Loop=true, PlayOnAwake=false로 설정)")]
        [SerializeField] private AudioSource _loopSource;
        [SerializeField] private AudioClip _washLoopClip;

        [Header("FX - Particles")]
        [Tooltip("세차 중 거품 파티클 (자식 오브젝트로 배치, Stop Action=None)")]
        [SerializeField] private ParticleSystem _foamParticle;

        [Header("Automation")]
        [Tooltip("true면 아르바이트생(Staff)만 있어도 세차가 자동 진행됩니다.")]
        [SerializeField] private bool _isAutomated = false;
        [Tooltip("이 베이에 배치된 아르바이트생 (_isAutomated가 true일 때 동작)")]
        [SerializeField] private StaffController _assignedStaff;
        [Tooltip("아르바이트생의 세차 속도 배율 (플레이어 대비, 업그레이드로 조절 가능)")]
        [SerializeField] [Range(0.1f, 1f)] private float _staffWashSpeedMultiplier = 0.6f;

        public float Progress { get; private set; }
        public int CurrentSoap { get; private set; }
        public int MaxSoap => _maxSoap;
        public bool IsAutomated => _isAutomated;
        public float StaffWashSpeedMultiplier
        {
            get => _staffWashSpeedMultiplier;
            set => _staffWashSpeedMultiplier = Mathf.Clamp(value, 0.1f, 1f);
        }

        /// <summary>UpgradeManager에서 세제 한도 업그레이드 시 호출.</summary>
        public void SetMaxSoap(int max)
        {
            _maxSoap = Mathf.Max(1, max);
            RefreshUI();
        }

        /// <summary>UpgradeManager에서 세차 속도 업그레이드 시 호출.</summary>
        public void SetBaseWashSpeed(float speed)
        {
            _baseWashSpeed = Mathf.Max(0.01f, speed);
        }

        /// <summary>Zone 해금 시 UnlockZone에서 호출. 아르바이트생이 미리 배치된 경우.</summary>
        public void ActivateAutomation()
        {
            _isAutomated = true;
        }

        /// <summary>Zone 해금 시 스태프를 외부에서 스폰 후 주입할 때 호출한다.</summary>
        public void ActivateAutomationWithStaff(StaffController staff)
        {
            _assignedStaff = staff;
            _isAutomated   = true;
        }

        private SoapData _loadedSoapData;
        private bool _playerInside;
        private bool _wasWashingActive;
        private bool _isCompleting;
        private bool _staffWorking;

        private void OnEnable()
        {
            _playerInside = false;
            _isCompleting = false;
            _wasWashingActive = false;
            _staffWorking = false;
            Progress = 0f;
        }

        private void Start()
        {
            if (_carSpawner == null)
                Debug.LogWarning($"[WashingInteraction] {gameObject.name}: _carSpawner가 null입니다. 인스펙터에서 연결해주세요.");
            if (_isAutomated && _assignedStaff == null)
                Debug.LogWarning($"[WashingInteraction] {gameObject.name}: isAutomated=true이지만 _assignedStaff가 연결되지 않았습니다.");

            RefreshUI();
        }

        private void Update()
        {
            if (_carSpawner == null) return;

            bool staffPresent = _isAutomated && _assignedStaff != null;
            bool workerPresent = _playerInside || staffPresent;
            bool active = !_isCompleting && workerPresent && _carSpawner.HasCarWashing && CurrentSoap > 0;

            if (active != _wasWashingActive)
            {
                _wasWashingActive = active;
                if (active) { _progressUI?.Show(); StartWashFX(); }
                else        { _progressUI?.Hide(); StopWashFX();  }
            }

            // 아르바이트생 시각 피드백: 플레이어 없이 Staff만 세차 중일 때만 작동 표시
            bool staffShouldWork = active && staffPresent && !_playerInside;
            if (staffShouldWork != _staffWorking)
            {
                _staffWorking = staffShouldWork;
                _assignedStaff?.SetWorking(_staffWorking);
            }

            if (!active) return;

            var equippedSoap = SoapInventoryManager.Instance != null ? SoapInventoryManager.Instance.EquippedSoap : _loadedSoapData;
            float speedMult = equippedSoap != null ? equippedSoap.washSpeedMultiplier : 1f;
            // 아르바이트생 단독 작업 시 속도 감소 적용
            if (!_playerInside && staffPresent)
                speedMult *= _staffWashSpeedMultiplier;

            // 광고 버프 속도 배율 반영 (버프 비활성 시 1f로 무효화)
            float adSpeedMult = ShinyReady.Ads.AdBuffManager.Instance != null
                ? ShinyReady.Ads.AdBuffManager.Instance.WashSpeedMultiplier : 1f;

            // 차종별 세차 시간 배율 반영 (배율이 클수록 진행 속도 감소)
            float washTimeMult = _carSpawner.CurrentCarWashTimeMultiplier;
            Progress = Mathf.MoveTowards(Progress, 1f, _baseWashSpeed * speedMult * adSpeedMult / washTimeMult * Time.deltaTime);
            _progressUI?.SetProgress(Progress);

            if (Progress >= 1f)
            {
                _isCompleting     = true;
                _wasWashingActive = false;
                _staffWorking     = false;
                Progress          = 0f;
                CurrentSoap       = Mathf.Max(0, CurrentSoap - _soapPerWash);
                RefreshUI();
                StopWashFX();
                SoundManager.Instance?.PlayWashComplete();
                _progressUI?.SetProgress(1f);
                _progressUI?.PlayCompleteEffect(() => _isCompleting = false);
                _carSpawner.NotifyWashComplete();
                _assignedStaff?.SetWorking(false);
            }
        }

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

        /// <summary>SoapRefillPoint(Kiosk)에서 세제를 주입할 때 호출.</summary>
        public void AddSoap(int amount, SoapData soapData)
        {
            if (soapData != null)
            {
                _loadedSoapData = soapData;
                SyncFoamColor(soapData);
            }

            CurrentSoap = Mathf.Min(_maxSoap, CurrentSoap + amount);
            RefreshUI();
        }

        private void RefreshUI()
        {
            _soapUI?.Refresh(CurrentSoap, _maxSoap);
        }

        private void StartWashFX()
        {
            if (_foamParticle != null && !_foamParticle.isPlaying)
                _foamParticle.Play();

            if (_loopSource != null && _washLoopClip != null && !_loopSource.isPlaying)
            {
                _loopSource.clip = _washLoopClip;
                _loopSource.loop = true;
                _loopSource.Play();
            }
        }

        private void StopWashFX()
        {
            if (_foamParticle != null && _foamParticle.isPlaying)
                _foamParticle.Stop();

            if (_loopSource != null && _loopSource.isPlaying)
                _loopSource.Stop();
        }

        private void SyncFoamColor(SoapData soap)
        {
            if (_foamParticle == null || soap == null) return;
            var main = _foamParticle.main;
            main.startColor = soap.boxColor;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!TryGetComponent<BoxCollider>(out var col)) return;
            Gizmos.color = _isAutomated
                ? new Color(1f, 0.6f, 0f, 0.25f)   // 자동화 베이: 주황
                : new Color(0f, 1f, 1f, 0.25f);     // 일반 베이: 청록
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = _isAutomated ? new Color(1f, 0.6f, 0f) : Color.cyan;
            Gizmos.DrawWireCube(col.center, col.size);
        }
#endif
    }
}
