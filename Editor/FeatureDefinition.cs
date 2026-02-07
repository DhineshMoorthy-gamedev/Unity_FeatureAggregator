using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace FeatureAggregator
{
    public enum FeatureTag
    {
        Core,
        Experimental,
        Legacy,
        UI,
        Gameplay,
        Optimization,
        Tooling
    }

    [CreateAssetMenu(fileName = "NewFeature", menuName = "Feature Aggregator/Feature Definition")]
    public class FeatureDefinition : ScriptableObject
    {
        public string featureName;
        [TextArea(3, 10)]
        public string description;

        public List<FeatureTag> tags = new List<FeatureTag>();

        public List<MonoScript> relatedScripts = new List<MonoScript>();
        public List<Object> relatedAssets = new List<Object>(); // Scenes, Prefabs, etc.

        public System.DateTime lastModified;

        private void OnValidate()
        {
            lastModified = System.DateTime.Now;
        }
    }
}
