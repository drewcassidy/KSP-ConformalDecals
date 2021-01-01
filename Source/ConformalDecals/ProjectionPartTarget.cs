using System;
using System.Collections.Generic;
using ConformalDecals.Util;
using UnityEngine;

namespace ConformalDecals {
    public class ProjectionPartTarget : IProjectionTarget {
        public const string NodeName = "PART_TARGET";

        // enabled flag
        public bool enabled;

        // locked flag, to prevent re-projection of loaded targets
        public readonly bool locked;

        public readonly Part                       part;
        public readonly List<ProjectionMeshTarget> meshTargets = new List<ProjectionMeshTarget>();


        public ProjectionPartTarget(Part part, bool useBaseNormal) {
            this.part = part;
            locked = false;

            foreach (var renderer in part.FindModelComponents<MeshRenderer>()) {
                var target = renderer.transform;
                var filter = target.GetComponent<MeshFilter>();

                // check if the target has any missing data
                if (!ProjectionMeshTarget.ValidateTarget(target, renderer, filter)) continue;

                // create new ProjectionTarget to represent the renderer
                var projectionTarget = new ProjectionMeshTarget(target, part.transform, renderer, filter.sharedMesh, useBaseNormal);

                // add the target to the list
                meshTargets.Add(projectionTarget);
            }
        }

        public ProjectionPartTarget(ConfigNode node, Vessel vessel, bool useBaseNormal) {
            if (node == null) throw new ArgumentNullException(nameof(node));
            locked = true;
            enabled = true;

            var flightID = ParseUtil.ParseUint(node, "part");

            part = vessel[flightID];
            if (part == null) throw new IndexOutOfRangeException("Vessel returned null part, part must be destroyed or detached");
            var root = part.transform;

            foreach (var meshTargetNode in node.GetNodes(ProjectionMeshTarget.NodeName)) {
                meshTargets.Add(new ProjectionMeshTarget(meshTargetNode, root, useBaseNormal));
            }

            Logging.Log($"Loaded target for part {part.name}");
        }

        public bool Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds) {
            if (locked) return true; // dont overwrite saved targets in flight mode
            enabled = false;
            foreach (var meshTarget in meshTargets) {
                enabled |= meshTarget.Project(orthoMatrix, projector, projectionBounds);
            }

            return enabled;
        }

        public void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera) {
            foreach (var target in meshTargets) {
                target.Render(decalMaterial, partMPB, camera);
            }
        }

        public ConfigNode Save() {
            var node = new ConfigNode(NodeName);
            node.AddValue("part", part.flightID);
            foreach (var meshTarget in meshTargets) {
                if (meshTarget.enabled) node.AddNode(meshTarget.Save());
            }

            Logging.Log($"Saved target for part {part.name}");

            return node;
        }
    }
}