using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Scripts.Managers
{
    //-----------------------------------------------------------------
    public class UI_Manager
    {
        //-----------------------------------------------------------------
        private int _order = 10;

        // 팝업 컴포넌트들을 스택 구조로 담음 (UI popup 구조에 적합)
        // 오브젝트가 아닌 컴포넌트를 담음 (popup canvas UI들을 담는다)
        private Stack<UI_Popup> _popupStack = new Stack<UI_Popup>();

        public Stack<UI_Popup> PopupStack
        {
            get => _popupStack;
            set => _popupStack = value;
        }

        private UI_Scene _uiScene = null; // 현재의 고정 캔버스 UI

        private GameObject showGo;

        public Define.LoginType LoginType = Define.LoginType.Waiting;
        //-----------------------------------------------------------------
        public GameObject Root
        {
            get
            {
                var root = GameObject.Find("@UI_Root");
                if (root == null)
                    root = new GameObject { name = "@UI_Root" };

                return root;
            }
        }
        //-----------------------------------------------------------------
        public void SetCanvas(GameObject go, bool sort = true)
        {
            var canvas = Utils.GetOrAddComponent<Canvas>(go);
            // Canvas의 render mode는 여기서 설정!
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;

            if (sort)
            {
                canvas.sortingOrder = _order;
                _order++;
            }
            else // !sort로 들어온 경우는 고정 UI라는 뜻
            {
                canvas.sortingOrder = 100;
            }
        }
        //-----------------------------------------------------------------
        public T MakeSubItem<T>(Transform parent = null, string name = null) where T : UI_Base
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            var prefab = Managers.Resource.Load<GameObject>($"Prefabs/UI/SubItem/{name}");

            var go = Managers.Resource.Instantiate(prefab);
            if (parent != null)
                go.transform.SetParent(parent);

            go.transform.localScale = Vector3.one;
            go.transform.localPosition = prefab.transform.position;

            return Utils.GetOrAddComponent<T>(go);
        }
        //-----------------------------------------------------------------
        // 고정 UI 캔버스 프리팹 생성 ShowSceneUI
        public T ShowSceneUI<T>(string name = null) where T : UI_Scene
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            // 프리팹 생성 
            var go = Managers.Resource.Instantiate($"UI/Scene/{name}");
            var uiScene = Utils.GetOrAddComponent<T>(go);
            _uiScene = uiScene;

            go.transform.SetParent(Root.transform); // UI_Root를 부모로 지정

            return uiScene;
        }
        //-----------------------------------------------------------------
        // Stack의 최상위에 있는 개채를 삭제하고 재생성하는 기능
        public T ResetPopupUI<T>(string name = null, bool isCommon = false) where T : UI_Popup
        {
            ClosePopupUI();
            var popup = ShowPopupUI<T>(name, isCommon);
            return popup;
        }
        //-----------------------------------------------------------------
        // 
        /// <summary>
        /// Popup UI 캔버스 프리팹 생성
        /// </summary>
        /// <param name="name">Prefab GameObject Name</param>
        /// <typeparam name="T">UI_Popup의 상속을 받는 popup 프리팹 운영 Script</typeparam>
        /// <returns>해당 타입 반환</returns>
        public T ShowPopupUI<T>(string name = null, bool isCommon = false) where T : UI_Popup // T는 UI_Popup 자식만
        {
            if (string.IsNullOrEmpty(name))
                name = typeof(T).Name;

            //프리팹 생성
            try
            {
                if (isCommon)
                {
                    showGo = Managers.Resource.Instantiate($"UI/Common/{name}");
                }
                else
                {
                    if (Managers.Game.ProjectType == Define.ProjectType.VM3S)
                    {
                        switch (Managers.Scene.CurrentSceneType)
                        {
                            case Define.S_Scene.None:
                            case Define.S_Scene.StartUp:
                                showGo = Managers.Resource.Instantiate($"UI/Main/{name}");
                                break;
                            case Define.S_Scene.ROM:
                                showGo = Managers.Resource.Instantiate($"UI/ROM/{name}");
                                break;
                            case Define.S_Scene.SongExerciseMenu:
                            case Define.S_Scene.SongExercisePlay:
                                showGo = Managers.Resource.Instantiate($"UI/SongExercise/{name}");
                                break;
                            case Define.S_Scene.VideoTraining:
                                showGo = Managers.Resource.Instantiate($"UI/VideoTraining/{name}");
                                break;
                            case Define.S_Scene.Posture:
                                showGo = Managers.Resource.Instantiate($"UI/Posture/{name}");
                                break;
                            case Define.S_Scene.SarcoTest:
                                showGo = Managers.Resource.Instantiate($"UI/Sarcopenia/{name}");
                                break;
                            case Define.S_Scene.CognitiveFunctionMenu:
                            case Define.S_Scene.CognitiveFunctionPlay:
                                showGo = Managers.Resource.Instantiate($"UI/CognitiveFunction/{name}");
                                break;
                            case Define.S_Scene.Inbody:
                            case Define.S_Scene.BPBIO:
                                showGo = Managers.Resource.Instantiate($"UI/Inbody/{name}");
                                break;
                            case Define.S_Scene.Ingrip:
                                showGo = Managers.Resource.Instantiate($"UI/Measurement/{name}");
                                break;
                            default:
                                showGo = Managers.Resource.Instantiate($"UI/Popup/{name}");
                                break;
                        }
                    }
                    else if (Managers.Game.ProjectType == Define.ProjectType.VM3F)
                    {
                        // F 버전 처리
                        switch (Managers.Scene.F_CurrentSceneType)
                        {
                            case Define.F_Scene.Posture:
                            case Define.F_Scene.ROM:
                            case Define.F_Scene.Fitness:
                                showGo = Managers.Resource.Instantiate($"UI/Measurement/{name}");
                                break;
                            default:
                                showGo = Managers.Resource.Instantiate($"UI/Popup/{name}");
                                break;
                        }
                    }
                }
                showGo.name = name;
                var popup = Utils.GetOrAddComponent<T>(showGo);

                _popupStack.Push(popup);
                showGo.transform.SetParent(Root.transform);
                return popup;
            }
            catch
            {
                Debug.Log($"Popup UI 캔버스 프리팹 생성 실패!");
                throw;
            }
        }
        //-----------------------------------------------------------------
        public T FindPopup<T>() where T : UI_Popup
        {
            return _popupStack.Where(x => x.GetType() == typeof(T)).FirstOrDefault() as T;
        }
        //-----------------------------------------------------------------
        public T PeekPopupUI<T>() where T : UI_Popup
        {
            if (_popupStack.Count == 0)
                return null;

            return _popupStack.Peek() as T;
        }
        //-----------------------------------------------------------------
        public T PeekSecondPopupUI<T>() where T : UI_Popup
        {
            if (_popupStack.Count < 2) // 스택에 두 개 이상의 요소가 있는지 확인합니다.
                return null;

            var item = default(T); // 두 번째 요소를 저장할 변수를 초기화합니다.
            var first = _popupStack.Pop(); // 최상위 요소를 잠시 스택에서 제거합니다. 
            item = _popupStack.Peek() as T; // 이제 최상위에 있는 요소가 두 번째 요소가 되었습니다.
            _popupStack.Push(first); // 최상위 요소를 다시 스택에 넣습니다.

            return item; // 두 번째 요소를 반환합니다.
        }
        //-----------------------------------------------------------------
        // Popup UI 닫기 overload (필요에 따라 아래 오버로드된 함수 안에서 사용됨)
        public void ClosePopupUI()
        {
            if (_popupStack.Count == 0) return; // 비어있는 스택이라면 삭제(peek) 불가

            var popup = _popupStack.Pop();
            Managers.Resource.Destroy(popup.gameObject);
            popup = null;
            _order--; //order 줄임
        }
        //-----------------------------------------------------------------
        // Popup UI 닫기 overload (안전장치 용도)
        public void ClosePopupUI(UI_Popup popup)
        {
            if (_popupStack.Count == 0) return; // 비어있는 스택이라면 삭제(peek) 불가

            if (_popupStack.Peek() != popup)
            {
                Debug.Log(_popupStack.Peek().gameObject.name);
                // 스택의 가장 위에 있는 아이템을 반환하는데, 해당 아이템이 popup이 아닌 경우 실패 메세지 출력 후 리턴
                Debug.Log("close Popup Failed!");
                return;
            }

            ClosePopupUI();
        }
        //-----------------------------------------------------------------
        // 모든 popup 닫기
        public void CloseAllPopupUI()
        {
            while (_popupStack.Count > 0)
                ClosePopupUI();
        }
        //-----------------------------------------------------------------
        // Clear (모든 팝업을 닫고 핵심 변수 비우기)
        public void Clear()
        {
            CloseAllPopupUI();
            _uiScene = null;
        }
        //----------------------------------------------------------------- 
    }
}