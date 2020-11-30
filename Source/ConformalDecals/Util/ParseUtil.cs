using System;
using System.Collections.Generic;
using System.Reflection;
using UniLinq;
using UnityEngine;

namespace ConformalDecals.Util {
    public static class ParseUtil {
        private static readonly Dictionary<string, Color> NamedColors = new Dictionary<string, Color>();
        private static readonly char[]                    Separator   = {',', ' ', '\t'};

        public delegate bool TryParseDelegate<T>(string valueString, out T value);

        static ParseUtil() {
            // setup named colors
            foreach (var propertyInfo in typeof(Color).GetProperties(BindingFlags.Static | BindingFlags.Public)) {
                if (!propertyInfo.CanRead) continue;
                if (propertyInfo.PropertyType != typeof(Color)) continue;

                NamedColors.Add(propertyInfo.Name, (Color) propertyInfo.GetValue(null, null));
            }

            foreach (var propertyInfo in typeof(XKCDColors).GetProperties(BindingFlags.Static | BindingFlags.Public)) {
                if (!propertyInfo.CanRead) continue;
                if (propertyInfo.PropertyType != typeof(Color)) continue;

                if (NamedColors.ContainsKey(propertyInfo.Name)) throw new Exception("duplicate key " + propertyInfo.Name);

                NamedColors.Add(propertyInfo.Name, (Color) propertyInfo.GetValue(null, null));
            }
        }

        public static string ParseString(ConfigNode node, string valueName, bool isOptional = false, string defaultValue = "") {
            if (node.HasValue(valueName)) return node.GetValue(valueName);
            
            if (isOptional) {
                return defaultValue;
            }
            else {
                throw new FormatException($"Missing value for {valueName}");
            }

        }

        public static bool ParseStringIndirect(ref string value, ConfigNode node, string valueName) {
            if (node.HasValue(valueName)) {
                value = node.GetValue(valueName);
                return true;
            }

            return false;
        }

        public static bool ParseBool(ConfigNode node, string valueName, bool isOptional = false, bool defaultValue = false) {
            return ParseValue(node, valueName, bool.TryParse, isOptional, defaultValue);
        }

        public static bool ParseBoolIndirect(ref bool value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, bool.TryParse);
        }

        public static float ParseFloat(ConfigNode node, string valueName, bool isOptional = false, float defaultValue = 0.0f) {
            return ParseValue(node, valueName, float.TryParse, isOptional, defaultValue);
        }

