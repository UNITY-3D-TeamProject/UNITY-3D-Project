using UnityEngine;

namespace Core.ObjectPool
{
    /// <summary>
    /// 오브젝트 풀에서 관리되는 모든 오브젝트가 구현해야 하는 계약 인터페이스.
    /// 풀에서 꺼낼 때(OnGet)와 반납할 때(OnRelease) 초기화/정리 로직을 정의한다.
    /// </summary>
    public interface IPoolable
    {
        // 총알을 예로 들면
        // Release(): 총알이 “나 이제 다 썼어. 풀에 넣어줘”라고 요청하는 함수
        // OnRelease(): 풀이 총알을 넣으면서 “다음에 다시 쓸 수 있게 상태를 정리해”라고 호출하는 함수
        // 총알의 이동 속도를 0으로 만드는 코드는 OnRelease()에 넣는 식


        /// <summary>풀에서 꺼내질 때 호출 — 오브젝트 초기화 로직을 여기에 작성한다.</summary>
        void OnGet();

        /// <summary>풀에 반납될 때 호출 — 오브젝트 정리 로직을 여기에 작성한다.</summary>
        void OnRelease();

        /// <summary>자신을 풀에 반납한다. (외부에서 Release 요청 시 사용)</summary>
        void Release();
    }
}
