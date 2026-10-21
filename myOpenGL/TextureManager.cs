using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקה מרכזית לניהול, טעינה ושמירת טקסטורות (Textures) בזיכרון מטמון עבור OpenGL.
    /// מרכזת את כל הפעולות המשוכפלות:
    /// 1. איתור גמיש של נתיבי קבצי תמונה (ResolvePath) בתיקיית הבסיס, בתיקיית pics וכו'.
    /// 2. פענוח תמונות (PNG, JPG, BMP) והמרתן לפורמט 32bpp עם תמיכה מלאה בשקיפות ואלפא.
    /// 3. היפוך אנכי (Flip Y) כדי להתאים למערכת הצירים של OpenGL.
    /// 4. הקצאה, העלאה ל-GPU (glTexImage2D) והגדרת סינון טקסטורה (GL_LINEAR / GL_CLAMP).
    /// 5. שמירת מזהי הטקסטורה במטמון (Cache) למניעת טעינות כפולות ובזבוז זיכרון וידאו.
    /// </summary>
    public static class TextureManager
    {
        #region זיכרון מטמון (Texture Cache)

        private static readonly Dictionary<string, uint> _textureCache = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region איתור נתיבי קבצים (Path Resolution)

        /// <summary>
        /// מאתר נתיב קובץ תמונה גמיש התואם למיקום ההרצה (bin/Debug), לתיקיית הפרויקט ולתיקיית pics.
        /// </summary>
        /// <param name="relativePath">נתיב יחסי או שם קובץ</param>
        /// <returns>הנתיב המלא לקובץ שנמצא, או הנתיב המקורי אם לא נמצא</returns>
        public static string ResolvePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath)) return relativePath;
            if (File.Exists(relativePath)) return Path.GetFullPath(relativePath);

            string fileName = Path.GetFileName(relativePath);
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string[] candidates = new string[]
            {
                Path.Combine(baseDir, relativePath),
                Path.Combine(baseDir, "pics", fileName),
                Path.Combine(baseDir, @"..\..\pics", fileName),
                Path.Combine(baseDir, @"..\..\..\pics", fileName),
                Path.Combine("pics", fileName),
                Path.Combine(@"..\..\pics", fileName),
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

        #endregion

        #region טעינת טקסטורות ל-OpenGL (Texture Loading)

        /// <summary>
        /// טוען קובץ תמונה לתוך מזהה טקסטורה של OpenGL (32bpp עם אלפא).
        /// אם הטקסטורה כבר נטענה בעבר, היא מוחזרת ישירות מהמטמון (Cache).
        /// </summary>
        /// <param name="relativePath">נתיב לקובץ התמונה</param>
        /// <param name="useCache">האם להשתמש במטמון למניעת טעינות כפולות</param>
        /// <param name="clamp">האם להגדיר חסימת גבולות (GL_CLAMP) או חזרתיות (GL_REPEAT)</param>
        /// <returns>מזהה טקסטורה (Texture ID) תקין ב-OpenGL, או 0 אם הטעינה נכשלה</returns>
        public static uint LoadTexture(string relativePath, bool useCache = true, bool clamp = true)
        {
            if (string.IsNullOrEmpty(relativePath)) return 0;

            string fullPath = ResolvePath(relativePath);

            // בדיקה האם הטקסטורה כבר טעונה בזיכרון המטמון
            if (useCache && _textureCache.TryGetValue(fullPath, out uint cachedId) && cachedId != 0)
            {
                return cachedId;
            }

            if (!File.Exists(fullPath))
            {
                return 0;
            }

            try
            {
                using (Bitmap original = new Bitmap(fullPath))
                {
                    // המרה ל-Format32bppArgb להבטחת ערוץ אלפא תקין ופריסה אחידה בזיכרון
                    using (Bitmap bitmap = new Bitmap(original.Width, original.Height, PixelFormat.Format32bppArgb))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.DrawImage(original, 0, 0, original.Width, original.Height);
                        }

                        // היפוך אנכי עבור מערכת הצירים של OpenGL (ציר Y ב-Windows פונה מטה, ב-OpenGL מעלה)
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

                        // שימוש ב-GL_RGBA8 וב-GL_BGRA_EXT
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

                        // הגדרת סינון ופריסה
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, (int)GL.GL_LINEAR);
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, (int)GL.GL_LINEAR);

                        int wrapMode = clamp ? (int)GL.GL_CLAMP : (int)GL.GL_REPEAT;
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_S, wrapMode);
                        GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_WRAP_T, wrapMode);

                        if (useCache && texId != 0)
                        {
                            _textureCache[fullPath] = texId;
                        }

                        return texId;
                    }
                }
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        #region עזרי ציור והפעלה (Binding & Lifetime)

        /// <summary>
        /// מפעיל מיפוי טקסטורות וקושר את הטקסטורה לציור ב-OpenGL
        /// </summary>
        /// <param name="textureId">מזהה הטקסטורה לקשירה</param>
        public static void Bind(uint textureId)
        {
            if (textureId != 0)
            {
                GL.glEnable(GL.GL_TEXTURE_2D);
                GL.glBindTexture(GL.GL_TEXTURE_2D, textureId);
            }
        }

        /// <summary>
        /// מנתק את הטקסטורה ומכבה מיפוי טקסטורות
        /// </summary>
        public static void Unbind()
        {
            GL.glBindTexture(GL.GL_TEXTURE_2D, 0);
            GL.glDisable(GL.GL_TEXTURE_2D);
        }

        /// <summary>
        /// משחרר טקסטורה מזיכרון ה-GPU ומהמטמון
        /// </summary>
        public static void DeleteTexture(uint textureId)
        {
            if (textureId != 0)
            {
                GL.glDeleteTextures(1, new uint[] { textureId });

                string foundKey = null;
                foreach (var kvp in _textureCache)
                {
                    if (kvp.Value == textureId)
                    {
                        foundKey = kvp.Key;
                        break;
                    }
                }

                if (foundKey != null)
                {
                    _textureCache.Remove(foundKey);
                }
            }
        }

        /// <summary>
        /// מנקה ומשחרר את כל הטקסטורות השמורות במטמון
        /// </summary>
        public static void ClearCache()
        {
            foreach (var texId in _textureCache.Values)
            {
                if (texId != 0)
                {
                    GL.glDeleteTextures(1, new uint[] { texId });
                }
            }
            _textureCache.Clear();
        }

        #endregion
    }
}
