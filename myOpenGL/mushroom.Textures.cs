using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על טעינה, פענוח והגדרת טקסטורות ב-OpenGL
    /// </summary>
    public partial class mushroom
    {
        #region משתני טקסטורה סטטיים ומקומיים

        private static uint _capTextureId = 0;
        private static uint _faceTextureId = 0;
        private static uint _mainTextureId = 0;
        private static bool _texturesLoaded = false;

        #endregion

        #region טעינת טקסטורות (Texture Loading via TextureManager)

        /// <summary>
        /// מוודא שטקסטורות הפטריה נטענו בהצלחה באמצעות TextureManager
        /// </summary>
        public static void EnsureTexturesLoaded()
        {
            if (_texturesLoaded) return;

            _mainTextureId = TextureManager.LoadTexture(@"pics\mushroom.png");
            _faceTextureId = TextureManager.LoadTexture(@"pics\mushroom_face.png");
            _capTextureId = TextureManager.LoadTexture(@"pics\mushroom_cap.png");

            // אם קובץ ספציפי לא נמצא, נשתמש בטקסטורה הראשית כגיבוי
            if (_faceTextureId == 0) _faceTextureId = _mainTextureId;
            if (_capTextureId == 0) _capTextureId = _mainTextureId;

            _texturesLoaded = (_mainTextureId != 0 || _faceTextureId != 0 || _capTextureId != 0);
        }

        #endregion
    }
}
