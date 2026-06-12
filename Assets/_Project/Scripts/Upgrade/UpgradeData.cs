using UnityEngine;

namespace ShinyReady.Upgrade
{
    [CreateAssetMenu(fileName = "NewUpgradeData", menuName = "ShinyReady/Upgrade Data")]
    public class UpgradeData : ScriptableObject
    {
        [Header("기본 정보")]
        public string upgradeName = "업그레이드";
        public string unit = "";

        [Header("레벨 설정")]
        public int maxLevel = 5;
        [Tooltip("레벨 0(기본)의 수치. 게임 시작 시 이 값이 적용됨.")]
        public float baseValue = 1f;
        [Tooltip("업그레이드 1레벨당 증가하는 수치")]
        public float valuePerLevel = 0.5f;

        [Header("비용 (개발 테스트용 저가)")]
        [Tooltip("costsPerLevel[i]: 레벨 i → i+1 업그레이드 비용. 배열 길이는 maxLevel과 일치시킬 것.")]
        public int[] costsPerLevel = { 5, 10, 20, 40, 80 };

        [Header("잠금 조건")]
        [Tooltip("0이면 항상 해금. 구역 해금 레벨이 이 값 이상일 때만 업그레이드 가능.")]
        public int requiredZoneLevel = 0;

        private int _currentLevel;
        private string SaveKey => $"Upg_{name}";

        public int CurrentLevel    => _currentLevel;
        public bool IsMaxLevel     => _currentLevel >= maxLevel;
        public float CurrentValue  => baseValue + valuePerLevel * _currentLevel;
        public float NextValue     => IsMaxLevel ? CurrentValue : baseValue + valuePerLevel * (_currentLevel + 1);
        public int UpgradeCost
        {
            get
            {
                if (IsMaxLevel || costsPerLevel == null || _currentLevel >= costsPerLevel.Length)
                    return 0;
                return costsPerLevel[_currentLevel];
            }
        }

        public void Load()
        {
            _currentLevel = Mathf.Clamp(PlayerPrefs.GetInt(SaveKey, 0), 0, maxLevel);
        }

        public bool TryLevelUp()
        {
            if (IsMaxLevel) return false;
            _currentLevel++;
            PlayerPrefs.SetInt(SaveKey, _currentLevel);
            PlayerPrefs.Save();
            return true;
        }

        // 에디터 테스트용 초기화
        [ContextMenu("레벨 초기화")]
        public void ResetLevel()
        {
            _currentLevel = 0;
            PlayerPrefs.DeleteKey(SaveKey);
        }
    }
}
