using UnityEngine;

namespace Map.Common
{
    /// <summary>
    /// 맵 생성기 설정 에셋들의 공통 부모. 씬 오브젝트를 가리키지 않는 값만 담는다.
    /// 에셋으로 두면 여러 벌을 만들어 바꿔 끼울 수 있고, 값을 바꿔도 씬 파일이 바뀌지 않는다.
    /// </summary>
    public abstract class SOMapSettings : ScriptableObject
    {
    }
}
