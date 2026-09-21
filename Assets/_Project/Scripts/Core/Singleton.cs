// 유니티 엔진 기능 사용 (네임스페이스)
using UnityEngine;

namespace Core
{
    // T는 클래스 타입, T는 Component를 상속받은 클래스여야 함
    // Object -> Component -> MonoBehaviour
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
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        // 빈 오브젝트 생성
                        GameObject obj = new GameObject();

                        // T 클래스의 이름 가져오기
                        obj.name = typeof(T).Name;

                        // T 타입 컴포넌트 추가 후 저장
                        _instance = obj.AddComponent<T>();
                    }
                }

                return _instance;
            }
        }

        public virtual void Awake()
        {
            // 싱글톤이 없다면 자기 자신을 싱글톤으로 등록
            if (_instance == null)
            {
                _instance = this as T;

                // 씬이 바뀌어도 존재하도록 설정
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 이미 싱글톤이 있다면 새 객체 삭제
                Destroy(gameObject);
            }
        }
    }
}

// 1. 씬의 하이어라키에 GameManager가 존재한다. 
// 2. Awake() 실행하여 GameManager 자신을 _instance에 넣는다.
// 3. 다른 코드에서 GameManager를 쓰고 싶을 때 프로퍼티를 통해 가져온다.(GameManager.Instance)
// 4. Awake()에서 이미 할당되어 있다면 그대로 _instance를 반환한다.
// 5. 없다면 씬에서 GameManager를 찾고, 그래도 없다면 새 GameObject를 생성하여 GameManager를 붙인다.
// => 어떤 씬에도 하이어라키에 GameManager 안넣었는데 호출한다면 null 에러 대신 그냥 즉시 만들어 버린다.

// 기존 함수(FindObjectOfType<T>())와 가장 직접적으로 대응하는 함수는
// FindFirstObjectByType<T>()지만,
// Unity 공식 문서에서도 임의의 인스턴스면 충분한 경우
// FindAnyObjectByType이 더 빠르다고 안내함.
