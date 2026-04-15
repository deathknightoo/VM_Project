using System;
using System.IO;
using System.Threading.Tasks;
using mybenefit.Common;
using UnityEngine;
using Scripts;
using Scripts.Model.Network;
using Scripts.Managers;
using System.Collections;
using Newtonsoft.Json;
using UnityEngine.Networking;
using Debug = UnityEngine.Debug;
using System.Collections.Generic;

public class DTO_Manager
{
    //-----------------------------------------------------------------
    public string phoneAuthCode;

    private ApplicationContext applicationContext;
    public bool bRecognitionGuide = true;
    public int prevMenuScene;

    public LoginInfoDTO lInfoDto;

    // 디바이스 코드 조회
    private DeviceCodeReader deviceCodeReader;
    private string deviceCode;

    private readonly NetworkRepository _netRepo = new();
    //-----------------------------------------------------------------
    public void Init()
    {
        if (applicationContext == null)
        {
            applicationContext = new ApplicationContext();
        }

        if (Debug.isDebugBuild && Application.isEditor)
        {
            PlayerPrefs.SetString("DeviceCode", "");
        }
        string devSeq = PlayerPrefs.GetString("DeviceCode", "");
        applicationContext.devCode = devSeq;

        //로그폴더 생성 - 로그폴더 없는 상태에서 로그파일 생성 시 오류발생에 대한 방어코드
        DirectoryInfo di = new DirectoryInfo("Logs/");
        if (di.Exists == false)
        {
            di.Create();
        }

        // 운동 정보 관련 인스턴스를 생성
        // applicationContext.workExecResultInfo = new UserWorkoutResultInfo();
        // applicationContext.CustomworkExecResultInfo = new UserCustomWorkoutResultInfo();
    }
    //-----------------------------------------------------------------
    public string GetUserName()
    {
        return applicationContext.playerInfo == null ? "GUEST" : applicationContext.playerInfo.User.name;
    }
    //-----------------------------------------------------------------
    public ApplicationContext GetApplicationContext()
    {
        return applicationContext;
    }
    //-----------------------------------------------------------------
    public PlayerInfo GetPlayerInfo()
    {
        if (applicationContext.playerInfo == null) return PlayerInfo.GetGuestPlayer();
        else return applicationContext.playerInfo;
    }
    //-----------------------------------------------------------------
    public void SetPlayerInfo(PlayerInfo playerInfo)
    {
        applicationContext.playerInfo = playerInfo;
    }
    //-----------------------------------------------------------------
    [Serializable]
    public class PublicSportuserName
    {
        public string Name;
    }
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------
    #region <<Login Method>>
    //-----------------------------------------------------------------
    // 서버로부터 등록된 장치 인증
    public async Task DevAuthTask()
    {
        if (Managers.Auth.isDeviceAuth) return;

        try
        {
            // 서버에 로그인 요청을 보냄
            Debug.Log("서버에 로그인 요청 보냄 =================");
            GetDevCodeRsp rsp = await _netRepo.DeviceAuthTask(Managers.Auth.serialNumBase64);

            // ===== null 체크 추가 =====
            if (rsp == null)
            {
                Debug.LogError("DevLogin failed: rsp is null (404 or network error)");
                applicationContext.devCode = null;
                return;  // throw 대신 return
            }
            // ===== null 체크 끝 =====
            
            if (rsp.retCode == "200")
            {
                Debug.Log(string.Format("DevLogin Success :{0}-->{1} ", Managers.Auth.serialNumBase64, rsp.DeviceCode, 5));
                PlayerPrefs.SetString("DeviceCode", rsp.DeviceCode);
                Managers.Auth.isDeviceAuth = true;
            }
            else
            {
                Debug.Log("DevLogin Fail");
                applicationContext.devCode = null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("DevLogin error : " + e.Message);
            // throw;
        }
    }
    //-----------------------------------------------------------------
    //전화번호로 로그인
    public async Task PhoneLoginTask(string userType, string id, string password, Action<bool, string> callback = null)
    {
        // 장비 인증이 안 되어 있으면 먼저 인증 시도
        if (!Managers.Auth.isDeviceAuth)
        {
            Debug.Log("device is null, attempting DevAuthTask...");
        
            await DevAuthTask();  // 장비 인증 시도
        
            // 재시도 후에도 실패하면 에러 반환
            if (!Managers.Auth.isDeviceAuth)
            {
                Debug.Log("DevAuthTask failed, cannot proceed with login");
                callback?.Invoke(false, "DeviceNull");
                return;
            }
        
            Debug.Log("DevAuthTask succeeded, proceeding with login");
        }

        // 서버에 로그인 요청을 보냄
        GetLoginRsp rsp = await _netRepo.PhoneLoginTask(id, password, userType);

        // 결과가 null인지 확인 (네트워크 오류 또는 서버 응답 실패)
        if (rsp == null)
        {
            //callback?.Invoke(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요.");
            // callback?.Invoke(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
            Debug.Log("=== rsp is null, calling callback with NetworkError ===");
            callback?.Invoke(false, "NetworkError");
            return;
        }

        // 서버의 응답 코드를 확인하고 처리
        if (rsp.retCode == "200")
        {
            switch (userType)
            {
                case "u": // 회원
                    applicationContext.playerInfo = ConvertUserInfo(rsp, rsp.UserKey);
                    applicationContext.playerInfo.User = rsp.User;
                    applicationContext.playerInfo.User.userSeq = rsp.User.userSeq;
                    applicationContext.playerInfo.Phone = id;
                    Debug.Log("playerInfo : " + applicationContext.playerInfo.User.userSeq);
                    callback?.Invoke(true, applicationContext.playerInfo.ChgPass);
                    break;
                
                case "tr": // 트레이너
                    applicationContext.playerInfo = new PlayerInfo();
                    applicationContext.playerInfo.User = rsp.User;
                    applicationContext.playerInfo.Mber_seq = rsp.UserKey;  // 첼린지에서 가져가기 위해 넣자 
                    callback?.Invoke(true, applicationContext.playerInfo.ChgPass);
                    break;

                default:
                    callback?.Invoke(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); // 방어코딩 추가
                    break;
            }
            
            PublicSportuserName pPut = new PublicSportuserName
            {
                Name = rsp.User.name
            };
            var log = JsonUtility.ToJson(pPut);
            Debug.Log("publicgameinfo name : " + rsp.User.name);
            File.WriteAllText("publicgameinfo.txt", log);
        }
        //else if (rsp.retCode == "701")
        //{
        //    PlayerPrefs.SetString("DeviceCode", "");
        //    callback?.Invoke(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); // 방어코딩 추가
        //    _ = DevAuthTask();
        //}
        //else if (rsp.retCode == "703")
        //{
        //    callback?.Invoke(false, "MEMBERSHIPFAIL");
        //}
        else if (rsp.retCode == "601")
        {
            callback?.Invoke(false, "ServerError");
        }
        else
        {
            // 로그인 실패 처리
            callback?.Invoke(false, "Else"/*"사용자가 정보가 조회되지 않습니다.\n확인 후 다시 시도해 주세요."*/);
        }
    }
    //-----------------------------------------------------------------
    //인증코드로 로그인
    public async Task AuthLoginTask(string userseq, Action<bool, string> callback = null)
    {
        // ===== 디바이스 인증 재시도 로직 추가 =====
        if (!Managers.Auth.isDeviceAuth)
        {
            Debug.Log("device is null, attempting DevAuthTask...");
            await DevAuthTask();
        
            if (!Managers.Auth.isDeviceAuth)
            {
                Debug.Log("DevAuthTask failed, cannot proceed with login");
                callback?.Invoke(false, "DeviceNull");
                return;
            }
            Debug.Log("DevAuthTask succeeded, proceeding with login");
        }

        // 서버에 로그인 요청을 보냄
        GetLoginRsp rsp = await _netRepo.AuthLoginTask(userseq);

        try
        {
            if (rsp.retCode == "200")
            {
                //rsp.
                //로그인 성공
                switch (rsp.UserType)
                {
                    case 1: // 회원
                        applicationContext.playerInfo = ConvertUserInfo(rsp, userseq);

                        Debug.Log("playerInfo : " + applicationContext.playerInfo.UserSeq);
                        if (callback != null) callback(true, "success");
                        break;
                    case 2: //트레이너
                        applicationContext.playerInfo = new PlayerInfo();
                        applicationContext.playerInfo.User.name = rsp.User.name;
                        applicationContext.playerInfo.Mber_seq = rsp.UserKey;  // 첼린지에서 가져가기 위해 넣자 
                        if (callback != null) callback(true, "trainersuccess");
                        break;
                    default:
                        //if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); //방어코딩 추가
                        if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
                        break;
                }

                PublicSportuserName pPut = new PublicSportuserName();
                pPut.Name = rsp.User.name;
                var log = JsonUtility.ToJson(pPut);
                Debug.Log("publicgameinfo name : " + rsp.User.name);
                File.WriteAllText("publicgameinfo.txt", log);
            }
            else if (rsp.retCode == "701")
            {
                PlayerPrefs.SetString("DeviceCode", "");
                //if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); //방어코딩 추가
                if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
                _ = DevAuthTask();
            }
            else if (rsp.retCode == "703")
            {
                if (callback != null) callback(false, "MEMBERSHIPFAIL");
            }
            else
            {
                //TODO 로그인 실패
                //if (callback != null) callback(false, "사용자가 정보가 조회되지 않습니다.\n확인 후 다시 시도해 주세요.");
                if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_04"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Auth Error: " + e.Message);
            callback?.Invoke(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
            return;
        }
    }
    //-----------------------------------------------------------------
    //아이디로 로그인
    public async Task IdLoginTask(string userType, string id, string password, Action<bool, string> callback = null)
    {
        // ===== 디바이스 인증 재시도 로직 추가 =====
        if (!Managers.Auth.isDeviceAuth)
        {
            Debug.Log("device is null, attempting DevAuthTask...");
            await DevAuthTask();
        
            if (!Managers.Auth.isDeviceAuth)
            {
                Debug.Log("DevAuthTask failed, cannot proceed with login");
                callback?.Invoke(false, "DeviceNull");
                return;
            }
            Debug.Log("DevAuthTask succeeded, proceeding with login");
        }

        // 서버에 로그인 요청을 보냄
        GetLoginRsp rsp = await _netRepo.IdLoginTask(id, password, userType);

        try
        {
            if (rsp.retCode == "200")
            {
                switch (userType)
                {
                    case "u": // 회원
                        applicationContext.playerInfo = ConvertUserInfo(rsp, rsp.UserKey);
                        Debug.Log("playerInfo : " + applicationContext.playerInfo.UserSeq);
                        if (callback != null) callback(true, applicationContext.playerInfo.ChgPass);
                        break;
                    case "tr": //트레이너
                        applicationContext.playerInfo = new PlayerInfo();
                        applicationContext.playerInfo.User.name = rsp.User.name;
                        applicationContext.playerInfo.Mber_seq = rsp.UserKey;  // 첼린지에서 가져가기 위해 넣자 
                        if (callback != null) callback(true, applicationContext.playerInfo.ChgPass);
                        break;
                    default:
                        //if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); //방어코딩 추가
                        if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
                        break;
                }

                PublicSportuserName pPut = new PublicSportuserName();
                pPut.Name = rsp.User.name;
                var log = JsonUtility.ToJson(pPut);
                Debug.Log("publicgameinfo name : " + rsp.User.name);
                File.WriteAllText("publicgameinfo.txt", log);
            }
            else if (rsp.retCode == "701")
            {
                PlayerPrefs.SetString("DeviceCode", "");
                //if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); //방어코딩 추가
                if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
                _ = DevAuthTask();
            }
            else if (rsp.retCode == "703")
            {
                if (callback != null) callback(false, "MEMBERSHIPFAIL");
            }
            else
            {
                //TODO 로그인 실패
                //if (callback != null) callback(false, "사용자가 정보가 조회되지 않습니다.\n확인 후 다시 시도해 주세요.");
                if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_04"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("Id Login Error" + e.Message);
            //if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요.");
            if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
            return;
        }
    }
    //-----------------------------------------------------------------
    public async Task FaceLoginTask(string userType, string faceId, Action<bool, string> callback = null)
    {
        // ===== 디바이스 인증 재시도 로직 추가 =====
        if (!Managers.Auth.isDeviceAuth)
        {
            Debug.Log("device is null, attempting DevAuthTask...");
            await DevAuthTask();
        
            if (!Managers.Auth.isDeviceAuth)
            {
                Debug.Log("DevAuthTask failed, cannot proceed with login");
                callback?.Invoke(false, "DeviceNull");
                return;
            }
            Debug.Log("DevAuthTask succeeded, proceeding with login");
        }

        // 서버에 로그인 요청을 보냄
        GetLoginRsp rsp = await _netRepo.FaceLoginTask(userType, faceId);

        Debug.Log(rsp.retCode + " <<< 이거는 안면인식 로그인");

        // 결과가 null인지 확인 (네트워크 오류 또는 서버 응답 실패)
        if (rsp == null)
        {
            //callback?.Invoke(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요.");
            callback?.Invoke(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
            return;
        }

        if (rsp.retCode == "200")
        {
            switch (userType)
            {
                case "u": // 회원
                    applicationContext.playerInfo = ConvertUserInfo(rsp, rsp.UserKey);
                    applicationContext.playerInfo.User = rsp.User;
                    Debug.Log("playerInfo : " + applicationContext.playerInfo.UserSeq);
                    if (callback != null) callback(true, "applicationContext.playerInfo.ChgPass");
                    break;
                case "tr": //트레이너
                    applicationContext.playerInfo = new PlayerInfo();
                    applicationContext.playerInfo.User.name = rsp.User.name;
                    applicationContext.playerInfo.Mber_seq = rsp.UserKey;  // 첼린지에서 가져가기 위해 넣자 
                    if (callback != null) callback(true, applicationContext.playerInfo.ChgPass);
                    break;
                default:
                    //if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); //방어코딩 추가
                    if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
                    break;
            }
        }
        //else if (rsp.retCode == "701")
        //{
        //    PlayerPrefs.SetString("DeviceCode", "");
        //    if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요."); //방어코딩 추가
        //    _ = DevAuthTask();
        //}
        //else if (rsp.retCode == "703")
        //{
        //    if (callback != null) callback(false, "MEMBERSHIPFAIL");
        //}
        else if (rsp.retCode == "601")
        {
            callback?.Invoke(false, "ServerError");
        }

        else
        {
            //TODO 로그인 실패
            if (callback != null) callback(false, "Else");
        }
    }
    //-----------------------------------------------------------------
    public async Task LogoutTask()
    {
        var logoutInfo = new LogoutInfo
        {
            sessionKey = Managers.DTO.GetApplicationContext().playerInfo.SessionKey,
            serialNum = Managers.Auth.serialNumBase64
        };

        try
        {
            // 서버에 로그아웃 요청을 보냄
            var rsp = await _netRepo.LogoutTask(logoutInfo);

            if (rsp.retCode == "200")
            {
                Debug.Log("Logout Success");
                Managers.UI.CloseAllPopupUI();

                if (Managers.Game.ProjectType == Define.ProjectType.VM3S)
                {
                    Managers.Scene.ChangeScene(Define.S_Scene.Login);
                }
                else if (Managers.Game.ProjectType == Define.ProjectType.VM3F)
                {
                    // F 버전 처리
                    //Managers.Scene.ChangeScene(Define.F_Scene.Login);
                }
            }
            else
            {
                Debug.LogError("Logout Success but return code is not 200");
                Managers.UI.CloseAllPopupUI();

                if (Managers.Game.ProjectType == Define.ProjectType.VM3S)
                {
                    Managers.Scene.ChangeScene(Define.S_Scene.Login);
                }
                else if (Managers.Game.ProjectType == Define.ProjectType.VM3F)
                {
                    // F 버전 처리
                    //Managers.Scene.ChangeScene(Define.F_Scene.Login);
                }
            }
            //로그아웃시 playerInfo 초기화
            Managers.User.ResetEntryFlags();
            Managers.DTO.GetApplicationContext().playerInfo = null;
        }
        catch (Exception ex)
        {
            Debug.LogError("Logout Fail  : " + ex.ToString());
            throw;
            //return false;
        }
    }
    //-----------------------------------------------------------------
    #endregion
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------

    #region <<Join Method>>

    //이메일 회원가입
    // public void JoinEmail(string email, string password, string phone, string gender, string birthday, string name, string height, string weight, Action<bool, string> callback = null)
    // {
    //     birthday = birthday.Substring(0, 4) + "-" + birthday.Substring(4, 2) + "-" + birthday.Substring(6, 2);
    //     _netRepo.JoinEmail(email, password, phone, gender, birthday, name, height, weight).Subscribe(rsp =>
    //     {
    //         if (rsp.retCode == "200")
    //         {
    //             //회원가입 성공
    //             if (callback != null) callback(true, rsp.retCode);
    //         }
    //         else if (rsp.retCode == "700")
    //         {
    //             //회원가입 중복
    //             if (callback != null) callback(true, rsp.retCode);
    //         }
    //         else
    //         {
    //             //회원가입 실패
    //             if (callback != null) callback(false, "사용자가 정보가 조회되지 않습니다.\n확인 후 다시 시도해 주세요.");
    //
    //         }
    //     }, error =>
    //     {
    //         Debug.Log("Login Error" + error);
    //         if (callback != null) callback(false, "서버로부터 응답이 없습니다.\n관리자에게 문의하세요.");
    //     });
    // }
    //-----------------------------------------------------------------
    ////전화번호 회원가입
    //public async Task PhoneSignUpTask(string password, string phone, string gender, string birthday, string name, string height, string weight, string regCode, Action<bool, string> callback = null)
    //{
    //    birthday = birthday.Substring(0, 4) + "-" + birthday.Substring(4, 2) + "-" + birthday.Substring(6, 2);

    //    try
    //    {
    //        // 서버에 로그인 요청을 보냄
    //        BaseResponse rsp = await _netRepo.PhoneSignUpTask(password, phone, gender, birthday, name, height, weight, regCode);

    //        switch (rsp.retCode)
    //        {
    //            case "200":
    //                //회원가입 성공
    //                if (callback != null) callback(true, rsp.retCode);
    //                break;
    //            case "700":
    //                //회원가입 중복
    //                if (callback != null) callback(true, rsp.retCode);
    //                break;
    //            default:
    //                //회원가입 실패
    //                if (callback != null) callback(false, "사용자가 정보가 조회되지 않습니다.\n확인 후 다시 시도해 주세요.");
    //                break;
    //        }
    //    }
    //    catch (Exception e)
    //    {
    //        Debug.LogError($"PhoneSignUpError: {e.Message}");
    //        throw;
    //    }
    //}
    //-----------------------------------------------------------------
    public async Task FaceSignUpTask(string password, string phone, string gender, string birthday, string name,
        string height, string weight, string faceID, string regCode, Action<bool, string, string> callback = null)
    {
        birthday = birthday.Substring(0, 4) + "-" + birthday.Substring(4, 2) + "-" + birthday.Substring(6, 2);
        Debug.Log(password + " " + phone + " " + gender + " " + birthday + " " + name + " " + height + " " + weight + " " + faceID + " " + regCode);

        try
        {
            // 서버에 회원가입 요청을 보냄
            BaseResponse rsp =
                await _netRepo.FaceSignUpTask(password, phone, gender, birthday, name, height, weight, faceID, regCode);

            // 결과가 null인지 확인 (네트워크 오류 또는 서버 응답 실패)
            if (rsp == null)
            {
                //callback?.Invoke(false, "", "서버로부터 응답이 없습니다.\n관리자에게 문의하세요.");
                callback?.Invoke(false, "", Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_03"));
                return;
            }

            switch (rsp.retCode)
            {
                case "200":
                    //회원가입 성공
                    if (callback != null) callback(true, rsp.retCode, "");
                    break;
                case "404":
                    //if (callback != null) callback(false, rsp.retCode, "정보를 찾을 수 없음");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_05"));
                    Debug.LogError("404" + "정보를 찾을 수 없음");
                    break;
                case "500":
                    //if (callback != null) callback(false, rsp.retCode, "서버 오류");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_06"));
                    Debug.LogError("500" + "서버 오류");
                    break;
                case "501":
                    //if (callback != null) callback(false, rsp.retCode, "요청한 데이터 없음");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_07"));
                    Debug.LogError("501" + "요청한 데이터 없음");
                    break;
                case "600":
                    //if (callback != null) callback(false, rsp.retCode, "실패");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_08"));
                    Debug.LogError("600" + "실패");
                    break;
                case "601":
                    //if (callback != null) callback(false, rsp.retCode, "서버 오류(담당자에게 문의)");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_09"));
                    Debug.LogError("601" + "서버 오류(담당자에게 문의)");
                    break;
                case "620":
                    if (callback != null)
                    {
                        string errorInput;
                        switch(rsp.retMsg)
                        {
                            case "phone":
                                //errorInput = "전화번호";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_10");
                                break;
                            case "password":
                                //errorInput = "비밀번호";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_10");
                                break;
                            case "name":
                                //errorInput = "닉네임";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_12");
                                break;
                            case "gender":
                                //errorInput = "성별";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_13");
                                break;
                            case "birthday":
                                //errorInput = "생년월일";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_14");
                                break;
                            case "faceId":
                                //errorInput = "안면인식";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "FaceInputError");
                                break;
                            default:
                                //errorInput = "파라미터";
                                errorInput = Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_15");
                                break;
                        }
                        //errorInput += " 입력 오류";
                        callback(false, rsp.retCode, errorInput);
                    }
                        
                    Debug.LogError("620" + "파라미터 정보가 부족함");
                    break;
                case "622":
                    //if (callback != null) callback(false, rsp.retCode, "관리자에게 문의하세요.");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_17"));
                    Debug.LogError("622" + "전달한 파라미터의 길이가 짧거나 긴 오류");
                    break;
                case "700":
                    //if (callback != null) callback(false, rsp.retCode, "안면인식 데이터가 중복됩니다.");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "FaceDuplicate"));
                    Debug.LogError("700" + "데이터 중복");
                    break;
                case "8003":
                    //if (callback != null) callback(false, rsp.retCode, "사용자 정보를 찾을 수 없음");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_16"));
                    Debug.LogError("8003" + "사용자 정보를 찾을 수 없음");
                    break;
                case "9003":
                    //if (callback != null) callback(false, rsp.retCode, "이미 가입된 회원");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_18"));
                    Debug.LogError("9003" + "이미 가입된 회원");
                    break;
                default:
                    //if (callback != null) callback(false, rsp.retCode, "관리자에게 문의하세요.");
                    if (callback != null) callback(false, rsp.retCode, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_17"));
                    Debug.Log("Default 에러 " + rsp.retCode);
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"FaceSignUpError: {e.Message}");
            return;
        }
    }
    #endregion
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------

    #region <<Authentication related>>

    //-----------------------------------------------------------------

    //-----------------------------------------------------------------
    public async Task RequestSMSAuthTask(string phone, string countryCode, Action<bool, string> callback = null)
    {
        try
        {
            GetLoginRsp rsp = await _netRepo.RequestSMSAuthTask(phone,countryCode);

            Debug.Log(rsp);
            if (rsp.retCode == "200")
            {
                callback(true, rsp.retCode);
                // Test Code
                if (rsp.Data != null && !string.IsNullOrEmpty(rsp.Data.authCode))
                {
                    Managers.DTO.phoneAuthCode = rsp.Data.authCode;
                    Debug.Log($"[SMS 인증번호] {rsp.Data.authCode}");
                }
                else
                {
                    Debug.LogWarning("[SMS] 서버 응답에 authCode가 없습니다. (rsp.Data null 여부: " + (rsp.Data == null) + ")");
                }
            }
            else
            {
                callback(false, rsp.retCode);
                Debug.Log("SMS 인증번호 발급 실패");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"RequestSMSAuth Fail: {e.Message}");
            Debug.Log($"RequestSMSAuth Fail: {e.Message}");
            return;
        }
    }
    //-----------------------------------------------------------------
    public async Task CheckSMSAuthTask(string phone, string authNum, Action<bool, string> callback = null)
    {
        try
        {
            GetLoginRsp rsp = await _netRepo.CheckSMSAuthTask(phone, authNum);

            if (rsp.retCode == "200")
            {
                callback(true, rsp.Data.regCode);
            }

            else if (rsp.retCode == "700")
            {
                callback(false, rsp.retCode);
                Debug.Log("전화번호 중복");
            }
            else
            {
                callback(false, rsp.retMsg);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"CheckSMSAuth Fail: {e.Message}");
            callback(false, "NETWORK_ERROR");
            return;
        }
    }
    //-----------------------------------------------------------------
    public async Task RegInquiryTask(string body, string email, Action<bool, string> callback = null)
    {
        try
        {
            BaseResponse rsp = await _netRepo.RegInquiryTask(body, email);

            if (rsp.retCode == "200") callback(true, rsp.retCode);
            else callback(false, rsp.retMsg);
        }
        catch (Exception e)
        {
            Debug.LogError("CheckSMSAuth Fail: " + e.Message);
            return;
        }
    }

    #endregion
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------
    #region <<Duplicate related>>

    //중복체크
    public async void EmailDuplicateCheckTask(string userType, string email, Action<bool, string> callback = null)
    {
        try
        {
            BaseResponse rsp = await _netRepo.EmailDuplicateCheckTask(userType, email);

            if (rsp.retCode == "200")
                //중복체크 성공
                if (callback != null) callback(true, rsp.retCode);
                else
                //중복체크 실패
                if (callback != null) callback(false, rsp.retCode);

        }
        catch (Exception e)
        {
            Debug.LogError($"Exception: {e.Message}");
            throw;
        }
    }
    //-----------------------------------------------------------------
    //중복체크
    public async Task NickDuplicateCheckTask(string nick, Action<bool, string> callback = null)
    {
        try
        {
            BaseResponse rsp = await _netRepo.NickDuplicateCheckTask(nick);

            if (rsp.retCode == "200")
                //중복체크 성공
                callback?.Invoke(true, rsp.retCode);
            else
                //중복체크 실패
                callback?.Invoke(false, rsp.retCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Exception: {e.Message}");
            return;
        }
    }
    //-----------------------------------------------------------------

    #endregion
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------
    #region <<Find Password>>
    //전화번호 아이디 비밀번호 찾기 ( 재설정을 위한 과정 )
    public async Task FindPhoneIdPasswordTask(string phone, string birthday, string gender, Action<bool, string> callback = null)
    {
        birthday = birthday.Substring(0, 4) + "-" + birthday.Substring(4, 2) + "-" + birthday.Substring(6, 2);

        try
        {
            GetLoginRsp rsp = await _netRepo.FindPhoneIdPasswordTask(phone, birthday, gender);

            if (rsp.retCode == "200")
            {
                //비밀번호 찾기 성공
                if (callback != null) callback(true, rsp.Data.regCode);
            }
            else
            {
                //if (callback != null) callback(false, "사용자가 정보가 조회되지 않습니다.\n확인 후 다시 시도해 주세요.");
                if (callback != null) callback(false, Managers.Locale.GetLocalizeValue("Login", "ErrorMessage_04"));
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"전화번호 아이디 비밀번호 찾기 error: {e.Message}");
            return;
        }
    }
    //-----------------------------------------------------------------
    //전화번호(아이디), 성별, 생년월일로 비밀번호 찾기 후 비밀번호 재설정
    public async Task ChangePhoneIdPasswordTask(string phone, string password, string regCode, Action<bool, string> callback = null)
    {
        try
        {
            GetLoginRsp rsp = await _netRepo.ChangePhoneIdPasswordTask(phone, password, regCode);

            if (rsp.retCode == "200")
            {
                //이메일아이디 찾기 성공
                if (callback != null) callback(true, rsp.retCode);
            }
            else
            {
                if (callback != null) callback(false, "fail");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ChangePhoneIdPasswordError: {e.Message}");
            return;
        }
    }
    //-----------------------------------------------------------------
    //임시 비밀번호 변경
    public async Task ChangePwTask(string oldPass, string newPass, Action<bool, string> callback = null)
    {
        try
        {
            BaseResponse rsp = await _netRepo.ChangePwTask(applicationContext.playerInfo.UserSeq.ToString(), oldPass,
                newPass, applicationContext.playerInfo.IsTempTime);

            if (rsp.retCode == "200")
            {
                //이메일아이디 찾기 성공
                callback?.Invoke(true, rsp.retCode);
            }
            else
            {
                callback?.Invoke(false, "fail");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ChangePwError: {e.Message}");
            return;
        }
    }
    #endregion
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------
    #region <<Create FaceID>>
    public async Task CreateFaceIDTask(Action<string> action_FaceID)
    {
        string str = "";

        try
        {
            CreateFaceID rsp = await _netRepo.CreateFaceIdTask();

            if (rsp.retCode == "200")
            {
                str = rsp.data.faceId;
                action_FaceID?.Invoke(str);
            }
            else
            {
                Debug.LogError("Failed to create face id");
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            return;
        }
    }

    #endregion
    //-----------------------------------------------------------------
    //-----------------------------------------------------------------
    #region<<서버통신>>
    /// <summary>
    /// 서버 Json Data 전송
    /// 세션: 0155158206bec8b8c3f4391bb
    /// 디바이스: NDlCOTcyQjI4OENCNTU2MdvZz7k1aMzZFghVEfQiAI0=
    /// 디바이스: MDQtN0MtMTYtQUEtNTgtRUU=
    /// </summary>
    /// <param name="url"></param>
    /// <param name="data"></param>
    /// <param name="sessionKey"></param>
    /// <param name="deviceCode"></param>
    /// <returns></returns>
    public IEnumerator JsonDataPost(string url, System.Object data, bool isForm = true, bool isPosture = false, Byte[] frontImage = null, Byte[] sideImage = null, Action<string> callback = null)
    {
        string sessionKey = Managers.DTO.GetApplicationContext().playerInfo.SessionKey;
        string serialNum = Managers.Auth.serialNumBase64;

        //string url = String.Format("https://vm3.mybenefit.co/measure/{0}", measureType);
        UnityEngine.Debug.Log("서버 주소: " + url);

        var jsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
        };

        string json = JsonConvert.SerializeObject(data, jsonSettings);
        UnityEngine.Debug.Log(json);

        UnityWebRequest www;
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);

        if (isForm)
        {
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormDataSection("measureData", bodyRaw));

            if (isPosture)
            {
                formData.Add(new MultipartFormFileSection("front", frontImage, "front.jpg", "image/jpg"));
                formData.Add(new MultipartFormFileSection("side", sideImage, "side.jpg", "image/jpg"));
            }

            www = UnityWebRequest.Post(url, formData);
            www.timeout = 3;
            www.SetRequestHeader("sessionKey", sessionKey);
            www.SetRequestHeader("serialNum", serialNum);
            yield return www.SendWebRequest();
        }
        else
        {
            www = new UnityWebRequest(url, "Post");
            www.timeout = 3;
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("sessionKey", sessionKey);
            www.SetRequestHeader("serialNum", serialNum);
            www.SetRequestHeader("Content-Type", "application/json");
            yield return www.SendWebRequest();
        }

        if (www.result != UnityWebRequest.Result.Success)
        {
            UnityEngine.Debug.Log($"[JsonDataPost 실패] URL: {url}");
            UnityEngine.Debug.Log($"[JsonDataPost 실패] Error: {www.error}");
            UnityEngine.Debug.Log($"[JsonDataPost 실패] ResponseCode: {www.responseCode}");
            UnityEngine.Debug.Log($"[JsonDataPost 실패] ResponseBody: {www.downloadHandler?.text}");
            callback?.Invoke(www.error);
        }
        else
        {
            UnityEngine.Debug.Log("Data Form upload complete!");
            UnityEngine.Debug.Log(www.downloadHandler.text);
            callback?.Invoke(www.downloadHandler.text);
        }
    }

    public IEnumerator JsonDataPostPosture(System.Object data, Byte[] frontImage, Byte[] sideImage)
    {
        yield return JsonDataPost($"{Managers.Auth.vm3SDomain}/measure/POSTURE", data, true, true, frontImage, sideImage);
    }

    //서버 데이터 수신
    public IEnumerator JsonDataGet(string url, Action<string> action_DataGet)
    {
        string sessionKey = Managers.DTO.GetApplicationContext().playerInfo.SessionKey;

        UnityWebRequest www = UnityWebRequest.Get(url);
        www.SetRequestHeader("sessionKey", sessionKey);

        yield return www.SendWebRequest();

        if (www.error == null)
        {
            UnityEngine.Debug.Log(www.downloadHandler.text);
            UnityEngine.Debug.Log("JSON 가져오기 성공");

            action_DataGet.Invoke(www.downloadHandler.text);
        }
        else
        {
            UnityEngine.Debug.Log("JSON 가져오기 실패");
        }
    }
    #endregion
    //-----------------------------------------------------------------

    //-----------------------------------------------------------------
    #region <<로그인 유저 데이터>>
    public static PlayerInfo ConvertUserInfo(GetLoginRsp loginRsp, string number)
    {
        PlayerInfo pi = new PlayerInfo();
        pi.SessionKey = loginRsp.SessionKey;
        pi.User = loginRsp.User;
        pi.LogKey = loginRsp.LogKey;
        pi.UserType = loginRsp.UserType;
        pi.UserSeq = loginRsp.UserSeq;

        if (pi.User.gender == null) pi.User.gender = "N";
        try
        {
            pi.User.height = $"{(float)Convert.ToDouble(loginRsp.User.height):F1}";
        }
        catch
        {
            pi.User.height = "0";
        }

        try
        {
            pi.User.weight = $"{(float)Convert.ToDouble(loginRsp.User.weight):F1}";
        }
        catch
        {
            pi.User.weight = "0";
        }
        pi.UserNumber = number;

        return pi;
    }
    #endregion
    //-----------------------------------------------------------------
}