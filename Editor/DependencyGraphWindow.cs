using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.Experimental.GraphView;
using System.Collections.Generic;
using System.Linq;

namespace FeatureAggregator
{
    public class DependencyGraphWindow : EditorWindow
    {
        private DependencyGraphView graphView;

        [MenuItem("Tools/Feature Aggregator/Dependency Graph")]
        public static void Open()
        {
            var window = GetWindow<DependencyGraphWindow>();
            window.titleContent = new GUIContent("Feature Dependency Graph");
            window.Show();
        }

        private void OnEnable()
        {
            ConstructGraphView();
            GenerateGraph();
        }

        private void OnDisable()
        {
            rootVisualElement.Remove(graphView);
        }

        private void ConstructGraphView()
        {
            graphView = new DependencyGraphView
            {
                name = "Feature Dependency Graph"
            };

            graphView.StretchToParentSize();
            rootVisualElement.Add(graphView);

            var toolbar = new UnityEditor.UIElements.Toolbar();
            toolbar.Add(new Button(() => GenerateGraph()) { text = "Refresh Graph" });
            rootVisualElement.Add(toolbar);
        }

        private void GenerateGraph()
        {
            graphView.ClearGraph();
            
            var features = FeatureManager.GetAllFeatures();
            var nodeDict = new Dictionary<FeatureDefinition, FeatureNode>();

            // Create Nodes
            int i = 0;
            int columns = Mathf.CeilToInt(Mathf.Sqrt(features.Count));
            float spacing = 250f;

            foreach (var feature in features)
            {
                var node = graphView.CreateNode(feature);
                nodeDict[feature] = node;

                // Simple grid layout
                float x = (i % columns) * spacing;
                float y = (i / columns) * spacing;
                node.SetPosition(new Rect(x + 50, y + 50, 200, 150));
                i++;
            }

            // Create Edges
            foreach (var feature in features)
            {
                if (feature.dependencies == null) continue;

                foreach (var dep in feature.dependencies)
                {
                    if (dep != null && nodeDict.ContainsKey(dep))
                    {
                        graphView.ConnectNodes(nodeDict[feature], nodeDict[dep]);
                    }
                }
            }
        }
    }

    public class DependencyGraphView : GraphView
    {
        public DependencyGraphView()
        {
            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            var grid = new GridBackground();
            Insert(0, grid);
            grid.StretchToParentSize();
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.ToList(); // Simple for now
        }

        public FeatureNode CreateNode(FeatureDefinition feature)
        {
            var node = new FeatureNode(feature);
            AddElement(node);
            
            // Random-ish position if not saved (could improve this with a layout engine)
            node.SetPosition(new Rect(Random.Range(50, 500), Random.Range(50, 500), 200, 150));
            
            return node;
        }

        public void ConnectNodes(FeatureNode from, FeatureNode to)
        {
            var edge = from.OutputPort.ConnectTo(to.InputPort);
            AddElement(edge);
        }

        public void ClearGraph()
        {
            graphElements.ForEach(RemoveElement);
        }
    }

    public class FeatureNode : Node
    {
        public FeatureDefinition Feature { get; private set; }
        public Port InputPort { get; private set; }
        public Port OutputPort { get; private set; }

        public FeatureNode(FeatureDefinition feature)
        {
            Feature = feature;
            title = feature.featureName;

            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(bool));
            InputPort.portName = "Used By";
            inputContainer.Add(InputPort);

            OutputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(bool));
            OutputPort.portName = "Depends On";
            outputContainer.Add(OutputPort);

            VisualElement tagsContainer = new VisualElement();
            tagsContainer.style.flexDirection = FlexDirection.Row;
            tagsContainer.style.flexWrap = Wrap.Wrap;
            tagsContainer.style.paddingLeft = 5;
            tagsContainer.style.paddingRight = 5;
            tagsContainer.style.paddingTop = 2;
            tagsContainer.style.paddingBottom = 2;

            foreach (var tag in feature.tags)
            {
                var tagLabel = new Label(tag.ToString());
                tagLabel.style.backgroundColor = GetTagColor(tag);
                tagLabel.style.color = Color.white;
                tagLabel.style.fontSize = 10;
                tagLabel.style.paddingLeft = 4;
                tagLabel.style.paddingRight = 4;
                tagLabel.style.marginRight = 2;
                tagLabel.style.marginBottom = 2;
                tagLabel.style.borderBottomLeftRadius = 4;
                tagLabel.style.borderBottomRightRadius = 4;
                tagLabel.style.borderTopLeftRadius = 4;
                tagLabel.style.borderTopRightRadius = 4;
                tagsContainer.Add(tagLabel);
            }
            
            mainContainer.Add(tagsContainer);

            RefreshExpandedState();
            RefreshPorts();
        }

        private Color GetTagColor(FeatureTag tag)
        {
            switch (tag)
            {
                case FeatureTag.Core: return new Color(0.2f, 0.4f, 0.2f); // Darker Green
                case FeatureTag.Experimental: return new Color(0.6f, 0.3f, 0.1f);
                case FeatureTag.Legacy: return new Color(0.5f, 0.2f, 0.2f);
                case FeatureTag.UI: return new Color(0.2f, 0.3f, 0.6f);
                case FeatureTag.Gameplay: return new Color(0.6f, 0.6f, 0.2f);
                case FeatureTag.Optimization: return new Color(0.4f, 0.2f, 0.6f);
                case FeatureTag.Tooling: return new Color(0.2f, 0.6f, 0.6f);
                default: return Color.gray;
            }
        }
    }
}
