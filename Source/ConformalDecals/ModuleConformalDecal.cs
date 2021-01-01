using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ConformalDecals.MaterialProperties;
using ConformalDecals.Util;
using UniLinq;
using UnityEngine;

namespace ConformalDecals {
    public class ModuleConformalDecal : PartModule {
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum DecalScaleMode {
            HEIGHT,
            WIDTH,
            AVERAGE,
            AREA,
            MINIMUM,
            MAXIMUM
        }

        // CONFIGURABLE VALUES

        [KSPField] public string shader = "ConformalDecals/Decal/Standard";

        [KSPField] public string decalFront     = "Decal-Front";
        [KSPField] public string decalBack      = "Decal-Back";
        [KSPField] public string decalModel     = "Decal-Model";
        [KSPField] public string decalProjector = "Decal-Projector";
        [KSPField] public string decalCollider  = "Decal-Collider";

        // Parameters

        [KSPField] public bool    scaleAdjustable = true;
        [KSPField] public float   defaultScale    = 1;
        [KSPField] public Vector2 scaleRange      = new Vector2(0, 5);

        [KSPField] public DecalScaleMode scaleMode = DecalScaleMode.HEIGHT;

        [KSPField] public bool    depthAdjustable = true;
        [KSPField] public float   defaultDepth    = 0.1f;
        [KSPField] public Vector2 depthRange      = new Vector2(0, 2);

        [KSPField] public bool    opacityAdjustable = true;
        [KSPField] public float   defaultOpacity    = 1;
        [KSPField] public Vector2 opacityRange      = new Vector2(0, 1);

        [KSPField] public bool    cutoffAdjustable = true;
        [KSPField] public float   defaultCutoff    = 0.5f;
        [KSPField] public Vector2 cutoffRange      = new Vector2(0, 1);

        [KSPField] public bool    useBaseNormal = true;
        [KSPField] public float   defaultWear   = 100;
        [KSPField] public Vector2 wearRange     = new Vector2(0, 100);

        [KSPField] public Rect    tileRect = new Rect(-1, -1, 0, 0);
        [KSPField] public Vector2 tileSize;
        [KSPField] public int     tileIndex = -1;

        [KSPField] public bool updateBackScale = true;
        [KSPField] public bool selectableInFlight;

        // INTERNAL VALUES
        [KSPField(guiName = "#LOC_ConformalDecals_gui-scale", guiActive = false, guiActiveEditor = true, isPersistant = true, guiFormat = "F2", guiUnits = "m"),
         UI_FloatRange()]
        public float scale = 1.0f;

        [KSPField(guiName = "#LOC_ConformalDecals_gui-depth", guiActive = false, guiActiveEditor = true, isPersistant = true, guiFormat = "F2", guiUnits = "m"),
         UI_FloatRange()]
        public float depth = 0.2f;

        [KSPField(guiName = "#LOC_ConformalDecals_gui-opacity", guiActive = false, guiActiveEditor = true, isPersistant = true, guiFormat = "P0"),
         UI_FloatRange()]
        public float opacity = 1.0f;

        [KSPField(guiName = "#LOC_ConformalDecals_gui-cutoff", guiActive = false, guiActiveEditor = true, isPersistant = true, guiFormat = "P0"),
         UI_FloatRange()]
        public float cutoff = 0.5f;

        [KSPField(guiName = "#LOC_ConformalDecals_gui-wear", guiActive = false, guiActiveEditor = true, isPersistant = true, guiFormat = "F0"),
         UI_FloatRange()]
        public float wear = 100;

        [KSPField(guiName = "#LOC_ConformalDecals_gui-multiproject", guiActive = false, guiActiveEditor = true, isPersistant = true),
         UI_Toggle()]
        public bool projectMultiple = true;

        [KSPField] public MaterialPropertyCollection materialProperties;

