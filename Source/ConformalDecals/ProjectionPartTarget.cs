using System;
using System.Collections.Generic;
using UnityEngine;

namespace ConformalDecals {
    public class ProjectionPartTarget : IProjectionTarget {
        public readonly Part                       part;
        public readonly List<ProjectionMeshTarget> meshTargets;

        public ProjectionPartTarget(Part part, bool useBaseNormal) {
            this.part = part;
            meshTargets = new List<ProjectionMeshTarget>();

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

        public void Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds) {
            foreach (var meshTarget in meshTargets) {
                meshTarget.Project(orthoMatrix, projector, projectionBounds);
            }
        }

        public void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera) {
            foreach (var target in meshTargets) {
                target.Render(decalMaterial, partMPB, camera);
            }
        }

        public ConfigNode Save() {
            var node = new ConfigNode("PART_TARGET");
            node.AddValue("part", part.flightID);
            foreach (var meshTarget in meshTargets) {
                node.AddNode(meshTarget.Save());
            }

            return node;
        }
    }
}