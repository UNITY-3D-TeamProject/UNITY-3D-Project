using UnityEngine;
using UnityEngine.SceneManagement;
using Attribute.Core;
using PlayerInput;
using SceneTransition;

namespace MapTest
{
    /// <summary>
    /// 맵 기믹 테스트용 화면 표시. 플레이어 어트리뷰트 값과 씬 전환 상태를 좌측 상단에 보여준다.
    /// 테스트 전용이며 Scenes/Test 의 테스트 씬에서만 쓴다.
    /// </summary>
    public class MapTestHud : MonoBehaviour
    {
        #region Private Fields
        private static readonly string[] AttributeKeys =
        {
            "CurrentHp", "MaxHp", "CurrentBattery", "MoveSpeed", "JumpPower",
        };

        private static readonly ESceneType[] SceneTypes =
        {
            ESceneType.Tutorial, ESceneType.Lobby, ESceneType.Feed,
            ESceneType.File, ESceneType.Security, ESceneType.Live,
        };

        // 씬이 바뀌어도 남도록 static. GameManager가 이전 씬 번호를 저장하는 것을 흉내 낸다.
        private static string _lastTransition = "(아직 없음)";

        private AttributeSet _attributes;
        private GUIStyle _style;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            SceneLoader.OnBeforeSceneChange += HandleBeforeSceneChange;
        }

        private void OnDisable()
        {
            SceneLoader.OnBeforeSceneChange -= HandleBeforeSceneChange;
        }

        private void Update()
        {
            if (_attributes != null) return;

            PlayerInputComponent player = FindAnyObjectByType<PlayerInputComponent>();
            if (player != null)
            {
                _attributes = player.GetComponentInChildren<AttributeSet>();
            }
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.label) { fontSize = 18, richText = true };
                _style.normal.textColor = Color.white;
            }

            Scene scene = SceneManager.GetActiveScene();

            GUILayout.BeginArea(new Rect(12.0f, 12.0f, 460.0f, 420.0f), GUI.skin.box);
            GUILayout.Label($"<b>씬</b>  {scene.buildIndex}번 / {scene.name}", _style);
            GUILayout.Label($"<b>직전 전환 이벤트</b>  {_lastTransition}", _style);

            string enterable = "";
            foreach (ESceneType type in SceneTypes)
            {
                if (SceneLoader.CanEnter(type)) enterable += type + " ";
            }
            GUILayout.Label($"<b>지금 갈 수 있는 씬</b>  {(enterable == "" ? "(없음)" : enterable)}", _style);

            GUILayout.Space(8.0f);

            if (_attributes == null)
            {
                GUILayout.Label("플레이어의 AttributeSet을 찾는 중...", _style);
            }
            else
            {
                foreach (string key in AttributeKeys)
                {
                    if (!_attributes.IsValidTarget(key)) continue;

                    GUILayout.Label($"<b>{key}</b>  {_attributes.GetValue(key):0.##}", _style);
                }
            }

            GUILayout.EndArea();
        }
        #endregion

        #region Private Methods
        private void HandleBeforeSceneChange(ESceneType previous, ESceneType destination)
        {
            _lastTransition = $"{previous}({(int)previous}) → {destination}({(int)destination})";
            Debug.Log($"[MapTestHud] OnBeforeSceneChange: {_lastTransition}");
        }
        #endregion
    }
}
