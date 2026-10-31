using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.PlayerHud
{
    public class PlayerHudView : BaseView
    {
        // 크로스헤어 원호 한 칸(90°). 원호·심은 같은 fill로, 글로우는 양 끝을 늘린 만큼 더 채운다.
        [Serializable]
        private struct SArcCell
        {
            public Image[] FillImages;
            public Image GlowImage;
        }

        // 원호 한 칸의 최대 fill (90° / 360°)
        private const float ARC_CELL_MAX_FILL = 0.25f;
        // 글로우는 칸 양 끝을 6°씩 늘려 두었으므로 그만큼 더 채운다. (12° / 360°)
        private const float GLOW_EXTRA_FILL = 12.0f / 360.0f;

        // HP 칸 게이지들. 최대 HP를 칸 개수로 나눈 값이 한 칸의 HP가 된다. (10칸, 최대 HP 100 => 한 칸 10)
        [Header("HP Cells")]
        [SerializeField] private Slider[] _hpCellSliders;
        [SerializeField] private TMP_Text _hpPercentText;

        // 크로스헤어 원호 칸들. 앞 칸부터 차례로 채운다. (2칸 => 0~50% 첫 칸, 50~100% 둘째 칸)
        [Header("Crosshair Arcs")]
        [SerializeField] private SArcCell[] _heatArcCells;
        [SerializeField] private TMP_Text _heatPercentText;
        [SerializeField] private SArcCell[] _batteryArcCells;
        [SerializeField] private TMP_Text _batteryPercentText;

        // 과열로 열이 내려가는 동안 히트 원호를 진한 색으로 바꾸고 빠르게 점멸시킨다.
        [Header("Overheat")]
        [SerializeField] private Color _overheatColor = new Color(0.85f, 0.04f, 0.04f, 1.0f);
        [Tooltip("켜짐/꺼짐 한 번씩 유지되는 시간 (초)")]
        [SerializeField, Min(0.01f)] private float _overheatBlinkInterval = 0.1f;
        [Tooltip("꺼졌을 때 남는 투명도 배율 (0 = 완전히 사라짐)")]
        [SerializeField, Range(0.0f, 1.0f)] private float _overheatBlinkMinAlpha = 0.15f;

        // 스킬 카드의 원형 쿨타임 바(Filled 이미지)와 남은 시간 글자
        [Header("Skill Cards")]
        [SerializeField] private Image _rollCooldownBar;
        [SerializeField] private TMP_Text _rollCooldownText;
        [SerializeField] private Image _bluetoothCooldownBar;
        [SerializeField] private TMP_Text _bluetoothCooldownText;
        [SerializeField] private Image _scanCooldownBar;
        [SerializeField] private TMP_Text _scanCooldownText;
        [SerializeField] private GameObject _rollCardRoot;
        [SerializeField] private GameObject _bluetoothCardRoot;
        [SerializeField] private GameObject _scanCardRoot;

        // 사격 차지 중에만 보이는 칸형 차지 바. 왼쪽 칸부터 빈 색 → 찬 색으로 차오른다.
        [Header("Charge Bar")]
        [SerializeField] private GameObject _chargeBarRoot;
        [SerializeField] private Image[] _chargeCellImages;
        [SerializeField] private Color _chargeCellFilledColor = new Color(1.0f, 0.42f, 0.31f, 1.0f);
        [SerializeField] private Color _chargeCellEmptyColor = new Color(1.0f, 0.42f, 0.31f, 0.15f);

        // 상호작용 대상을 조준하는 동안에만 보이는 안내 ("E 상호작용")
        [Header("Interact Label")]
        [SerializeField] private GameObject _interactLabelRoot;

        private CanvasGroup _rollCardGroup;
        private CanvasGroup _bluetoothCardGroup;
        private CanvasGroup _scanCardGroup;

        // 히트 원호 이미지들과 원래 색. 과열이 끝나면 이 색으로 되돌린다.
        private readonly List<Image> _heatImages = new List<Image>();
        private readonly List<Color> _heatBaseColors = new List<Color>();
        private bool _isHeatOverheated;

        private void Awake()
        {
            CacheHeatImages();
        }

        private void Update()
        {
            if (_isHeatOverheated)
            {
                UpdateOverheatBlink();
            }
        }

        public void SetHp(float currentValue, float maxValue)
        {
            SetHpCells(currentValue, maxValue);
        }

        public void SetBattery(float currentValue, float maxValue)
        {
            SetArcCells(_batteryArcCells, _batteryPercentText, currentValue, maxValue);
        }

        public void SetHeat(float currentValue, float maxValue)
        {
            SetArcCells(_heatArcCells, _heatPercentText, currentValue, maxValue);
        }

        // 과열 중이면 히트 원호를 진한 색으로 점멸시키고, 끝나면 원래 색으로 되돌린다.
        public void SetHeatOverheated(bool isOverheated)
        {
            if (_isHeatOverheated == isOverheated)
            {
                return;
            }

            _isHeatOverheated = isOverheated;

            if (isOverheated)
            {
                UpdateOverheatBlink();
                return;
            }

            for (int i = 0; i < _heatImages.Count; i++)
            {
                _heatImages[i].color = _heatBaseColors[i];
            }
        }

        // 남은 쿨타임 비율(0~1)과 남은 시간(초)을 표시한다. 비율 1 = 방금 사용, 0 = 사용 가능
        public void SetRollCooldown(float remainingRatio, float remainingTime)
        {
            SetCooldownGauge(_rollCooldownBar, _rollCooldownText, remainingRatio, remainingTime);
        }

        public void SetBluetoothCooldown(float remainingRatio, float remainingTime)
        {
            SetCooldownGauge(_bluetoothCooldownBar, _bluetoothCooldownText, remainingRatio, remainingTime);
        }

        public void SetScanCooldown(float remainingRatio, float remainingTime)
        {
            SetCooldownGauge(_scanCooldownBar, _scanCooldownText, remainingRatio, remainingTime);
        }

        public void SetSkillCardsLocked(bool isRollLocked, bool isBluetoothLocked, bool isScanLocked)
        {
            SetCardLocked(_rollCardRoot, ref _rollCardGroup, isRollLocked);
            SetCardLocked(_bluetoothCardRoot, ref _bluetoothCardGroup, isBluetoothLocked);
            SetCardLocked(_scanCardRoot, ref _scanCardGroup, isScanLocked);
        }

        public void SetChargeBarVisible(bool isVisible)
        {
            if (_chargeBarRoot != null)
            {
                _chargeBarRoot.SetActive(isVisible);
            }
        }

        public void SetInteractLabelVisible(bool isVisible)
        {
            if (_interactLabelRoot != null)
            {
                _interactLabelRoot.SetActive(isVisible);
            }
        }

        // 차지 진행도(0~1)를 칸마다 나눠 채우는 함수
        // ex) 10칸, 진행도 0.35 => 앞 3칸 찬 색, 4번째 칸 절반쯤 밝아짐, 나머지 빈 색
        public void SetChargeRatio(float chargeRatio)
        {
            if (_chargeCellImages == null)
            {
                return;
            }

            float ratio = Mathf.Clamp01(chargeRatio);

            for (int i = 0; i < _chargeCellImages.Length; i++)
            {
                if (_chargeCellImages[i] == null)
                {
                    continue;
                }

                // 앞 칸들이 담은 몫을 뺀 나머지가 이 칸에 담기는 비율
                float cellValue = Mathf.Clamp01((ratio * _chargeCellImages.Length) - i);
                _chargeCellImages[i].color = Color.Lerp(_chargeCellEmptyColor, _chargeCellFilledColor, cellValue);
            }
        }

        // HP를 칸마다 나눠 채우고 퍼센트 글자를 갱신하는 함수
        // ex) 최대 100, 10칸 => 한 칸 10. 현재 85 => 앞 8칸 가득, 9번째 칸 절반, 10번째 칸 빔
        private void SetHpCells(float currentValue, float maxValue)
        {
            if (_hpPercentText != null)
            {
                float ratio = maxValue > 0.0f ? Mathf.Clamp01(currentValue / maxValue) : 0.0f;
                _hpPercentText.text = $"{Mathf.RoundToInt(ratio * 100.0f)}%";
            }

            if ((_hpCellSliders == null) || (_hpCellSliders.Length == 0))
            {
                return;
            }

            float hpPerCell = maxValue / _hpCellSliders.Length;

            for (int i = 0; i < _hpCellSliders.Length; i++)
            {
                // 이 칸이 시작하는 HP를 뺀 나머지가 이 칸에 담기는 HP
                SetGaugeValue(_hpCellSliders[i], currentValue - (hpPerCell * i), hpPerCell);
            }
        }

        // 비율을 원호 칸마다 나눠 채우고 퍼센트 글자를 갱신하는 함수
        // ex) 2칸, 현재 75% => 첫 칸 가득, 둘째 칸 절반
        private void SetArcCells(SArcCell[] cells, TMP_Text percentText, float currentValue, float maxValue)
        {
            float ratio = maxValue > 0.0f ? Mathf.Clamp01(currentValue / maxValue) : 0.0f;

            if (percentText != null)
            {
                percentText.text = $"{Mathf.RoundToInt(ratio * 100.0f)}%";
            }

            if (cells == null)
            {
                return;
            }

            for (int i = 0; i < cells.Length; i++)
            {
                // 앞 칸들이 담은 몫을 뺀 나머지가 이 칸에 담기는 비율
                float cellValue = Mathf.Clamp01((ratio * cells.Length) - i);
                FillArcCell(cells[i], cellValue);
            }
        }

        private void CacheHeatImages()
        {
            if (_heatArcCells == null)
            {
                return;
            }

            foreach (SArcCell cell in _heatArcCells)
            {
                if (cell.FillImages != null)
                {
                    foreach (Image fillImage in cell.FillImages)
                    {
                        AddHeatImage(fillImage);
                    }
                }

                AddHeatImage(cell.GlowImage);
            }
        }

        private void AddHeatImage(Image image)
        {
            if (image == null)
            {
                return;
            }

            _heatImages.Add(image);
            _heatBaseColors.Add(image.color);
        }

        // 켜짐 구간엔 진한 색 그대로, 꺼짐 구간엔 투명도를 낮춘다. 이미지마다 원래 투명도 비율은 유지한다.
        private void UpdateOverheatBlink()
        {
            bool isBlinkOn = Mathf.Repeat(Time.time, _overheatBlinkInterval * 2.0f) < _overheatBlinkInterval;
            float alphaScale = isBlinkOn ? 1.0f : _overheatBlinkMinAlpha;

            for (int i = 0; i < _heatImages.Count; i++)
            {
                Color color = _overheatColor;
                color.a = _overheatColor.a * _heatBaseColors[i].a * alphaScale;
                _heatImages[i].color = color;
            }
        }

        private void FillArcCell(SArcCell cell, float cellValue)
        {
            float arcFill = ARC_CELL_MAX_FILL * cellValue;

            if (cell.FillImages != null)
            {
                foreach (Image fillImage in cell.FillImages)
                {
                    if (fillImage != null)
                    {
                        fillImage.fillAmount = arcFill;
                    }
                }
            }

            if (cell.GlowImage != null)
            {
                // 빈 칸은 글로우도 끈다. (늘린 끝부분만 남아 보이지 않도록)
                cell.GlowImage.fillAmount = cellValue > 0.0f ? arcFill + GLOW_EXTRA_FILL : 0.0f;
            }
        }

        // 쿨타임 게이지(Filled 이미지)는 쓰면 빈 채로 시작해 쿨타임이 끝나면 꽉 찬다.
        // 남은 시간 글자는 쿨 중에만 보인다. ex) 2.8s, 사용 가능하면 빈 글자
        private void SetCooldownGauge(Image cooldownBar, TMP_Text cooldownText, float remainingRatio, float remainingTime)
        {
            if (cooldownBar != null)
            {
                cooldownBar.fillAmount = 1.0f - Mathf.Clamp01(remainingRatio);
            }

            if (cooldownText != null)
            {
                // 0.1초 단위로 올림해서 끝나기 직전에도 0.0s가 보이지 않게 한다.
                float shownTime = Mathf.Ceil(remainingTime * 10.0f) / 10.0f;
                cooldownText.text = remainingTime > 0.0f ? $"{shownTime:F1}s" : string.Empty;
            }
        }

        private void SetCardLocked(GameObject cardRoot, ref CanvasGroup cardGroup, bool isLocked)
        {
            if (cardRoot == null) return;
            if (cardGroup == null)
            {
                cardGroup = cardRoot.GetComponent<CanvasGroup>();
                if (cardGroup == null) cardGroup = cardRoot.AddComponent<CanvasGroup>();
            }

            cardGroup.alpha = isLocked ? 0.3f : 1.0f;
        }

        private void SetGaugeValue(Slider slider, float currentValue, float maxValue)
        {
            // Slider가 없으면 종료
            if (slider == null)
            {
                return;
            }
            // 현재 수치를 0~1 비율로 계산 
            // ex) 현재 체력 70, 최대 체력 100 => 0.7
            slider.normalizedValue = maxValue > 0.0f
                ? Mathf.Clamp01(currentValue / maxValue) //무조건 0~1사이로 제한
                : 0.0f;
        }
    }
}
