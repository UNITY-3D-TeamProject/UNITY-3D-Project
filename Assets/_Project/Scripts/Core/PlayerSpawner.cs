using Attribute.Core;
using Core;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _playerPrefab;

    private void Start()
    {
        GameObject player = Instantiate(
            _playerPrefab, 
            transform.position, 
            transform.rotation);

        // 데이터 요청해서 가져와서 플레이어의 SO값(딕셔너리)에 넣어주기

        // 방금 생성한 플레이어 인스턴스의 어트리뷰트셋 가져오기
        // AttributeSet은 Dictionary <string,AttributeData> 형식
        AttributeSet _spawnedPlayerattributeSet = player.GetComponent<AttributeSet>();


        // 혹은 그냥 소환만 하고 player에서 가져오기 (근데 게임매니저를 알아야함)

        // GameManager 하나에만 알린다. UI도 모르고 PlayerState에서도 모른다. (GameManager가 모두 직접 알려준다.)
        GameManager.Instance.RegisterPlayer(_spawnedPlayerattributeSet);
    }
}
