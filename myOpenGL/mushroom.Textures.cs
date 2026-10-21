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

        #region טעינת טקסטורות (Texture Loading)

        /// <summary>
        /// איתור נתיב קובץ תמונה גמיש התואם למיקום ההרצה ולתיקיית הפרויקט
        /// </summary>
        private static string ResolvePath(string relativePath)
        {
            if (File.Exists(relativePath)) return relativePath;

            string fileName = Path.GetFileName(relativePath);

            string[] candidates = new string[]
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relativePath),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "pics", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\pics", fileName),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\..\pics", fileName),
                Path.Combine("pics", fileName),
                relativePath
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return Path.GetFullPath(candidate);
                }
            }

            return relativePath;
        }

        /// <summary>
        /// טוען קובץ תמונה לתוך מזהה טקסטורה של OpenGL (32bpp עם אלפא)
        /// </summary>
        private static uint LoadTextureFromFile(string relativePath)
        {
            string fullPath = ResolvePath(relativePath);
            if (!File.Exists(fullPath))
            {
                return 0;
            }

            try
            {
                using (Bitmap original = new Bitmap(fullPath))
                {
                    // המרה ל-Format32bppArgb להבטחת ערוץ אלפא תקין
                    using (Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(original, 0, 0, original.Width, original.Height);
                        }

                        // היפוך אנכי עבור מערכת הצירים של OpenGL
                        bitmap.RotateFlip(RotateFlipType.RotateNoneFlipY);

                        uint[] texIds = new uint[1];
                        GL.glGenTextures(1, texIds);
                        uint texId = texIds[0];

                        GL.glBindTexture(GL.GL_TEXTURE_2D, texId);

                        BitmapData data = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadOnly,
                            PixelFormat.Format32bppArgb
                        );

                        // שימוש ב-GL_RGBA8 ו-GL_BGRA_EXT
                        GL.glTexImage2D(
                            GL.GL_TEXTURE_2D,
                            0,
                            (int)GL.GL_RGBA8,
                            bitmap.Width,
                            bitmap.Height,
                            0,
                            GL.GL_BGRA_EXT,
                            GL.GL_UNSIGNED_byte,
                            data.Scan0
                        );

                        bitmap.UnlockBits(data);

                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, (int)GL.GL_LINEAR);
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, (int)GL.GL_LINEAR);
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, (int)GL.GL_CLAMP);
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, (int)GL.GL_CLAMP);

                        return texId;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// מוודא שטקסטורות הפטריה נטענו בהצלחה
        /// </summary>
        public static void EnsureTexturesLoaded()
        {
            if (_texturesLoaded) return;

            _mainTextureId = LoadTextureFromFile(@"pics\mushroom.png");
            _faceTextureId = LoadTextureFromFile(@"pics\mushroom_face.png");
            _capTextureId = LoadTextureFromFile(@"pics\mushroom_cap.png");

            // אם קובץ ספציפי לא נמצא, נשתמש בטקסטורה הראשית כגיבוי
            if (_faceTextureId == 0) _faceTextureId = _mainTextureId;
            if (_capTextureId == 0) _capTextureId = _mainTextureId;

            _texturesLoaded = (_mainTextureId != 0 || _faceTextureId != 0 || _capTextureId != 0);
        }

        #endregion
    }
}