        [KSPField] public Transform decalFrontTransform;
        [KSPField] public Transform decalBackTransform;
        [KSPField] public Transform decalModelTransform;
        [KSPField] public Transform decalProjectorTransform;
        [KSPField] public Transform decalColliderTransform;

        [KSPField] public Material backMaterial;
        [KSPField] public Vector2  backTextureBaseScale;

        private const  int DecalQueueMin      = 2100;
        private const  int DecalQueueMax      = 2400;
        private static int _decalQueueCounter = -1;

        private Dictionary<Part, ProjectionPartTarget> _targets = new Dictionary<Part, ProjectionPartTarget>();

        private bool      _isAttached;
        private Matrix4x4 _orthoMatrix;

        private Material     _decalMaterial;
        private Material     _previewMaterial;
        private MeshRenderer _boundsRenderer;

        private int DecalQueue {
            get {
                _decalQueueCounter++;
                if (_decalQueueCounter > DecalQueueMax || _decalQueueCounter < DecalQueueMin) {
                    _decalQueueCounter = DecalQueueMin;
                }

                return _decalQueueCounter;
            }
        }

        // EVENTS

        /// <inheritdoc />
        public override void OnAwake() {
            base.OnAwake();

            if (materialProperties == null) {
                materialProperties = ScriptableObject.CreateInstance<MaterialPropertyCollection>();
            }
            else {
                materialProperties = ScriptableObject.Instantiate(materialProperties);
            }
        }

        /// <inheritdoc />
        public override void OnLoad(ConfigNode node) {
            // Load
            try {
                LoadDecal(node);
            }
            catch (Exception e) {
                this.LogException("Error loading decal", e);
            }

            // Setup
            try {
                SetupDecal();
            }
            catch (Exception e) {
                this.LogException("Error setting up decal", e);
            }
        }

        /// <inheritdoc />
        public override void OnIconCreate() {
            UpdateTextures();
            UpdateProjection();
        }

        /// <inheritdoc />
        public override void OnStart(StartState state) {
            materialProperties.RenderQueue = DecalQueue;

            _boundsRenderer = decalProjectorTransform.GetComponent<MeshRenderer>();

            // handle tweakables
            if (HighLogic.LoadedSceneIsEditor) {
                GameEvents.onEditorPartEvent.Add(OnEditorEvent);
                GameEvents.onVariantApplied.Add(OnVariantApplied);

                UpdateTweakables();
            }
        }

        /// Called after OnStart is finished for all parts
        /// This is mostly used to make sure all B9 variants are already in place for the rest of the vessel
        public override void OnStartFinished(StartState state) {
            // handle game events
            if (HighLogic.LoadedSceneIsGame) {
                // set initial attachment state
                if (part.parent == null) {
                    OnDetach();
                }
                else {
                    OnAttach();
                }
            }

            // handle flight events
            if (HighLogic.LoadedSceneIsFlight) {
                GameEvents.onPartWillDie.Add(OnPartWillDie);

                if (part.parent == null) part.explode();

                Part.layerMask |= 1 << DecalConfig.DecalLayer;
                decalColliderTransform.gameObject.layer = DecalConfig.DecalLayer;

                if (!selectableInFlight || !DecalConfig.SelectableInFlight) {
                    decalColliderTransform.GetComponent<Collider>().enabled = false;
                    _boundsRenderer.enabled = false;
                }
            }
        }

        /// Called when the decal is destroyed
        public virtual void OnDestroy() {
            // remove GameEvents
            if (HighLogic.LoadedSceneIsEditor) {
                GameEvents.onEditorPartEvent.Remove(OnEditorEvent);
                GameEvents.onVariantApplied.Remove(OnVariantApplied);
            }

            if (HighLogic.LoadedSceneIsFlight) {
                GameEvents.onPartWillDie.Remove(OnPartWillDie);
            }

            // remove from preCull delegate
            Camera.onPreCull -= Render;

            // destroy material properties object
            Destroy(materialProperties);
        }

