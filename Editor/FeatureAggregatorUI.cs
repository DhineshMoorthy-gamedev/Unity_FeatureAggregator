using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using FeatureAggregator.Utils;
using System.Linq;

namespace FeatureAggregator
{
    [System.Serializable]
    public class FeatureAggregatorUI
    {
        private List<FeatureDefinition> features;
        private FeatureDefinition selectedFeature;
        private Vector2 leftScrollPos;
        private Vector2 rightScrollPos;
        private string searchString = "";
        private FeatureTag? filterTag = null;
        private string newFeatureName = "";
        private bool isCreatingNew = false;
        
        // Needed to trigger repaints or other window events if necessary
        private EditorWindow hostWindow;

        public void Initialize(EditorWindow window)
        {
            hostWindow = window;
            RefreshFeatureList();
        }

        public void OnFocus()
        {
            RefreshFeatureList();
        }

        public void RefreshFeatureList()
        {
            features = FeatureManager.GetAllFeatures();
        }

        public void Draw()
        {
            // Toolbar
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Features", EditorStyles.boldLabel, GUILayout.Width(70));
            searchString = GUILayout.TextField(searchString, EditorStyles.toolbarSearchField, GUILayout.Width(200)); 
            if (GUILayout.Button("X", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                searchString = "";
                GUI.FocusControl(null);
            }

            GUILayout.Space(10);
            
            string tagLabel = filterTag.HasValue ? filterTag.Value.ToString() : "All Tags";
            if (GUILayout.Button(tagLabel, EditorStyles.toolbarDropDown, GUILayout.Width(100)))
            {
                GenericMenu menu = new GenericMenu();
                menu.AddItem(new GUIContent("All Tags"), !filterTag.HasValue, () => filterTag = null);
                foreach (FeatureTag tag in System.Enum.GetValues(typeof(FeatureTag)))
                {
                    menu.AddItem(new GUIContent(tag.ToString()), filterTag == tag, () => filterTag = tag);
                }
                menu.ShowAsContext();
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("+ New Feature", EditorStyles.toolbarButton))
            {
                isCreatingNew = true;
                selectedFeature = null;
                newFeatureName = "";
            }
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton))
            {
                RefreshFeatureList();
            }
            if (GUILayout.Button("Graph View", EditorStyles.toolbarButton))
            {
                DependencyGraphWindow.Open();
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();

            // Left Panel - Feature List
            DrawFeatureList();

            // Divider
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true));

            // Right Panel - Selected Feature Details
            DrawFeatureDetails();

