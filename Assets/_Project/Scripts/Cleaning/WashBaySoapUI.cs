using TMPro;
using UnityEngine;

namespace ShinyReady.Cleaning
{
    /// <summary>
    /// WashBay 위의 World Space Canvas에 부착.
    /// 세제 잔량과 "Out of Soap!" 경고를 표시한다.
    /// </summary>
    public class WashBaySoapUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _soapCountText;
        [SerializeField] private GameObject _outOfSoapWarning;

        private void Awake()
        {
            transform.rotation = Quaternion.Euler(55f, 45f, 0f);
        }

        public void Refresh(int current, int max)
        {
            if (_soapCountText != null)
                _soapCountText.text = $"Soap: {current}/{max}";

            if (_outOfSoapWarning != null)
                _outOfSoapWarning.SetActive(current <= 0);
        }
    }
}
