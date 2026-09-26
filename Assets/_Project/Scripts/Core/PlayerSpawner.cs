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
        // 근데 순서를 잘 맞춰서 가져와야함.
        AttributeSet attributeSet = player.GetComponent<AttributeSet>();
        GameManager.Instance.playerState.RegisterPlayer(attributeSet);
        // 혹은 그냥 소환만 하고 player에서 가져오기 (근데 게임매니저를 알아야함)
    }
}
