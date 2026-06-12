using UnityEngine;
using UnityEngine.SceneManagement;

namespace ShinyReady.Save
{
    /// <summary>
    /// 게임 저장 데이터 전체 초기화 매니저.
    /// 씬에 하나만 배치. 개발 테스트용 리셋 버튼 포함.
    /// </summary>
    public class GameSaveManager : MonoBehaviour
    {
        public static GameSaveManager Instance { get; private set; }

        [Header("개발 전용 리셋 버튼")]
        [Tooltip("true로 설정하면 화면에 리셋 버튼이 표시됩니다. 릴리즈 빌드 전에 반드시 false로 변경.")]
        [SerializeField] private bool _showDevResetButton = true;

        private bool _confirmPending;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// 모든 저장 데이터(재화, 업그레이드, 구역 해금)를 삭제하고 씬을 재시작한다.
        /// </summary>
        public void ResetAllProgress()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private void OnGUI()
        {
            if (!_showDevResetButton) return;

            const float btnW = 160f;
            const float btnH = 40f;
            const float margin = 10f;
            float x = Screen.width - btnW - margin;
            float y = margin;

            GUI.color = _confirmPending ? Color.red : new Color(1f, 0.4f, 0.4f);
            string label = _confirmPending ? "한 번 더 누르면 초기화" : "[DEV] 진행사항 초기화";

            if (GUI.Button(new Rect(x, y, btnW, btnH), label))
            {
                if (_confirmPending)
                {
                    ResetAllProgress();
                }
                else
                {
                    _confirmPending = true;
                    Invoke(nameof(CancelConfirm), 3f);
                }
            }

            GUI.color = Color.white;
        }

        private void CancelConfirm() => _confirmPending = false;
#endif
    }
}
