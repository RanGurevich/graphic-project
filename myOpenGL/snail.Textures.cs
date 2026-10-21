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

        #region טעינת טקסטורות (Texture Loading)

        /// <summary>
        /// איתור נתיב קובץ תמונה גמיש
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
                    using (Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(original, 0, 0, original.Width, original.Height);
                        }

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
        /// מוודא שטקסטורות החילזון נטענו בהצלחה
        /// </summary>
        public static void EnsureTexturesLoaded()
        {
            if (_snailTexturesLoaded) return;

            _redSnailTextureId = LoadTextureFromFile(@"pics\snail_red.png");
            _blueSnailTextureId = LoadTextureFromFile(@"pics\snail_blue.png");

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
