using System;
using System.Collections.Generic;
using UnityEngine;
using Core.Audio;

namespace CharacterSound
{
    /// <summary>
    /// 캐릭터 한 명의 소리 설정과 재생 상태를 가진다. 공통 소리(발소리·점프·착지·피격)는 직렬화 칸으로,
    /// 스킬 소리는 (실제 스킬 이름, 단계) 키로 SO 를 찾는다. 스킬 종류나 이름 문자열로 분기하지 않는다.
    /// 단발음(발소리 포함)은 AudioManager.PlaySfx 로 재생하고, 반복음(보드 이동음·차지음)은 "재생을 원하는 상태"와 실제 시작 기록을 관리한다.
    /// 발소리는 이동 애니메이션의 발 디딤 알림(PlayFootstep)마다 한 번 내며, 낼 수 있는지는 LateUpdate 의 이동 상태 표본으로 정한다.
    /// 반복음은 AudioManager.PlaySfxLoop 로 시작해 받은 핸들을 시작한 매니저와 함께 기록하고, StopSfx(핸들)로 즉시 정지한다.
    /// 시작하지 못한 원하는 반복음은 LateUpdate 에서 다시 시도한다.
    /// 키 비교(설정 조회·중복 요청·탈것 반복음)는 모두 SSoundKey 의 같은 규칙(이름 대소문자 무시 + 단계 값)을 쓴다.
    /// </summary>
    public class SoundController : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Common")]
        [Tooltip("이동 애니메이션에서 발이 땅에 닿을 때마다 한 번 내는 발소리. 보드가 붙어 있는 동안에는 내지 않는다")]
        [SerializeField] private SOAudioData _footstep;
        [SerializeField] private SOAudioData _jump;
        [SerializeField] private SOAudioData _land;
        [SerializeField] private SOAudioData _hit;

        [Header("Landing")]
        [Tooltip("공중에 이 시간(초) 이상 떠 있다가 접지해야 착지음이 난다. 점프와 낙하에 공통 적용된다")]
        [SerializeField, Min(0.0f)] private float _minAirTime = 0.1f;

        [Header("Move")]
        [Tooltip("이 수평 속도(m/s)를 넘게 실제로 움직일 때만 발소리를 낸다")]
        [SerializeField, Min(0.0f)] private float _minMoveSpeed = 0.05f;

        [Header("Skill Sounds")]
        [Tooltip("스킬 소리 표. 실제 스킬 이름(대소문자 무시)과 단계가 같은 행의 소리를 쓴다. 표에 없으면 무음")]
        [SerializeField] private SSkillSound[] _skillSounds;
        #endregion

        #region Private Fields
        // 시작에 성공한 반복음의 기록. 정지는 시작한 매니저에게 핸들로 요청한다
        private readonly struct SActiveLoop
        {
            public AudioManager Manager { get; }
            public SAudioHandle Handle { get; }

            public SActiveLoop(AudioManager manager, SAudioHandle handle)
            {
                Manager = manager;
                Handle = handle;
            }
        }

        private readonly HashSet<SSoundKey> _desiredLoops = new();
        private readonly Dictionary<SSoundKey, SActiveLoop> _activeLoops = new();
        private readonly List<SSoundKey> _releaseBuffer = new();
        private Dictionary<SSoundKey, SOAudioData> _skillSoundMap;
        // 마지막 이동 상태 표본에서 발소리를 낼 수 있었는지
        private bool _canPlayFootstep;
        // 실제 보드가 붙어 있는 동안 계속 재생하는 반복음 키 (보드 스킬 이름 + Move). 있으면 발소리를 내지 않는다
        private SSoundKey? _vehicleLoopKey;
        private bool _hasGroundSample;
        private bool _wasGrounded;
        private float _airborneStartTime;
        // 이동 상태 표본을 읽는 델리게이트. 하나라도 없으면 표본을 만들지 않는다
        private Func<bool> _getHasMoveIntent;
        private Func<float> _getHorizontalSpeed;
        private Func<bool> _getGrounded;
        private Func<bool> _getRolling;
        #endregion

        #region Properties
        private bool CanPlay => isActiveAndEnabled;
        #endregion

        #region Events
        /// <summary>
        /// 이 컴포넌트가 켜져 소리를 낼 준비가 되었을 때 발생한다. 비활성 중 잃은 상태를 받는 쪽이 다시 맞추는 신호이다.
        /// </summary>
        public event Action OnReady;
        #endregion

        #region Unity Lifecycle
        private void OnEnable()
        {
            OnReady?.Invoke();
        }

