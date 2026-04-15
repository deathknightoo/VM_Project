using Microsoft.Win32;
using mybenefit.Common;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

namespace Scripts.Managers
{
    public class ExternalManager : MonoBehaviour
    {
        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
        #region <<Variable Field>>

        // Config 불러오기
        private ConfigDataHandler handler;

        /// <summary>
        /// 외부 프로그램 실행 여부 판단
        /// </summary>
        //[SerializeField, ReadOnly] private bool isProcess;

        // 디바이스 코드 조회
        private DeviceCodeReader deviceCodeReader;
        private string deviceCode;

        [Header("Command Data")]
        [ReadOnly] public string cmdUid;
        [ReadOnly] public string cmdGender;
        [ReadOnly] public string cmdAge;

        //CUBOX 안면인식에 사용될 카메라 우선순위 배열
        private string[] Arr_CUBOXCam = new string[]
        {
            "HD Pro Webcam C920",
            "Intel F450",
            "Intel(R) RealSense(TM) Depth Camera 435 with RGB Module RGB",
            "USB Camera0",
            "HD Webcam",
            "Orbbec Femto Bolt RGB Camera"
        };

        public class CUBoxConfigData
        {
            public CUBoxConfigData(string _DEVICENO, string _SPEED, string _WIDTH, string _HEIGTH, string _VIEWSIZE, string _FACESIZE, string _URL, string _TOKEN, string _THRESHOLD)
            {
                DEVICENO = _DEVICENO;
                SPEED = _SPEED;
                WIDTH = _WIDTH;
                HEIGTH = _HEIGTH;
                VIEWSIZE = _VIEWSIZE;
                FACESIZE = _FACESIZE;
                URL = _URL;
                TOKEN = _TOKEN;
                THRESHOLD = _THRESHOLD;
            }

            public string DEVICENO;
            public string SPEED;
            public string WIDTH;
            public string HEIGTH;
            public string VIEWSIZE;
            public string FACESIZE;
            public string URL;
            public string TOKEN;
            public string THRESHOLD;
        }

        // 인바디 모듈
        //[ReadOnly] public InbodyModule inbodyModule;
        [ReadOnly] public MedicalModule medicalModule;

        #endregion
        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
        #region <<Method>>

        /// <summary>
        /// 처음 진입시 해당 isAllFocus가 비활성화면 일반 프로그램 & 활성화시 항시 포커스 프로그램
        /// </summary>
        public void Init()
        {
            GetCmdData();

            deviceCodeReader = this.gameObject.AddComponent<DeviceCodeReader>();
            //inbodyModule = this.gameObject.AddComponent<InbodyModule>();
            medicalModule = this.gameObject.AddComponent<MedicalModule>();

            Set_FaceCamera();
        }
        //-----------------------------------------------------------------


        //-----------------------------------------------------------------

        /// <summary>
        /// CMD 데이터 전송 받은 필드 값 출력
        /// </summary>
        public void GetCmdData()
        {
            string[] cmdLine = Environment.GetCommandLineArgs();

            for (int i = 0; i < cmdLine.Length; i++)
            {
                UnityEngine.Debug.Log($"Argument {i}: {cmdLine[i]}");

                switch (cmdLine[i]) {
                    case "-uid":
                        if (i + 1 < cmdLine.Length)
                        {
                            cmdUid = cmdLine[i + 1];

                            UnityEngine.Debug.Log($"name 1 value: {cmdLine[i + 1]}");
                            i++; // Skip the next argument since it's a value
                        }
                        else
                        {
                            UnityEngine.Debug.Log("name 1 requires a value.");
                        }
                        break;
                    case "-gender":
                        if (i + 1 < cmdLine.Length)
                        {
                            cmdGender = cmdLine[i + 1];

                            UnityEngine.Debug.Log($"name 1 value: {cmdLine[i + 1]}");
                            i++; // Skip the next argument since it's a value
                        }
                        else
                        {
                            UnityEngine.Debug.Log("name 1 requires a value.");
                        }
                        break;
                    case "-age":
                        if (i + 1 < cmdLine.Length)
                        {
                            cmdAge = cmdLine[i + 1];

                            UnityEngine.Debug.Log($"name 1 value: {cmdLine[i + 1]}");
                            i++; // Skip the next argument since it's a value
                        }
                        else
                        {
                            UnityEngine.Debug.Log("name 1 requires a value.");
                        }
                        break;
                }
            }

            UnityEngine.Debug.Log($"아이디: {cmdUid} / 성별: {cmdGender} / 나이: {cmdAge}");
        }

        /// <summary>
        /// CMD로 응용프로그램 실행하여 옵션값으로 데이터 전송
        /// </summary>
        public void PostCmdData(string path)
        {
            System.Diagnostics.Process[] processes1 = System.Diagnostics.Process.GetProcessesByName("FaceAuthentication");

            //if (processes1.Length == 0)
            {
                string fullPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(Application.dataPath, @path));
                UnityEngine.Debug.Log(fullPath);
                //System.Diagnostics.Process.Start(fullPath);

                Process p = new Process();
                p.StartInfo.FileName = "FaceRegistration.exe";
                //p.StartInfo.Arguments = "/c dir";
                //p.StartInfo.Arguments = "/c VirtualMate.exe -uid test1 -gender M -age 25";
                //p.StartInfo.Arguments = "/c " + fullPath;
                p.StartInfo.CreateNoWindow = false;
                p.StartInfo.WorkingDirectory = Environment.GetEnvironmentVariable(path);
                p.StartInfo.UseShellExecute = true;
                p.Start();

                UnityEngine.Debug.Log("외부 프로그램 실행");
                //isProcess = true;


                //Process p = new Process();
                //p.StartInfo.FileName = "cmd";
                //p.StartInfo.Arguments = "/c dir";
                //p.StartInfo.Arguments = "/c VirtualMate.exe -uid test1 -gender M -age 25";
                //p.Start();
            }
        }

