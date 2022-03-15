using ConformalDecals.MaterialProperties;
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

                // If we are not in game, use the default flag (for icon rendering)
                return DefaultFlag;
            }
        }

        public override void OnLoad(ConfigNode node) {
            base.OnLoad(node);

            // Since OnLoad is called for all modules, we only need to update this module
            // Updating symmetry counterparts would be redundent
            UpdateFlag();
        }

        public override void OnStart(StartState state) {
            base.OnStart(state);

            if (HighLogic.LoadedSceneIsEditor) {
                // Register flag change event
                GameEvents.onMissionFlagSelect.Add(OnEditorFlagSelected);
                
                // Register reset button event
                Events[nameof(ResetFlag)].guiActiveEditor = useCustomFlag;
            }

            // Since OnStart is called for all modules, we only need to update this module
            // Updating symmetry counterparts would be redundent
            UpdateFlag();
        }

        public override void OnDestroy() {
            if (HighLogic.LoadedSceneIsEditor) {
                // Unregister flag change event
                GameEvents.onMissionFlagSelect.Remove(OnEditorFlagSelected);
            }
            
            base.OnDestroy();
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_ConformalDecals_gui-select-flag")]
        public void SelectFlag() {
            // Button for selecting a flag
            // This is a bit of a hack to bring up the stock flag selection menu
            // When its done, it calls OnCustomFlagSelected()
            
            // ReSharper disable once PossibleNullReferenceException
            var flagBrowser = (Instantiate((Object) (new FlagBrowserGUIButton(null, null, null, null)).FlagBrowserPrefab) as GameObject).GetComponent<FlagBrowser>();
            flagBrowser.OnFlagSelected = OnCustomFlagSelected;
        }

        [KSPEvent(guiActive = false, guiActiveEditor = true, guiName = "#LOC_ConformalDecals_gui-reset-flag")]
        public void ResetFlag() {
            
            // we are no longer using a custom flag, so instead use the mission or agency flag
            useCustomFlag = false;
            flagUrl = "Mission";
            UpdateFlag(true);
            
            // disable the reset button, since it no longer makes sense
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
            // Callback for when a flag is selected in the menu spawned by SelectFlag()
            
            // we are now using a custom flag with the URL of the new flag entry
            useCustomFlag = true;
            flagUrl = newFlagEntry.textureInfo.name;
            UpdateFlag(true);
            
            // make sure the reset button is now available
            Events[nameof(ResetFlag)].guiActiveEditor = true;
            flagUrl = newFlagEntry.textureInfo.name;
            useCustomFlag = true;
            UpdateAll();

        private void OnEditorFlagSelected(string newFlagUrl) {
            if (!useCustomFlag) {
                flagUrl = newFlagUrl;
                // Since this callback is called for all modules, we only need to update this module
                // Updating symmetry counterparts would be redundent
                UpdateFlag();
            }
        }

        // Update the displayed flag texture for this decal or optionally any symmetry counterparts
        private void UpdateFlag(bool recursive = false) {
            // get the decal material property for the decal texture
            var textureProperty = materialProperties.AddOrGetTextureProperty("_Decal", true);
            
            if(useCustomFlag) {
                // set the texture to the custom flag
                textureProperty.TextureUrl = flagUrl;
            }
            else {
                // set the texture to the mission flag
                textureProperty.TextureUrl = MissionFlagUrl;
            }
            
            UpdateMaterials();
            UpdateScale();

            if (recursive) {
                // for each symmetry counterpart, copy this part's properties and update it in turn
                foreach (var counterpart in part.symmetryCounterparts) {
                    var decal = counterpart.GetComponent<ModuleConformalFlag>();
                    
                    decal.useCustomFlag = useCustomFlag;
                    decal.flagUrl = flagUrl;
                    decal.UpdateFlag();
                }
            }
        }
    }
}
