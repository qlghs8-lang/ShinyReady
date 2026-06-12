using UnityEngine;
using ShinyReady.Player;

namespace ShinyReady.Upgrade
{
    /// <summary>
    /// 사무실 구역 트리거. 플레이어 진입 시 업그레이드 UI 활성화 + 카메라 줌인.
    /// BoxCollider(isTrigger)와 함께 사용.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class OfficeTrigger : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("씬의 UpgradePanel 루트 GameObject를 연결.")]
        [SerializeField] private GameObject _upgradePanel;

        [Header("카메라 줌인 (선택 사항)")]
        [SerializeField] private bool _enableCameraZoom = true;
        [Tooltip("메인 카메라에 부착된 CameraFollow 컴포넌트.")]
        [SerializeField] private CameraFollow _cameraFollow;
        [Tooltip("사무실 내부 뷰를 위한 카메라 오프셋. 기본값보다 낮게/가깝게 설정.")]
        [SerializeField] private Vector3 _officeViewOffset = new Vector3(0f, 8f, -4f);
        [Tooltip("줌인 전환 부드러움. 작을수록 빠름.")]
        [SerializeField] private float _zoomSmoothTime = 0.25f;

        private void Awake()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Start()
        {
            _upgradePanel?.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _upgradePanel?.SetActive(true);

            if (_enableCameraZoom && _cameraFollow != null)
                _cameraFollow.SetOffsetOverride(_officeViewOffset, _zoomSmoothTime);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            _upgradePanel?.SetActive(false);

            if (_enableCameraZoom && _cameraFollow != null)
                _cameraFollow.ClearOffsetOverride();
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!TryGetComponent<BoxCollider>(out var col)) return;
            Gizmos.color = new Color(1f, 0.9f, 0.2f, 0.25f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = new Color(1f, 0.9f, 0.2f);
            Gizmos.DrawWireCube(col.center, col.size);
        }
#endif
    }
}
