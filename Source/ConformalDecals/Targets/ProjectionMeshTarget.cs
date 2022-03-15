using UnityEngine;
using UnityEngine.Rendering;

namespace ConformalDecals.Targets {
    public class ProjectionMeshTarget : IProjectionTarget {
        public const string NodeName = "MESH_TARGET";
        
        // enabled flag
        public bool enabled;

        // Target object data
        public readonly Transform    target;
        public readonly Transform    root;
        public readonly Mesh         mesh;
        public readonly MeshRenderer renderer;

        // Projection data
        private Matrix4x4 _decalMatrix;
        private Vector3   _decalNormal;
        private Vector3   _decalTangent;

        // property block
        private readonly MaterialPropertyBlock _decalMPB;

        public ProjectionMeshTarget(Transform target, Transform root, MeshRenderer renderer, Mesh mesh, bool useBaseNormal) {
            this.root = root;
            this.target = target;
            this.renderer = renderer;
            this.mesh = mesh;
            _decalMPB = new MaterialPropertyBlock();

            SetNormalMap(renderer.sharedMaterial, useBaseNormal);
        }

        private void SetNormalMap(Material targetMaterial, bool useBaseNormal) {
            if (useBaseNormal && targetMaterial.HasProperty(DecalPropertyIDs._BumpMap)) {
                _decalMPB.SetTexture(DecalPropertyIDs._BumpMap, targetMaterial.GetTexture(DecalPropertyIDs._BumpMap));

                var normalScale = targetMaterial.GetTextureScale(DecalPropertyIDs._BumpMap);
                var normalOffset = targetMaterial.GetTextureOffset(DecalPropertyIDs._BumpMap);

                _decalMPB.SetVector(DecalPropertyIDs._BumpMap_ST, new Vector4(normalScale.x, normalScale.y, normalOffset.x, normalOffset.y));
            }
            else {
                _decalMPB.SetTexture(DecalPropertyIDs._BumpMap, DecalConfig.BlankNormal);
            }
        }

        public bool Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds) {
            if (projectionBounds.Intersects(renderer.bounds)) {
                enabled = true;
                
                var projectorToTargetMatrix = target.worldToLocalMatrix * projector.localToWorldMatrix;

                _decalMatrix = orthoMatrix * projectorToTargetMatrix.inverse;
                _decalNormal = projectorToTargetMatrix.MultiplyVector(Vector3.back).normalized;
                _decalTangent = projectorToTargetMatrix.MultiplyVector(Vector3.right).normalized;

                _decalMPB.SetMatrix(DecalPropertyIDs._ProjectionMatrix, _decalMatrix);
                _decalMPB.SetVector(DecalPropertyIDs._DecalNormal, _decalNormal);
                _decalMPB.SetVector(DecalPropertyIDs._DecalTangent, _decalTangent);
            }
            else {
                enabled = false;
            }

            return enabled;
        }

        public void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera) {
            if (!enabled) return;

            _decalMPB.SetFloat(PropertyIDs._RimFalloff, partMPB.GetFloat(PropertyIDs._RimFalloff));
            _decalMPB.SetColor(PropertyIDs._RimColor, partMPB.GetColor(PropertyIDs._RimColor));

            Graphics.DrawMesh(mesh, target.localToWorldMatrix, decalMaterial, 0, camera, 0, _decalMPB, ShadowCastingMode.Off, true);
        }

        public static bool ValidateTarget(Transform target, MeshRenderer renderer, MeshFilter filter) {
            if (renderer == null) return false;
            if (filter == null) return false;
            if (!target.gameObject.activeInHierarchy) return false;

            var material = renderer.material;
            if (material == null) return false;
            if (DecalConfig.IsBlacklisted(material.shader)) return false;

            if (filter.sharedMesh == null) return false;

            return true;
        }
    }
}