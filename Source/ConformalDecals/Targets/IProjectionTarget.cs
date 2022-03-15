using UnityEngine;

namespace ConformalDecals.Targets {
    public interface IProjectionTarget {
        bool Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds);
        void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera);
    }
}