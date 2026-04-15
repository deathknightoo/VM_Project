using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using mybenefit.Common;
using UnityEngine.Networking;
using static mybenefit.Common.Structs;
using UnityEngine.SceneManagement;
using com.rfilkov.kinect;
using System;
using UnityEngine.Serialization;
using Scripts.Events;

namespace Scripts.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("New Version Settings")]
        public bool useNewVersionSheet = false;
        
        public bool isSkip = false; // 유의 사항 스킵가능한지 여부
        #region 진행도 데이터
        //비디오 트레이닝
        public VideoTraining_Data videoTrainingData;
        public List<VideoTraining_Exer_Data> list_VideoExerData = new List<VideoTraining_Exer_Data>();

        //노래운동
        public SongExercise_Data songExerciseData;
        public List<SongExercise_Exer_Data> list_SongExerData = new List<SongExercise_Exer_Data>();

        //비디오 트레이닝 전체 운동시간 저장
        public int videoTraining_ExerciseTime;
        #endregion


        // 구글시트 데이터 처리
        [FormerlySerializedAs("_isURLDownloaded")] public bool isURLDownloaded;
        public bool isGetData;
        public Dictionary<string, string> tableURL_Dict = new Dictionary<string, string>();

        // 화면 터치 유무 타이머
        public int touchTime;
        public int touchMaxTime;

        public bool isLock;
        public bool isCameraOn;
        public bool isAliveTouchOn;
        
        public Coroutine timeCountClick;

        public Define.ProjectType ProjectType { get; private set; }
        public bool loginCheck = false;
        public void Init()
        {
            //프로젝트 버전 확인
            ProjectType = GameObject.Find("@Project").GetComponent<ProjectController>().ProjectType;
            
            // Config에서 useNewVersionSheet 값 로드
            string configValue = Managers.LocalData._configHandler.GetConfig(
                Enums.ConfigList.common, "useNewVersionSheet");
            useNewVersionSheet = string.Equals(configValue, "true", StringComparison.OrdinalIgnoreCase);

            isSkip = false;
            AliveClear();
            touchMaxTime = 60;
            AppEventBus.Subscribe<SceneLoaded>(OnSceneLoaded);
            AppEventBus.Subscribe<LogoutRequested>(OnLogoutRequested);

            StartCoroutine(CheckInternet());
        }
        private void Update()
        {
            CheckAliveTouch();
        }

        private void OnDestroy()
        {
            AppEventBus.Unsubscribe<SceneLoaded>(OnSceneLoaded);
            AppEventBus.Unsubscribe<LogoutRequested>(OnLogoutRequested);
        }

        public void Clear()
        {

        }

        /// <summary>
        /// 터치 타이머 관리 코루틴
        /// 일정 시간 감지가 안되었을시 처리 담당
        /// </summary>
        /// <returns></returns>
        private IEnumerator TimeCountClick()
        {
            int i = 0;

            while (true)
            {
                yield return new WaitForSeconds(1.0f);

                if (i < touchMaxTime && !KinectManager.Instance.IsUserDetected(0))
                {
                    i++;
                    touchTime = i;
                    //Debug.Log(i + "초 지남");
                }

                if (touchMaxTime - 10 <= i)

                    if (touchMaxTime <= i)
                    {
                        Debug.Log("사용안한지 " + touchMaxTime + "초 경과");

                        isAliveTouchOn = true;

                        if (Managers.Game.ProjectType == Define.ProjectType.VM3S)
                        {
                            switch ((Define.S_Scene)Enum.Parse(typeof(Define.S_Scene), SceneManager.GetActiveScene().name))
                            {
                                case Define.S_Scene.Login:
                                    break;
                                default:

                                    if (Managers.LocalData._configHandler.IsTheme(Enums.ConfigTheme.Senior) == false)
                                    {
                                        // 싱가포르 사용시 주석, 평소에는 사용
                                        AppEventBus.Publish(new LogoutRequested { Reason = LogoutReason.Timeout });
                                    }

                                    break;
                            }
                        }
                        else if (Managers.Game.ProjectType == Define.ProjectType.VM3F)
                        {
                            // F 버전 처리
                        }

                        break;
                    }

                if (KinectManager.Instance.IsUserDetected(0))
                {
                    i = 0;
                    touchTime = i;
                    isAliveTouchOn = false;
                }
            }
        }

        /// <summary>
        /// 씬 변경시 1번 작동되는 트리거 (AppEventBus 경유)
        /// 씬별로 로그아웃 시간을 변경
        /// </summary>
        private void OnSceneLoaded(SceneLoaded evt)
        {
            Debug.Log("Scene Loaded Trigger: " + evt.SceneType + " 변경됨");
            AliveClear();
            SkeletonHelper.SetFrame = 60;

            if (Managers.Game.ProjectType == Define.ProjectType.VM3S)
            {
                switch (evt.SceneType)
                {
                    case Define.S_Scene.Login:
                        touchMaxTime = 60;
                        break;
                    case Define.S_Scene.StartUp:
                        touchMaxTime = 100;
                        CheckConfigTheme(evt.SceneType.ToString());
                        break;
                    case Define.S_Scene.VideoTraining:
                        touchMaxTime = 3600;
                        break;
                    default:
                        touchMaxTime = 180;
                        break;
                }
            }
            else if (Managers.Game.ProjectType == Define.ProjectType.VM3F)
            {
                // F 버전 처리
            }
        }

        /// <summary>
        /// Config 데이터에 따른 테마 변경 담당
        /// > 씬 변경 이벤트가 존재하여 여기로 이동
        /// > 추후 개선 필요
        /// </summary>
        /// <param name="sceneName"></param>
        private void CheckConfigTheme(string sceneName)
        {
            GameObject[] findScene = GameObject.FindGameObjectsWithTag("Scene");
            foreach (var s in findScene) s.gameObject.SetActive(false);

            if (Managers.LocalData._configHandler.IsTheme(Enums.ConfigTheme.V2C))
            {
                Debug.Log("V2C 버전임");

                for (int i = 0; i < findScene.Length; i++)
                {
                    if (String.Equals(findScene[i].name, "@Scene-V2C"))
                        findScene[i].SetActive(true);
                }
            }
            else
            {
                Debug.Log("기본 테마임");

                for (int i = 0; i < findScene.Length; i++)
                {
                    if (String.Equals(findScene[i].name, "@Scene"))
                        findScene[i].SetActive(true);
                }
            }
        }

        /// <summary>
        /// 사용자가 기기를 사용중인지 감지
        /// 기준: 터치 유무, 카메라 유저 활성화 유무
        /// </summary>
        private void CheckAliveTouch()
        {
            if (Input.GetMouseButtonDown(0) && !isLock && !isCameraOn)
            {
                touchTime = 0;
                isLock = true;
                isAliveTouchOn = false;
            }

            if (isLock && !isCameraOn)
            {
                isLock = false;

                if (timeCountClick != null) StopCoroutine(timeCountClick);

                timeCountClick = StartCoroutine(TimeCountClick());
            }

            if (isLock && isCameraOn)
            {
                isLock = false;
                if (timeCountClick != null) StopCoroutine(timeCountClick);
            }
        }

        /// <summary>
        /// 터치 타이머 초기화
        /// </summary>
        public void AliveClear()
        {
            touchTime = 0;

            isLock = true;
            isCameraOn = false;
            isAliveTouchOn = false;
        }
        //-----------------------------------------------------------------
        /// <summary>
        /// 인터넷 연결 유무 검색
        /// </summary>
        public IEnumerator CheckInternet()
        {
            bool isConnect = true;
            bool isBGMPlaying = false;
            int countTime = 0;

            while (true)
            {
                //일정시간 이상 인터넷이 끊길 경우
                if (countTime == 10)
                {
                    if (Managers.DTO.GetPlayerInfo() != null)
                    {
                        Managers.DTO.SetPlayerInfo(null);
                        Managers.UI.CloseAllPopupUI();

                        UnityEngine.Debug.Log("로그아웃");
                    }
                }

                UnityWebRequest www = new UnityWebRequest("http://google.com");
                yield return www.SendWebRequest();

                //인터넷이 끊겼을 때
                if (www.error != null)
                {
                    if (isConnect)
                    {
                        isConnect = false;
                        if (Managers.Sound._audioSources[(int)Define.Sound.Bgm].isPlaying)
                        {
                            Managers.Sound.Pause(Define.Sound.Bgm);
                            isBGMPlaying = true;
                        }

                        AppEventBus.Publish(new NetworkStatusChanged { IsConnected = false, ConsecutiveFailCount = countTime });
                    }
                    UnityEngine.Debug.Log(countTime);
                    if (countTime < 10)
                    {
                        countTime++;
                    }
                }
                else if (!isConnect && www.error == null)
                {
                    if (!Managers.Auth.isDeviceAuth)
                        _ = Managers.DTO.DevAuthTask();

                    if (countTime == 10)
                    {
                        if (Managers.LocalData._configHandler.IsTheme(Enums.ConfigTheme.Senior))
                        {
                            //Debug.Log("대한노인회 버전임");

                            // 게임 종료
                            Managers.Camera.GameExit();
                        }
                        else
                        {
                            if (Managers.Game.ProjectType == Define.ProjectType.VM3S)
                            {
                                Managers.Scene.ChangeScene(Define.S_Scene.Login);
                            }
                            else if (Managers.Game.ProjectType == Define.ProjectType.VM3F)
                            {
                                // F 버전 처리
                            }
                        }
                    }

                    isConnect = true;
                    if (isBGMPlaying)
                    {
                        Managers.Sound._audioSources[(int)Define.Sound.Bgm].Play();
                        isBGMPlaying = false;
                    }

                    AppEventBus.Publish(new NetworkStatusChanged { IsConnected = true, ConsecutiveFailCount = 0 });
                    countTime = 0;
                }

                yield return new WaitForSeconds(5.0f);
            }
        }
        //-----------------------------------------------------------------
        private void OnLogoutRequested(LogoutRequested evt)
        {
            LogoutProcess();
        }

        public void LogoutProcess()
        {
            Managers.UI.ShowPopupUI<UI_LogoutAutoMenuV2C_Popup>("LogoutAutoMenuV2C_Popup", true);
        }
        //-----------------------------
        
        //-----------------------------
    }

}