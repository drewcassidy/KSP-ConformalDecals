using System;
using System.Text.RegularExpressions;
using TMPro;

namespace ConformalDecals.Text {
    public class DecalText : IEquatable<DecalText> {
        private readonly string     _text;
        private readonly DecalFont  _font;
        private readonly FontStyles _style;
        private readonly bool       _vertical;
        private readonly float      _lineSpacing;
        private readonly float      _charSpacing;

        /// Raw text contents
        public string Text => _text;

        /// Font asset used by this text snippet
        public DecalFont Font => _font;

        /// Style used by this text snippet
        public FontStyles Style => _style;

        /// If this text snippet is vertical
        public bool Vertical => _vertical;

        /// The text snippet's line spacing
        public float LineSpacing => _lineSpacing;

        /// The text snippet's character spacing
        public float CharSpacing => _charSpacing;

        /// The text formatted with newlines for vertical text
        public string FormattedText {
            get {
                if (string.IsNullOrWhiteSpace(Text)) return "•";
                if (Vertical) {
                    return Regex.Replace(Text, @"(.)", "$1\n");
                }
                else {
                    return Text;
                }
            }
        }


        public DecalText(string text, DecalFont font, FontStyles style, bool vertical, float linespacing, float charspacing) {
            if (font == null) throw new ArgumentNullException(nameof(font));
            _text = text;
            _font = font;
            _style = style;
            _vertical = vertical;
            _lineSpacing = linespacing;
            _charSpacing = charspacing;
        }

        public bool Equals(DecalText other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return _text == other._text && Equals(_font, other._font) && _style == other._style && _vertical == other._vertical && _lineSpacing.Equals(other._lineSpacing) &&
                   _charSpacing.Equals(other._charSpacing);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DecalText) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (_text != null ? _text.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (_font != null ? _font.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) _style;
                hashCode = (hashCode * 397) ^ _vertical.GetHashCode();
                hashCode = (hashCode * 397) ^ _lineSpacing.GetHashCode();
                hashCode = (hashCode * 397) ^ _charSpacing.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(DecalText left, DecalText right) {
            return Equals(left, right);
        }

        public static bool operator !=(DecalText left, DecalText right) {
            return !Equals(left, right);
        }

        public override string ToString() {
            return $"{nameof(_text)}: {_text}, {nameof(_font)}: {_font}, {nameof(_style)}: {_style}, {nameof(_vertical)}: {_vertical}, {nameof(_lineSpacing)}: {_lineSpacing}, {nameof(_charSpacing)}: {_charSpacing}";
        }
    }
}