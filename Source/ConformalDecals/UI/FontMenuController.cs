using System;
using System.Collections.Generic;
using ConformalDecals.Text;
using UniLinq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ConformalDecals.UI {
    public class FontMenuController : MonoBehaviour {
        [Serializable]
        public class FontUpdateEvent : UnityEvent<DecalFont> { }
        
        [SerializeField] private FontUpdateEvent _onFontChanged = new FontUpdateEvent();

        [SerializeField] private GameObject _menuItem;
        [SerializeField] private GameObject _menuList;

        private DecalFont _currentFont;

        public static FontMenuController Create(IEnumerable<DecalFont> fonts, DecalFont currentFont, UnityAction<DecalFont> fontUpdateCallback) {
            var menu = Instantiate(UILoader.FontMenuPrefab, MainCanvasUtil.MainCanvas.transform, true);
            menu.AddComponent<DragPanel>();
            MenuNavigation.SpawnMenuNavigation(menu, Navigation.Mode.Automatic, true);

            var controller = menu.GetComponent<FontMenuController>();
            controller._currentFont = currentFont;
            controller._onFontChanged.AddListener(fontUpdateCallback);

            controller.Populate(fonts);
            return controller;
        }

        public void OnClose() {
            Destroy(gameObject);
        }

        public void OnFontSelected(DecalFont font) {
            _currentFont = font ?? throw new ArgumentNullException(nameof(font));
            _onFontChanged.Invoke(_currentFont);
        }

        public void Populate(IEnumerable<DecalFont> fonts) {
            if (fonts == null) throw new ArgumentNullException(nameof(fonts));

            Toggle active = null;

            foreach (var font in fonts.OrderBy(x => x.title)) {
                Debug.Log(font.title);
                var listItem = GameObject.Instantiate(_menuItem, _menuList.transform);
                listItem.name = font.title;
                listItem.SetActive(true);

                var fontItem = listItem.AddComponent<FontMenuItem>();
                fontItem.Font = font;
                fontItem.fontSelectionCallback = OnFontSelected;

                if (font == _currentFont) active = fontItem.toggle;
            }

            if (active != null) active.isOn = true;
        }
    }
}