using UnityEngine;

namespace Map.Tutorial
{
    /// <summary>
    /// 튜토리얼 첫 상호작용 자리에 누워 잠든 펫 연출. 실제 펫(PetFollower)과는 별개의 보여주기용 모델이다.
    /// 시작하면 R11_Death 끝 자세(바닥에 누움)로 멈추고 눈·입을 감은 표정으로 둔다.
    /// 누운 자세는 얼굴이 바닥을 향하므로, 씬에서 이 오브젝트를 몸통 중심으로 앞으로 기울여 얼굴이 보이게 둔다.
    /// 상호작용(InteractButton) On Interacted 에서 이 오브젝트를 끄고 대화와 Tutorial.UnlockPet 을 실행한다.
    /// </summary>
    public class SleepingPet : MonoBehaviour
    {
        #region Constants
        private static readonly int SleepStateHash = Animator.StringToHash("Sleep");
        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        // 표정 텍스처(Emotions3)는 가로로 10칸이다
        private const int EMOTION_COUNT = 10;
        #endregion

        #region Serialized Fields
        [Header("References")]
        [Tooltip("Sleep(기본) 상태가 있는 애니메이터")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Renderer _eyesRenderer;
        [SerializeField] private Renderer _mouthRenderer;

        [Header("Face")]
        [Tooltip("잠든 동안의 눈·입 표정 칸 (0~9). 3 은 감은 눈 — —, 일자 입 —")]
        [SerializeField, Range(0, EMOTION_COUNT - 1)] private int _sleepEyesIndex = 3;
        [SerializeField, Range(0, EMOTION_COUNT - 1)] private int _sleepMouthIndex = 3;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            // 쓰러지는 동작 없이 바로 누운 자세로 시작한다
            if (_animator) _animator.Play(SleepStateHash, 0, 1.0f);
            SetFace(_sleepEyesIndex, _sleepMouthIndex);
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 눈·입 표정을 표정 텍스처의 해당 칸으로 바꾼다. 이 오브젝트의 머티리얼 사본만 바뀐다.
        /// </summary>
        /// <param name="eyesIndex">눈 표정 칸 (0~9)</param>
        /// <param name="mouthIndex">입 표정 칸 (0~9)</param>
        private void SetFace(int eyesIndex, int mouthIndex)
        {
            if (_eyesRenderer) _eyesRenderer.material.SetTextureOffset(BaseMapId, new Vector2((float)eyesIndex / EMOTION_COUNT, 0.0f));
            if (_mouthRenderer) _mouthRenderer.material.SetTextureOffset(BaseMapId, new Vector2((float)mouthIndex / EMOTION_COUNT, 0.0f));
        }
        #endregion
    }
}
