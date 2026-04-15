using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Collections;
using Object = UnityEngine.Object;

namespace Scripts.Managers
{
    public class CoroutineRunner : MonoBehaviour { }
    public class SoundManager
    {
        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
        #region <<Variable Field>>

        public float SoundVolume { get; private set; }

        public AudioSource[] _audioSources = new AudioSource[(int)Define.Sound.MaxCount];
        // key: 오디오 클립의 path / value: sound clip
        private Dictionary<string, AudioClip> _audioClips = new Dictionary<string, AudioClip>();
        private GameObject _soundRoot = null;

        private BGM_Storage _bgmStorage;
        private Effect_Storage _effectStorage;
        private Speech_Storage _speechStorage;

        private CoroutineRunner _coroutineRunner;

        /*오디오 볼륨범위
        BGM     0~0.7
        Speech  0~1
        Effect  0~0.7
        Song    0~0.7
        */
        #endregion
        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
        #region <<Method>>

        public void Init()
        {
            _soundRoot = GameObject.Find("@SoundRoot");
            if (_soundRoot == null)
            {
                _soundRoot = new GameObject { name = "@SoundRoot" };
                Object.DontDestroyOnLoad(_soundRoot);

                // 코루틴 러너 추가
                _coroutineRunner = _soundRoot.AddComponent<CoroutineRunner>();

                string[] soundTypeName = Enum.GetNames(typeof(Define.Sound));
                for (int count = 0; count < soundTypeName.Length - 1; count++)
                {
                    var go = new GameObject() { name = soundTypeName[count] };
                    _audioSources[count] = go.AddComponent<AudioSource>();
                    go.transform.parent = _soundRoot.transform;
                    _audioSources[count].playOnAwake = false;
                }

                _bgmStorage = ScriptableObject.CreateInstance<BGM_Storage>();
                _effectStorage = ScriptableObject.CreateInstance<Effect_Storage>();
                _speechStorage = ScriptableObject.CreateInstance<Speech_Storage>();

                _bgmStorage = Managers.Resource.Load<BGM_Storage>("ScriptableObject/BGM_Storage");
                _effectStorage = Managers.Resource.Load<Effect_Storage>("ScriptableObject/Effect_Storage");
                _speechStorage = Managers.Resource.Load<Speech_Storage>("ScriptableObject/Speech_Storage_NewNew");

                _audioSources[(int)Define.Sound.Bgm].loop = true;
            }
            else
            {
                // 이미 존재하면 CoroutineRunner 가져오기
                _coroutineRunner = _soundRoot.GetComponent<CoroutineRunner>();
                if (_coroutineRunner == null)
                {
                    _coroutineRunner = _soundRoot.AddComponent<CoroutineRunner>();
                }
            }

            SoundVolume = 1.0f;
        }
        //-----------------------------------------------------------------
        // audioSources 초기화
        public void Clear()
        {
            // 모든 sound 재생, 모든 sound clip 삭제
            foreach (var audioSource in _audioSources)
                audioSource.Stop();
            _audioClips.Clear();
        }
        //-----------------------------------------------------------------
        // AudioClip Play
        public bool Play(Define.Sound type, string path, float volume = 1.0f)
        {
            if (string.IsNullOrEmpty(path)) return false;

            var audioSource = _audioSources[(int)type];
            if (path.Contains("Sound/") == false)
                path = $"Sound/{path}";

            audioSource.volume = volume;

            if (type == Define.Sound.Bgm)
            {
                var audioClip = Managers.Resource.Load<AudioClip>(path);
                if (audioClip == null) return false;

                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.clip = audioClip;
                audioSource.Play();

                return true;
            }
            else if (type == Define.Sound.Effect)
            {
                var audioClip = GetAudioClip(path);
                if (audioClip == null) return false;

                audioSource.PlayOneShot(audioClip);

                return true;
            }
            else if (type == Define.Sound.Speech)
            {
                var audioClip = GetAudioClip(path);
                if (audioClip == null) return false;

                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.clip = audioClip;
                audioSource.Play();

                return true;
            }

            return false;
        }
        //-----------------------------------------------------------------
        public bool Play<T>(T soundId, float volume = 1f, bool isLoop = false) where T : Enum
        {
            var soundType = soundId.GetType().ToString();
            var key = Util_String.RemoveAfterFirstUnderscore(soundType);

            if (key == "BGM")
            {
                var audioSource = _audioSources[(int)Define.Sound.Bgm];
                audioSource.volume = volume;

                var audioClip = _bgmStorage.GetBGM((soundId));
                if (audioClip == null) return false;

                if (audioSource.isPlaying)
                    audioSource.Stop();

                audioSource.loop = isLoop;

                audioSource.clip = audioClip;
                audioSource.Play();

                return true;
            }
            else if (key == "Effect")
            {
                var audioSource = _audioSources[(int)Define.Sound.Effect];
                audioSource.volume = volume * 0.5f;

                var audioClip = _effectStorage.GetEffect((soundId));
                if (audioClip == null) return false;

                audioSource.loop = isLoop;
                audioSource.PlayOneShot(audioClip);

                return true;
            }
            else if (key == "Speech")
            {
                var audioSource = _audioSources[(int)Define.Sound.Speech];
                audioSource.volume = volume;

                //var audioClip = _speechStorage_renew.GetSpeech((soundId));
                var audioClip = _speechStorage.GetSpeech((soundId));
                if (audioClip == null)
                    return false;

                if (audioSource.isPlaying)
                    audioSource.Stop();

                Debug.Log($"Sound Speed : {audioClip.name} ");
                // audioSource.loop = isLoop;
                audioSource.clip = audioClip;
                audioSource.Play();

                return true;
            }

            return false;
        }
        //-----------------------------------------------------------------
        public void Stop(Define.Sound type)
        {
            var audioSource = _audioSources[(int)type];
            audioSource.Stop();
        }

