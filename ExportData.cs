using System;

namespace Demonixis.UnityJSONSceneExporter
{
    [Serializable]
    public class UTransform
    {
        public string Parent;
        public float[] LocalPosition;
        public float[] LocalRotation;
        public float[] LocalScale;
    }

    [Serializable]
    public class UNavMeshData
    {
        public float[][] Verticies;
        public int[] Indices;
        public int[] AreaData;
        public int Areas;
    }

    [Serializable]
    public class ULight
    {
        public bool Enabled;
        public float Radius;
        public float Intensity;
        public float IndirectMultiplier;
        public float Type;
        public float Angle;
        public float[] Color;
        public bool ShadowsEnabled;
    }

    [Serializable]
    public class UGameObject
    {
        public string Id;
        public string Name;
        public string Tag;
        public int Layer;
        public bool IsStatic;
        public bool IsActive;
        public UTransform Transform;
        public ULight Light;
        public UNavMeshData navMeshData = null;
    }
}
