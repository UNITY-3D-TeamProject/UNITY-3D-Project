using UnityEngine;

namespace Map.Platforms
{
    /// <summary>발판이 맡은 역할. 역할마다 겉모습이 달라, 보기만 해도 어떤 발판인지 알 수 있다.</summary>
    public enum EPlatformRole
    {
        Basic,      // 움직이지 않는 기본 발판
        Unstable,   // 사라지거나 떨어지는 발판 (점멸, 낙하, 붕괴, 사격 연동, 점프 연동)
        Moving,     // 움직이는 발판 (왕복, 원운동, 밟으면 출발, 가라앉기, 저울 등)
        Rest,       // 쉬어 가는 발판 (구간 사이, 기믹 묶음 사이)
    }

    /// <summary>
    /// 발판의 역할에 맞는 겉모습을 입힌다. 발판 프리팹의 루트에 붙이며, 맵 생성기가 발판을 놓은 직후 SetRole 을 부른다.
    /// 부르지 않으면 프리팹에 저장된 모습(기본 발판) 그대로다.
    /// </summary>
    public class PlatformSkin : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Body")]
        [Tooltip("역할에 따라 머티리얼을 바꿀 몸체 렌더러")]
        [SerializeField] private Renderer _body;

        [Tooltip("기본 발판의 머티리얼")]
        [SerializeField] private Material _basicMaterial;

        [Tooltip("사라지거나 떨어지는 발판의 머티리얼")]
        [SerializeField] private Material _unstableMaterial;

        [Tooltip("움직이는 발판의 머티리얼")]
        [SerializeField] private Material _movingMaterial;

        [Tooltip("쉬어 가는 발판의 머티리얼")]
        [SerializeField] private Material _restMaterial;

        [Header("Extras")]
        [Tooltip("사라지거나 떨어지는 발판일 때만 남기는 자식 오브젝트")]
        [SerializeField] private GameObject[] _unstableExtras;

        [Tooltip("쉬어 가는 발판일 때만 남기는 자식 오브젝트")]
        [SerializeField] private GameObject[] _restExtras;
        #endregion

        #region Public Methods
        /// <summary>
        /// 역할에 맞는 겉모습으로 바꾼다. 그 역할에 쓰이지 않는 장식은 지운다.
        /// 사라지는 발판은 다시 나타날 때 자식을 모두 켜므로, 꺼 두기만 하면 엉뚱한 장식이 함께 켜진다.
        /// </summary>
        /// <param name="role">이 발판이 맡은 역할</param>
        public void SetRole(EPlatformRole role)
        {
            Material material = GetMaterial(role);
            if ((_body != null) && (material != null))
            {
                _body.sharedMaterial = material;
            }

            KeepOrRemove(_unstableExtras, role == EPlatformRole.Unstable);
            KeepOrRemove(_restExtras, role == EPlatformRole.Rest);
        }


        /// <summary>
        /// 그 역할의 발판에 쓰는 머티리얼. 같은 태그의 다른 오브젝트(기믹 묶음 안의 발판 등)를 같은 모습으로 맞출 때 쓴다.
        /// </summary>
        /// <param name="role">발판의 역할</param>
        public Material GetMaterial(EPlatformRole role)
        {
            switch (role)
            {
                case EPlatformRole.Unstable: return _unstableMaterial;
                case EPlatformRole.Moving:   return _movingMaterial;
                case EPlatformRole.Rest:     return _restMaterial;
                default:                     return _basicMaterial;
            }
        }
        #endregion

        #region Private Methods
        private static void KeepOrRemove(GameObject[] extras, bool shouldKeep)
        {
            if (extras == null) return;

            foreach (GameObject extra in extras)
            {
                if (extra == null) continue;

                if (shouldKeep)
                {
                    extra.SetActive(true);
                    continue;
                }

                // Destroy 는 프레임 끝에 처리되므로 그 전에 보이지 않게 먼저 끈다
                extra.SetActive(false);
                if (Application.isPlaying)
                {
                    Destroy(extra);
                }
                else
                {
                    DestroyImmediate(extra);
                }
            }
        }
        #endregion
    }
}
