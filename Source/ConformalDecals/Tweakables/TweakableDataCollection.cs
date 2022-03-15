using System.Collections;
using System.Collections.Generic;
using UniLinq;
using UnityEngine;

namespace ConformalDecals.Tweakables {
    public class TweakableDataCollection : IEnumerable<TweakableData>, ISerializationCallbackReceiver {
        public readonly Dictionary<string, TweakableData> tweakables = new Dictionary<string, TweakableData>();

        [SerializeField] private TweakableData[] _serializedTweakables;
        
        public IEnumerator<TweakableData> GetEnumerator() {
            return tweakables.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void OnBeforeSerialize() {
            _serializedTweakables = tweakables.Values.ToArray();
        }

        public void OnAfterDeserialize() {
            foreach (var tweakable in _serializedTweakables) {
                tweakables.Add(tweakable.name, tweakable);
            }
        }
    }
}