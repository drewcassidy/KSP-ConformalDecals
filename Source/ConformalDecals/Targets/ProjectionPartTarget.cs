using System.Collections.Generic;
using UnityEngine;

namespace ConformalDecals.Targets {
    public class ProjectionPartTarget : IProjectionTarget {
        public const string NodeName = "PART_TARGET";

        // enabled flag
        public bool enabled;

        public readonly Part                       part;
        public readonly List<ProjectionMeshTarget> meshTargets = new List<ProjectionMeshTarget>();


        public ProjectionPartTarget(Part part, bool useBaseNormal) {
            this.part = part;

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

        public bool Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds) {
            enabled = false;
            foreach (var meshTarget in meshTargets) {
                enabled |= meshTarget.Project(orthoMatrix, projector, projectionBounds);
            }

            return enabled;
        }

        public void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera) {
            if (!enabled) return;
            
            foreach (var target in meshTargets) {
                target.Render(decalMaterial, partMPB, camera);
            }
        }
    }
}