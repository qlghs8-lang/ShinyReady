using UnityEngine;

namespace ShinyReady.Audio
{
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [SerializeField] private AudioSource _sfxSource;

        [Header("업그레이드 SFX")]
        [SerializeField] private AudioClip _successSFX;
        [SerializeField] private AudioClip _failSFX;

        [Header("세차 / 광택 SFX")]
        [SerializeField] private AudioClip _washCompleteSFX;
        [SerializeField] private AudioClip _detailCompleteSFX;

        private void Awake()
        {
            if (Instance != null) { Destroy(gameObject); return; }
            Instance = this;

            if (_sfxSource == null)
                _sfxSource = GetComponent<AudioSource>();
        }

        public void PlaySuccess()
        {
            if (_sfxSource != null && _successSFX != null) _sfxSource.PlayOneShot(_successSFX);
        }

        public void PlayFail()
        {
            if (_sfxSource != null && _failSFX != null) _sfxSource.PlayOneShot(_failSFX);
        }

        /// <summary>업그레이드 외 다른 곳에서도 사용할 수 있는 범용 SFX 재생</summary>
        public void PlaySFX(AudioClip clip)
        {
            if (_sfxSource != null && clip != null) _sfxSource.PlayOneShot(clip);
        }

        public void PlayWashComplete()
        {
            if (_sfxSource != null && _washCompleteSFX != null)
                _sfxSource.PlayOneShot(_washCompleteSFX);
        }

        public void PlayDetailComplete()
        {
            if (_sfxSource != null && _detailCompleteSFX != null)
                _sfxSource.PlayOneShot(_detailCompleteSFX);
        }
    }
}
