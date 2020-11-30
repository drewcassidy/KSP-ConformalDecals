using System;
using System.Collections.Generic;
using ConformalDecals.Util;
using TMPro;
using UniLinq;
using UnityEngine;

namespace ConformalDecals.Text {
    public class DecalFont : ScriptableObject, ISerializationCallbackReceiver, IEquatable<DecalFont> {
        [SerializeField] private string        _title;
        [SerializeField] private TMP_FontAsset _fontAsset;
        [SerializeField] private FontStyles    _fontStyle;
        [SerializeField] private FontStyles    _fontStyleMask;

        /// Human-readable name for the font
        public string Title => _title;

        /// Internal name for the font
        public string Name => _fontAsset.name;

        /// The font asset itself
        public TMP_FontAsset FontAsset => _fontAsset;

        /// Styles that are forced on for this font,
        /// e.g. smallcaps for a font without lower case characters
        public FontStyles FontStyle => _fontStyle;

        public bool Bold => (_fontStyle & FontStyles.Bold) != 0;

        public bool Italic => (_fontStyle & FontStyles.Italic) != 0;

        public bool Underline => (_fontStyle & FontStyles.Underline) != 0;

        public bool SmallCaps => (_fontStyle & FontStyles.SmallCaps) != 0;

        /// Styles that are forced off for this font,
        /// e.g. underline for a font with no underscore character
        public FontStyles FontStyleMask => _fontStyleMask;

        public bool BoldMask => (_fontStyleMask & FontStyles.Bold) != 0;

        public bool ItalicMask => (_fontStyleMask & FontStyles.Italic) != 0;

        public bool UnderlineMask => (_fontStyleMask & FontStyles.Underline) != 0;

        public bool SmallCapsMask => (_fontStyleMask & FontStyles.SmallCaps) != 0;


        public DecalFont(ConfigNode node, IEnumerable<TMP_FontAsset> fontAssets) {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (fontAssets == null) throw new ArgumentNullException(nameof(fontAssets));

            var name = ParseUtil.ParseString(node, "name");
            _fontAsset = fontAssets.First(o => o.name == name);
            if (FontAsset == null) {
                throw new FormatException($"Could not find font asset named {name}");
            }

            _title = ParseUtil.ParseString(node, "title", true, name);
            _fontStyle = (FontStyles) ParseUtil.ParseInt(node, "style", true);
            _fontStyleMask = (FontStyles) ParseUtil.ParseInt(node, "styleMask", true);
        }


        public void SetupSample(TMP_Text tmp) {
            if (tmp == null) throw new ArgumentNullException(nameof(tmp));
            if (FontAsset == null) throw new InvalidOperationException("DecalFont has not been initialized and Font is null.");

            tmp.text = Title;
            tmp.font = FontAsset;
            tmp.fontStyle = FontStyle;
        }

        public bool Equals(DecalFont other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Title == other.Title && Equals(FontAsset, other.FontAsset) && FontStyle == other.FontStyle && FontStyleMask == other.FontStyleMask;
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((DecalFont) obj);
        }

        public override int GetHashCode() {
            unchecked {
                var hashCode = (Title != null ? Title.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (FontAsset != null ? FontAsset.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (int) FontStyle;
                hashCode = (hashCode * 397) ^ (int) FontStyleMask;
                return hashCode;
            }
        }

        public static bool operator ==(DecalFont left, DecalFont right) {
            return Equals(left, right);
        }

        public static bool operator !=(DecalFont left, DecalFont right) {
            return !Equals(left, right);
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() { }
    }
}