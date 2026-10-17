using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Map.Lobby.SkySign
{
    /// <summary>
    /// 음악 전광판. 곡 목록을 차례로 "재생"하며 CD 를 돌리고, 제목·재생바·시간·대표 색을 갱신한다.
    /// 지금은 가상 곡 목록을 쓰고, 나중에 로비 BGM 의 실제 곡 정보로 바꿀 예정이다.
    /// </summary>
    public class SkySignMusicPlayer : MonoBehaviour
    {
        #region Serialized Fields
        [Header("Data")]
        [SerializeField] private SOMusicTrackList _trackList;

        [Header("Disc")]
        [Tooltip("돌아가는 CD (라벨 포함)")]
        [SerializeField] private RectTransform _disc;

        [Tooltip("CD 가 한 바퀴 도는 데 걸리는 시간 (초)")]
        [SerializeField] private float _discTurnSeconds = 2.6f;

        [Header("Texts")]
        [SerializeField] private SkySignMarquee _titleMarquee;
        [SerializeField] private TMP_Text _artistText;
        [SerializeField] private TMP_Text _trackNumberText;
        [SerializeField] private TMP_Text _currentTimeText;
        [SerializeField] private TMP_Text _durationText;

        [Header("Progress")]
        [Tooltip("재생바 채움 (가로 앵커로 길이를 바꾼다)")]
        [SerializeField] private RectTransform _progressFill;

        [Header("Tint")]
        [Tooltip("곡 대표 색으로 물들일 그래픽 (알파는 각자 값을 유지)")]
        [SerializeField] private Graphic[] _tintTargets;

        [Tooltip("곡이 바뀔 때 색이 넘어가는 시간 (초)")]
        [SerializeField] private float _tintFadeSeconds = 1.0f;
        #endregion

        #region Private Fields
        private int _trackIndex;
        private float _position;
        private Color _fromColor;
        private Color _toColor;
        private float _tintElapsed;
        #endregion

        #region Unity Lifecycle
        private void Start()
        {
            Debug.Assert(_trackList != null && _trackList.Count > 0, $"[{name}] 곡 목록 누락");

            _trackIndex = Random.Range(0, _trackList.Count);
            _position = Random.Range(0.0f, _trackList.Get(_trackIndex).Duration * 0.5f);
            LoadTrack();
            _fromColor = _toColor;
            _tintElapsed = _tintFadeSeconds;
            ApplyTint(_toColor);
        }

        private void Update()
        {
            _disc.Rotate(0.0f, 0.0f, -360.0f / _discTurnSeconds * Time.deltaTime);

            SMusicTrack track = _trackList.Get(_trackIndex);
            _position += Time.deltaTime;
            if (_position >= track.Duration)
            {
                _position = 0.0f;
                _trackIndex = (_trackIndex + 1) % _trackList.Count;
                LoadTrack();
                track = _trackList.Get(_trackIndex);
            }

            _progressFill.anchorMax = new Vector2(_position / track.Duration, _progressFill.anchorMax.y);
            _currentTimeText.text = FormatTime(_position);

            if (_tintElapsed < _tintFadeSeconds)
            {
                _tintElapsed += Time.deltaTime;
                ApplyTint(Color.Lerp(_fromColor, _toColor, _tintElapsed / _tintFadeSeconds));
            }
        }
        #endregion

        #region Private Methods
        private void LoadTrack()
        {
            SMusicTrack track = _trackList.Get(_trackIndex);
            _titleMarquee.SetText($"{track.Title} — {track.Artist}");
            _artistText.text = track.Artist;
            _trackNumberText.text = $"TRACK {_trackIndex + 1:00}";
            _durationText.text = FormatTime(track.Duration);

            _fromColor = _toColor;
            _toColor = track.Color;
            _tintElapsed = 0.0f;
        }

        private void ApplyTint(Color color)
        {
            foreach (Graphic graphic in _tintTargets)
            {
                graphic.color = new Color(color.r, color.g, color.b, graphic.color.a);
            }
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.FloorToInt(seconds);
            return $"{total / 60:00}:{total % 60:00}";
        }
        #endregion
    }
}
