using System;
using System.Collections.Generic;
using ConformalDecals.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace ConformalDecals.Text {
    /// Class handing text rendering.
    public static class TextRenderer {
        /// Texture format used for returned textures.
        /// Unfortunately due to how Unity textures work, this cannot be R8 or Alpha8,
        /// so theres always a superfluous green channel using memory
        public static TextureFormat textTextureFormat = TextureFormat.RG16;

        /// Render Texture format used when rendering
        /// Overriden below to be ARGB32 on DirectX because DirectX is dumb
        public static RenderTextureFormat textRenderTextureFormat = RenderTextureFormat.R8;

        private const string ShaderName     = "ConformalDecals/Text Blit";
        private const int    MaxTextureSize = 4096;
        private const float  FontSize       = 100;
        private const float  PixelDensity   = 5;

        private static Shader    _blitShader;
        private static Texture2D _blankTexture;

        private static readonly Dictionary<DecalText, TextRenderOutput> RenderCache = new Dictionary<DecalText, TextRenderOutput>();

        /// Update text immediately without using job queue
        public static TextRenderOutput UpdateText(DecalText oldText, DecalText newText) {
            if (newText == null) throw new ArgumentNullException(nameof(newText));

            if (!(oldText is null)) UnregisterText(oldText);

            // now that all old references are handled, begin rendering the new output
            if (!RenderCache.TryGetValue(newText, out var renderOutput)) {
                renderOutput = RenderText(newText);
                RenderCache.Add(newText, renderOutput);
            }

            renderOutput.UserCount++;
            return renderOutput;
        }

        /// Unregister a user of a piece of text
        public static void UnregisterText(DecalText text) {
            if (text == null) throw new ArgumentNullException(nameof(text));

            if (RenderCache.TryGetValue(text, out var renderedText)) {
                renderedText.UserCount--;
                if (renderedText.UserCount <= 0) {
                    RenderCache.Remove(text);
                    var texture = renderedText.Texture;
                    if (texture != _blankTexture) Object.Destroy(texture);
                }
            }
        }


        /// Render a piece of text to a given texture
        public static TextRenderOutput RenderText(DecalText text) {
            if (text == null) throw new ArgumentNullException(nameof(text));

            var tmpObject = new GameObject("Text Mesh Pro renderer");
            var tmp = tmpObject.AddComponent<TextMeshPro>();

            // SETUP TMP OBJECT FOR RENDERING
            tmp.text = text.FormattedText;
            tmp.font = text.Font.FontAsset;
            tmp.fontStyle = text.Style | text.Font.FontStyle;
            tmp.lineSpacing = text.LineSpacing;
            tmp.characterSpacing = text.CharSpacing;

            tmp.extraPadding = true;
            tmp.enableKerning = true;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontSize = FontSize;

            // GENERATE MESH
            tmp.ClearMesh(false);
            tmp.ForceMeshUpdate();

            var meshFilters = tmpObject.GetComponentsInChildren<MeshFilter>();
            var meshes = new Mesh[meshFilters.Length];
            var materials = new Material[meshFilters.Length];

            var bounds = new Bounds();

            // SETUP MATERIALS AND BOUNDS
            for (int i = 0; i < meshFilters.Length; i++) {
                var renderer = meshFilters[i].gameObject.GetComponent<MeshRenderer>();

                meshes[i] = meshFilters[i].mesh;
                if (i == 0) meshes[i] = tmp.mesh;

                materials[i] = Object.Instantiate(renderer.material);
                materials[i].shader = _blitShader;

                if (renderer == null) throw new FormatException($"Object {meshFilters[i].gameObject.name} has filter but no renderer");
                if (meshes[i] == null) throw new FormatException($"Object {meshFilters[i].gameObject.name} has a null mesh");

                if (i == 0) {
                    bounds = meshes[i].bounds;
                }
                else {
                    bounds.Encapsulate(meshes[i].bounds);
                }
            }

            // CALCULATE SIZES
            var size = bounds.size * PixelDensity;
            size.x = Mathf.Max(size.x, 0.1f);
            size.y = Mathf.Max(size.y, 0.1f);

            var textureSize = new Vector2Int {
                x = Mathf.NextPowerOfTwo((int) size.x),
                y = Mathf.NextPowerOfTwo((int) size.y)
            };

            if (textureSize.x == 0 || textureSize.y == 0) {
                Logging.LogError("No text present or error in texture size calculation. Aborting.");
                Object.Destroy(tmpObject);
                return new TextRenderOutput(_blankTexture, Rect.zero);
            }

            // make sure texture isnt too big, scale it down if it is
            // this is just so you dont crash the game by pasting in the entire script of The Bee Movie
            if (textureSize.x > MaxTextureSize) {
                textureSize.y /= textureSize.x / MaxTextureSize;
                textureSize.x = MaxTextureSize;
            }

            if (textureSize.y > MaxTextureSize) {
                textureSize.x /= textureSize.y / MaxTextureSize;
                textureSize.y = MaxTextureSize;
            }

            // scale up everything to fit the texture for maximum usage
            float sizeRatio = Mathf.Min(textureSize.x / size.x, textureSize.y / size.y);

            // calculate where in the texture the used area actually is
            var window = new Rect {
                size = size * sizeRatio,
                center = (Vector2) textureSize / 2
            };

            // SETUP TEXTURE
            var texture = new Texture2D(textureSize.x, textureSize.y, textTextureFormat, false);

            // GENERATE PROJECTION MATRIX
            var halfSize = (Vector2) textureSize / PixelDensity / 2 / sizeRatio;
            var matrix = Matrix4x4.Ortho(bounds.center.x - halfSize.x, bounds.center.x + halfSize.x,
                bounds.center.y - halfSize.y, bounds.center.y + halfSize.y, -1, 1);

            // GET RENDERTEX
            var renderTex = RenderTexture.GetTemporary(textureSize.x, textureSize.y, 0, textRenderTextureFormat, RenderTextureReadWrite.Linear);

            // RENDER
            Graphics.SetRenderTarget(renderTex);
            GL.PushMatrix();
            GL.LoadProjectionMatrix(matrix);
            GL.LoadIdentity();
            GL.Clear(false, true, Color.black);

            for (var i = 0; i < meshes.Length; i++) {
                if (meshes[i].vertexCount >= 3) {
                    materials[i].SetPass(0);
                    Graphics.DrawMeshNow(meshes[i], Matrix4x4.identity);
                }
            }

            // COPY RENDERTEX INTO TEXTURE 
            var prevRT = RenderTexture.active;
            RenderTexture.active = renderTex;
            texture.ReadPixels(new Rect(0, 0, textureSize.x, textureSize.y), 0, 0, false);
            texture.Apply(false, true);
            RenderTexture.active = prevRT;

            GL.PopMatrix();

            // RELEASE RENDERTEX
            RenderTexture.ReleaseTemporary(renderTex);

            // DESTROY THE RENDERER OBJECT
            Object.Destroy(tmpObject);

            return new TextRenderOutput(texture, window);
        }
        
        /// Setup shader and texture
        public static void ModuleManagerPostLoad() {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12) {
                textRenderTextureFormat = RenderTextureFormat.ARGB32; // DirectX is dumb
            }

            if (!SystemInfo.SupportsTextureFormat(textTextureFormat)) {
                Logging.LogError($"Text texture format {textTextureFormat} not supported on this platform.");
            }

            if (!SystemInfo.SupportsRenderTextureFormat(textRenderTextureFormat)) {
                Logging.LogError($"Text texture format {textRenderTextureFormat} not supported on this platform.");
            }

            _blankTexture = Texture2D.blackTexture;
            _blitShader = Shabby.Shabby.FindShader(ShaderName);
            if (_blitShader == null) Logging.LogError($"Could not find text blit shader named '{ShaderName}'");
        }

    }
}