        /// Called when the decal's projection and scale is modified through a tweakable
        protected void OnProjectionTweakEvent(BaseField field, object obj) {
            // scale or depth values have been changed, so update scale
            // and update projection matrices if attached
            UpdateProjection();
            UpdateTargets();

            foreach (var counterpart in part.symmetryCounterparts) {
                var decal = counterpart.GetComponent<ModuleConformalDecal>();
                decal.UpdateProjection();
                decal.UpdateTargets();
            }
        }

        /// Called when the decal's material is modified through a tweakable
        protected void OnMaterialTweakEvent(BaseField field, object obj) {
            materialProperties.SetOpacity(opacity);
            materialProperties.SetCutoff(cutoff);
            if (useBaseNormal) {
                materialProperties.SetWear(wear);
            }

            foreach (var counterpart in part.symmetryCounterparts) {
                var decal = counterpart.GetComponent<ModuleConformalDecal>();
                decal.materialProperties.SetOpacity(opacity);
                decal.materialProperties.SetCutoff(cutoff);
                if (useBaseNormal) {
                    decal.materialProperties.SetWear(wear);
                }
            }
        }

        /// Called when a new variant is applied in the editor
        protected void OnVariantApplied(Part eventPart, PartVariant variant) {
            if (_isAttached && eventPart != null && (!projectMultiple || eventPart == part.parent)) {
                _targets.Remove(eventPart);
                UpdateTargets();
            }
        }

        /// Called when an editor event occurs
        protected void OnEditorEvent(ConstructionEventType eventType, Part eventPart) {
            switch (eventType) {
                case ConstructionEventType.PartAttached:
                    OnPartAttached(eventPart);
                    break;
                case ConstructionEventType.PartDetached:
                    OnPartDetached(eventPart);
                    break;
                case ConstructionEventType.PartOffsetting:
                case ConstructionEventType.PartRotating:
                    OnPartTransformed(eventPart);
                    break;
            }
        }

        /// Called when a part is transformed in the editor
        protected void OnPartTransformed(Part eventPart) {
            if (this.part == eventPart) {
                UpdateProjection();
                UpdateTargets();
            }
            else if (_isAttached && projectMultiple) {
                UpdatePartTarget(eventPart, _boundsRenderer.bounds);
                // recursively call for child parts
                foreach (var child in eventPart.children) {
                    OnPartTransformed(child);
                }
            }
        }

        /// Called when a part is attached in the editor
        protected void OnPartAttached(Part eventPart) {
            if (this.part == eventPart) {
                OnAttach();
            }
            else if (projectMultiple) {
                UpdatePartTarget(eventPart, _boundsRenderer.bounds);
                // recursively call for child parts
                foreach (var child in eventPart.children) {
                    OnPartAttached(child);
                }
            }
        }

        /// Called when a part is detached in the editor
        protected void OnPartDetached(Part eventPart) {
            if (this.part == eventPart) {
                OnDetach();
            }
            else if (projectMultiple) {
                _targets.Remove(eventPart);
                // recursively call for child parts
                foreach (var child in eventPart.children) {
                    OnPartDetached(child);
                }
            }
        }

        /// Called when part `willDie` will be destroyed
        protected void OnPartWillDie(Part willDie) {
            if (willDie == part.parent) {
                this.Log("Parent part about to be destroyed! Killing decal part.");
                part.Die();
            }
            else if (projectMultiple) {
                _targets.Remove(willDie);
            }
        }

        /// Called when decal is attached to a new part
        protected virtual void OnAttach() {
            if (part.parent == null) {
                this.LogError("Attach function called but part has no parent!");
                _isAttached = false;
                return;
            }

            _isAttached = true;

            // hide model
            decalModelTransform.gameObject.SetActive(false);

            // unhide projector
            decalProjectorTransform.gameObject.SetActive(true);

            // add to preCull delegate
            Camera.onPreCull += Render;

            UpdateMaterials();
            UpdateProjection();
            UpdateTargets();
        }

