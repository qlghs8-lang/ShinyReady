using UnityEngine;

namespace ShinyReady.Cleaning
{
    [CreateAssetMenu(fileName = "SoapData", menuName = "ShinyReady/Soap Data")]
    public class SoapData : ScriptableObject
    {
        [Tooltip("세제 이름 (UI 표시용)")]
        public string soapName = "Basic Soap";

        [Tooltip("세차 속도 배율 (1.0 = 기본)")]
        [Min(0.1f)]
        public float washSpeedMultiplier = 1f;

        [Tooltip("세차 완료 수익 배율 (1.0 = 기본)")]
        [Min(0.1f)]
        public float incomeMultiplier = 1f;

        [Tooltip("광택 구역 진입 확률 보너스 (0 = 없음, 0.2 = +20%)")]
        [Range(0f, 1f)]
        public float detailingChanceBonus = 0f;

        [Tooltip("해금 비용 (0 = 기본 지급)")]
        [Min(0)]
        public int unlockCost = 0;

        [Tooltip("박스 1개당 WashBay에 충전되는 세제량")]
        [Min(1)]
        public int soapAmountPerBox = 1;

        [Tooltip("스택 박스 색상")]
        public Color boxColor = new Color(0.3f, 0.7f, 1f);
    }
}