        public static bool ParseFloatIndirect(ref float value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, float.TryParse);
        }

        public static int ParseInt(ConfigNode node, string valueName, bool isOptional = false, int defaultValue = 0) {
            return ParseValue(node, valueName, int.TryParse, isOptional, defaultValue);
        }

        public static bool ParseIntIndirect(ref int value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, int.TryParse);
        }

        public static Color32 ParseColor32(ConfigNode node, string valueName, bool isOptional = false, Color32 defaultValue = default) {
            return ParseValue(node, valueName, TryParseColor32, isOptional, defaultValue);
        }

        public static bool ParseColor32Indirect(ref Color32 value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, TryParseColor32);
        }

        public static Rect ParseRect(ConfigNode node, string valueName, bool isOptional = false, Rect defaultValue = default) {
            return ParseValue(node, valueName, ParseExtensions.TryParseRect, isOptional, defaultValue);
        }

        public static bool ParseRectIndirect(ref Rect value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, ParseExtensions.TryParseRect);
        }

        public static Vector2 ParseVector2(ConfigNode node, string valueName, bool isOptional = false, Vector2 defaultValue = default) {
            return ParseValue(node, valueName, ParseExtensions.TryParseVector2, isOptional, defaultValue);
        }

        public static bool ParseVector2Indirect(ref Vector2 value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, ParseExtensions.TryParseVector2);
        }

        public static Vector3 ParseVector3(ConfigNode node, string valueName, bool isOptional = false, Vector3 defaultValue = default) {
            return ParseValue(node, valueName, ParseExtensions.TryParseVector3, isOptional, defaultValue);
        }

        public static bool ParseVector3Indirect(ref Vector3 value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, ParseExtensions.TryParseVector3);
        }

        public static Matrix4x4 ParseMatrix4x4(ConfigNode node, string valueName, bool isOptional = false, Matrix4x4 defaultValue = default) {
            return ParseValue(node, valueName, ParseUtil.TryParseMatrix4x4, isOptional, defaultValue);
        }

        public static bool ParseMatrix4x4Indirect(ref Matrix4x4 value, ConfigNode node, string valueName) {
            return ParseValueIndirect(ref value, node, valueName, ParseUtil.TryParseMatrix4x4);

        }

        public static T ParseValue<T>(ConfigNode node, string valueName, TryParseDelegate<T> tryParse, bool isOptional = false, T defaultValue = default) {
            string valueString = node.GetValue(valueName);

            if (isOptional) {
                if (string.IsNullOrEmpty(valueString)) return defaultValue;
            }
            else {
                if (valueString == null)
                    throw new FormatException($"Missing {typeof(T)} value for {valueName}");

                if (valueString == string.Empty)
                    throw new FormatException($"Empty {typeof(T)} value for {valueName}");
            }

            if (tryParse(valueString, out var value)) {
                return value;
            }

            if (isOptional) {
                return defaultValue;
            }

            else {
                throw new FormatException($"Improperly formatted {typeof(T)} value for {valueName} : '{valueString}");
            }
        }

        public static bool ParseValueIndirect<T>(ref T value, ConfigNode node, string valueName, TryParseDelegate<T> tryParse) {
            if (!node.HasValue(valueName)) return false;

            var valueString = node.GetValue(valueName);
            if (tryParse(valueString, out var newValue)) {
                value = newValue;
                return true;
            }

            throw new FormatException($"Improperly formatted {typeof(T)} value for {valueName} : '{valueString}");
        }

        public static bool TryParseMatrix4x4(string valueString, out Matrix4x4 value) {
            value = new Matrix4x4();

            var split = valueString.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split.Length; i++) {
                split[i] = split[i].Trim();
            }

            if (split.Length != 16) return false;
            int index = 0;
            for (int row = 0; row < 4; row++) {
                for (int col = 0; col < 4; col++) {
                    if (!float.TryParse(split[index], out float component)) return false;
                    value[row, col] = component;
                }
            }

            return true;
        }

        public static bool TryParseHexColor(string valueString, out Color32 value) {
            value = new Color32(0, 0, 0, byte.MaxValue);

            if (!uint.TryParse(valueString, System.Globalization.NumberStyles.HexNumber, null, out var hexColor)) return false;

            switch (valueString.Length) {
                case 8: // RRGGBBAA
                    value.a = (byte) (hexColor & 0xFF);
                    hexColor >>= 8;
                    goto case 6;

                case 6: // RRGGBB
                    value.b = (byte) (hexColor & 0xFF);
                    hexColor >>= 8;
                    value.g = (byte) (hexColor & 0xFF);
                    hexColor >>= 8;
                    value.r = (byte) (hexColor & 0xFF);
                    return true;

                case 4: // RGBA
                    value.a = (byte) ((hexColor & 0xF) << 4);
                    hexColor >>= 4;
                    goto case 3;

                case 3: // RGB
                    value.b = (byte) (hexColor & 0xF << 4);
                    hexColor >>= 4;
                    value.g = (byte) (hexColor & 0xF << 4);
                    hexColor >>= 4;
                    value.r = (byte) (hexColor & 0xF << 4);
                    return true;

                default:
                    return false;
            }
        }

        public static bool TryParseColor32(string valueString, out Color32 value) {
            value = new Color32(0, 0, 0, byte.MaxValue);

            // hex color
            if (valueString[0] == '#') {
                var hexColorString = valueString.Substring(1);

                if (TryParseHexColor(hexColorString, out var hexColor)) {
                    value = hexColor;
                    return true;
                }
            }

            // named color
            if (NamedColors.TryGetValue(valueString, out var namedColor)) {
                value = namedColor;
                return true;
            }

            // float color
            var split = valueString.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < split.Length; i++) {
                split[i] = split[i].Trim();
            }

            switch (split.Length) {
                case 4:
                    if (!float.TryParse(split[4], out var alpha)) return false;
                    value.a = (byte) (alpha * 0xFF);
                    goto case 3;

                case 3:
                    if (!float.TryParse(split[0], out var red)) return false;
                    if (!float.TryParse(split[1], out var green)) return false;
                    if (!float.TryParse(split[2], out var blue)) return false;

                    value.r = (byte) (red * 0xFF);
                    value.g = (byte) (green * 0xFF);
                    value.b = (byte) (blue * 0xFF);
                    return true;
                case 1: // try again for hex color
                    if (TryParseHexColor(split[0], out var hexcolor)) {
                        value = hexcolor;
                        return true;
                    }
                    else {
                        return false;
                    }
                default:
                    return false;
            }
        }
    }
}