        /// Called when decal is detached from its parent part
        protected virtual void OnDetach() {
            _isAttached = false;

            // unhide model
            decalModelTransform.gameObject.SetActive(true);

            // hide projector
            decalProjectorTransform.gameObject.SetActive(false);

            // remove from preCull delegate
            Camera.onPreCull -= Render;

            UpdateMaterials();
            UpdateProjection();
        }

        // FUNCTIONS

        /// Load any settings from the decal config
        protected virtual void LoadDecal(ConfigNode node) {
            // PARSE TRANSFORMS
            decalFrontTransform = part.FindModelTransform(decalFront);
            if (decalFrontTransform == null) throw new FormatException($"Could not find decalFront transform: '{decalFront}'.");

            decalBackTransform = part.FindModelTransform(decalBack);
            if (decalBackTransform == null) throw new FormatException($"Could not find decalBack transform: '{decalBack}'.");

            decalModelTransform = part.FindModelTransform(decalModel);
            if (decalModelTransform == null) throw new FormatException($"Could not find decalModel transform: '{decalModel}'.");

            decalProjectorTransform = part.FindModelTransform(decalProjector);
            if (decalProjectorTransform == null) throw new FormatException($"Could not find decalProjector transform: '{decalProjector}'.");

            decalColliderTransform = part.FindModelTransform(decalCollider);
            if (decalColliderTransform == null) throw new FormatException($"Could not find decalCollider transform: '{decalCollider}'.");

            // SETUP BACK MATERIAL
            if (updateBackScale) {
                var backRenderer = decalBackTransform.GetComponent<MeshRenderer>();
                if (backRenderer == null) {
                    this.LogError($"Specified decalBack transform {decalBack} has no renderer attached! Setting updateBackScale to false.");
                    updateBackScale = false;
                }
                else {
                    backMaterial = backRenderer.material;
                    if (backMaterial == null) {
                        this.LogError($"Specified decalBack transform {decalBack} has a renderer but no material! Setting updateBackScale to false.");
                        updateBackScale = false;
                    }
                    else {
                        if (backTextureBaseScale == default) backTextureBaseScale = backMaterial.GetTextureScale(PropertyIDs._MainTex);
                    }
                }
            }

            // PARSE MATERIAL PROPERTIES
            // set shader
            materialProperties.SetShader(shader);
            materialProperties.AddOrGetProperty<MaterialKeywordProperty>("DECAL_BASE_NORMAL").value = useBaseNormal;
            materialProperties.Load(node);

            // handle texture tiling parameters
            var tileString = node.GetValue("tile");
            if (!string.IsNullOrEmpty(tileString)) {
                var tileValid = ParseExtensions.TryParseRect(tileString, out tileRect);
                if (!tileValid) throw new FormatException($"Invalid rect value for tile '{tileString}'");
            }

            if (tileRect.x >= 0) {
                materialProperties.UpdateTile(tileRect);
            }
            else if (tileIndex >= 0) {
                materialProperties.UpdateTile(tileIndex, tileSize);
            }
        }

        /// Setup decal by calling update functions relevent for the current situation
        protected virtual void SetupDecal() {
            if (HighLogic.LoadedSceneIsEditor) {
                // Update tweakables in editor mode
                UpdateTweakables();
            }

            if (HighLogic.LoadedSceneIsGame) {
                UpdateAll();
            }
            else {
                scale = defaultScale;
                depth = defaultDepth;
                opacity = defaultOpacity;
                cutoff = defaultCutoff;
                wear = defaultWear;

                UpdateAll();

                // QUEUE PART FOR ICON FIXING IN VAB
                DecalIconFixer.QueuePart(part.name);
            }
        }

