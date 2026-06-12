using System;
using UnityEngine;

namespace ShinyReady.Car
{
    public enum CarState
    {
        Moving,
        InQueue,
        Washing,
        Done,
        Exiting
    }

    public class Car : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 4f;
        [SerializeField] private float _rotationSpeed = 8f;
        [SerializeField] private float _arrivalDistance = 0.05f;

        public CarState State { get; private set; }

        private Transform _target;
        private Action _onArrived;
        private bool _isMoving;

        // CarSpawner가 목적지를 지정할 때마다 호출
        public void MoveTo(Transform target, Action onArrived)
        {
            if (target == null)
            {
                Debug.LogWarning($"[Car] {gameObject.name}: MoveTo에 null Target이 전달됐습니다. CarSpawner의 Waypoint/Point 연결을 확인해주세요.");
                return;
            }

            _target = target;
            _onArrived = onArrived;
            _isMoving = true;
            State = CarState.Moving;

            // 목적지 방향으로 즉시 정렬 (쿼터뷰 어색함 방지)
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir);
        }

        public void SetInQueue()
        {
            _isMoving = false;
            State = CarState.InQueue;
        }

        public void SetWashing()
        {
            _isMoving = false;
            State = CarState.Washing;
        }

        public void SetDone()
        {
            State = CarState.Done;
        }

        public void StartExiting(Transform exitPoint, Action onExited)
        {
            MoveTo(exitPoint, onExited);
            State = CarState.Exiting; // MoveTo가 Moving으로 덮어쓰므로 재지정
        }

        private void Update()
        {
            if (!_isMoving || _target == null) return;

            Vector3 targetPos = new Vector3(_target.position.x, transform.position.y, _target.position.z);
            Vector3 dir = targetPos - transform.position;

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.y = 0f;
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    _rotationSpeed * Time.deltaTime);
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                _moveSpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPos) <= _arrivalDistance)
            {
                transform.position = targetPos;
                _isMoving = false;
                Action callback = _onArrived;
                _onArrived = null;
                callback?.Invoke();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_target == null || !_isMoving) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, _target.position);
        }
#endif
    }
}
