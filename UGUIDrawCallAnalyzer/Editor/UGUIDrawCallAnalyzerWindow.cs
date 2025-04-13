using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

namespace Venus.UGUIDrawCallAnalyzer
{
    public class UGUIDrawCallAnalyzerWindow : EditorWindow
    {
        public struct BatchInfo
        {
            public Material Material;
            public Texture Texture;
            public List<Graphic> Elements;
            public int FirstElementIndex;
            public bool Mask;
        }

        private GUIStyle _batchStyle;
        private GUIStyle _drawCallCountStyle;
        private GUIStyle _tipsStyle;

        private Canvas _canvas;
        private List<BatchInfo> _batchInfos = new List<BatchInfo>();
        private Vector3 _scrollPos;

        private static readonly Color[] BatchColors = new Color[]
        {
            Color.red,
            Color.green,
            Color.blue,
            Color.yellow,
            Color.cyan,
            Color.magenta,
            new Color(1, 0.5f, 0),
            new Color(0.5f, 0, 1)
        };

        [MenuItem("Tools/Venus/UGUI DrawCall Analyzer")]
        public static void ShowWindowMenu()
        {
            UGUIDrawCallAnalyzerWindow window = GetWindow<UGUIDrawCallAnalyzerWindow>("UGUI DrawCall Analyzer");
            window.minSize = new Vector2(1500, 400);
        }

