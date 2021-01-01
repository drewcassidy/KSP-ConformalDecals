using UnityEngine;

namespace ConformalDecals {
    public interface IProjectionTarget {
        void Project(Matrix4x4 orthoMatrix, Transform projector, Bounds projectionBounds);
        void Render(Material decalMaterial, MaterialPropertyBlock partMPB, Camera camera);
        ConfigNode Save();
    }
}