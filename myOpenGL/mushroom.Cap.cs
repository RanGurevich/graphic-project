using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על כיפת הפטריה (Cap), זימי הפטריה (Gills), שפת הכיפה (Rim) והנקודות הצהובות
    /// </summary>
    public partial class mushroom
    {
        #region ציור כיפת הפטריה (Cap Mesh)

        /// <summary>
        /// ציור כיפת הפטריה (Cap) הכתומה עם נקודות צהובות ושוליים מעוגלים
        /// </summary>
        private void DrawMushroomCap(bool isForShadow)
        {
            int stacks = 18;
            int slices = 36;
            float capBaseZ = 0.74f;
            float capApexZ = 1.62f;
            float capTotalHeight = capApexZ - capBaseZ;

            bool canUseTex = !isForShadow && UseTexture && (_capTextureId != 0 || _mainTextureId != 0);
            uint texToUse = _capTextureId != 0 ? _capTextureId : _mainTextureId;

            // הטיית הכיפה מעט לאחור ולצד כמו באיור
            GL.glPushMatrix();
            GL.glTranslatef(0.0f, 0.0f, capBaseZ);
            GL.glRotatef(-8.0f, 1.0f, 0.0f, 0.0f); // הטיה קלה לאחור החושפת את הזימים
            GL.glRotatef(3.5f, 0.0f, 1.0f, 0.0f);  // הטיה קלה הצידה
            GL.glTranslatef(0.0f, 0.0f, -capBaseZ);

            if (canUseTex)
            {
                GL.glEnable(GL.GL_TEXTURE_2D);
                GL.glBindTexture(GL.GL_TEXTURE_2D, texToUse);
            }

            // צבע בסיס של הכיפה: כתום בהיר, חם, חי ומבריק
            float capR = 1.0f, capG = 0.60f, capB = 0.16f;

            for (int i = 0; i < stacks; i++)
            {
                float u0 = (float)i / stacks;
                float u1 = (float)(i + 1) / stacks;

                float z0 = capBaseZ + u0 * capTotalHeight;
                float z1 = capBaseZ + u1 * capTotalHeight;

                float r0 = GetCapRadius(u0);
                float r1 = GetCapRadius(u1);

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

                    // נורמלים מוחלקים של הכיפה הפונים כלפי חוץ ומעלה
                    float nx0 = sinA * (1.0f - u0 * 0.3f);
                    float ny0 = cosA * (1.0f - u0 * 0.3f);
                    float nz0 = u0 * 1.2f + 0.15f;

                    float nx1 = sinA * (1.0f - u1 * 0.3f);
                    float ny1 = cosA * (1.0f - u1 * 0.3f);
                    float nz1 = u1 * 1.2f + 0.15f;

                    float shade0 = 0.92f + 0.18f * (float)Math.Sin(u0 * Math.PI * 0.85);
                    float shade1 = 0.92f + 0.18f * (float)Math.Sin(u1 * Math.PI * 0.85);

                    if (!isForShadow)
                    {
                        // גרדיאנט כתום בהיר ומואר
                        GL.glColor3f(capR * shade0, capG * shade0, capB * shade0);
                        GL.glNormal3f(nx0, ny0, nz0);

                        if (canUseTex)
                        {
                            float s0 = 0.50f + (x0 / 2.50f) * 0.90f;
                            float t0 = 0.45f + u0 * 0.50f;
                            GL.glTexCoord2f(s0, t0);
                        }
                    }

                    GL.glVertex3f(x0, y0, z0);

                    if (!isForShadow)
                    {
                        GL.glColor3f(capR * shade1, capG * shade1, capB * shade1);
                        GL.glNormal3f(nx1, ny1, nz1);

                        if (canUseTex)
                        {
                            float s1 = 0.50f + (x1 / 2.50f) * 0.90f;
                            float t1 = 0.45f + u1 * 0.50f;
                            GL.glTexCoord2f(s1, t1);
                        }
                    }

                    GL.glVertex3f(x1, y1, z1);
                }
                GL.glEnd();
            }

            // שפת הכיפה המעוגלת (Curved Rounded Rim / Brim)
            DrawCapRim(capBaseZ, isForShadow);

            if (canUseTex)
            {
                GL.glDisable(GL.GL_TEXTURE_2D);
            }

            // ציור נקודות צהובות תלת-ממדיות על הכיפה (Yellow Polka Dots)
            if (!isForShadow)
            {
                DrawYellowSpots(capBaseZ, capTotalHeight);
            }

            GL.glPopMatrix();
        }

        /// <summary>
        /// פרופיל רדיוס כיפת הפטריה (כיפה רחבה ועגולה המתחדדת כלפי מעלה)
        /// </summary>
        private float GetCapRadius(float u)
        {
            // u=0 (בסיס הכיפה / שוליים), u=1 (קודקוד הכיפה)
            if (u < 0.15f)
            {
                return 1.15f + 0.10f * (float)Math.Sin(u / 0.15f * Math.PI * 0.5);
            }
            else
            {
                float t = (u - 0.15f) / 0.85f;
                return 1.25f * (float)Math.Sqrt(Math.Max(0.0f, 1.0f - t * t));
            }
        }

        /// <summary>
        /// ציור שפת הכיפה המעוגלת והרכה
        /// </summary>
        private void DrawCapRim(float capBaseZ, bool isForShadow)
        {
            int slices = 36;
            float rOuter = 1.25f;
            float rInner = 1.12f;

            GL.glBegin(GL.GL_QUAD_STRIP);
            for (int j = 0; j <= slices; j++)
            {
                double angle = j * 2.0 * Math.PI / slices;
                float sinA = (float)Math.Sin(angle);
                float cosA = (float)Math.Cos(angle);

                if (!isForShadow)
                {
                    GL.glNormal3f(sinA * 0.7f, cosA * 0.7f, -0.6f);
                    GL.glColor3f(0.98f, 0.52f, 0.12f); // גוון כתום בהיר בשוליים
                }

                GL.glVertex3f(rOuter * sinA, rOuter * cosA, capBaseZ + 0.02f);
                GL.glVertex3f(rInner * sinA, rInner * cosA, capBaseZ - 0.03f);
            }
            GL.glEnd();
        }

        #endregion

        #region זימי פטריה ונקודות (Gills & Spots)

        /// <summary>
        /// ציור זימי הפטריה התלת-ממדיים (Gills) בחלק התחתון של הכיפה
        /// קפלים רדיאליים בצבע בז'-שמנת עם הצללות עדינות
        /// </summary>
        private void DrawMushroomGills(bool isForShadow)
        {
            int gillCount = 36;
            float innerRadius = 0.44f;
            float outerRadius = 1.10f;
            float gillZ = 0.75f;

            GL.glBegin(GL.GL_QUADS);
            for (int i = 0; i < gillCount; i++)
            {
                double a0 = i * 2.0 * Math.PI / gillCount;
                double a1 = (i + 1) * 2.0 * Math.PI / gillCount;

                float xInner0 = innerRadius * (float)Math.Sin(a0);
                float yInner0 = innerRadius * (float)Math.Cos(a0);
                float xOuter0 = outerRadius * (float)Math.Sin(a0);
                float yOuter0 = outerRadius * (float)Math.Cos(a0);

                float xInner1 = innerRadius * (float)Math.Sin(a1);
                float yInner1 = innerRadius * (float)Math.Cos(a1);
                float xOuter1 = outerRadius * (float)Math.Sin(a1);
                float yOuter1 = outerRadius * (float)Math.Cos(a1);

                if (!isForShadow)
                {
                    GL.glNormal3f(0.0f, 0.0f, -1.0f);
                    // גוונים מתחלפים ליצירת עומק רדיאלי של קפלי זימים
                    if (i % 2 == 0)
                    {
                        GL.glColor3f(0.88f, 0.83f, 0.72f);
                    }
                    else
                    {
                        GL.glColor3f(0.72f, 0.65f, 0.52f);
                    }
                }

                GL.glVertex3f(xInner0, yInner0, gillZ);
                GL.glVertex3f(xOuter0, yOuter0, gillZ - 0.04f);
                GL.glVertex3f(xOuter1, yOuter1, gillZ - 0.04f);
                GL.glVertex3f(xInner1, yInner1, gillZ);
            }
            GL.glEnd();
        }

        /// <summary>
        /// ציור הנקודות הצהובות התלת-ממדיות על גבי כיפת הפטריה
        /// הנקודות ממוקמות בדיוק במיקומי הנקודות שבאיור הפטריה
        /// </summary>
        private void DrawYellowSpots(float capBaseZ, float capTotalHeight)
        {
            GL.glDisable(GL.GL_LIGHTING);

            // צבע צהוב-זהב חי ומבריק
            float spotR = 1.0f, spotG = 0.85f, spotB = 0.18f;

            // רשימת נקודות (u גובה יחסי, angle זווית סביב המרכז, גודל נקודה)
            var spots = new[]
            {
                new { U = 0.75f, AngleDeg = -30.0f, RadiusX = 0.17f, RadiusY = 0.13f }, // נקודה עליונה-שמאלית
                new { U = 0.72f, AngleDeg =  40.0f, RadiusX = 0.16f, RadiusY = 0.14f }, // נקודה עליונה-ימנית
                new { U = 0.45f, AngleDeg = -55.0f, RadiusX = 0.19f, RadiusY = 0.15f }, // נקודה גדולה קדמית-שמאלית
                new { U = 0.40f, AngleDeg =   5.0f, RadiusX = 0.22f, RadiusY = 0.18f }, // נקודה מרכזית קדמית גדולה
                new { U = 0.38f, AngleDeg =  65.0f, RadiusX = 0.18f, RadiusY = 0.14f }, // נקודה קדמית-ימנית
                new { U = 0.60f, AngleDeg = 140.0f, RadiusX = 0.16f, RadiusY = 0.13f }, // נקודה אחורית-ימנית
                new { U = 0.55f, AngleDeg = -140.0f, RadiusX = 0.17f, RadiusY = 0.14f }, // נקודה אחורית-שמאלית
                new { U = 0.88f, AngleDeg =   0.0f, RadiusX = 0.13f, RadiusY = 0.11f }  // נקודה קטנה ליד הקודקוד
            };

            foreach (var spot in spots)
            {
                float z = capBaseZ + spot.U * capTotalHeight;
                float r = GetCapRadius(spot.U);
                double rad = spot.AngleDeg * Math.PI / 180.0;

                float cx = r * (float)Math.Sin(rad);
                float cy = r * (float)Math.Cos(rad);

                // נורמל הכיפה בנקודה זו
                float nx = (float)Math.Sin(rad) * 0.8f;
                float ny = (float)Math.Cos(rad) * 0.8f;
                float nz = spot.U + 0.3f;

                // הסטה קלה החוצה מעל הכיפה למניעת Z-fighting
                float offsetX = cx + nx * 0.006f;
                float offsetY = cy + ny * 0.006f;
                float offsetZ = z + nz * 0.006f;

                GL.glColor3f(spotR, spotG, spotB);
                GL.glBegin(GL.GL_POLYGON);
                for (int i = 0; i < 16; i++)
                {
                    double a = i * Math.PI * 2.0 / 16.0;
                    float dx = (float)Math.Cos(a) * spot.RadiusX;
                    float dz = (float)Math.Sin(a) * spot.RadiusY;

                    // סיבוב הנקודה במשיק לפני השטח של הכיפה
                    float px = offsetX + dx * (float)Math.Cos(rad);
                    float py = offsetY - dx * (float)Math.Sin(rad);
                    float pz = offsetZ + dz;

                    GL.glVertex3f(px, py, pz);
                }
                GL.glEnd();
            }

            GL.glEnable(GL.GL_LIGHTING);
        }

        #endregion
    }
}
