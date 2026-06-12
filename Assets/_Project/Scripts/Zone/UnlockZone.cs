using ShinyReady.Cleaning;
using ShinyReady.Currency;
using ShinyReady.Player;
using UnityEngine;

namespace ShinyReady.Zone
{
    /// <summary>
    /// Trigger Collider를 가진 GameObject에 부착.
    /// 플레이어 진입 시 확인 팝업을 띄우고, 승인 시 비용을 차감한 뒤
    /// TargetObject를 활성화하고 자신을 파기한다.
    ///
    /// - _requiredZoneSaveId: 선행 구역 SaveId. 해당 구역이 해금되어야만 팝업이 열린다.
    /// - _staffPrefab / _staffSpawnPoint / _staffTargetBay: 해금 시 스태프를 스폰해 배치한다.
    /// </summary>
    public class UnlockZone : MonoBehaviour
    {
        [Header("해금 설정")]
        [SerializeField] private int _unlockCost = 100;
        [SerializeField] private GameObject _targetObject;
        [Tooltip("이 구역 해금 시 UpgradeManager.ZoneLevel에 더할 값. 0이면 업그레이드 잠금에 영향 없음.")]
        [SerializeField] private int _upgradeZoneLevel = 1;

        [Header("저장 설정")]
        [Tooltip("씬 내 고유 ID. 각 UnlockZone마다 반드시 다른 값을 입력할 것. (예: Zone_1, Zone_2)")]
        [SerializeField] private string _saveId = "Zone_1";

        [Header("선행 구역 조건")]
        [Tooltip("이 값이 비어 있지 않으면 해당 SaveId의 구역이 먼저 해금돼야만 팝업이 열린다. (예: Zone_1)")]
        [SerializeField] private string _requiredZoneSaveId = "";

        [Header("팝업 UI")]
        [Tooltip("씬의 Screen Space 캔버스 하위에 배치된 UnlockZonePopupUI 패널을 연결")]
        [SerializeField] private UnlockZonePopupUI _popupUI;

        [Header("정지 방식")]
        [Tooltip("true = Time.timeScale 0 (완전 정지) | false = 플레이어 이동만 멈춤 (하이퍼캐주얼 권장)")]
        [SerializeField] private bool _pauseTimeOnPopup = false;

        [Header("Zone 해금 시 자동화 활성화 (스태프 미리 배치된 경우)")]
        [Tooltip("이 Zone 해금 시 자동 세차를 활성화할 WashingInteraction 배열")]
        [SerializeField] private WashingInteraction[] _activateAutomationOnUnlock;

        [Header("Zone 해금 시 스태프 활성화 & 배치")]
        [Tooltip("씬에 미리 배치해 둔 스태프 GameObject (Inspector에서 비활성화 상태로 둘 것). 해금 시 활성화됨.")]
        [SerializeField] private GameObject _staffObject;
        [Tooltip("활성화된 스태프를 연결할 WashingInteraction (자동화가 함께 활성화됨)")]
        [SerializeField] private WashingInteraction _staffTargetBay;

        [Header("Zone 해금 시 파괴할 오브젝트")]
        [Tooltip("해금 시 제거할 플레이스홀더 오브젝트들 (예: Base_Washbay_02, Base_DetailingZone)")]
        [SerializeField] private GameObject[] _objectsToDestroyOnUnlock;

        private PlayerController _playerController;
        private bool _popupOpen;

        private string SaveKey         => $"Zone_{_saveId}";
        private string RequiredSaveKey => $"Zone_{_requiredZoneSaveId}";

        private void Start()
        {
            if (PlayerPrefs.GetInt(SaveKey, 0) == 1)
            {
                if (_targetObject != null) _targetObject.SetActive(true);
                // ZoneLevel은 UpgradeManager가 저장된 값으로 직접 로드하므로 여기서 증가시키지 않음
                ActivateAutomationBays();
                SpawnAndConnectStaff();
                DestroyPlaceholders();
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_popupOpen) return;
            if (!IsPrerequisiteMet()) return;   // 선행 구역 미해금 시 팝업 차단

            _playerController = other.GetComponent<PlayerController>();
            if (_playerController == null) return;
            OpenPopup();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!_popupOpen) return;
            if (other.GetComponent<PlayerController>() == null) return;
            ClosePopup();
        }

        // ── 선행 조건 ────────────────────────────────────────────

        private bool IsPrerequisiteMet()
        {
            if (string.IsNullOrEmpty(_requiredZoneSaveId)) return true;
            return PlayerPrefs.GetInt(RequiredSaveKey, 0) == 1;
        }

        // ── 팝업 ─────────────────────────────────────────────────

        private void OpenPopup()
        {
            _popupOpen = true;

            if (_pauseTimeOnPopup)
                Time.timeScale = 0f;
            else
                _playerController.SetMovementEnabled(false);

            _popupUI.Show(_unlockCost, OnConfirm, OnCancel);
        }

        private void ClosePopup()
        {
            _popupOpen = false;
            _popupUI.Hide();

            if (_pauseTimeOnPopup)
                Time.timeScale = 1f;
            else
                _playerController?.SetMovementEnabled(true);
        }

        private void OnConfirm()
        {
            if (!CurrencyManager.Instance.HasEnoughMoney(_unlockCost))
            {
                _popupUI.FlashInsufficientFunds();
                return;
            }

            CurrencyManager.Instance.SpendMoney(_unlockCost);
            if (_targetObject != null) _targetObject.SetActive(true);

            // 업그레이드 잠금 해제
            if (_upgradeZoneLevel > 0)
            {
                var mgr = ShinyReady.Upgrade.UpgradeManager.Instance;
                if (mgr != null) mgr.ZoneLevel += _upgradeZoneLevel;
            }

            ActivateAutomationBays();
            SpawnAndConnectStaff();
            DestroyPlaceholders();

            PlayerPrefs.SetInt(SaveKey, 1);
            PlayerPrefs.Save();

            ClosePopup();
            Destroy(gameObject);
        }

        private void OnCancel()
        {
            ClosePopup();
        }

        // ── 자동화 & 스태프 ──────────────────────────────────────

        private void ActivateAutomationBays()
        {
            if (_activateAutomationOnUnlock == null) return;
            foreach (var bay in _activateAutomationOnUnlock)
                bay?.ActivateAutomation();
        }

        private void DestroyPlaceholders()
        {
            if (_objectsToDestroyOnUnlock == null) return;
            foreach (var obj in _objectsToDestroyOnUnlock)
                if (obj != null) Destroy(obj);
        }

        private void SpawnAndConnectStaff()
        {
            if (_staffObject == null || _staffTargetBay == null) return;

            _staffObject.SetActive(true);

            StaffController staff = _staffObject.GetComponent<StaffController>();
            if (staff == null)
            {
                Debug.LogWarning($"[UnlockZone] {gameObject.name}: _staffObject에 StaffController가 없습니다.");
                return;
            }

            _staffTargetBay.ActivateAutomationWithStaff(staff);
        }
    }
}
