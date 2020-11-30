using System;
using ConformalDecals.Text;
using ConformalDecals.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ConformalDecals.UI {
    public class TextEntryController : MonoBehaviour {
        [Serializable]
        public delegate void TextUpdateDelegate(string newText, DecalFont newFont, FontStyles style, bool vertical, float linespacing, float charspacing);

        [SerializeField] private Selectable _textBox;
        [SerializeField] private Button     _fontButton;

        [SerializeField] private Slider     _lineSpacingSlider;
        [SerializeField] private Selectable _lineSpacingTextBox;

        [SerializeField] private Slider     _charSpacingSlider;
        [SerializeField] private Selectable _charSpacingTextBox;

        [SerializeField] private Toggle _boldButton;
        [SerializeField] private Toggle _italicButton;
        [SerializeField] private Toggle _underlineButton;
        [SerializeField] private Toggle _smallCapsButton;
        [SerializeField] private Toggle _verticalButton;

        private string             _text;
        private DecalFont          _font;
        private FontStyles         _style;
        private bool               _vertical;
        private float              _lineSpacing;
        private float              _charSpacing;
        private Vector2            _lineSpacingRange;
        private Vector2            _charSpacingRange;
        private TMP_InputField     _textBoxTMP;
        private TextUpdateDelegate _onValueChanged;

        private FontMenuController _fontMenu;

        private bool   _ignoreUpdates;
        private bool   _isLocked;
        private string _lockString;
        private static int _lockCounter;

        public static TextEntryController Create(
            string text, DecalFont font, FontStyles style, bool vertical, float linespacing, float charspacing,
            Vector2 lineSpacingRange, Vector2 charSpacingRange,
            TextUpdateDelegate textUpdateCallback) {

            var window = Instantiate(UILoader.TextEntryPrefab, MainCanvasUtil.MainCanvas.transform, true);
            window.AddComponent<DragPanel>();
            MenuNavigation.SpawnMenuNavigation(window, Navigation.Mode.Automatic, true);

            var controller = window.GetComponent<TextEntryController>();
            controller._text = text;
            controller._font = font;
            controller._style = style;
            controller._vertical = vertical;
            controller._lineSpacing = linespacing;
            controller._charSpacing = charspacing;
            controller._lineSpacingRange = lineSpacingRange;
            controller._charSpacingRange = charSpacingRange;
            controller._onValueChanged = textUpdateCallback;

            return controller;
        }

        public void Close() {
            if (_fontMenu != null) _fontMenu.Close();
            Destroy(gameObject);
        }

        public void SetControlLock(string value = null) {
            if (_isLocked) return;
            InputLockManager.SetControlLock(_lockString);
            _isLocked = true;
        }

        public void RemoveControlLock(string value = null) {
            if (!_isLocked) return;
            InputLockManager.RemoveControlLock(_lockString);
            _isLocked = false;
        }

        public void OnTextUpdate(string newText) {
            this._text = newText;

            OnValueChanged();
        }

        public void OnFontMenu() {
            if (_fontMenu == null) _fontMenu = FontMenuController.Create(DecalConfig.Fonts, _font, OnFontUpdate);
        }

        public void OnFontUpdate(DecalFont font) {
            if (_ignoreUpdates) return;

            _font = font;
            font.SetupSample(_fontButton.GetComponentInChildren<TextMeshProUGUI>());

            _textBoxTMP.text = _text;
            _textBoxTMP.textComponent.fontStyle = _style | _font.FontStyle & ~_font.FontStyleMask;
            _textBoxTMP.fontAsset = _font.FontAsset;

            UpdateStyleButtons();
            OnValueChanged();
        }

        public void OnLineSpacingUpdate(float value) {
            if (_ignoreUpdates) return;

            _lineSpacing = Mathf.Lerp(_lineSpacingRange.x, _lineSpacingRange.y, value);

            UpdateLineSpacing();
            OnValueChanged();
        }

        public void OnLineSpacingUpdate(string text) {
            if (_ignoreUpdates) return;

            if (float.TryParse(text, out var value)) {
                _lineSpacing = Mathf.Clamp(value, _lineSpacingRange.x, _lineSpacingRange.y);
            }
            else {
                Logging.LogWarning("Line spacing value '{text}' could not be parsed.");
            }

            UpdateLineSpacing();
            OnValueChanged();
        }

        public void OnCharSpacingUpdate(float value) {
            if (_ignoreUpdates) return;

            _charSpacing = Mathf.Lerp(_charSpacingRange.x, _charSpacingRange.y, value);

            UpdateCharSpacing();
            OnValueChanged();
        }

        public void OnCharSpacingUpdate(string text) {
            if (_ignoreUpdates) return;

            if (float.TryParse(text, out var value)) {
                _charSpacing = Mathf.Clamp(value, _charSpacingRange.x, _charSpacingRange.y);
            }
            else {
                Logging.LogWarning("Char spacing value '{text}' could not be parsed.");
            }

            UpdateCharSpacing();
            OnValueChanged();
        }

        public void OnBoldUpdate(bool state) {
            if (_ignoreUpdates) return;

            if (state)
                _style |= FontStyles.Bold;
            else
                _style &= ~FontStyles.Bold;

            _textBoxTMP.textComponent.fontStyle = _style | _font.FontStyle & ~_font.FontStyleMask;
            OnValueChanged();
        }

        public void OnItalicUpdate(bool state) {
            if (_ignoreUpdates) return;

            if (state)
                _style |= FontStyles.Italic;
            else
                _style &= ~FontStyles.Italic;

            _textBoxTMP.textComponent.fontStyle = _style | _font.FontStyle & ~_font.FontStyleMask;
            OnValueChanged();
        }

        public void OnUnderlineUpdate(bool state) {
            if (_ignoreUpdates) return;

            if (state)
                _style |= FontStyles.Underline;
            else
                _style &= ~FontStyles.Underline;

            _textBoxTMP.textComponent.fontStyle = _style | _font.FontStyle & ~_font.FontStyleMask;
            OnValueChanged();
        }

        public void OnSmallCapsUpdate(bool state) {
            if (_ignoreUpdates) return;

            if (state)
                _style |= FontStyles.SmallCaps;
            else
                _style &= ~FontStyles.SmallCaps;

            _textBoxTMP.textComponent.fontStyle = _style | _font.FontStyle & ~_font.FontStyleMask;
            OnValueChanged();
        }

        public void OnVerticalUpdate(bool state) {
            if (_ignoreUpdates) return;

            _vertical = state;
            OnValueChanged();
        }

        private void Start() {
            _lockString = $"ConformalDecals_TextEditor_{_lockCounter++}";
                
            _textBoxTMP = ((TMP_InputField) _textBox);
            _textBoxTMP.text = _text;
            _textBoxTMP.textComponent.fontStyle = _style | _font.FontStyle & ~_font.FontStyleMask;
            _textBoxTMP.fontAsset = _font.FontAsset;
            _textBoxTMP.onSelect.AddListener(SetControlLock);
            _textBoxTMP.onDeselect.AddListener(RemoveControlLock);

            _font.SetupSample(_fontButton.GetComponentInChildren<TextMeshProUGUI>());

            UpdateStyleButtons();
            UpdateLineSpacing();
            UpdateCharSpacing();
        }

        private void OnDestroy() {
            RemoveControlLock();
        }

        private void OnValueChanged() {
            _onValueChanged(_text, _font, _style, _vertical, _lineSpacing, _charSpacing);
        }

        private void UpdateStyleButtons() {
            _ignoreUpdates = true;

            if (_font.Bold) {
                _boldButton.interactable = false;
                _boldButton.isOn = true;
            }
            else if (_font.BoldMask) {
                _boldButton.interactable = false;
                _boldButton.isOn = false;
            }
            else {
                _boldButton.interactable = true;
                _boldButton.isOn = (_style & FontStyles.Bold) != 0;
            }

            if (_font.Italic) {
                _italicButton.interactable = false;
                _italicButton.isOn = true;
            }
            else if (_font.ItalicMask) {
                _italicButton.interactable = false;
                _italicButton.isOn = false;
            }
            else {
                _italicButton.interactable = true;
                _italicButton.isOn = (_style & FontStyles.Italic) != 0;
            }

            if (_font.Underline) {
                _underlineButton.interactable = false;
                _underlineButton.isOn = true;
            }
            else if (_font.UnderlineMask) {
                _underlineButton.interactable = false;
                _underlineButton.isOn = false;
            }
            else {
                _underlineButton.interactable = true;
                _underlineButton.isOn = (_style & FontStyles.Underline) != 0;
            }

            if (_font.SmallCaps) {
                _smallCapsButton.interactable = false;
                _smallCapsButton.isOn = true;
            }
            else if (_font.SmallCapsMask) {
                _smallCapsButton.interactable = false;
                _smallCapsButton.isOn = false;
            }
            else {
                _smallCapsButton.interactable = true;
                _smallCapsButton.isOn = (_style & FontStyles.SmallCaps) != 0;
            }

            _verticalButton.isOn = _vertical;

            _ignoreUpdates = false;
        }

        private void UpdateLineSpacing() {
            _ignoreUpdates = true;

            _lineSpacingSlider.value = Mathf.InverseLerp(_lineSpacingRange.x, _lineSpacingRange.y, _lineSpacing);
            ((TMP_InputField) _lineSpacingTextBox).text = $"{_lineSpacing:F1}";

            _ignoreUpdates = false;
        }

        private void UpdateCharSpacing() {
            _ignoreUpdates = true;

            _charSpacingSlider.value = Mathf.InverseLerp(_charSpacingRange.x, _charSpacingRange.y, _charSpacing);
            ((TMP_InputField) _charSpacingTextBox).text = $"{_charSpacing:F1}";

            _ignoreUpdates = false;
        }
    }
}