        public void Pause(Define.Sound type)
        {
            var audioSource = _audioSources[(int)type];
            audioSource.Pause();
        }

        public void StopAllSound()
        {
            foreach (var audioSource in _audioSources)
                audioSource.Stop();
        }

        //-----------------------------------------------------------------
        public void SetAllVolume(float volume)
        {
            var audioSourceBGM = _audioSources[(int)Define.Sound.Bgm];
            audioSourceBGM.volume = volume;

            var audioSourceEffect = _audioSources[(int)Define.Sound.Effect];
            audioSourceEffect.volume = volume * 0.7f;

            var audioSourceSpeech = _audioSources[(int)Define.Sound.Speech];
            audioSourceSpeech.volume = volume;

            var audioSourceVideo = _audioSources[(int)Define.Sound.Video];
            audioSourceVideo.volume = volume;
        }
        //-----------------------------------------------------------------
        // 최초 로드를 제외하고 Dictionary에서 효과음 클립을 로컬 폴더로부터 로드없이 가져오게 되므로 성능상 효율적이다.
        private AudioClip GetOrAddAudioClip(string path, Define.Sound type = Define.Sound.Effect)
        {
            if (path.Contains("Sounds/") == false)
                path = $"Sounds/{path}"; // Sounds 폴더 안에 저장될 수 있게 하는 작업

            AudioClip audioClip = null;

            if (type == Define.Sound.Bgm) // BGM 클립 붙이기
                audioClip = Managers.Resource.Load<AudioClip>(path);
            else // Effect sound 붙이기
            {
                if (_audioClips.TryGetValue(path, out audioClip) == false)
                {
                    audioClip = Managers.Resource.Load<AudioClip>(path);
                    _audioClips.Add(path, audioClip);
                }
            }

            if (audioClip == null)
                Debug.Log($"AudioClip not found in: {path}");

            return audioClip;
        }
        //-----------------------------------------------------------------
        private AudioClip GetAudioClip(string path)
        {
            AudioClip audioClip = null;
            if (_audioClips.TryGetValue(path, out audioClip))
                return audioClip;

            audioClip = Managers.Resource.Load<AudioClip>(path);
            _audioClips.Add(path, audioClip);
            return audioClip;
        }
        //-----------------------------------------------------------------
        public bool IsAudioPlay(Define.Sound type)
        {
            return _audioSources[(int)type].isPlaying;
        }

        public void ChangeVolume(float volume)
        {
            SoundVolume = volume;
            //SoundChange.Invoke(soundVolume);
        }

        public void Reset_NarrDict()
        {
            _speechStorage.Reset_Narr();
        }
        #endregion
        //-----------------------------------------------------------------
        // SoundManager.cs에 추가
        
        /// <summary>
        /// 외부 경로의 오디오 파일을 로드하여 재생
        /// </summary>
        public void PlayFromExternalPath(string filePath, Define.Sound type, float volume = 1.0f, bool isLoop = false, Action onComplete = null)
        {
            // null 체크 및 초기화
            if (_coroutineRunner == null)
            {
                if (_soundRoot == null)
                {
                    _soundRoot = GameObject.Find("@SoundRoot");
                }
        
                if (_soundRoot != null)
                {
                    _coroutineRunner = _soundRoot.GetComponent<CoroutineRunner>();
                    if (_coroutineRunner == null)
                    {
                        _coroutineRunner = _soundRoot.AddComponent<CoroutineRunner>();
                    }
                }
                else
                {
                    Debug.LogError("SoundRoot가 존재하지 않습니다.");
                    return;
                }
            }
    
            _coroutineRunner.StartCoroutine(LoadAndPlayExternal(filePath, type, volume, isLoop, onComplete));
        }


        private IEnumerator LoadAndPlayExternal(string filePath, Define.Sound type, float volume, bool isLoop, Action onComplete)
        {
            // 경로 형식 변환
            string url = "file:///" + filePath.Replace("\\", "/");
            
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    
                    var audioSource = _audioSources[(int)type];
                    audioSource.volume = volume;
                    audioSource.loop = isLoop;
                    
                    if (audioSource.isPlaying)
                        audioSource.Stop();
                    
                    audioSource.clip = clip;
                    audioSource.Play();
                    
                    onComplete?.Invoke();
                }
                else
                {
                    Debug.LogError($"외부 오디오 로드 실패: {filePath}\n{www.error}");
                }
            }
        }
        //-----------------------------------------------------------------
    }
    //-----------------------------------------------------------------
}