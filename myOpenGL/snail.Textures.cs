using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על טעינה, פענוח והגדרת טקסטורות של החלזונות (אדום וכחול)
    /// </summary>
    public partial class snail
    {
        #region משתני טקסטורה סטטיים ומקומיים

        private static uint _redSnailTextureId = 0;
        private static uint _blueSnailTextureId = 0;
        private static bool _snailTexturesLoaded = false;

        #endregion

        #region טעינת טקסטורות (Texture Loading via TextureManager)

        /// <summary>
        /// מוודא שטקסטורות החילזון נטענו בהצלחה באמצעות TextureManager
        /// </summary>
        public static void EnsureTexturesLoaded()
        {
            if (_snailTexturesLoaded) return;

            _redSnailTextureId = TextureManager.LoadTexture(@"pics\snail_red.png");
            _blueSnailTextureId = TextureManager.LoadTexture(@"pics\snail_blue.png");

            _snailTexturesLoaded = (_redSnailTextureId != 0 || _blueSnailTextureId != 0);
        }

        /// <summary>
        /// מחזיר את מזהה הטקסטורה המתאים לצבע החילזון הנוכחי
        /// </summary>
        private uint GetCurrentTextureId()
        {
            if (ColorType == SnailColor.Blue)
            {
                return _blueSnailTextureId != 0 ? _blueSnailTextureId : _redSnailTextureId;
            }
            return _redSnailTextureId != 0 ? _redSnailTextureId : _blueSnailTextureId;
        }

        #endregion
    }
}
