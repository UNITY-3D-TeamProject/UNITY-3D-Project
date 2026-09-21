using UnityEngine; // 유니티 엔진 기능 사용 (네임스페이스)

namespace System
{
    // T는 클래스 타입
    // T는 Component를 상속받은 클래스여야함
    public class Singleton<T> : MonoBehaviour where T : Component 
    {
        private static T _instance; // 하나만 존재하는 싱글톤 객체

        public static T Instance // 프로퍼티
        {
            get // 외부에서 싱글톤 가져오기
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<T>();
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject(); // 빈 오브젝트 생성
                        obj.name = typeof(T).Name; // T 클래스의 이름 가져오기
                        _instance = obj.AddComponent<T>(); // T타입 컴포넌트 추가후 저장
                    }
                }
                return _instance;
            }
        }
        public virtual void Awake()
        {
            if (_instance == null) // 싱글톤이 없다면
            {
                _instance = this as T; // 자기 자신을 싱글톤으로 등록
                DontDestroyOnLoad(gameObject); // 씬 바꿔도 존재하게 설정
            }
            else
            {
                Destroy(gameObject); // 이미 싱글톤이 있다면 새 객체 삭제
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