            GUILayout.EndHorizontal();
        }

        private void DrawFeatureList()
        {
            GUILayout.BeginVertical(GUILayout.Width(250));
            leftScrollPos = GUILayout.BeginScrollView(leftScrollPos);

            if (isCreatingNew)
            {
                DrawCreateNewInterface();
            }

            if (features == null) return;

            foreach (var feature in features)
            {
                if (feature == null) continue; // Safety check
                
                if (!string.IsNullOrEmpty(searchString) && !feature.featureName.ToLower().Contains(searchString.ToLower()))
                    continue;

                if (filterTag.HasValue && !feature.tags.Contains(filterTag.Value))
                    continue;

                GUIStyle style = new GUIStyle(EditorStyles.label); // Default doesn't have background, use button or custom
                if (GUI.skin != null) style = new GUIStyle(GUI.skin.button);
                
                style.alignment = TextAnchor.MiddleLeft;
                if (selectedFeature == feature)
                {
                    style.normal.textColor = Color.cyan;
                }

                GUILayout.BeginHorizontal(style, GUILayout.Height(30));
                
                if (GUILayout.Button(feature.featureName, EditorStyles.label, GUILayout.ExpandWidth(true), GUILayout.Height(30)))
                {
                    selectedFeature = feature;
                    isCreatingNew = false;
                    GUI.FocusControl(null);
                }

                // Draw tiny tag indicators
                foreach (var tag in feature.tags)
                {
                    DrawTagIndicator(tag);
                }

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawCreateNewInterface()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Create New Feature", EditorStyles.boldLabel);
            newFeatureName = EditorGUILayout.TextField("Name", newFeatureName);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cancel"))
            {
                isCreatingNew = false;
            }
            if (GUILayout.Button("Create", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(newFeatureName))
                {
                    selectedFeature = FeatureManager.CreateFeature(newFeatureName);
                    RefreshFeatureList();
                    isCreatingNew = false;
                    newFeatureName = "";
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }

        private void DrawFeatureDetails()
        {
            GUILayout.BeginVertical();
            rightScrollPos = GUILayout.BeginScrollView(rightScrollPos);

            if (selectedFeature != null)
            {
                // Create SerializedObject for Undo/Redo support and property drawing
                SerializedObject so = null;
                try 
                {
                    so = new SerializedObject(selectedFeature);
                } 
                catch 
                {
                    // Handle case where object might have been deleted but reference lingers
                    selectedFeature = null;
                    GUILayout.EndScrollView();
                    GUILayout.EndVertical();
                    return;
                }

                so.Update();

                // Header
                GUILayout.Space(10);
                EditorGUILayout.LabelField(selectedFeature.featureName, EditorStyles.boldLabel);
                EditorGUILayout.ObjectField(selectedFeature, typeof(FeatureDefinition), false); // Debug link
                GUILayout.Space(5);

                // Description
                SerializedProperty descProp = so.FindProperty("description");
                EditorGUILayout.PropertyField(descProp);
                
                GUILayout.Space(10);

                // Tags
                DrawTagEditor(so);

                GUILayout.Space(10);

                // Actions
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Open All Scripts", GUILayout.Height(30)))
                {
                    ScriptOpener.OpenScriptsInIDE(selectedFeature.relatedScripts);
                }
                GUILayout.EndHorizontal();

                GUILayout.Space(10);

                // Scripts List
                EditorGUILayout.LabelField($"Scripts ({selectedFeature.relatedScripts.Count})", EditorStyles.boldLabel);
                
                SerializedProperty scriptsProp = so.FindProperty("relatedScripts");
                
                // Custom list drawing to allow "Open" buttons per script
                for (int i = 0; i < scriptsProp.arraySize; i++)
                {
                    SerializedProperty scriptRef = scriptsProp.GetArrayElementAtIndex(i);
                    MonoScript script = (MonoScript)scriptRef.objectReferenceValue;

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(scriptRef, GUIContent.none);
                    if (script != null && GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        AssetDatabase.OpenAsset(script);
                    }
                    if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        scriptsProp.DeleteArrayElementAtIndex(i);
                    }
                    GUILayout.EndHorizontal();
                }

                // Drag and drop area for scripts
                DropAreaGUI("Drop Scripts Here", (obj) => 
                {
                    if (obj is MonoScript script)
                    {
                         FeatureManager.AddScriptToFeature(selectedFeature, script);
                    }
                });
                
                GUILayout.Space(10);

                // Related Assets (Scenes/Prefabs)
                EditorGUILayout.LabelField($"Related Assets ({selectedFeature.relatedAssets.Count})", EditorStyles.boldLabel);
                 SerializedProperty assetsProp = so.FindProperty("relatedAssets");
                 for (int i = 0; i < assetsProp.arraySize; i++)
                {
                    SerializedProperty assetRef = assetsProp.GetArrayElementAtIndex(i);
                    Object asset = assetRef.objectReferenceValue;

                    GUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(assetRef, GUIContent.none);
                    if (asset != null && GUILayout.Button("Open", GUILayout.Width(50)))
                    {
                        AssetDatabase.OpenAsset(asset);
                    }
                     if (GUILayout.Button("X", GUILayout.Width(25)))
                    {
                        assetsProp.DeleteArrayElementAtIndex(i);
                    }
                    GUILayout.EndHorizontal();
                }
                 
                  // Drag and drop area for Assets
                DropAreaGUI("Drop Other Assets Here", (obj) => 
                {
                    if (obj != null && !(obj is MonoScript))
                    {
                         FeatureManager.AddAssetToFeature(selectedFeature, obj);
                    }
                });
                
                GUILayout.Space(10);

                // Dependencies
                DrawDependencyEditor(so);

                GUILayout.Space(10);

                if (GUILayout.Button("Delete Feature", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("Delete Feature", 
                        $"Are you sure you want to delete {selectedFeature.featureName}?", "Yes", "No"))
                    {
                        FeatureManager.DeleteFeature(selectedFeature);
                        selectedFeature = null;
                        RefreshFeatureList();
                    }
                }

                so.ApplyModifiedProperties();
            }
            else
            {
                GUILayout.Label("Select a feature to view details", EditorStyles.centeredGreyMiniLabel);
            }

            GUILayout.EndScrollView();
            GUILayout.EndVertical();
        }

        private void DrawTagIndicator(FeatureTag tag)
        {
            Color color = GetTagColor(tag);
            GUI.color = color;
            GUILayout.Box("", GUILayout.Width(10), GUILayout.Height(10));
            GUI.color = Color.white;
        }

        private void DrawTagEditor(SerializedObject so)
        {
            EditorGUILayout.LabelField("Tags", EditorStyles.boldLabel);
            SerializedProperty tagsProp = so.FindProperty("tags");

            GUILayout.BeginHorizontal();
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                var tagProp = tagsProp.GetArrayElementAtIndex(i);
                FeatureTag tag = (FeatureTag)tagProp.enumValueIndex;

                GUIStyle tagStyle = new GUIStyle(EditorStyles.miniButton);
                tagStyle.fixedHeight = 20;
                
                if (GUILayout.Button($"{tag} x", tagStyle))
                {
                    tagsProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            if (GUILayout.Button("+", GUILayout.Width(25)))
            {
                GenericMenu menu = new GenericMenu();
                foreach (FeatureTag tag in System.Enum.GetValues(typeof(FeatureTag)))
                {
                    bool alreadyHas = false;
                    for (int j = 0; j < tagsProp.arraySize; j++)
                    {
                        if (tagsProp.GetArrayElementAtIndex(j).enumValueIndex == (int)tag)
                        {
                            alreadyHas = true;
                            break;
                        }
                    }

                    if (!alreadyHas)
                    {
                        menu.AddItem(new GUIContent(tag.ToString()), false, () => 
                        {
                            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).enumValueIndex = (int)tag;
                            so.ApplyModifiedProperties();
                        });
                    }
                }
                menu.ShowAsContext();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawDependencyEditor(SerializedObject so)
        {
            EditorGUILayout.LabelField("Dependencies", EditorStyles.boldLabel);
            SerializedProperty depsProp = so.FindProperty("dependencies");

            for (int i = 0; i < depsProp.arraySize; i++)
            {
                var depProp = depsProp.GetArrayElementAtIndex(i);
                FeatureDefinition dep = (FeatureDefinition)depProp.objectReferenceValue;

                GUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(depProp, GUIContent.none);
                if (GUILayout.Button("X", GUILayout.Width(25)))
                {
                    depsProp.DeleteArrayElementAtIndex(i);
                }
                GUILayout.EndHorizontal();
            }

            // Drop area for dependencies
            DropAreaGUI("Drop Feature Definitions here to add Dependencies", (obj) => 
            {
                if (obj is FeatureDefinition dep && dep != selectedFeature)
                {
                    bool alreadyExists = false;
                    for (int i = 0; i < depsProp.arraySize; i++)
                    {
                        if (depsProp.GetArrayElementAtIndex(i).objectReferenceValue == dep)
                        {
                            alreadyExists = true;
                            break;
                        }
                    }

                    if (!alreadyExists)
                    {
                        depsProp.InsertArrayElementAtIndex(depsProp.arraySize);
                        depsProp.GetArrayElementAtIndex(depsProp.arraySize - 1).objectReferenceValue = dep;
                        so.ApplyModifiedProperties();
                    }
                }
            });
        }

        private Color GetTagColor(FeatureTag tag)
        {
            switch (tag)
            {
                case FeatureTag.Core: return new Color(0.2f, 0.8f, 0.2f); // Green
                case FeatureTag.Experimental: return new Color(0.8f, 0.4f, 0.1f); // Orange
                case FeatureTag.Legacy: return new Color(0.7f, 0.2f, 0.2f); // Red
                case FeatureTag.UI: return new Color(0.2f, 0.5f, 0.9f); // Blue
                case FeatureTag.Gameplay: return new Color(0.8f, 0.8f, 0.2f); // Yellow
                case FeatureTag.Optimization: return new Color(0.5f, 0.2f, 0.8f); // Purple
                case FeatureTag.Tooling: return new Color(0.2f, 0.8f, 0.8f); // Cyan
                default: return Color.gray;
            }
        }

        private void DropAreaGUI(string label, System.Action<Object> onDrop)
        {
            Event evt = Event.current;
            Rect dropArea = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, label, EditorStyles.helpBox);

            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!dropArea.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();

                        foreach (Object dragged_object in DragAndDrop.objectReferences)
                        {
                            onDrop?.Invoke(dragged_object);
                        }
                    }
                    Event.current.Use();
                    break;
            }
        }
    }
}
