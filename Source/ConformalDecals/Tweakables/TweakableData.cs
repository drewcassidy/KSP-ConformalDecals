using System;
using ConformalDecals.Util;
using UnityEngine;

namespace ConformalDecals.Tweakables {
    [AttributeUsage(AttributeTargets.Field)]
    public abstract class TweakableData : System.Attribute, ISerializationCallbackReceiver {
        public string name;

        public bool   adjustable = true;
        public string adjustableKey;

        // public string fieldChangedCallback;
        public bool useSymmetry = true;

        protected TweakableData(string name) {
            this.name = name;
            adjustableKey = name + "Adjustable";
        }

        public virtual void Load(ConfigNode node) {
            ParseUtil.ParseBoolIndirect(ref adjustable, node, adjustableKey);
        }

        public virtual void Apply(BaseField baseField, PartModule module) {
            baseField.guiActiveEditor = adjustable;
        }

        public void OnBeforeSerialize() { }

        public void OnAfterDeserialize() { }
    }
}