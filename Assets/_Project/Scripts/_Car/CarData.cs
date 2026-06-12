using UnityEngine;

namespace ShinyReady.Car
{
    /// <summary>
    /// 차량 프리팹에 붙이는 차종별 데이터 컴포넌트.
    /// CarSpawner가 스폰 시 참조하여 세차 시간 배율과 기본 수익 배율을 결정한다.
    /// </summary>
    public class CarData : MonoBehaviour
    {
        [Tooltip("true이면 이 차종은 항상 고급 차량으로 취급됨. 색상 변경 없이 수익 배율만 적용 (van, vanbig 등)")]
        [SerializeField] private bool _isVip = false;

        [Header("Wash")]
        [Tooltip("세단 등 일반 차량은 1.0 / SUV·트럭 등 대형은 1.5~2.0 권장")]
        [SerializeField] [Range(0.5f, 10f)] private float _washTimeMultiplier = 1f;

        [Tooltip("세차 완료 시 수익 배율. 소수점 사용 가능")]
        [SerializeField] [Range(1f, 10f)] private float _moneyMultiplier = 1f;

        [Header("Detailing")]
        [Tooltip("광택 소요 시간 배율. 클수록 광택이 오래 걸림")]
        [SerializeField] [Range(0.5f, 10f)] private float _detailingTimeMultiplier = 1f;

        [Tooltip("광택 완료 시 수익 배율. 소수점 사용 가능")]
        [SerializeField] [Range(1f, 10f)] private float _detailingMoneyMultiplier = 1f;

        public bool IsVip => _isVip;
        public float WashTimeMultiplier => _washTimeMultiplier;
        public float MoneyMultiplier => _moneyMultiplier;
        public float DetailingTimeMultiplier => _detailingTimeMultiplier;
        public float DetailingMoneyMultiplier => _detailingMoneyMultiplier;
    }
}
