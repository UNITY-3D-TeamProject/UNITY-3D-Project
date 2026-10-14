using System;
using System.Collections;
using UnityEngine;

namespace Pet
{
    /// <summary>
    /// 플레이어가 생성되면 펫을 생성해 pivot 을 따라가게 하고, 전달받은 캐릭터 상태를 펫 애니메이션으로 넘긴다.
    /// 펫은 플레이어의 자식이 아닌 월드 오브젝트로 생성하며, 이 컴포넌트가 파괴될 때 함께 파괴한다.
    /// 스캔 요청을 받으면 펫을 스캔 위치(_scanPivot)로 보냈다가, 대기 시간 뒤 펫 위치를 알리고 그 자리에 잠시 머문 뒤 pivot 으로 되돌린다.
    /// </summary>
    public class PetSpawner : MonoBehaviour
    {
        #region Serialized Fields
        [Header("References")]
        [SerializeField] private PetFollower _petPrefab;
        [Tooltip("펫이 따라갈 위치. 캐릭터와 함께 회전하는 Transform 아래에 둔다")]
        [SerializeField] private Transform _pivot;
        [Tooltip("스캔할 때 펫이 이동할 위치. pivot 과 같은 Transform 아래, 캐릭터 정면에 둔다")]
        [SerializeField] private Transform _scanPivot;

        [Header("Scan")]
        [Tooltip("스캔 요청 후 펫 위치에서 스캔을 시작하기까지 기다리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _scanDelay = 0.3f;
        [Tooltip("스캔 시작 후 펫이 그 자리에 머물다 pivot 으로 돌아가기까지 기다리는 시간(초)")]
        [SerializeField, Min(0.0f)] private float _scanReturnDelay = 0.5f;
        #endregion

        #region Private Fields
        private PetFollower _pet;
        private PetAnimator _petAnimator;
        private Coroutine _scanRoutine;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            if (!_petPrefab || !_pivot) return;

            _pet = Instantiate(_petPrefab, _pivot.position, _pivot.rotation);
            _pet.SetTarget(_pivot);
            _petAnimator = _pet.GetComponent<PetAnimator>();
        }

        private void OnDestroy()
        {
            if (_pet) Destroy(_pet.gameObject);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 펫 점프 애니메이션을 재생한다.
        /// </summary>
        public void PlayJump()
        {
            if (_petAnimator) _petAnimator.PlayJump();
        }

        /// <summary>
        /// 펫 착지 애니메이션을 재생한다.
        /// </summary>
        public void PlayLand()
        {
            if (_petAnimator) _petAnimator.PlayLand();
        }

        /// <summary>
        /// 펫 피격 애니메이션을 재생한다.
        /// </summary>
        /// <param name="direction">캐릭터 기준 피격 방향. 0: 앞, 1: 뒤, 2: 왼쪽, 3: 오른쪽</param>
        public void PlayHit(int direction)
        {
            if (_petAnimator) _petAnimator.PlayHit(direction);
        }

        /// <summary>
        /// 펫 특수 Idle 애니메이션을 재생한다.
        /// </summary>
        /// <param name="specialIdleIndex">캐릭터가 재생한 특수 Idle 인덱스</param>
        public void PlaySpecialIdle(int specialIdleIndex)
        {
            if (_petAnimator) _petAnimator.PlaySpecialIdle(specialIdleIndex);
        }

        /// <summary>
        /// 펫 추종을 멈추고 사망 애니메이션을 재생한다.
        /// </summary>
        public void PlayDeath()
        {
            CancelScan();
            if (_pet) _pet.StopFollow();
            if (_petAnimator) _petAnimator.PlayDeath();
        }

        /// <summary>
        /// 펫을 스캔 위치로 보내고, 대기 시간 뒤 펫 위치를 onReady 로 알린다. 펫은 그 자리에 머물다 pivot 으로 돌아간다.
        /// 펫이나 스캔 위치가 없으면 기다리지 않고 pivot 위치를 바로 알린다. 진행 중이던 스캔 대기는 버린다.
        /// </summary>
        /// <param name="onReady">스캔 위치(월드 좌표)를 받을 콜백</param>
        public void PlayScan(Action<Vector3> onReady)
        {
            CancelScan();

            if (!_pet || !_scanPivot)
            {
                onReady?.Invoke(_pivot ? _pivot.position : transform.position);
                return;
            }

            _pet.MoveToTarget(_scanPivot, _pivot, _scanDelay);
            _scanRoutine = StartCoroutine(CoScan(onReady));
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// 진행 중인 스캔 대기·머묾을 멈추고 펫을 pivot 추종으로 되돌린다.
        /// </summary>
        private void CancelScan()
        {
            if (_scanRoutine == null) return;

            StopCoroutine(_scanRoutine);
            _scanRoutine = null;
            if (_pet) _pet.ChangeTarget(_pivot);
        }
        #endregion

        #region Coroutines
        /// <summary>
        /// 대기 시간 뒤 펫 위치를 알리고, 펫을 그 자리에 머물게 했다가 pivot 추종으로 되돌린다.
        /// </summary>
        private IEnumerator CoScan(Action<Vector3> onReady)
        {
            yield return new WaitForSeconds(_scanDelay);

            if (!_pet)
            {
                _scanRoutine = null;
                onReady?.Invoke(_pivot.position);
                yield break;
            }

            _pet.HoldPosition();
            onReady?.Invoke(_pet.transform.position);

            yield return new WaitForSeconds(_scanReturnDelay);

            _scanRoutine = null;
            if (_pet) _pet.ChangeTarget(_pivot);
        }
        #endregion
    }
}
