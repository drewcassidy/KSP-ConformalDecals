using System;
using System.Collections.Generic;
using ConformalDecals.Text;
using ConformalDecals.Util;
using TMPro;
using UnityEngine;

namespace ConformalDecals {
    public static class DecalConfig {
        private static Texture2D                     _blankNormal;
        private static List<string>                  _shaderBlacklist;
        private static Dictionary<string, DecalFont> _fontList;
        private static int                           _decalLayer = 31;
        private static bool                          _selectableInFlight;

        private struct LegacyShaderEntry {
            public string   name;
            public string[] keywords;
        }

        private static readonly Dictionary<string, LegacyShaderEntry> LegacyShaderPairs = new Dictionary<string, LegacyShaderEntry>() {
            ["ConformalDecals/Feature/Bumped"] = new LegacyShaderEntry() {
                name = "ConformalDecals/Decal/Standard",
                keywords = new[] {"DECAL_BUMPMAP"}
            },
            ["ConformalDecals/Paint/Diffuse"] = new LegacyShaderEntry() {
                name = "ConformalDecals/Decal/Standard",
                keywords = new string[] { }
            },
            ["ConformalDecals/Paint/Specular"] = new LegacyShaderEntry() {
                name = "ConformalDecals/Decal/Standard",
                keywords = new[] {"DECAL_SPECMAP"}
            },
            ["ConformalDecals/Paint/DiffuseSDF"] = new LegacyShaderEntry() {
                name = "ConformalDecals/Decal/Standard",
                keywords = new[] {"DECAL_SDF_ALPHA"}
            },
            ["ConformalDecals/Paint/SpecularSDF"] = new LegacyShaderEntry() {
                name = "ConformalDecals/Decal/Standard",
                keywords = new[] {"DECAL_SDF_ALPHA", "DECAL_SPECMAP"}
            },
        };


        public static Texture2D BlankNormal => _blankNormal;

        public static int DecalLayer => _decalLayer;

        public static bool SelectableInFlight => _selectableInFlight;

        public static IEnumerable<DecalFont> Fonts => _fontList.Values;

        public static bool IsBlacklisted(Shader shader) {
            return IsBlacklisted(shader.name);
        }

        public static bool IsBlacklisted(string shaderName) {
            return _shaderBlacklist.Contains(shaderName);
        }

        public static bool IsLegacy(string shaderName, out string newShader, out string[] keywords) {
            if (LegacyShaderPairs.TryGetValue(shaderName, out var entry)) {
                newShader = entry.name;
                keywords = entry.keywords;
                return true;
            }

            newShader = null;
            keywords = null;
            return false;
        }

        public static DecalFont GetFont(string name) {
            if (_fontList.TryGetValue(name, out var font)) {
                return font;
            }
            else {
                throw new KeyNotFoundException($"Font {name} not found");
            }
        }

        private static void ParseConfig(ConfigNode node) {

            ParseUtil.ParseIntIndirect(ref _decalLayer, node, "decalLayer");
            ParseUtil.ParseBoolIndirect(ref _selectableInFlight, node, "selectableInFlight");

            foreach (var blacklist in node.GetNodes("SHADERBLACKLIST")) {
                foreach (var shaderName in blacklist.GetValuesList("shader")) {
                    _shaderBlacklist.Add(shaderName);
                }
            }

            var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();

            foreach (var fontNode in node.GetNodes("FONT")) {
                try {
                    var font = DecalFont.Parse(fontNode, allFonts);
                    _fontList.Add(font.Name, font);
                }
                catch (Exception e) {
                    Debug.LogException(e);
                }
            }
        }

        private static Texture2D MakeBlankNormal() {
            Logging.Log("Generating neutral normal map texture");
            var width = 2;
            var height = 2;
            var color = new Color32(255, 128, 128, 128);
            var colors = new[] {color, color, color, color};

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);

            tex.SetPixels32(colors);
            tex.Apply();

            return tex;
        }

        // ReSharper disable once UnusedMember.Global
        public static void ModuleManagerPostLoad() {
            _shaderBlacklist = new List<string>();
            _fontList = new Dictionary<string, DecalFont>();

            var configs = GameDatabase.Instance.GetConfigs("CONFORMALDECALS");

            if (configs.Length > 0) {
                Logging.Log("Loading config");
                foreach (var config in configs) {
                    ParseConfig(config.config);
                }
            }

            // setup physics for decals, ignore collision with everything
            Physics.IgnoreLayerCollision(_decalLayer, 1, true); // default
            Physics.IgnoreLayerCollision(_decalLayer, 17, true); // EVA
            Physics.IgnoreLayerCollision(_decalLayer, 19, true); // PhysicalObjects
            Physics.IgnoreLayerCollision(_decalLayer, 23, true); // AeroFXIgnore
            Physics.IgnoreLayerCollision(_decalLayer, 26, true); // wheelCollidersIgnore
            Physics.IgnoreLayerCollision(_decalLayer, 27, true); // wheelColliders

            _blankNormal = MakeBlankNormal();
        }
    }
}