        /// Update decal editor tweakables
        protected virtual void UpdateTweakables() {
            // setup tweakable fields
            var scaleField = Fields[nameof(scale)];
            var depthField = Fields[nameof(depth)];
            var opacityField = Fields[nameof(opacity)];
            var cutoffField = Fields[nameof(cutoff)];
            var wearField = Fields[nameof(wear)];
            var multiprojectField = Fields[nameof(projectMultiple)];

            scaleField.guiActiveEditor = scaleAdjustable;
            depthField.guiActiveEditor = depthAdjustable;
            opacityField.guiActiveEditor = opacityAdjustable;
            cutoffField.guiActiveEditor = cutoffAdjustable;
            wearField.guiActiveEditor = useBaseNormal;

            var steps = 20;

            if (scaleAdjustable) {
                var minValue = Mathf.Max(Mathf.Epsilon, scaleRange.x);
                var maxValue = Mathf.Max(minValue, scaleRange.y);

                var scaleEditor = (UI_FloatRange) scaleField.uiControlEditor;
                scaleEditor.minValue = minValue;
                scaleEditor.maxValue = maxValue;
                scaleEditor.stepIncrement = 0.01f; //1cm
                scaleEditor.onFieldChanged = OnProjectionTweakEvent;
            }

            if (depthAdjustable) {
                var minValue = Mathf.Max(Mathf.Epsilon, depthRange.x);
                var maxValue = Mathf.Max(minValue, depthRange.y);

                var depthEditor = (UI_FloatRange) depthField.uiControlEditor;
                depthEditor.minValue = minValue;
                depthEditor.maxValue = maxValue;
                depthEditor.stepIncrement = 0.01f; //1cm
                depthEditor.onFieldChanged = OnProjectionTweakEvent;
            }

            if (opacityAdjustable) {
                var minValue = Mathf.Max(0, opacityRange.x);
                var maxValue = Mathf.Max(minValue, opacityRange.y);
                maxValue = Mathf.Min(1, maxValue);

                var opacityEditor = (UI_FloatRange) opacityField.uiControlEditor;
                opacityEditor.minValue = minValue;
                opacityEditor.maxValue = maxValue;
                opacityEditor.stepIncrement = (maxValue - minValue) / steps;
                opacityEditor.onFieldChanged = OnMaterialTweakEvent;
            }

            if (cutoffAdjustable) {
                var minValue = Mathf.Max(0, cutoffRange.x);
                var maxValue = Mathf.Max(minValue, cutoffRange.y);
                maxValue = Mathf.Min(1, maxValue);

                var cutoffEditor = (UI_FloatRange) cutoffField.uiControlEditor;
                cutoffEditor.minValue = minValue;
                cutoffEditor.maxValue = maxValue;
                cutoffEditor.stepIncrement = (maxValue - minValue) / steps;
                cutoffEditor.onFieldChanged = OnMaterialTweakEvent;
            }

            if (useBaseNormal) {
                var minValue = Mathf.Max(0, wearRange.x);
                var maxValue = Mathf.Max(minValue, wearRange.y);

                var wearEditor = (UI_FloatRange) wearField.uiControlEditor;
                wearEditor.minValue = minValue;
                wearEditor.maxValue = maxValue;
                wearEditor.stepIncrement = (maxValue - minValue) / steps;
                wearEditor.onFieldChanged = OnMaterialTweakEvent;
            }

            var multiprojectEditor = (UI_Toggle) multiprojectField.uiControlEditor;
            multiprojectEditor.onFieldChanged = OnProjectionTweakEvent;
        }

        /// Updates textures, materials, scale and targets
        protected virtual void UpdateAll() {
            UpdateTextures();
            UpdateMaterials();
            UpdateProjection();
            UpdateTargets();
        }

        /// Update decal textures
        protected virtual void UpdateTextures() { }

        /// Update decal materials
        protected virtual void UpdateMaterials() {
            materialProperties.UpdateMaterials();
            materialProperties.SetOpacity(opacity);
            materialProperties.SetCutoff(cutoff);
            if (useBaseNormal) {
                materialProperties.SetWear(wear);
            }

            _decalMaterial = materialProperties.DecalMaterial;
            _previewMaterial = materialProperties.PreviewMaterial;

            if (!_isAttached) decalFrontTransform.GetComponent<MeshRenderer>().material = _previewMaterial;
        }

