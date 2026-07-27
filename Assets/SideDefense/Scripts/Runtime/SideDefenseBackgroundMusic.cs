using UnityEngine;

namespace Game3.SideDefense
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioSource))]
    public sealed class SideDefenseBackgroundMusic : MonoBehaviour
    {
        private const string MusicResourcePath = "Audio/InfiniteDarkness";
        private const float TargetVolume = 0.32f;
        private const float FadeInDuration = 2.5f;

        private AudioSource audioSource;
        private SideDefenseGameFlow gameFlow;
        private float fadeElapsedTime;
        private bool pausedForStoppedGame;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateForSideDefenseScene()
        {
            SideDefenseMonsterWaveController waveController =
                FindAnyObjectByType<SideDefenseMonsterWaveController>();
            if (waveController == null ||
                FindAnyObjectByType<SideDefenseBackgroundMusic>() != null)
            {
                return;
            }

            GameObject musicObject =
                new GameObject("SideDefense Background Music");
            musicObject.transform.SetParent(
                waveController.transform,
                false);
            musicObject.AddComponent<SideDefenseBackgroundMusic>();
        }

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            gameFlow = GetComponentInParent<SideDefenseGameFlow>();
            AudioClip musicClip =
                Resources.Load<AudioClip>(MusicResourcePath);
            if (musicClip == null)
            {
                Debug.LogWarning(
                    $"Missing background music at Resources/{MusicResourcePath}.",
                    this);
                enabled = false;
                return;
            }

            audioSource.clip = musicClip;
            audioSource.playOnAwake = false;
            audioSource.loop = true;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 0f;
            audioSource.priority = 64;
            fadeElapsedTime = 0f;
            pausedForStoppedGame = false;
            audioSource.Play();
        }

        private void Update()
        {
            if (audioSource == null)
            {
                return;
            }

            bool shouldPauseForStoppedGame =
                Time.timeScale <= 0f &&
                (gameFlow == null || !gameFlow.IsWaitingToStart);
            if (shouldPauseForStoppedGame)
            {
                if (!pausedForStoppedGame)
                {
                    audioSource.Pause();
                    pausedForStoppedGame = true;
                }

                return;
            }

            if (pausedForStoppedGame)
            {
                audioSource.UnPause();
                pausedForStoppedGame = false;
            }

            if (!audioSource.isPlaying)
            {
                return;
            }

            fadeElapsedTime += Time.unscaledDeltaTime;
            audioSource.volume =
                Mathf.Lerp(
                    0f,
                    TargetVolume,
                    Mathf.Clamp01(fadeElapsedTime / FadeInDuration));
        }
    }
}
