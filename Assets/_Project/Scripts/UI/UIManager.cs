using UnityEngine;

namespace UI
{
    public class UIManager : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] private PlayerHudView _playerHudView;

        public void ShowPlayerHud()
        {
            _playerHudView?.Show();
        }

        public void HidePlayerHud()
        {
            _playerHudView?.Hide();
        }
    }
}