        /// Update decal scale and projection
        protected void UpdateProjection() {

            // Update scale and depth
            scale = Mathf.Max(0.01f, scale);
            depth = Mathf.Max(0.01f, depth);
            var aspectRatio = Mathf.Max(0.01f, materialProperties.AspectRatio);
            Vector2 size;

            switch (scaleMode) {
                default:
                case DecalScaleMode.HEIGHT:
                    size = new Vector2(scale / aspectRatio, scale);
                    break;
                case DecalScaleMode.WIDTH:
                    size = new Vector2(scale, scale * aspectRatio);
                    break;
                case DecalScaleMode.AVERAGE:
                    var width1 = 2 * scale / (1 + aspectRatio);
                    size = new Vector2(width1, width1 * aspectRatio);
                    break;
                case DecalScaleMode.AREA:
                    var width2 = Mathf.Sqrt(scale / aspectRatio);
                    size = new Vector2(width2, width2 * aspectRatio);
                    break;
                case DecalScaleMode.MINIMUM:
                    if (aspectRatio > 1) goto case DecalScaleMode.WIDTH;
                    else goto case DecalScaleMode.HEIGHT;
                case DecalScaleMode.MAXIMUM:
                    if (aspectRatio > 1) goto case DecalScaleMode.HEIGHT;
                    else goto case DecalScaleMode.WIDTH;
            }

            // update material scale
            materialProperties.UpdateScale(size);

            if (_isAttached) {
                // update orthogonal matrix
                _orthoMatrix = Matrix4x4.identity;
                _orthoMatrix[0, 3] = 0.5f;
                _orthoMatrix[1, 3] = 0.5f;

                decalProjectorTransform.localScale = new Vector3(size.x, size.y, depth);
            }
            else {
                // rescale preview model
                decalModelTransform.localScale = new Vector3(size.x, size.y, (size.x + size.y) / 2);

                // update back material scale
                if (updateBackScale) {
                    backMaterial.SetTextureScale(PropertyIDs._MainTex, new Vector2(size.x * backTextureBaseScale.x, size.y * backTextureBaseScale.y));
                }
            }
        }

        /// Called when updating decal targets
        protected void UpdateTargets() {
            if (!_isAttached) return;

            var projectionBounds = _boundsRenderer.bounds;

            // collect list of potential targets
            IEnumerable<Part> targetParts;
            if (projectMultiple) {
                targetParts = HighLogic.LoadedSceneIsFlight ? part.vessel.parts : EditorLogic.fetch.ship.parts;
            }
            else {
                targetParts = new[] {part.parent};
            }

            foreach (var targetPart in targetParts) {
                UpdatePartTarget(targetPart, projectionBounds);
            }
        }

        protected void UpdatePartTarget(Part targetPart, Bounds projectionBounds) {
            if (targetPart.GetComponent<ModuleConformalDecal>() != null) return; // skip other decals

            this.Log($"Updating projection onto part {targetPart.name}");

            if (!_targets.TryGetValue(targetPart, out var target)) {
                var rendererList = targetPart.FindModelComponents<MeshRenderer>();

                if (rendererList.Any(o => projectionBounds.Intersects(o.bounds))) {
                    target = new ProjectionPartTarget(targetPart, useBaseNormal);
                    _targets.Add(targetPart, target);
                }
                else {
                    return;
                }
            }

            this.Log($"valid target: {targetPart.name}");

            target.Project(_orthoMatrix, decalProjectorTransform, projectionBounds);
        }

        /// Render the decal
        public void Render(Camera camera) {
            if (!_isAttached) return;

            // render on each target object
            foreach (var target in _targets.Values) {
                target.Render(_decalMaterial, part.mpb, camera);
            }
        }
    }
}