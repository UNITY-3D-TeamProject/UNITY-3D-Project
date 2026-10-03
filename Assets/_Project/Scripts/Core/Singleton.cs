// 유니티 엔진 기능 사용 (네임스페이스)
using UnityEngine;

namespace Core
{
    // T는 클래스 타입, T는 Component를 상속받은 클래스여야 함
    // Object -> Component -> Behaviour -> MonoBehaviour
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        // 하나만 존재하는 싱글톤 객체
        private static T _instance;

        // 싱글톤 인스턴스를 제공하는 프로퍼티
        public static T Instance
        {
            // 외부에서 싱글톤 가져오기
            get
            {
                if (_instance == null)
                {
                    // 존재하는 T 타입 인스턴스 아무거나 하나 찾기
                    // => 플레이어 SO가 연결된 GameManager 프리팹을 씬에 미리 배치해두면
                    // 씬에 있는 GameManager를 찾아서 반환해준다.
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        // Inspector 참조가 필요한 객체를 빈 GameObject로 자동 생성하면 (ex) GameManager)
                        // 필요한 참조가 연결되지 않는다.
                        // 공용 Singleton<T>에 GameManager 전용 프리팹 생성 처리를 넣지 않고,
                        // 필요한 객체를 씬에 미리 배치하도록 오류를 출력한다.

                        Debug.LogError(
                        $"{typeof(T).Name}이 없습니다. 필요한 객체를 씬에 배치하세요.");

                        //// 빈 오브젝트 생성
                        //GameObject obj = new GameObject();

                        //// T 클래스의 이름 가져오기
                        //obj.name = typeof(T).Name;

                        //// T 타입 컴포넌트 추가 후 저장
                        //_instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        public virtual void Awake()
        {
            // 첫씬에 GameManager A가 있고 두번째씬에 GameManager B가 있는 경우
            // _instance = A , this = B 라면 B를 삭제하고 return 한다.
            if ((_instance !=null)&& (_instance != this))
            {
                Destroy(gameObject);
                return;
            }

            // Instance에서 미리 발견했어도 이 과정은 실행한다.

            // A를 여전히 게임매니저로 등록함
            _instance = this as T;
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            // 등록된 A가 삭제될 때 _instance == this 이므로 _instance = null로 해준다. (정상)
            // A가 존재하는데 B도 존재한다면 B는 삭제되어야할 녀석이다. _instance = A, this = B이므로 _instance(A)는
            // 건드리지 않고 삭제된다.
            if(_instance == this)
            {
                _instance = null;
            }
        }
    }
}


// 기존 싱글톤 코드에서 바꾼 이유)

// 원래 코드는 아래와 같다.
// 1. 게임매니저 A를 씬에 미리 배치해 놓음
// 2. 그런데 A의 Awake()보다 다른 스크립트의 Awake()가 먼저 실행되서 GameManager.Instance를 호출
// 3. _instance가 비어 있어서 FindAnyObjectByType<T>();로 A를 찾아서 저장.
// 4. <문제 발생> A의 원래 Awake()에서 _instance가 들어있는지 확인 후 있으면 A를 삭제하는 코드였음

// => 등록된 객체가 있어도 그게 나(A)라면 삭제하지 말자라는 조건으로 바꿈.


// 1. 첫 씬에 필요한 SO(플레이어)가 연결된 GameManager 프리팹을 미리 배치한다.
// 2. Awake()에서 자신을 _instance에 등록하고,
//    DontDestroyOnLoad로 씬이 바뀌어도 유지한다.
// 3. 다른 코드에서는 GameManager.Instance로 등록된 객체에 접근한다.
// 4. Instance 접근 시 _instance가 비어 있으면 씬에서 기존 객체를 찾는다.
//    찾은 객체의 Awake()가 아직 실행되지 않았을 수도 있다.
// 5. 씬에서도 찾지 못하면 오류를 출력하고 null을 반환한다.
//    필요한 싱글톤 객체는 (Inspector 참조를 연결할 필요가 있다면 연결해서) 씬에 미리 배치해야 한다.
// 6. 다른 GameManager가 이미 등록되어 있으면 Awake()에서 중복 객체만 삭제한다.
// 7. 등록된 객체가 파괴되면 OnDestroy()에서 _instance를 비운다.


// 기존 함수(FindObjectOfType<T>())와 가장 직접적으로 대응하는 함수는
// FindFirstObjectByType<T>()지만,
// Unity 공식 문서에서도 임의의 인스턴스면 충분한 경우
// FindAnyObjectByType이 더 빠르다고 안내함.