        // 모터가 Update 에서 이동한 결과를 읽도록 LateUpdate 에서 표본을 만든다
        private void LateUpdate()
        {
            bool hasMoveState = (_getHasMoveIntent != null) && (_getHorizontalSpeed != null) &&
                                (_getGrounded != null) && (_getRolling != null);
            if (hasMoveState)
            {
                UpdateLocomotion(
                    _getHasMoveIntent(),
                    _getHorizontalSpeed() > _minMoveSpeed,
                    _getGrounded(),
                    _getRolling());
            }

            SyncDesiredLoops();
        }

        private void OnDisable()
        {
            StopAllSounds();
            ResetLocomotionTracking();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 점프 시작음을 낸다. 공중 점프도 점프를 시작할 때마다 낸다.
        /// </summary>
        public void PlayJump()
        {
            PlayOneShot(_jump, transform.position);
        }

        /// <summary>
        /// 피격음을 낸다.
        /// </summary>
        public void PlayHit()
        {
            PlayOneShot(_hit, transform.position);
        }

        /// <summary>
        /// 이동 애니메이션의 발 디딤마다 발소리를 한 번 낸다. 마지막 이동 상태 표본에서 발소리를 낼 수 없었으면 생략한다.
        /// </summary>
        public void PlayFootstep()
        {
            if (!_canPlayFootstep) return;

            PlayOneShot(_footstep, transform.position);
        }

        /// <summary>
        /// 스킬 단발음을 낸다. 표에 (skillName, phase) 행이 없으면 아무것도 하지 않는다.
        /// </summary>
        /// <param name="skillName">실제 스킬 이름</param>
        /// <param name="phase">소리 단계</param>
        /// <param name="position">소리 위치. null 이면 이 캐릭터 위치</param>
        public void PlaySkillSound(string skillName, ESoundPhase phase, Vector3? position)
        {
            if (string.IsNullOrEmpty(skillName)) return;
            if (!TryGetSound(new SSoundKey(skillName, phase), out SOAudioData sfx)) return;

            PlayOneShot(sfx, position ?? transform.position);
        }

        /// <summary>
        /// 스킬 반복음을 원하는 상태로 두거나 해제한다. 해제는 표 조회와 무관하게 항상 처리한다.
        /// </summary>
        /// <param name="skillName">실제 스킬 이름</param>
        /// <param name="phase">소리 단계</param>
        /// <param name="isOn">켜면 true</param>
        public void SetSkillLoop(string skillName, ESoundPhase phase, bool isOn)
        {
            if (string.IsNullOrEmpty(skillName)) return;

            SSoundKey key = new SSoundKey(skillName, phase);
            if (isOn)
                RequestLoop(key);
            else
                ReleaseLoop(key);
        }

        /// <summary>
        /// 실제 탈것이 붙어 있는 동안 계속 재생할 반복음을 정한다. 탈것이 붙었을 때 그 스킬 이름으로 호출하고, 하차하면 빈 이름으로 해제한다.
        /// 붙어 있는 동안은 이동·접지 여부와 관계없이 (스킬 이름, Move) 소리를 내고 발소리는 내지 않는다. 표에 소리가 없으면 무음이다.
        /// </summary>
        /// <param name="skillName">탈것 스킬의 실제 이름. 비어 있으면 해제</param>
        public void SetVehicleSound(string skillName)
        {
            // 꺼진 동안은 걸지 않는다 (해제는 항상 처리). 켜질 때 현재 상태로 다시 맞춘다
            if (!CanPlay && !string.IsNullOrEmpty(skillName)) return;

            SSoundKey? key = string.IsNullOrEmpty(skillName)
                ? null
                : new SSoundKey(skillName, ESoundPhase.Move);
            if (_vehicleLoopKey == key) return;

            if (_vehicleLoopKey.HasValue) ReleaseLoop(_vehicleLoopKey.Value);
            _vehicleLoopKey = key;
            if (key.HasValue) RequestLoop(key.Value);
        }

        /// <summary>
        /// 이동 상태를 읽기 위한 델리게이트 주입. 이동 입력 여부, 실제 수평 속도, 접지 여부, 구르는 중 여부.
        /// 넣어 둔 동안 매 LateUpdate 에 표본을 만들어 착지음과 이동음을 갱신한다.
        /// </summary>
        public void SetGetMoveState(
            Func<bool> getHasMoveIntent,
            Func<float> getHorizontalSpeed,
            Func<bool> getGrounded,
            Func<bool> getRolling)
        {
            if ((getHasMoveIntent == null) || (getHorizontalSpeed == null) || (getGrounded == null) || (getRolling == null)) return;

            _getHasMoveIntent = getHasMoveIntent;
            _getHorizontalSpeed = getHorizontalSpeed;
            _getGrounded = getGrounded;
            _getRolling = getRolling;
        }

        /// <summary>
        /// 이동 상태 델리게이트 제거. 이후 표본을 만들지 않는다.
        /// </summary>
        public void ClearGetMoveState()
        {
            _getHasMoveIntent = null;
            _getHorizontalSpeed = null;
            _getGrounded = null;
            _getRolling = null;
            _canPlayFootstep = false;
        }

        /// <summary>
        /// 착지 판정 기준을 비운다. 다음 표본은 기준으로만 쓰이고 소리를 내지 않는다.
        /// 재활성화와 순간이동·리스폰처럼 위치가 불연속하게 바뀐 뒤 호출한다.
        /// </summary>
        public void ResetLocomotionTracking()
        {
            _hasGroundSample = false;
            _wasGrounded = false;
            _airborneStartTime = 0.0f;
        }

        /// <summary>
        /// API 호환용. 상태를 저장하거나 사망을 판단하지 않고 반복음 정리만 한다.
        /// 생존/사망에 따른 재생 허용은 호출하는 조율 계층이 정한다.
        /// </summary>
        public void NotifyDeath()
        {
            StopAllSounds();
        }

        /// <summary>
        /// API 호환용. 상태를 저장하거나 사망을 판단하지 않고 착지 판정 기준 초기화만 한다.
        /// </summary>
        public void ResetForReuse()
        {
            ResetLocomotionTracking();
        }

        /// <summary>
        /// 원하는 반복음을 모두 해제하고 탈것 반복음 설정도 해제한다. 현재 상태는 필요하면 다시 맞춘다.
        /// </summary>
        public void StopAllSounds()
        {
            _releaseBuffer.Clear();
            _releaseBuffer.AddRange(_desiredLoops);
            foreach (SSoundKey key in _releaseBuffer)
            {
                ReleaseLoop(key);
            }

            _releaseBuffer.Clear();
            _canPlayFootstep = false;
            _vehicleLoopKey = null;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 이동 상태 표본으로 착지음을 내고 발소리를 낼 수 있는지 갱신한다. 매 프레임 LateUpdate 에서 호출한다.
        /// 발소리는 이동 의도, 실제 이동, 접지, 비구르기를 모두 만족하고 탈것이 붙어 있지 않으며 컴포넌트가 켜져 있을 때만 낸다.
        /// </summary>
        /// <param name="hasMoveIntent">이동 입력(의도)이 있는지</param>
        /// <param name="isMoving">실제로 이동 중인지. 수평 속도가 _minMoveSpeed 를 넘으면 true</param>
        /// <param name="isGrounded">접지 중인지</param>
        /// <param name="isRolling">구르는 중인지</param>
        private void UpdateLocomotion(bool hasMoveIntent, bool isMoving, bool isGrounded, bool isRolling)
        {
            UpdateLanding(isGrounded);

            bool isFootMoving = hasMoveIntent && isMoving && isGrounded && !isRolling;
            _canPlayFootstep = isFootMoving && !_vehicleLoopKey.HasValue && CanPlay;
        }

        /// <summary>
        /// 공중에서 접지로 바뀐 순간에 공중에 머문 시간이 충분하면 착지음을 낸다. 첫 표본은 기준으로만 쓴다.
        /// </summary>
        /// <param name="isGrounded">접지 중인지</param>
        private void UpdateLanding(bool isGrounded)
        {
            if (!_hasGroundSample)
            {
                _hasGroundSample = true;
                _wasGrounded = isGrounded;
                _airborneStartTime = Time.time;
                return;
            }

            if (_wasGrounded && !isGrounded)
            {
                _airborneStartTime = Time.time;
            }
            else if (!_wasGrounded && isGrounded)
            {
                bool hasBeenAirborne = (Time.time - _airborneStartTime) >= _minAirTime;
                if (hasBeenAirborne) PlayOneShot(_land, transform.position);
            }

            _wasGrounded = isGrounded;
        }

        /// <summary>
        /// key 의 반복음을 원하는 상태로 기록하고 시작을 요청한다. 이미 원하는 상태면 다시 요청하지 않는다.
        /// </summary>
        /// <param name="key">반복음 키</param>
        /// <returns>원하는 상태로 기록되어 있으면 true. 재생할 수 없거나 소리가 없으면 false</returns>
        private bool RequestLoop(SSoundKey key)
        {
            if (!CanPlay) return false;
            if (!TryGetSound(key, out SOAudioData sfx)) return false;

            if (_desiredLoops.Add(key)) StartLoop(key, sfx);
            return true;
        }

        /// <summary>
        /// key 의 반복음을 원하는 상태에서 빼고 정지를 요청한다. 소리 조회에 의존하지 않는다.
        /// </summary>
        /// <param name="key">반복음 키</param>
        private void ReleaseLoop(SSoundKey key)
        {
            if (_desiredLoops.Remove(key)) StopLoop(key);
        }

        /// <summary>
        /// 원하는 반복음 중 실제 재생 기록이 없는 키의 시작을 다시 시도한다. 매니저가 늦게 생기거나 시작이 실패한 경우를 보완한다.
        /// 살아 있는 매니저의 기록은 유지하며, 파괴된 매니저의 기록만 제거한다.
        /// </summary>
        private void SyncDesiredLoops()
        {
            if (!CanPlay) return;

            foreach (SSoundKey key in _desiredLoops)
            {
                if (_activeLoops.TryGetValue(key, out SActiveLoop activeLoop))
                {
                    if (activeLoop.Manager != null) continue;

                    _activeLoops.Remove(key);
                }

                if (TryGetSound(key, out SOAudioData sfx)) StartLoop(key, sfx);
            }
        }

        /// <summary>
        /// key 의 반복음을 transform 을 따라가며 시작하고, 발급된 핸들을 시작한 매니저와 함께 기록한다.
        /// 매니저나 클립이 없어 시작하지 못하면 기록하지 않으며 원하는 상태는 유지되어 SyncDesiredLoops 가 다시 시도한다.
        /// </summary>
        /// <param name="key">반복음 키</param>
        /// <param name="sfx">재생할 소리</param>
        private void StartLoop(SSoundKey key, SOAudioData sfx)
        {
            if (!CanPlay) return;

            if (_activeLoops.TryGetValue(key, out SActiveLoop activeLoop))
            {
                if (activeLoop.Manager != null) return;

                _activeLoops.Remove(key);
            }

            AudioManager manager = AudioManager.Instance;
            if (manager == null) return;
            if ((sfx == null) || (sfx.ClipCount == 0)) return;

            SAudioHandle handle = manager.PlaySfxLoop(sfx, transform);
            if (handle.IsValid) _activeLoops[key] = new SActiveLoop(manager, handle);
        }

        /// <summary>
        /// key 의 반복음을 시작했던 매니저에 즉시 정지를 요청하고 기록을 지운다.
        /// 현재 AudioManager.Instance 나 SO 조회에 의존하지 않는다. 매니저가 파괴되었으면 기록만 지운다.
        /// </summary>
        /// <param name="key">반복음 키</param>
        private void StopLoop(SSoundKey key)
        {
            if (!_activeLoops.TryGetValue(key, out SActiveLoop activeLoop)) return;

            if (activeLoop.Manager != null) activeLoop.Manager.StopSfx(activeLoop.Handle);
            _activeLoops.Remove(key);
        }

        /// <summary>
        /// 단발음을 position 에서 낸다. 비활성 상태, 소리 없음, 클립 없음, 매니저 없음이면 생략한다.
        /// </summary>
        /// <param name="sfx">재생할 소리</param>
        /// <param name="position">소리 위치</param>
        private void PlayOneShot(SOAudioData sfx, Vector3 position)
        {
            if (!CanPlay) return;
            if ((sfx == null) || (sfx.ClipCount == 0)) return;
            if (AudioManager.Instance == null) return;

            AudioManager.Instance.PlaySfx(sfx, position);
        }

        /// <summary>
        /// key 에 해당하는 소리를 스킬 소리 표에서 찾는다.
        /// </summary>
        /// <param name="key">소리 키</param>
        /// <param name="sfx">찾은 소리</param>
        /// <returns>소리를 찾았으면 true</returns>
        private bool TryGetSound(SSoundKey key, out SOAudioData sfx)
        {
            EnsureSkillSoundMap();
            return _skillSoundMap.TryGetValue(key, out sfx) && (sfx != null);
        }

        /// <summary>
        /// 스킬 소리 표를 Dictionary 로 만든다. 이름이 비었거나 소리가 없는 행은 빼고, 키가 겹치면 먼저 나온 행을 쓴다.
        /// 다른 컴포넌트가 Awake 보다 먼저 부를 수 있어 처음 쓸 때 만든다.
        /// </summary>
        private void EnsureSkillSoundMap()
        {
            if (_skillSoundMap != null) return;

            _skillSoundMap = new Dictionary<SSoundKey, SOAudioData>();
            if (_skillSounds == null) return;

            foreach (SSkillSound row in _skillSounds)
            {
                if (string.IsNullOrWhiteSpace(row.SkillName) || (row.AudioData == null)) continue;

                SSoundKey key = new SSoundKey(row.SkillName, row.Phase);
                if (!_skillSoundMap.TryAdd(key, row.AudioData))
                {
                    Debug.LogWarning($"[{name}] 스킬 소리 키가 겹칩니다. 먼저 나온 행을 씁니다: {row.SkillName} / {row.Phase}", this);
                }
            }
        }
        #endregion
    }
}
