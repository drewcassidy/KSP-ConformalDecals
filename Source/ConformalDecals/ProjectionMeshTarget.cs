using System;
using System.Text;
using ConformalDecals.Util;
using UniLinq;
using UnityEngine;
using UnityEngine.Rendering;

namespace ConformalDecals {
    public class ProjectionMeshTarget : IProjectionTarget {
        // enabled flag
        public bool enabled = true;

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

        public ProjectionMeshTarget(ConfigNode node, Transform root, bool useBaseNormal) {
            if (node == null) throw new ArgumentNullException(nameof(node));
            if (root == null) throw new ArgumentNullException(nameof(root));

            var targetPath = ParseUtil.ParseString(node, "targetPath");
            var targetName = ParseUtil.ParseString(node, "targetName");

            _decalMatrix = ParseUtil.ParseMatrix4x4(node, "decalMatrix");
            _decalNormal = ParseUtil.ParseVector3(node, "decalNormal");
            _decalTangent = ParseUtil.ParseVector3(node, "decalTangent");
            _decalMPB = new MaterialPropertyBlock();

            target = LoadTransformPath(targetPath, root);
            if (target.name != targetName) throw new FormatException("Target name does not match");

            renderer = target.GetComponent<MeshRenderer>();
            var filter = target.GetComponent<MeshFilter>();

            if (!ValidateTarget(target, renderer, filter)) throw new FormatException("Invalid target");

            mesh = filter.sharedMesh;

            SetNormalMap(renderer.sharedMaterial, useBaseNormal);
            
            _decalMPB.SetMatrix(DecalPropertyIDs._ProjectionMatrix, _decalMatrix);
            _decalMPB.SetVector(DecalPropertyIDs._DecalNormal, _decalNormal);
            _decalMPB.SetVector(DecalPropertyIDs._DecalTangent, _decalTangent); 
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

        public void Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds) {
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
        }

        public void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera) {
            if (!enabled) return;

            _decalMPB.SetFloat(PropertyIDs._RimFalloff, partMPB.GetFloat(PropertyIDs._RimFalloff));
            _decalMPB.SetColor(PropertyIDs._RimColor, partMPB.GetColor(PropertyIDs._RimColor));

            Graphics.DrawMesh(mesh, target.localToWorldMatrix, decalMaterial, 0, camera, 0, _decalMPB, ShadowCastingMode.Off, true);
        }

        public ConfigNode Save() {
            var node = new ConfigNode("MESH_TARGET");
            node.AddValue("decalMatrix", _decalMatrix);
            node.AddValue("decalNormal", _decalNormal);
            node.AddValue("decalTangent", _decalTangent);
            node.AddValue("targetPath", SaveTransformPath(target, root)); // used to find the target transform
            node.AddValue("targetName", target.name); // used to validate the mesh has not changed since last load

            return node;
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

        private static string SaveTransformPath(Transform leaf, Transform root) {
            var builder = new StringBuilder($"{leaf.GetSiblingIndex()}");
            var current = leaf.parent;

            while (current != root) {
                builder.Insert(0, "/");
                builder.Insert(0, current.GetSiblingIndex());
                current = current.parent;
                if (current == null) throw new FormatException("Leaf does not exist as a child of root");
            }

            return builder.ToString();
        }

        private static Transform LoadTransformPath(string path, Transform root) {
            var indices = path.Split('/').Select(int.Parse);
            var current = root;

            foreach (var index in indices) {
                if (index > current.childCount) throw new FormatException("Child index path is invalid");
                current = current.GetChild(index);
            }

            return current;
        }
    }
}