using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Scripts.Managers
{
    // ILocalManager 인터페이스 정의
    public interface ILocaleManager
    {
        Task ChangeLocaleAsync(int index);
        Task LoadAndProcessStringTable(string tableName);
        Task LoadAndProcessAllStringTables();
    }
    //-----------------------------------------------------------------
    public class LocaleManager : ILocaleManager
    {
        //-----------------------------------------------------------------
        private bool _isChanging;
        private Define.LocaleCode _currentLocaleCode;
        private Locale _locale;

        private static readonly Dictionary<Define.LocaleCode, string> _localeCodeMap = new()
        {
            { Define.LocaleCode.Ko, "ko" },
            { Define.LocaleCode.En, "en" },
            { Define.LocaleCode.Ja, "ja" },
            { Define.LocaleCode.Th, "th" },
        };

        public LocaleManagerDecorator LocaleM_Decorator;
        //-----------------------------------------------------------------

        //TODO 현재 사용 용도가 명확하지 않음
        //현재 로컬라이징된 언어 저장 및 반환
        public Define.LocaleCode CurrentLocaleCode
        {
            get
            {
                if (_currentLocaleCode == Define.LocaleCode.Ko)
                    return Define.LocaleCode.Ko;
                if (_currentLocaleCode == Define.LocaleCode.En)
                    return Define.LocaleCode.En;
                if (_currentLocaleCode == Define.LocaleCode.Ja)
                    return Define.LocaleCode.Ja;
                if (_currentLocaleCode == Define.LocaleCode.Th)
                    return Define.LocaleCode.Th;

                return Define.LocaleCode.Unknown;
            }
            set => _currentLocaleCode = value;
        }

        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
        #region <<Functions>>

        public async Task ChangeLocaleAsync(int index)
        {
            if (_isChanging)
                return; // 이미 변경 중인 경우, 단순 리턴

            _isChanging = true;
            // Wait for the LocalizationSettings to initialize.
            await LocalizationSettings.InitializationOperation.Task;

            // enum 캐스팅으로 LocaleCode 결정 (인덱스 순서 의존 제거)
            var localeCode = (Define.LocaleCode)index;
            if (!_localeCodeMap.TryGetValue(localeCode, out var code))
            {
                Debug.LogError($"Unknown LocaleCode: {localeCode}");
                _isChanging = false;
                return;
            }

            // Identifier.Code로 로케일 검색 (Addressable 그룹 순서에 무관)
            var targetLocale = LocalizationSettings.AvailableLocales.Locales
                .FirstOrDefault(l => l.Identifier.Code == code);

            if (targetLocale == null)
            {
                Debug.LogError($"Locale not found: {code}");
                _isChanging = false;
                return;
            }

            LocalizationSettings.SelectedLocale = targetLocale;
            _currentLocaleCode = localeCode;

            // Ensure new locale is applied fully
            await Task.Delay(100);

            _isChanging = false;
        }
        //-----------------------------------------------------------------
        // 특정 String Table의 모든 엔트리를 비동기로 불러와서 처리하는 메서드
        public async Task LoadAndProcessStringTable(string tableName)
        {
            // 비동기로 지정한 이름의 String Table을 불러옴
            var tableHandle = LocalizationSettings.StringDatabase.GetTableAsync(tableName);

            // 비동기 작업 끝날 때까지 대기
            await tableHandle.Task;

            // 불러온 String Table이 유효한지 확인
            if (tableHandle.Result == null)
            {
                Debug.LogError($"String Table '{tableName}' 찾을 수 없음.");
                return;
            }

            // Table의 모든 엔트리 순회
            foreach (var entry in tableHandle.Result)
            {
                // 각 엔트리의 키에 대한 로컬라이즈된 문자열 비동기로 요청
                var localizedStringHandle =
                    LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, entry.Key);
                // 비동기 작업을 Task<string>으로 변환해서 await 사용 가능하게 함
                var localizedString = await Utils.ToTask(localizedStringHandle);
                // 로컬라이즈된 문자열 사용해서 필요한 작업 수행. 예: UI 업데이트, 로그 출력 등
                Debug.Log($"Key: {entry.Key}, Localized String: {localizedString}");
            }
        }
        //-----------------------------------------------------------------
        public async Task LoadAndProcessAllStringTables()
        {
            // 비동기로 지정한 모든 String Table을 불러옴
            var tablesHandle = LocalizationSettings.StringDatabase.GetAllTables();
            // 비동기 작업 끝날 때까지 대기
            await tablesHandle.Task;

            // 불러온 String Table이 유효한지 확인
            if (tablesHandle.Result == null)
            {
                Debug.LogError($"Table List를 찾을 수 없습니다.");
                return;
            }

            // Table의 모든 엔트리 순회
            foreach (var table in tablesHandle.Result)
            {
                var tableName = table.TableCollectionName;

                // GetAllTables로 가져온 테이블은 key를 long type ID로 가져오기 때문에 SharedData 프로퍼티를 사용
                foreach (var entry in table.SharedData.Entries)
                {
                    // 각 엔트리의 키에 대한 로컬라이즈된 문자열 비동기로 요청
                    var localizedStringHandle =
                        LocalizationSettings.StringDatabase.GetLocalizedStringAsync(tableName, entry.Key);
                    // 비동기 작업을 Task<string>으로 변환해서 await 사용 가능하게 함
                    var localizedString = await Utils.ToTask(localizedStringHandle);
                    // 로컬라이즈된 문자열 사용해서 필요한 작업 수행. 예: UI 업데이트, 로그 출력 등
                    Debug.Log($"Key: {entry.Key}, Localized String: {localizedString}");
                }
            }
        }
        //-----------------------------------------------------------------
        /// <summary>
        /// Locale Text 값을 변경하는 Method, 부가적으로 해당 Text Object의 이름도 바꿀 key값으로 바뀐다.
        /// </summary>
        /// <param name="go">바꿀 Text Component를 가진 GameObject</param>
        /// <param name="key">바꿀 key값 (GameObject 변수명을 typeof 변환을 거쳐 string으로 추출하는 방식으로 사용을 권장)</param>
        public string ChangeTextValue(GameObject go, string key, string tag = null)
        {
            go.name = key;
            var stringEvent = go.GetComponent<LocalizeStringEvent>();

            if (tag != null && go.tag.ToString() != tag)
            {
                go.tag = tag;
            }

            // 이름(=key)이 바뀔 때마다 StringReference를 갱신해야 함
            go.GetComponent<TextLocalizer>()?.SetNewTable();

            if (stringEvent != null)
            {
                stringEvent.StringReference.RefreshString();
                go.GetComponent<TextMeshProUGUI>().text = go.GetComponent<TextMeshProUGUI>().text.Replace("[]", "\n");
            }

            return go.GetComponent<TextMeshProUGUI>().text;
        }

        public string GetLocalizeValue(string table, string key)
        {
            return LocalizationSettings.StringDatabase.GetLocalizedString(table, key, LocalizationSettings.SelectedLocale).Replace("[]", "\n");
        }

        //-----------------------------------------------------------------
        // 현재 설정된 로케일에 따라 경로를 받아옴 ▶️ 추후에 수정
        public string GetPathByLocale()
        {
            // 언어에 대한 분기 처리 ▶️ 로컬에서 불러오는 폴더의 이름 적용
            string language = null;
            switch (LocalizationSettings.SelectedLocale.ToString()) // 현재 로케일 코드에 따라
            {
                case "Korean (ko)": language = "Korean(ko)"; break; // 한국어 일 때,
                case "English (en)": language = "English(en)"; break; // 영어일 때,
                case "Japanese (ja)": language = "Japanese(ja)"; break; // 일본어일 때,
                case "Thai (th)": language = "Thai(th)"; break; // 태국어일 때,
                default: language = "Korean (ko)"; break; // 그 외 Defalut 값 ▶️ 한국어 처리
            }
            return language;
        }

        #endregion
        //-----------------------------------------------------------------
        //-----------------------------------------------------------------
        #region <<Init and Event Method>>

        public void Init()
        {
            ILocaleManager baseManager = this;
            var lastCalledFunction = "No Function Yet";
            // LocaleManagerDecorator를 필요한 파라미터와 함께 초기화
            LocaleM_Decorator = new LocaleManagerDecorator(baseManager, lastCalledFunction);
            
            // Unity Localization이 초기화될 때까지 기다린 후 현재 선택된 언어를 CurrentLocaleCode에 반영
            LocalizationSettings.InitializationOperation.Completed += (operation) =>
            {
                if (operation.Status == AsyncOperationStatus.Succeeded)
                {
                    SyncCurrentLocaleCode();
                }
            };

            LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
        }
        //-----------------------------------------------------------------
        // 현재 선택된 Locale을 CurrentLocaleCode에 동기화하는 메서드
        private void SyncCurrentLocaleCode()
        {
            var selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale == null)
            {
                _currentLocaleCode = Define.LocaleCode.Unknown;
                return;
            }

            // Locale 이름을 기반으로 LocaleCode 설정
            string localeName = selectedLocale.Identifier.Code; // "ko", "en", "ja" 등
    
            switch (localeName.ToLower())
            {
                case "ko":
                    _currentLocaleCode = Define.LocaleCode.Ko;
                    break;
                case "en":
                    _currentLocaleCode = Define.LocaleCode.En;
                    break;
                case "ja":
                    _currentLocaleCode = Define.LocaleCode.Ja;
                    break;
                case "th":
                    _currentLocaleCode = Define.LocaleCode.Th;
                    break;
                default:
                    _currentLocaleCode = Define.LocaleCode.Unknown;
                    break;
            }
    
            Debug.Log($"Current Locale Synced: {_currentLocaleCode}");
        }

        //-----------------------------------------------------------------
        private void OnLocaleChanged(Locale locale)
        {
            Debug.Log("Locale Changed!");
            SyncCurrentLocaleCode(); // 언어 변경 시에도 동기화
        }
        //-----------------------------------------------------------------
        private void OnDestroy()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        }

        #endregion
        //----------------------------------------------------------------- 
        //-----------------------------------------------------------------
    }

    /// <summary>
    /// 기존 LocalManager 객체의 행동을 동적으로 확장가능하게 하는 클래스 (Decoration Pattern 적용)
    /// </summary>
    public class LocaleManagerDecorator : ILocaleManager
    {
        private readonly ILocaleManager _baseManager;

        public string LastCalledFunction { get; set; }

        public LocaleManagerDecorator(ILocaleManager baseManager, string lastCalledFunction)
        {
            _baseManager = baseManager;
            LastCalledFunction = lastCalledFunction;
        }

        public async Task ChangeLocaleAsync(int index)
        {
            LastCalledFunction = "ChangeLocaleAsync";
            await _baseManager.ChangeLocaleAsync(index);
        }

        public async Task LoadAndProcessStringTable(string tableName)
        {
            // 이 조건문은 LocalizationSettings 초기화 과정을 두번 진행하지 않기 위한 방어 코드
            if (LastCalledFunction != "ChangeLocaleAsync")
            {
                // ChangeLocaleAsync가 마지막으로 호출되지 않았다면,
                // LocalizationSettings 초기화 작업이 끝날 때까지 대기. 이렇게 해서 Localization 시스템이 준비되기 전에 작업 시작하지 않도록 함
                await LocalizationSettings.InitializationOperation.Task;
            }

            // lastCalledFunction 업데이트는 이 조건부 로직 후에 수행
            LastCalledFunction = "LoadAndProcessStringTable";
            await _baseManager.LoadAndProcessStringTable(tableName);
        }

        public async Task LoadAndProcessAllStringTables()
        {
            // 이 조건문은 LocalizationSettings 초기화 과정을 두번 진행하지 않기 위한 방어 코드
            if (LastCalledFunction != "ChangeLocaleAsync")
            {
                // ChangeLocaleAsync가 마지막으로 호출되지 않았다면,
                // LocalizationSettings 초기화 작업이 끝날 때까지 대기. 이렇게 해서 Localization 시스템이 준비되기 전에 작업 시작하지 않도록 함
                await LocalizationSettings.InitializationOperation.Task;
            }

            // lastCalledFunction 업데이트는 이 조건부 로직 후에 수행
            LastCalledFunction = "LoadAndProcessStringTable";
            await _baseManager.LoadAndProcessAllStringTables();
        }
    }
}