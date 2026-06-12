using System.Collections.Generic;
using UnityEngine;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// Player GameObject에 부착. 세제 박스 소지 및 시각적 스택을 관리한다.
    /// </summary>
    public class PlayerSoapCarrier : MonoBehaviour
    {
        [Header("Carry Settings")]
        [SerializeField] private int _maxSoapBoxes = 5;
        // 2m 캐릭터 기준: 등 뒤 0.5m, 높이 1.0m 지점에서 스택 시작
        [SerializeField] private Vector3 _stackBaseLocalOffset = new Vector3(0f, 1.0f, -0.5f);
        // 박스 높이(0.3) + 여백(0.05)
        [SerializeField] private float _stackSpacing = 0.35f;

        [Header("Box Visual")]
        // 유니티 표준 스케일: 약 50x30x50cm 박스
        [SerializeField] private Vector3 _boxScale = new Vector3(0.5f, 0.3f, 0.5f);

        public int CurrentBoxCount => _boxes.Count;
        public int MaxBoxes => _maxSoapBoxes;
        public bool IsFull => _boxes.Count >= _maxSoapBoxes;

        /// <summary>UpgradeManager에서 적재량 업그레이드 적용 시 호출.</summary>
        public void SetMaxBoxes(int max) => _maxSoapBoxes = Mathf.Max(1, max);
        public bool HasSoap => _boxes.Count > 0;
        public SoapData CarriedSoapData { get; private set; }

        private readonly List<GameObject> _boxes = new();

        /// <summary>박스 1개를 스택에 추가한다. 가득 찼으면 false 반환.</summary>
        public bool TryAddBox(SoapData soapData)
        {
            if (IsFull) return false;

            if (_boxes.Count == 0)
                CarriedSoapData = soapData;

            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.transform.SetParent(transform, false);
            box.transform.localScale = _boxScale;
            box.transform.localPosition = _stackBaseLocalOffset + Vector3.up * (_boxes.Count * _stackSpacing);

            // 플레이어 이동을 방해하지 않도록 콜라이더 제거
            Destroy(box.GetComponent<Collider>());

            if (soapData != null)
            {
                var rend = box.GetComponent<Renderer>();
                var mat = new Material(rend.sharedMaterial) { color = soapData.boxColor };
                rend.material = mat;
            }

            _boxes.Add(box);
            return true;
        }

        /// <summary>가장 위(마지막으로 추가된) 박스 1개를 제거한다. 성공하면 true.</summary>
        public bool RemoveOne()
        {
            if (_boxes.Count == 0) return false;

            int last = _boxes.Count - 1;
            Destroy(_boxes[last]);
            _boxes.RemoveAt(last);

            if (_boxes.Count == 0)
                CarriedSoapData = null;

            return true;
        }

        /// <summary>현재 가장 위에 있는 박스의 월드 위치를 반환한다. (발사체 시작 위치용)</summary>
        public Vector3 GetTopBoxWorldPosition()
        {
            if (_boxes.Count == 0)
                return transform.position + Vector3.up;
            return _boxes[_boxes.Count - 1].transform.position;
        }

        /// <summary>소지한 박스를 모두 제거하고 제거된 수를 반환한다.</summary>
        public int RemoveAll()
        {
            int count = _boxes.Count;
            foreach (var box in _boxes)
                Destroy(box);
            _boxes.Clear();
            CarriedSoapData = null;
            return count;
        }
    }
}
