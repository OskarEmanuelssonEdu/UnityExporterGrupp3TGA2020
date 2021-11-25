using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Scripting;

namespace Demonixis.UnityJSONSceneExporter
{
#if UNITY_EDITOR
    [CustomEditor(typeof(SceneExporter))]
    public class SceneExporterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var script = (SceneExporter)target;

            if (GUILayout.Button("Export"))
                script.Export();
        }
    }
#endif

    public class SceneExporter : MonoBehaviour
    {
        // Name, RelativePath
        private Dictionary<string, string> m_ExportedTextures = new Dictionary<string, string>();

        [SerializeField]
        private bool m_LogEnabled = true;
        [SerializeField]
        public bool m_ExportMeshData = true;
        [SerializeField]
        private Formatting m_JSONFormat = Formatting.Indented;
        [SerializeField]
        private bool m_ExportTextures = true;
        [SerializeField]
        private bool m_ExportAllScene = false;
        [SerializeField]
        private string m_ExportPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "UnitySceneExporter");
        [SerializeField]
        private string m_ExportFilename = "GameMap";
        [SerializeField]
        private bool m_MonoGameExport = false;

        [ContextMenu("Export")]
        public void Export()
        {
            var transforms = GetComponentsInChildren<Transform>(true);

#if UNITY_EDITOR
            if (m_ExportAllScene)
            {
                var tr = Resources.FindObjectsOfTypeAll(typeof(Transform));
                Array.Resize(ref transforms, tr.Length);

                for (var i = 0; i < tr.Length; i++)
                    transforms[i] = (Transform)tr[i];
            }
#endif
            // Name, Path

            var list = new List<UGameObject>();

            m_ExportedTextures.Clear();

            if (m_MonoGameExport)
                MonoGameExporter.BeginContentFile("Windows");

            foreach (var tr in transforms)
            {
                list.Add(ExportObject(tr));

                if (m_LogEnabled)
                    Debug.Log($"Exporter: {tr.name}");
            }

            if (!Directory.Exists(m_ExportPath))
                Directory.CreateDirectory(m_ExportPath);


            #region "Navmesh Stuff" 

            var mesh = NavMesh.CalculateTriangulation();

            float[][] navMeshVerticies = new float[mesh.vertices.Length][];
            List<int> navMeshIndicies = new List<int>();
            int navMeshAreas = 0;

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                navMeshVerticies[i] = (ToFloat3(mesh.vertices[i]));
            }
            foreach (int index in mesh.indices)
            {
                navMeshIndicies.Add(index);
            }
            navMeshAreas = mesh.areas.Length;

            var someNavMeshData = new UNavMeshData
            {
                Areas = navMeshAreas,
                Indices = navMeshIndicies.ToArray(),
                Verticies = navMeshVerticies
            };
           

            #endregion

            var navMeshObject = new UGameObject
            {
                Id = "navMeshId",
                Name = "navMesh",
                IsStatic = true,
                IsActive = false,
                Transform = new UTransform
                {
                    Parent = null,
                    LocalPosition = ToFloat3(Vector3.zero),
                    LocalRotation = ToFloat3(Vector3.zero),
                    LocalScale = ToFloat3(Vector3.zero)
                }
            };

            navMeshObject.navMeshData = someNavMeshData;

            list.Add(navMeshObject);

            var json = JsonConvert.SerializeObject(list.ToArray(), m_JSONFormat);
            var path = Path.Combine(m_ExportPath, $"{m_ExportFilename}.json");

            File.WriteAllText(path, json);

            if (m_MonoGameExport)
            {
                MonoGameExporter.AddMap(path);
                File.WriteAllText(Path.Combine(m_ExportPath, "Content.mgcb"), MonoGameExporter.GetContentData());
            }

            if (m_LogEnabled)
                Debug.Log($"Exported: {list.Count} objects");
        }

        public UGameObject ExportObject(Transform tr)
        {
            var uGameObject = new UGameObject
            {
                Id = tr.GetInstanceID().ToString(),
                Name = tr.name,
                Tag = tr.gameObject.tag,
                Layer = tr.gameObject.layer,
                IsStatic = tr.gameObject.isStatic,
                IsActive = tr.gameObject.activeSelf,
                Transform = new UTransform
                {
                    Parent = tr.transform.parent?.GetInstanceID().ToString() ?? null,
                    LocalPosition = ToFloat3(tr.transform.localPosition),
                    LocalRotation = ToFloat3(tr.transform.localRotation.eulerAngles),
                    LocalScale = ToFloat3(tr.transform.localScale)
                }
            };

            var light = tr.GetComponent<Light>();
            if (light != null)
            {
                var lightType = 0;
                if (light.type == LightType.Point)
                    lightType = 1;
                else if (light.type == LightType.Spot)
                    lightType = 2;
                else
                    lightType = -1;

                uGameObject.Light = new ULight
                {                  
                    Intensity = light.intensity,
                    IndirectMultiplier = light.bounceIntensity,
                    Radius = light.range,
                    Color = ToFloat4(light.color),
                    Angle = light.spotAngle,
                    ShadowsEnabled = light.shadows != LightShadows.None,
                    Enabled = light.enabled,
                    Type = lightType
                };
            }

            return uGameObject;
        }

        public static float[] ToFloat2(Vector2 vector) => new[] { vector.x, vector.y };
        public static float[] ToFloat3(Vector3 vector) => new[] { vector.x, vector.y, vector.z };
        public static float[] ToFloat3(Color color) => new[] { color.r, color.g, color.b };
        public static float[] ToFloat4(Color color) => new[] { color.r, color.g, color.b, color.a };

        public static float[] ToFloat2(Vector2[] vecs)
        {
            var list = new List<float>();

            foreach (var vec in vecs)
            {
                list.Add(vec.x);
                list.Add(vec.y);
            }

            return list.ToArray();
        }

        public static float[] ToFloat3(Vector3[] vecs)
        {
            var list = new List<float>();

            foreach (var vec in vecs)
            {
                list.Add(vec.x);
                list.Add(vec.y);
                list.Add(vec.z);
            }

            return list.ToArray();
        }
    }
}