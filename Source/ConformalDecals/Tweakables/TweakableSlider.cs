using System;
using ConformalDecals.Util;
using UnityEngine;

namespace ConformalDecals.Tweakables {
    [AttributeUsage(AttributeTargets.Field)]
    public class TweakableSlider : TweakableData {
        // The default value for the slider
        public float  defaultValue;
        public string defaultValueKey;

        // The range of the slider as a vector of <min, max>
        public float  min = 0;
        public float  max = 1;
        public string rangeKey;

        // The step size of the slider
        public float  step;
        public string stepKey;

        public TweakableSlider(string name) : base(name) {
            defaultValueKey = name + "Default";
            rangeKey = name + "Range";
            stepKey = name + "Step";
        }

        public override void Load(ConfigNode node) {
            base.Load(node);

            var range = new Vector2(min, max);
            ParseUtil.ParseVector2Indirect(ref range, node, rangeKey);
            min = Mathf.Max(Mathf.Epsilon, range.x);
            max = Mathf.Max(min, range.y);

            ParseUtil.ParseFloatIndirect(ref step, node, stepKey);

            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) {
                ParseUtil.ParseFloatIndirect(ref defaultValue, node, defaultValueKey);
            }
        }

        public override void Apply(BaseField baseField, PartModule module) {
            base.Apply(baseField, module);
            var uiControlEditor = (UI_FloatRange) baseField.uiControlEditor;

            uiControlEditor.minValue = min;
            uiControlEditor.maxValue = max;
            uiControlEditor.stepIncrement = step;

            // Set the default value on first load
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) {
                baseField.FieldInfo.SetValue(module, defaultValue);
            }
        }
    }
}