using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על ציור גוף הפטריה התלת-ממדי (Body / Stem) ותווי הפנים המדויקים (Face)
    /// </summary>
    public partial class mushroom
    {
        #region ציור גוף הפטריה (Body Mesh)

        /// <summary>
        /// ציור הגוף השמנמן והחמוד של הפטריה (צבע שמנת + פנים מקדימה)
        /// </summary>
        private void DrawMushroomBody(bool isForShadow)
        {
            int stacks = 14;
            int slices = 32;
            float bodyHeight = 0.72f;

            bool canUseTex = !isForShadow && UseTexture && (_faceTextureId != 0 || _mainTextureId != 0);
            uint texToUse = _faceTextureId != 0 ? _faceTextureId : _mainTextureId;

            if (canUseTex)
            {
                GL.glEnable(GL.GL_TEXTURE_2D);
                GL.glBindTexture(GL.GL_TEXTURE_2D, texToUse);
            }

            // צבע בסיס של הגוף: שמנת חמה ורכה
            float baseR = 0.98f, baseG = 0.94f, baseB = 0.85f;

            for (int i = 0; i < stacks; i++)
            {
                float v0 = (float)i / stacks;
                float v1 = (float)(i + 1) / stacks;

                float z0 = v0 * bodyHeight;
                float z1 = v1 * bodyHeight;

                // פרופיל רדיוס הגוף (שמנמן באמצע עם לחיים עגולות)
                float r0 = GetBodyRadius(v0);
                float r1 = GetBodyRadius(v1);

                GL.glBegin(GL.GL_QUAD_STRIP);
                for (int j = 0; j <= slices; j++)
                {
                    double angle = j * 2.0 * Math.PI / slices;
                    float sinA = (float)Math.Sin(angle);
                    float cosA = (float)Math.Cos(angle);

                    float x0 = r0 * sinA;
                    float y0 = r0 * cosA;
                    float x1 = r1 * sinA;
                    float y1 = r1 * cosA;

                    // חישוב נורמלים מוחלקים
                    float nx0 = sinA * 0.9f;
                    float ny0 = cosA * 0.9f;
                    float nz0 = (v0 < 0.5f) ? -0.3f : 0.4f;

                    float nx1 = sinA * 0.9f;
                    float ny1 = cosA * 0.9f;
                    float nz1 = (v1 < 0.5f) ? -0.3f : 0.4f;

                    float shade0 = 0.88f + 0.12f * v0;
                    float shade1 = 0.88f + 0.12f * v1;

                    if (!isForShadow)
                    {
                        // הצללה הדרגתית של הגוף: תחתית מעט חמימה יותר
                        GL.glColor3f(baseR * shade0, baseG * shade0, baseB * shade0);
                        GL.glNormal3f(nx0, ny0, nz0);

                        if (canUseTex)
                        {
                            // פריסת טקסטורת הפנים בחזית (y <= 0)
                            float s0 = 0.50f + (x0 / 1.55f) * 0.85f;
                            float t0 = 0.05f + v0 * 0.85f;
                            GL.glTexCoord2f(s0, t0);
                        }
                    }

                    GL.glVertex3f(x0, y0, z0);

                    if (!isForShadow)
                    {
                        GL.glColor3f(baseR * shade1, baseG * shade1, baseB * shade1);
                        GL.glNormal3f(nx1, ny1, nz1);

                        if (canUseTex)
                        {
                            float s1 = 0.50f + (x1 / 1.55f) * 0.85f;
                            float t1 = 0.05f + v1 * 0.85f;
                            GL.glTexCoord2f(s1, t1);
                        }
                    }

                    GL.glVertex3f(x1, y1, z1);
                }
                GL.glEnd();
            }

            // תחתית הגוף (סגירת בסיס הפטריה)
            GL.glBegin(GL.GL_TRIANGLE_FAN);
            if (!isForShadow)
            {
                GL.glNormal3f(0.0f, 0.0f, -1.0f);
                GL.glColor3f(baseR * 0.80f, baseG * 0.80f, baseB * 0.80f);
                if (canUseTex) GL.glTexCoord2f(0.5f, 0.05f);
            }
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            float rBase = GetBodyRadius(0.0f);
            for (int j = 0; j <= slices; j++)
            {
                double angle = j * 2.0 * Math.PI / slices;
                float sinA = (float)Math.Sin(angle);
                float cosA = (float)Math.Cos(angle);

                if (!isForShadow && canUseTex)
                {
                    GL.glTexCoord2f(0.5f + (rBase * sinA / 1.6f) * 0.4f, 0.05f);
                }
                GL.glVertex3f(rBase * sinA, rBase * cosA, 0.0f);
            }
            GL.glEnd();

            if (canUseTex)
            {
                GL.glDisable(GL.GL_TEXTURE_2D);
            }

            // ציור תווי הפנים המדויקים (עיניים שחורות בורקות, לחיים ופה) בגאומטריה קדמית
            if (!isForShadow)
            {
                DrawFacialFeatures();
            }
        }

        /// <summary>
        /// פונקציית פרופיל הרדיוס של גוף הפטריה
        /// </summary>
        private float GetBodyRadius(float v)
        {
            // גוף שמנמן דמוי אגס מעוגל
            if (v < 0.25f)
            {
                return 0.55f + 0.24f * (float)Math.Sin(v / 0.25f * Math.PI * 0.5);
            }
            else if (v < 0.65f)
            {
                return 0.79f - 0.06f * (v - 0.25f) / 0.40f;
            }
            else
            {
                return 0.73f - 0.25f * (float)Math.Sin((v - 0.65f) / 0.35f * Math.PI * 0.5);
            }
        }

        #endregion

        #region תווי פנים (Facial Features)

        /// <summary>
        /// ציור תווי הפנים בחזית הגוף: עיניים שחורות גדולות עם נקודות ברק לבנות,
        /// לחיים ורודות סמוקות ופה חתולי (:3) בדיוק כמו באיור
        /// </summary>
        private void DrawFacialFeatures()
        {
            // כיבוי תאורה זמני לצבעים נקיים וחזקים של תווי הפנים
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_TEXTURE_2D);

            float faceZ = 0.42f;
            float eyeSpacing = 0.38f;
            float eyeRadiusX = 0.065f;
            float eyeRadiusZ = 0.085f;

            // --- עין שמאל ---
            DrawEye(-eyeSpacing, faceZ + 0.02f, eyeRadiusX, eyeRadiusZ);

            // --- עין ימין ---
            DrawEye(eyeSpacing, faceZ + 0.01f, eyeRadiusX, eyeRadiusZ);

            // --- לחי סמוקה שמאל ---
            DrawBlush(-eyeSpacing - 0.11f, faceZ - 0.07f, 0.085f, 0.045f);

            // --- לחי סמוקה ימין ---
            DrawBlush(eyeSpacing + 0.09f, faceZ - 0.07f, 0.085f, 0.045f);

            // --- פה חתולי (:3) ---
            DrawCuteMouth(0.0f, faceZ - 0.035f, 0.09f);

            GL.glEnable(GL.GL_LIGHTING);
        }

        /// <summary>
        /// ציור עין שחורה מבריקה עם ברק לבן
        /// </summary>
        private void DrawEye(float centerX, float centerZ, float rx, float rz)
        {
            float dist = (float)Math.Sqrt(centerX * centerX);
            float rBody = GetBodyRadius(centerZ / 0.72f);
            float surfaceY = -(float)Math.Sqrt(Math.Max(0.01f, rBody * rBody - dist * dist)) - 0.008f;

            // אישון שחור עמוק
            GL.glColor3f(0.08f, 0.08f, 0.08f);
            GL.glBegin(GL.GL_POLYGON);
            for (int i = 0; i < 16; i++)
            {
                double a = i * Math.PI * 2.0 / 16.0;
                float px = centerX + (float)Math.Cos(a) * rx;
                float pz = centerZ + (float)Math.Sin(a) * rz;
                GL.glVertex3f(px, surfaceY, pz);
            }
            GL.glEnd();

            // נקודת ברק לבנה (Specular Reflection Highlight) בחלק העליון
            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glBegin(GL.GL_POLYGON);
            float hx = centerX + rx * 0.28f;
            float hz = centerZ + rz * 0.38f;
            float hr = rx * 0.34f;
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI * 2.0 / 12.0;
                float px = hx + (float)Math.Cos(a) * hr;
                float pz = hz + (float)Math.Sin(a) * hr;
                GL.glVertex3f(px, surfaceY - 0.002f, pz);
            }
            GL.glEnd();
        }

        /// <summary>
        /// ציור לחי ורודה סמוקה (Blush)
        /// </summary>
        private void DrawBlush(float centerX, float centerZ, float rx, float rz)
        {
            float dist = (float)Math.Sqrt(centerX * centerX);
            float rBody = GetBodyRadius(centerZ / 0.72f);
            float surfaceY = -(float)Math.Sqrt(Math.Max(0.01f, rBody * rBody - dist * dist)) - 0.008f;

            // ורוד מתוק רך
            GL.glColor3f(1.0f, 0.58f, 0.65f);
            GL.glBegin(GL.GL_POLYGON);
            for (int i = 0; i < 16; i++)
            {
                double a = i * Math.PI * 2.0 / 16.0;
                float px = centerX + (float)Math.Cos(a) * rx;
                float pz = centerZ + (float)Math.Sin(a) * rz;
                GL.glVertex3f(px, surfaceY, pz);
            }
            GL.glEnd();
        }

        /// <summary>
        /// ציור פה חתולי מחייך (:3 / w)
        /// </summary>
        private void DrawCuteMouth(float centerX, float centerZ, float width)
        {
            float rBody = GetBodyRadius(centerZ / 0.72f);
            float surfaceY = -rBody - 0.008f;

            GL.glColor3f(0.12f, 0.08f, 0.08f);
            GL.glLineWidth(2.5f);

            float halfW = width * 0.5f;

            // קשת שמאלית של ה-3:
            GL.glBegin(GL.GL_LINE_STRIP);
            for (int i = 0; i <= 8; i++)
            {
                float t = (float)i / 8.0f;
                double a = Math.PI + t * Math.PI; // חצי עיגול כלפי מטה
                float px = centerX - halfW * 0.5f + (float)Math.Cos(a) * (halfW * 0.5f);
                float pz = centerZ + (float)Math.Sin(a) * 0.035f;
                GL.glVertex3f(px, surfaceY, pz);
            }
            GL.glEnd();

            // קשת ימנית של ה-3:
            GL.glBegin(GL.GL_LINE_STRIP);
            for (int i = 0; i <= 8; i++)
            {
                float t = (float)i / 8.0f;
                double a = Math.PI + t * Math.PI;
                float px = centerX + halfW * 0.5f + (float)Math.Cos(a) * (halfW * 0.5f);
                float pz = centerZ + (float)Math.Sin(a) * 0.035f;
                GL.glVertex3f(px, surfaceY, pz);
            }
            GL.glEnd();

            GL.glLineWidth(1.0f);
        }

        #endregion
    }
}