        /// <summary>
        /// 윈도우 포커스시 확인 이벤트 트리거 발동
        /// </summary>
        /// <param name="hasFocus"></param>
        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                UnityEngine.Debug.Log("창이 포커스를 얻었습니다.");

                //FindRegistry();

                /*var token = FindRegistry("Software\\SilverKiosk", "accessToken");

                // 해당 레지스트리가 있으면
                if (token != "0" || token != "" || token != null)
                {
                    //var token = "123123";//임시
                    RestryLogin(token);
                }*/
            }
            else
            {
                UnityEngine.Debug.Log("창이 포커스를 잃었습니다.");
                //StopAllCoroutines();
            }
        }

        /// <summary>
        /// 레지스트리 검색
        /// </summary>
        /// <param name="path">경로</param>
        /// <param name="name">레지스트리 이름</param>
        /// <returns></returns>
        public string FindRegistry(string path, string name)
        {
            string result = "";

            RegistryKey reg = Registry.CurrentUser;//경로

            reg = reg.OpenSubKey(path);

            if (reg != null)
            {
                var val = reg.GetValue(name);//읽을값이름

                if (null != val)
                {
                    UnityEngine.Debug.Log("reg:" + Convert.ToString(val));
                    result = Convert.ToString(val);
                }
            }

            return result;
        }

        /// <summary>
        /// 레지스트리 토큰값 읽음 > 로그인 시도 - VM2
        /// </summary>
        /// <param name="token"></param>
        public void RestryLogin(string token)
        {
            UnityEngine.Debug.Log("토큰값: " + token);
            /*GameObject.Find("DataTransferObjectManager").GetComponent<S_DTOManagerScript>().TokenLogin("u", token, (isSuccess2, reason2) =>
            {
                if (isSuccess2)
                {
                    SceneManager.LoadScene("_SceneStartUP");
                }
                else
                {
                    Debug.Log("오류, 관리자에게 문의하세요.");
                    IsAliveTouch.Instance.IsLock = true;
                    IsAliveTouch.Instance.IsKinect_On = true;
                    // 10초뒤에 자동 로그인
                    // 실제로는 게스트 아이디로 로그인되게함 - 고정 토큰값을 받아야함
                    StartCoroutine(DelayChangeScene("", 5.0f));
                    //RestryLogin("게스트 토근값");
                }
            });*/
            //btnAutoLogin();
        }

        /// <summary>
        /// 윈도우 시스템 종료, 타이머 시간 설정
        /// </summary>
        /// <param name="Case">종료, 재시작 등등</param>
        /// <param name="Time">종료 시간</param>
        private void Systemoff(int Case, int Time)
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "shutdown.exe";
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.Arguments = string.Format("-{0} -t {1}", Case == 0 ? "s" : "r", Time);

            System.Diagnostics.Process.Start(startInfo);
        }

        /// <summary>
        /// 안면인식 프로그램 상단에 노출
        /// </summary>
        public IEnumerator FindUpFaceRecode()
        {
            yield return new WaitForSeconds(0.5f);

            System.Diagnostics.Process[] reg = System.Diagnostics.Process.GetProcessesByName("FaceRegistration");
            System.Diagnostics.Process[] auth = System.Diagnostics.Process.GetProcessesByName("FaceAuthentication");

            UnityEngine.Debug.Log("카메라 인식 감지 On");

            IntPtr regName = WindowManager.Window.FindWindow(null, "FacePass");

            if (!regName.Equals(IntPtr.Zero))
            {
                WindowManager.Window.ShowWindowAsync(regName, WindowManager.Window.showNORMAL);
                WindowManager.Window.SetForegroundWindow(regName);
                WindowManager.Window.SetWindowPos(regName, (int)WindowManager.Window.HWND_TOPMOST, 0, 0, 0, 0, WindowManager.Window.SWP.TOPMOST);
            }

            if (reg.Length > 0 || auth.Length > 0)
            {
                yield return new WaitForSeconds(0.5f);

                StartCoroutine(FindUpFaceRecode());
            }
            else
            {
                UnityEngine.Debug.Log("카메라 인식 감지 Off");
            }
        }

        private void Set_FaceCamera()
        {
            string cuBoxConfigPath = Globals.ExternalDataPathAbs + "FaceRecognition/CUBOX/cusdk/Conf/PassConfig.json";
            string dataString = File.ReadAllText(cuBoxConfigPath);

            CUBoxConfigData cuboxConfigData = JsonConvert.DeserializeObject<CUBoxConfigData>(dataString);

            WebCamDevice[] webcams = WebCamTexture.devices;

            int index = 0;
            bool isBreak = false;
            for (int i = 0; i < webcams.Length; i++)
            {
                foreach (var v in webcams)
                {
                    if (v.name == Arr_CUBOXCam[index])
                    {
                        cuboxConfigData.DEVICENO = v.name;
                        isBreak = true;
                        break;
                    }
                }
                if (isBreak)
                {
                    break;
                }
                else
                {
                    index++;
                }
            }


            string saveJson = JsonUtility.ToJson(cuboxConfigData);
            File.WriteAllText(cuBoxConfigPath, saveJson);
        }

        //-----------------------------------------------------------------
        public void Clear()
        {
        }
        #endregion
        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
    }
}