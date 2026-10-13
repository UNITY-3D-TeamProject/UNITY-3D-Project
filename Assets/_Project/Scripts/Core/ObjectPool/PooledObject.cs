using UnityEngine;

namespace Core.ObjectPool
{
    /// <summary>
    /// 풀에서 만든 오브젝트에 자동으로 붙어 "돌아갈 풀"을 기억하는 컴포넌트. (풀 내부용)
    /// 사용하는 쪽은 이 컴포넌트를 몰라도 된다. 반납은 ObjectPoolManager.Instance.Release(gameObject)로 한다.
    /// ParticleSystem의 Stop Action을 Callback으로 두면 재생이 끝날 때 자동으로 반납된다.
    /// 재사용 시 상태 초기화는 이 컴포넌트가 하지 않는다. 프리팹 컴포넌트가 OnEnable에서 직접 한다.
    /// </summary>
    // 이 컴포넌트는 직접 붙이지 않는다. PrefabPool.CreateObject가 생성 시 자동으로 붙이고 소속 풀·키를 기록한다.
    //
    // 사용 예 (총알이 무언가에 맞았을 때)
    //   변경 전: Destroy(gameObject);
    //   변경 후: ObjectPoolManager.Instance.Release(gameObject);  → 내부에서 이 컴포넌트의 Release()가 불린다.
    //
    // [DisallowMultipleComponent]: 한 오브젝트에 두 개 붙지 않게 막는다. (돌아갈 풀은 하나뿐)
    // [AddComponentMenu("")]: Add Component 메뉴에서 숨긴다. (직접 붙일 일이 없으므로)
    [DisallowMultipleComponent]
    [AddComponentMenu("")]
    public class PooledObject : MonoBehaviour
    {
        #region Private Fields
        // 이 오브젝트를 만든 카테고리 풀. 반납할 때 여기로 돌아간다.
        // 풀을 거치지 않고 프리팹에 직접 붙여 둔 경우에는 null이다.
        private PrefabPool _owner;

        // 이 오브젝트가 등록된 키. 카테고리 안에서 "어느 풀"로 돌아갈지 찾을 때 쓴다.
        private string      _key;

        // 풀에 들어가 있는(반납된) 상태인지. 처음 생성됐을 때는 아직 꺼내지기 전이므로 true.
        // 활성/비활성(activeSelf)으로 판단하지 않는 이유: 사용 중에 게임 로직이 오브젝트를 꺼 둘 수도 있기 때문.
        private bool        _isReleased = true;
        #endregion

        #region Properties
        // 풀에 들어가 있는(반납된) 상태인지 알려준다.
        public bool IsReleased => _isReleased;

        // 풀이 만들어 소속 풀이 기록됐는지. 프리팹에 직접 붙여 둔 경우에는 false다.
        // ObjectPoolManager.Release가 반납할지 Destroy할지 정할 때 쓴다.
        public bool IsBound => _owner != null;
        #endregion

        #region Unity Lifecycle
        /// <summary>
        /// 루트 ParticleSystem의 Stop Action이 Callback이면 재생이 끝날 때 호출된다.
        /// </summary>
        // [호출] 유니티가 자동 호출한다. 같은 GameObject의 ParticleSystem 재생이 끝나고 Stop Action이 Callback일 때만.
        // 이펙트는 코드에서 반납을 따로 호출하지 않아도 재생이 끝나면 풀로 돌아간다.
        private void OnParticleSystemStopped()
        {
            Release();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// 자신을 풀에 반납한다. 이미 반납된 상태면 무시한다.
        /// </summary>
        // [호출] ObjectPoolManager.Release, 파티클 재생 종료 콜백에서 호출된다. 사용하는 쪽은 매니저의 Release를 쓴다.
        public void Release()
        {
            // 이중 반납 방지: 이미 풀에 있으면 아무것도 하지 않는다.
            // (예: 총알이 수명 종료와 충돌을 같은 프레임에 겪어 두 번 호출되는 경우)
            // 풀을 거치지 않은 오브젝트(_owner == null)도 _isReleased가 true라서 여기서 걸러진다.
            if (_isReleased) return;

            // 씬이 내려가는 중이라 매니저가 이미 파괴됐으면 돌아갈 곳이 없으므로 파괴한다.
            if (!_owner.IsAlive)
            {
                Destroy(gameObject);
                return;
            }

            // 자기를 만든 카테고리 풀에 "이 키의 풀로 돌려보내 줘"라고 요청한다.
            _owner.Release(_key, this);
        }

        /// <summary>풀이 인스턴스를 만들 때 소속 카테고리 풀과 키를 기록한다.</summary>
        // [호출] PrefabPool.CreateObject에서 생성 직후 한 번 호출된다.
        public void Bind(PrefabPool owner, string key)
        {
            _owner = owner;
            _key   = key;
        }

        /// <summary>사용 중 상태로 바꾼다. 이제 Release가 동작한다.</summary>
        // [호출] PrefabPool.Get에서 위치 설정 후, 활성화 직전에 호출된다.
        //        활성화 전에 바꾸므로 OnEnable에서 IsReleased는 false로 보인다.
        public void PrepareForGet()
        {
            _isReleased = false;
        }

        /// <summary>반납 상태로 바꾼다.</summary>
        // [호출] PrefabPool.OnReleaseObject에서 비활성화 직전에 호출된다.
        //        먼저 바꾸므로 OnDisable 도중 Release가 다시 불려도 무시된다.
        public void PrepareForRelease()
        {
            _isReleased = true;
        }
        #endregion
    }
}
