using System;
using System.Reflection;
using ConformalDecals.Util;

namespace ConformalDecals.Tweakables {
    [AttributeUsage(AttributeTargets.Field)]
    public class TweakableToggle : TweakableData {
        // The default value for the toggle 
        public bool   defaultValue;
        public string defaultValueKey;

        public TweakableToggle(string name) : base(name) {
            defaultValueKey = name + "Default";
        }

        public override void Load(ConfigNode node) {
            base.Load(node);

            // Set the default value on first load
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) {
                ParseUtil.ParseBoolIndirect(ref defaultValue, node, defaultValueKey);
            }
        }

        public override void Apply(BaseField baseField, PartModule module) {
            base.Apply(baseField, module);

            // Set the default value on first load
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight) {
                baseField.FieldInfo.SetValue(module, defaultValue);
            }
        }
    }
}