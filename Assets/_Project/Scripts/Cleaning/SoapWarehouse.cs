using UnityEngine;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// 세제 창고. BoxCollider(IsTrigger)와 함께 배치.
    /// 플레이어가 영역 안에 머물면 일정 간격으로 세제 박스를 하나씩 지급한다.
    /// </summary>
    public class SoapWarehouse : MonoBehaviour
    {
        [Header("Soap Settings")]
        [SerializeField] private SoapData _soapData;

        [Tooltip("박스 하나가 지급되는 데 걸리는 시간 (초)")]
        [SerializeField] private float _fillInterval = 0.8f;

        private PlayerSoapCarrier _carrier;
        private float _timer;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
                _carrier = other.GetComponent<PlayerSoapCarrier>();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            _carrier = null;
            _timer = 0f;
        }

        private void Update()
        {
            if (_carrier == null || _carrier.IsFull) return;

            _timer += Time.deltaTime;
            if (_timer >= _fillInterval)
            {
                _timer = 0f;
                _carrier.TryAddBox(_soapData);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!TryGetComponent<BoxCollider>(out var col)) return;
            Gizmos.color = new Color(1f, 0.9f, 0f, 0.2f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.center, col.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(col.center, col.size);
        }
#endif
    }
}
