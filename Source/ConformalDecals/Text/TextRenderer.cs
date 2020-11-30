using System;
using System.Collections.Generic;
using ConformalDecals.Util;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace ConformalDecals.Text {
    // TODO: Testing shows the job system is unnecessary, so remove job system code.

    /// Class handing text rendering.
    /// Is a singleton referencing a single gameobject in the scene which contains the TextMeshPro component
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class TextRenderer : MonoBehaviour {
        /// Texture format used for returned textures.
        /// Unfortunately due to how Unity textures work, this cannot be R8 or Alpha8,
        /// so theres always a superfluous green channel using memory
        public static TextureFormat textTextureFormat = TextureFormat.RG16;

        /// Render Texture format used when rendering
        /// Overriden below to be ARGB32 on DirectX because DirectX is dumb
        public static RenderTextureFormat textRenderTextureFormat = RenderTextureFormat.R8;

        /// The text renderer object within the scene which contains the TextMeshPro component used for rendering.
        public static TextRenderer Instance {
            get {
                if (!_instance._isSetup) {
                    _instance.Setup();
                }

                return _instance;
            }
        }

        /// Text Render unityevent, used with the job system to signal render completion
        [Serializable]
        public class TextRenderEvent : UnityEvent<TextRenderOutput> { }

        private const string ShaderName     = "ConformalDecals/Text Blit";
        private const int    MaxTextureSize = 4096;
        private const float  FontSize       = 100;
        private const float  PixelDensity   = 5;

        private static TextRenderer _instance;

        private bool        _isSetup;
        private TextMeshPro _tmp;
        private Shader      _blitShader;

        private static readonly Dictionary<DecalText, TextRenderOutput> RenderCache = new Dictionary<DecalText, TextRenderOutput>();
        private static readonly Queue<TextRenderJob>                    RenderJobs  = new Queue<TextRenderJob>();

        /// Update text using the job queue
        public static TextRenderJob UpdateText(DecalText oldText, DecalText newText, UnityAction<TextRenderOutput> renderFinishedCallback) {
            if (newText == null) throw new ArgumentNullException(nameof(newText));

            var job = new TextRenderJob(oldText, newText, renderFinishedCallback);
            RenderJobs.Enqueue(job);
            return job;
        }

        /// Update text immediately without using job queue
        public static TextRenderOutput UpdateTextNow(DecalText oldText, DecalText newText) {
            if (newText == null) throw new ArgumentNullException(nameof(newText));

            return Instance.RunJob(new TextRenderJob(oldText, newText, null), out _);
        }

        /// Unregister a user of a piece of text
        public static void UnregisterText(DecalText text) {
            if (RenderCache.TryGetValue(text, out var renderedText)) {
                renderedText.UserCount--;
                if (renderedText.UserCount <= 0) {
                    RenderCache.Remove(text);
                    Destroy(renderedText.Texture);
                }
            }
        }

        private void Start() {
            if (_instance != null) {
                Logging.LogError("Duplicate TextRenderer created???");
            }

            Logging.Log("Creating TextRenderer Object");
            _instance = this;
            DontDestroyOnLoad(gameObject);
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D12) {
                textRenderTextureFormat = RenderTextureFormat.ARGB32; // DirectX is dumb
            }

            if (!SystemInfo.SupportsTextureFormat(textTextureFormat)) {
                Logging.LogError($"Text texture format {textTextureFormat} not supported on this platform.");
            }

            if (!SystemInfo.SupportsRenderTextureFormat(textRenderTextureFormat)) {
                Logging.LogError($"Text texture format {textRenderTextureFormat} not supported on this platform.");
            }
        }

        /// Setup this text renderer instance for rendering
        private void Setup() {
            if (_isSetup) return;

            Logging.Log("Setting Up TextRenderer Object");

            _tmp = gameObject.AddComponent<TextMeshPro>();
            _tmp.renderer.enabled = false; // dont automatically render

            _blitShader = Shabby.Shabby.FindShader(ShaderName);
            if (_blitShader == null) Logging.LogError($"Could not find text blit shader named '{ShaderName}'");

            _isSetup = true;
        }

        /// Run a text render job
        private TextRenderOutput RunJob(TextRenderJob job, out bool renderNeeded) {
            if (!job.Needed) {
                renderNeeded = false;
                return null;
            }

            job.Start();

            Texture2D texture = null;
            if (job.OldText != null && RenderCache.TryGetValue(job.OldText, out var oldRender)) {
                // old output still exists

                oldRender.UserCount--;

                if (oldRender.UserCount <= 0) {
                    // this is the only usage of this output, so we are free to re-render into the texture

                    texture = oldRender.Texture;
                    RenderCache.Remove(job.OldText);
                }
            }

            // now that all old references are handled, begin rendering the new output

            if (RenderCache.TryGetValue(job.NewText, out var renderOutput)) {
                renderNeeded = false;
            }
            else {
                renderNeeded = true;

                renderOutput = RenderText(job.NewText, texture);
                RenderCache.Add(job.NewText, renderOutput);
            }

            renderOutput.UserCount++;

            job.Finish(renderOutput);
            return renderOutput;
        }

        /// Render a piece of text to a given texture
        public TextRenderOutput RenderText(DecalText text, Texture2D texture) {
            if (text == null) throw new ArgumentNullException(nameof(text));
            if (_tmp == null) throw new InvalidOperationException("TextMeshPro object not yet created.");

            // SETUP TMP OBJECT FOR RENDERING
            _tmp.text = text.FormattedText;
            _tmp.font = text.Font.FontAsset;
            _tmp.fontStyle = text.Style | text.Font.FontStyle;
            _tmp.lineSpacing = text.LineSpacing;
            _tmp.characterSpacing = text.CharSpacing;

            _tmp.extraPadding = true;
            _tmp.enableKerning = true;
            _tmp.enableWordWrapping = false;
            _tmp.overflowMode = TextOverflowModes.Overflow;
            _tmp.alignment = TextAlignmentOptions.Center;
            _tmp.fontSize = FontSize;

            // GENERATE MESH
            _tmp.ClearMesh(false);
            _tmp.ForceMeshUpdate();

            var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            var meshes = new Mesh[meshFilters.Length];
            var materials = new Material[meshFilters.Length];

            var bounds = new Bounds();

            // SETUP MATERIALS AND BOUNDS
            for (int i = 0; i < meshFilters.Length; i++) {
                var renderer = meshFilters[i].gameObject.GetComponent<MeshRenderer>();

                meshes[i] = meshFilters[i].mesh;
                if (i == 0) meshes[i] = _tmp.mesh;

                materials[i] = Instantiate(renderer.material);
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
                Logging.LogWarning("No text present or error in texture size calculation. Aborting.");
                return new TextRenderOutput(Texture2D.blackTexture, Rect.zero);
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
            if (texture == null) {
                texture = new Texture2D(textureSize.x, textureSize.y, textTextureFormat, true);
            }
            else if (texture.width != textureSize.x || texture.height != textureSize.y || texture.format != textTextureFormat) {
                texture.Resize(textureSize.x, textureSize.y, textTextureFormat, true);
            }

            // GENERATE PROJECTION MATRIX
            var halfSize = (Vector2) textureSize / PixelDensity / 2 / sizeRatio;
            var matrix = Matrix4x4.Ortho(bounds.center.x - halfSize.x, bounds.center.x + halfSize.x,
                bounds.center.y - halfSize.y, bounds.center.y + halfSize.y, -1, 1);

            // GET RENDERTEX
            var renderTex = new RenderTexture(textureSize.x, textureSize.y, 0, textRenderTextureFormat, RenderTextureReadWrite.Linear) {autoGenerateMips = false};

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

            // COPY TEXTURE BACK INTO RAM
            var prevRT = RenderTexture.active;
            RenderTexture.active = renderTex;
            texture.ReadPixels(new Rect(0, 0, textureSize.x, textureSize.y), 0, 0, true);
            texture.Apply();
            RenderTexture.active = prevRT;

            GL.PopMatrix();
            
            // RELEASE RENDERTEX
            renderTex.Release();
            RenderTexture.Destroy(renderTex);

            // CLEAR SUBMESHES
            _tmp.text = "";

            for (int i = 0; i < transform.childCount; i++) {
                var child = transform.GetChild(i);
                Destroy(child.gameObject);
            }

            return new TextRenderOutput(texture, window);
        }
    }
}