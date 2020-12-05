using ConformalDecals.MaterialProperties;
using ConformalDecals.Util;
using UniLinq;
using UnityEngine;

namespace ConformalDecals {
    public class ModuleConformalFlag : ModuleConformalDecal {
        private const string DefaultFlag = "Squad/Flags/default";

        [KSPField(isPersistant = true)] public string flagUrl = DefaultFlag;

        [KSPField(isPersistant = true)] public bool useCustomFlag;

        private MaterialTextureProperty _flagTextureProperty;

        public string MissionFlagUrl {
            get {
                if (HighLogic.LoadedSceneIsEditor) {
                    return string.IsNullOrEmpty(EditorLogic.FlagURL) ? HighLogic.CurrentGame.flagURL : EditorLogic.FlagURL;
                }

                if (HighLogic.LoadedSceneIsFlight) {
                    return string.IsNullOrEmpty(part.flagURL) ? HighLogic.CurrentGame.flagURL : part.flagURL;
                }

                return DefaultFlag;
            }
        }

        public override void OnStart(StartState state) {
            base.OnStart(state);

            if (HighLogic.LoadedSceneIsGame) {
                GameEvents.onMissionFlagSelect.Add(OnEditorFlagSelected);
            }

            if (HighLogic.LoadedSceneIsEditor) {
                Events[nameof(ResetFlag)].guiActiveEditor = useCustomFlag;
            }
        }

        public override void OnDestroy() {
            GameEvents.onMissionFlagSelect.Remove(OnEditorFlagSelected);
            base.OnDestroy();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_ConformalDecals_gui-select-flag")]
        public void SelectFlag() {
            var flagBrowser = (Instantiate((Object) (new FlagBrowserGUIButton(null, null, null, null)).FlagBrowserPrefab) as GameObject).GetComponent<FlagBrowser>();
            flagBrowser.OnFlagSelected = OnCustomFlagSelected;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_ConformalDecals_gui-reset-flag")]
        public void ResetFlag() {
            Events[nameof(ResetFlag)].guiActiveEditor = false;
            flagUrl = MissionFlagUrl;
            useCustomFlag = false;
            UpdateAll();
            foreach (var decal in part.symmetryCounterparts.Select(o => o.GetComponent<ModuleConformalFlag>())) {
                decal.Events[nameof(ResetFlag)].guiActiveEditor = false;
                decal.flagUrl = flagUrl;
                decal.useCustomFlag = false;
                decal.UpdateAll();
            }
        }

        private void OnCustomFlagSelected(FlagBrowser.FlagEntry newFlagEntry) {
            Events[nameof(ResetFlag)].guiActiveEditor = true;
            flagUrl = newFlagEntry.textureInfo.name;
            useCustomFlag = true;
            UpdateAll();

            foreach (var decal in part.symmetryCounterparts.Select(o => o.GetComponent<ModuleConformalFlag>())) {
                decal.Events[nameof(ResetFlag)].guiActiveEditor = true;
                decal.flagUrl = flagUrl;
                decal.useCustomFlag = true;
                decal.UpdateAll();
            }
        }

        private void OnEditorFlagSelected(string newFlagUrl) {
            if (!useCustomFlag) UpdateAll();
        }

        protected override void UpdateTextures() {
            _flagTextureProperty ??= materialProperties.AddOrGetTextureProperty("_Decal");

            base.UpdateTextures();
            if (useCustomFlag) {
                _flagTextureProperty.TextureUrl = flagUrl;
            }
            else {
                _flagTextureProperty.TextureUrl = MissionFlagUrl;
            }
        }
    }
}