        public static void ShowWindow(Canvas canvas)
        {
            UGUIDrawCallAnalyzerWindow window = GetWindow<UGUIDrawCallAnalyzerWindow>("UGUI DrawCall Analyzer");
            window.minSize = new Vector2(1500, 400);
            window.SetCanvas(canvas);
            window.ProcessBatch();
            SceneView.RepaintAll();
        }

        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
            InitStyle();
        }

        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        void OnGUI()
        {
            _canvas = (Canvas)EditorGUILayout.ObjectField("Target Canvas", _canvas, typeof(Canvas), true);

            string buttonName = "Analyze Canvas Draw Call";
            bool continueDraw = true;
            if (_canvas == null)
            {
                buttonName = "Canvas is Null!";
                continueDraw = false;
                GUI.color = Color.red;
            }

            if (GUILayout.Button(buttonName))
            {
                if (_canvas == null) return;
                ProcessBatch();
                SceneView.RepaintAll();
            }

            GUI.color = Color.white;

            if (continueDraw == false) return;
            if (_batchInfos.Count > 0)
            {
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                EditorGUILayout.LabelField($"Total DrawCalls: {_batchInfos.Count}", _drawCallCountStyle);
                GUILayout.Space(8);
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                
                _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, false, true);
                for (int i = 0; i < _batchInfos.Count; i++)
                {
                    BatchInfo info = _batchInfos[i];
                    Material mat = info.Material;
                    string matName = mat != null ? mat.name : "None";

                    Texture tex = info.Texture;
                    string texName = tex != null ? tex.name : "None";

                    string batchInfoStr =
                        $"<color=yellow>Batch Step: {i + 1}</color> | <color=cyan>Material: {matName}</color> | Texture: {texName}";
                    if (info.Mask)
                    {
                        batchInfoStr =
                            $"<color=yellow>Batch Step: {i + 1}</color> <color=red>(Exist Mask Component!)</color> | <color=cyan>Material: {matName}</color> | Texture: {texName}";
                    }

                    EditorGUILayout.LabelField(batchInfoStr, _batchStyle);
                    foreach (var element in info.Elements)
                    {
                        EditorGUILayout.ObjectField(element.name, element, typeof(Graphic), true);
                    }

                    EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                }
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                GUILayout.Label("No Graphic In Canvas!", _tipsStyle);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
                GUILayout.FlexibleSpace();
            }
        }

        private void ProcessBatch()
        {
            _scrollPos = Vector3.zero;
            if (_canvas == null) return;

            _batchInfos.Clear();

            List<Graphic> elements = new List<Graphic>();
            GetComponentsRecursive(_canvas.transform, elements);

            // Process elements in hierarchy order (important for correct overlap detection)
            foreach (Graphic element in elements)
            {
                bool addedToExistingBatch = false;

                // Try to add to existing batch with same material/texture
                foreach (var batch in _batchInfos)
                {
                    if (CanBatchTogether(batch, element))
                    {
                        // Check if this element is overlapped by any blocking elements
                        // that were added after the batch was created
                        bool isOverlapped = false;
                        for (int i = batch.FirstElementIndex; i < elements.Count; i++)
                        {
                            Graphic other = elements[i];
                            if (other == element) break; // Only check elements before current one

                            // Only need to check elements that would break batching
                            if (!CanBatchTogether(batch, other) && CheckOverlap(element, other))
                            {
                                isOverlapped = true;
                                break;
                            }
                        }

                        if (!isOverlapped)
                        {
                            batch.Elements.Add(element);
                            addedToExistingBatch = true;
                            break;
                        }
                    }
                }

                if (!addedToExistingBatch)
                {
                    // Create new batch
                    _batchInfos.Add(new BatchInfo()
                    {
                        Material = element.materialForRendering,
                        Texture = element.mainTexture,
                        Elements = new List<Graphic> { element },
                        FirstElementIndex = elements.IndexOf(element)
                    });
                }

                Mask mask = element.GetComponent<Mask>();
                if (mask != null && mask.isActiveAndEnabled)
                {
                    _batchInfos.Add(new BatchInfo
                    {
                        Material = element.materialForRendering,
                        Texture = element.mainTexture,
                        Elements = new List<Graphic> { element },
                        FirstElementIndex = elements.IndexOf(element),
                        Mask = true
                    });
                }
            }
        }

        private bool CanBatchTogether(BatchInfo batch, Graphic element)
        {
            return batch.Material == element.materialForRendering && batch.Texture == element.mainTexture;
        }

        private bool CheckOverlap(Graphic a, Graphic b)
        {
            if (a == b) return false;

            Rect rectA = GetWorldRect(a.rectTransform);
            Rect rectB = GetWorldRect(b.rectTransform);

            return rectA.Overlaps(rectB);
        }

        private void GetComponentsRecursive(Transform parent, List<Graphic> results)
        {
            Graphic graphic = parent.GetComponent<Graphic>();
            if (graphic != null && graphic.isActiveAndEnabled && graphic.color.a > 0 && graphic.rectTransform.lossyScale != Vector3.zero)
            {
                results.Add(graphic);
            }

            foreach (Transform child in parent)
            {
                if (child.GetComponent<Canvas>()) continue;
                GetComponentsRecursive(child, results);
            }
        }

        private Rect GetWorldRect(RectTransform rectTransform)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);
            float minX = corners.Min(c => c.x);
            float maxX = corners.Max(c => c.x);
            float minY = corners.Min(c => c.y);
            float maxY = corners.Max(c => c.y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            if (_batchInfos.Count <= 0 || _canvas == null) return;

            for (int i = 0; i < _batchInfos.Count; i++)
            {
                Color color = BatchColors[i % BatchColors.Length];
                color.a = 0.3f; // Add some transparency

                foreach (Graphic element in _batchInfos[i].Elements)
                {
                    DrawRectOutline(element.rectTransform, color, 2);

                    // Draw batch number
                    Vector3[] corners = new Vector3[4];
                    element.rectTransform.GetWorldCorners(corners);
                    Vector3 center = (corners[0] + corners[2]) * 0.5f;
                    Handles.color = Color.magenta;
                    Handles.Label(center, i.ToString(), EditorStyles.boldLabel);
                    Handles.color = Color.white;
                }
            }
        }

        private void SetCanvas(Canvas canvas)
        {
            _canvas = canvas;
        }

        private void DrawRectOutline(RectTransform rectTransform, Color color, float thickness = 1)
        {
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            Handles.color = color;
            for (int i = 0; i < 4; i++)
            {
                Handles.DrawLine(corners[i], corners[(i + 1) % 4], thickness);
            }
        }

        private void InitStyle()
        {
            _batchStyle = new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
                richText = true,
            };

            _drawCallCountStyle = new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    textColor = new Color(1, 0.5f, 0)
                },
                fontSize = 20,
                fontStyle = FontStyle.Bold,
            };

            _tipsStyle = new GUIStyle()
            {
                normal = new GUIStyleState()
                {
                    textColor = Color.white,
                },
                fontSize = 40,
                fontStyle = FontStyle.Bold,
            };
        }
    }
}