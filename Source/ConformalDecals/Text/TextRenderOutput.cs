using UnityEngine;

namespace ConformalDecals.Text {
    /// Texture render output, used for cacheing and is the datastructure returned to the ModuleConformalText class
    public class TextRenderOutput {
        /// Texture with the rendered text
        public Texture2D Texture { get; private set; }

        /// The rectangle that the rendered text takes up within the texture
        public Rect Window { get; private set; }

        /// The number of users for this render output. If 0, it can be discarded from the cache
        public int UserCount { get; set; }

        public TextRenderOutput(Texture2D texture, Rect window) {
            Texture = texture;
            Window = window;
        }
    }
}