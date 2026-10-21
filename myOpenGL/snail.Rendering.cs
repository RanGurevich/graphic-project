using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על ציור הגאומטריה התלת-ממדית המלאה והוולומטרית של החילזון (Snail):
    /// - גוף שמנמן מוארך ועגול (Slug Foot, Belly, Head & Snout) עם נפח מלא, כף רגל ומבנה אורגני
    /// - קונכייה ספירלית גדולה בעלת נפח עבה (Bulbous 3D Spiral Shell) באדום או כחול עם תבליטי ספירלה תלת-ממדיים משני הצדדים
    /// - מחושי עיניים (Eyestalks) וזוג עיניים כדוריות גדולות עם עפעפיים רדומים ואישונים בהשראת MapleStory
    /// </summary>
    public partial class snail
    {
        #region ציור גוף החילזון (Snail Body - Volumetric Slug)

        /// <summary>
        /// ציור גוף החילזון התלת-ממדי בנפח מלא (ללא טקסטורה שטוחה, מבנה תלת-ממדי עגול ואורגני)
        /// </summary>
        private void DrawSnailBody(bool isForShadow)
        {
            // 1. ציור מעטפת הגוף התלת-ממדית (כף רגל, בטן שמנמנה, צוואר וראש מורם)
            DrawVolumetricBodyMesh(isForShadow);

            // 2. תחתית כף הרגל השטוחה על הרצפה (Foot Sole)
            DrawFootSole(isForShadow);

            // 3. תווי פנים חמודים: שקע פה קטן ולחיים סמוקות
            if (!isForShadow)
            {
                DrawBodyFaceDetails();
            }
        }

        /// <summary>
        /// ציור מעטפת הגוף הוולומטרית בעזרת חתכים אליפטיים עגולים ונורמלים מוחלקים
        /// </summary>
        private void DrawVolumetricBodyMesh(bool isForShadow)
        {
            int stacks = 18;
            int slices = 32;
            float totalHeight = 0.74f;

            // צבעי גוף החילזון: גווני שמנת חמה ורכה עם תחתית חמימה יותר
            float baseR = 0.98f, baseG = 0.94f, baseB = 0.86f;

            for (int i = 0; i < stacks; i++)
            {
                float u0 = (float)i / stacks;
                float u1 = (float)(i + 1) / stacks;

                float z0 = u0 * totalHeight;
                float z1 = u1 * totalHeight;

                // חישוב מרכז ורדיוסי החתך בכל גובה
                GetBodyCrossSection(u0, out float rx0, out float ry0, out float yc0);
                GetBodyCrossSection(u1, out float rx1, out float ry1, out float yc1);

                // גוון הצללה הדרגתי עדין לפי גובה
                float shade0 = (u0 < 0.12f) ? (0.86f + u0 * 0.8f) : (0.94f + (1.0f - u0) * 0.06f);
                float shade1 = (u1 < 0.12f) ? (0.86f + u1 * 0.8f) : (0.94f + (1.0f - u1) * 0.06f);

                GL.glBegin(GL.GL_QUAD_STRIP);
                for (int j = 0; j <= slices; j++)
                {
                    double angle = j * 2.0 * Math.PI / slices;
                    float sinA = (float)Math.Sin(angle);
                    float cosA = (float)Math.Cos(angle);

                    float x0 = rx0 * sinA;
                    float y0 = yc0 + ry0 * cosA;

                    float x1 = rx1 * sinA;
                    float y1 = yc1 + ry1 * cosA;

                    // נורמלים מוחלקים הפונים החוצה ב-360 מעלות
                    float nx0 = sinA;
                    float ny0 = cosA;
                    float nz0 = (u0 < 0.15f) ? -0.20f : (u0 > 0.65f ? 0.60f : 0.10f);

                    float nx1 = sinA;
                    float ny1 = cosA;
                    float nz1 = (u1 < 0.15f) ? -0.20f : (u1 > 0.65f ? 0.60f : 0.10f);

                    if (!isForShadow)
                    {
                        GL.glColor3f(baseR * shade0, baseG * shade0, baseB * shade0);
                        GL.glNormal3f(nx0, ny0, nz0);
                    }
                    GL.glVertex3f(x0, y0, z0);

                    if (!isForShadow)
                    {
                        GL.glColor3f(baseR * shade1, baseG * shade1, baseB * shade1);
                        GL.glNormal3f(nx1, ny1, nz1);
                    }
                    GL.glVertex3f(x1, y1, z1);
                }
                GL.glEnd();
            }
        }

        /// <summary>
        /// פרופיל הגוף של החילזון (שמנמן, מתעקל קדימה באזור הראש והחרטום)
        /// </summary>
        private void GetBodyCrossSection(float u, out float rx, out float ry, out float yc)
        {
            if (u <= 0.10f)
            {
                // שולי כף הרגל (Foot Skirt) המתרחבת מעט בתחתית
                float t = u / 0.10f;
                rx = 0.45f - t * 0.03f;
                ry = 0.66f - t * 0.04f;
                yc = 0.0f;
            }
            else if (u <= 0.55f)
            {
                // בטן שמנמנה וחמודה (Belly) הנוטה מעט קדימה
                float t = (u - 0.10f) / 0.45f;
                rx = 0.42f - t * 0.06f;
                ry = 0.62f - t * 0.14f;
                yc = -0.14f * (float)Math.Sin(t * Math.PI * 0.75);
            }
            else
            {
                // ראש וחרטום מורמים קדימה (Head & Snout)
                float t = (u - 0.55f) / 0.45f;
                float dome = (float)Math.Sqrt(Math.Max(0.001f, 1.0f - t * t));
                rx = 0.36f * dome;
                ry = 0.48f * dome;
                yc = -0.14f - 0.16f * t;
            }
        }

        /// <summary>
        /// כף הרגל של החילזון שטוחה על הרצפה
        /// </summary>
        private void DrawFootSole(bool isForShadow)
        {
            int slices = 24;
            float footRx = 0.45f;
            float footRy = 0.66f;

            GL.glBegin(GL.GL_TRIANGLE_FAN);
            if (!isForShadow)
            {
                GL.glNormal3f(0.0f, 0.0f, -1.0f);
                GL.glColor3f(0.88f, 0.82f, 0.72f);
            }
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            for (int j = 0; j <= slices; j++)
            {
                double angle = j * 2.0 * Math.PI / slices;
                float sinA = (float)Math.Sin(angle);
                float cosA = (float)Math.Cos(angle);
                GL.glVertex3f(footRx * sinA, footRy * cosA, 0.0f);
            }
            GL.glEnd();
        }

        /// <summary>
        /// פרטי פנים חמודים: קו פה קטן ולחיים סמוקות
        /// </summary>
        private void DrawBodyFaceDetails()
        {
            GL.glDisable(GL.GL_LIGHTING);

            // פה קטן כהה (-) בקדמת החרטום
            GL.glColor3f(0.20f, 0.14f, 0.12f);
            GL.glLineWidth(3.0f);

            float mouthY = -0.44f;
            float mouthZ = 0.30f;
            float halfW = 0.065f;

            GL.glBegin(GL.GL_LINES);
            GL.glVertex3f(-halfW, mouthY, mouthZ);
            GL.glVertex3f(halfW, mouthY, mouthZ);
            GL.glEnd();

            GL.glLineWidth(1.0f);

            // לחיים סמוקות ורודות ורכות (Soft Pink Blush) משני צדי הפנים
            DrawBlushSpot(-0.21f, mouthY + 0.04f, mouthZ + 0.06f);
            DrawBlushSpot(0.21f, mouthY + 0.04f, mouthZ + 0.06f);

            GL.glEnable(GL.GL_LIGHTING);
        }

        private void DrawBlushSpot(float cx, float cy, float cz)
        {
            GL.glColor3f(1.0f, 0.62f, 0.68f);
            GL.glBegin(GL.GL_POLYGON);
            float rx = 0.048f;
            float rz = 0.032f;
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI * 2.0 / 12.0;
                float px = cx + (float)Math.Cos(a) * rx;
                float pz = cz + (float)Math.Sin(a) * rz;
                GL.glVertex3f(px, cy - 0.003f, pz);
            }
            GL.glEnd();
        }

        #endregion

        #region ציור הקונכייה הספירלית התלת-ממדית (Volumetric 3D Spiral Shell)

        /// <summary>
        /// ציור הקונכייה הספירלית התלת-ממדית המלאה:
        /// בעלת עובי ממשי (נפח גדול ורחב), תבליט ספירלה תלת-ממדי פיזי משני הצדדים, וצוואר מחבר
        /// </summary>
        private void DrawSnailShell(bool isForShadow)
        {
            // מיקום הקונכייה על גב החילזון
            float shellX = 0.0f;
            float shellY = 0.16f;
            float shellZ = 0.68f;

            // מימדי הקונכייה (עובי רחב ונפח עגול מלא)
            float shellHalfWidth = 0.42f; // רוחב כולל של כ-0.84 בציר X
            float shellRadiusY = 0.58f;   // קוטר אורך כ-1.16
            float shellRadiusZ = 0.60f;   // קוטר גובה כ-1.20

            // פלטת צבעים עשירה לפי צבע החילזון (אדום או כחול)
            float primaryR, primaryG, primaryB;
            float highlightR, highlightG, highlightB;
            float darkR, darkG, darkB;

            if (ColorType == SnailColor.Blue)
            {
                // כחול רויאל בוהק ומלא חיים
                primaryR = 0.12f; primaryG = 0.44f; primaryB = 0.98f;
                highlightR = 0.40f; highlightG = 0.75f; highlightB = 1.00f;
                darkR = 0.03f; darkG = 0.12f; darkB = 0.52f;
            }
            else
            {
                // אדום רובי עשיר ומבריק
                primaryR = 0.92f; primaryG = 0.10f; primaryB = 0.20f;
                highlightR = 1.00f; highlightG = 0.38f; highlightB = 0.42f;
                darkR = 0.50f; darkG = 0.03f; darkB = 0.10f;
            }

            GL.glPushMatrix();
            GL.glTranslatef(shellX, shellY, shellZ);

            // הטיה עדינה של הקונכייה לאחור כפי שיושבת על הגב
            GL.glRotatef(10.0f, 1.0f, 0.0f, 0.0f);

            // 1. ציור הגוף העיקרי של הקונכייה (מעטפת מעוגלת עבה בנפח תלת-ממדי מלא)
            DrawMainShellBody(shellHalfWidth, shellRadiusY, shellRadiusZ, primaryR, primaryG, primaryB, darkR, darkG, darkB, isForShadow);

            // 2. ציור תבליט הספירלה התלת-ממדי (3D Sculpted Spiral) בצד ימין
            DrawSculptedSpiralFlank(shellHalfWidth, shellRadiusY * 0.85f, primaryR, primaryG, primaryB, darkR, darkG, darkB, highlightR, highlightG, highlightB, false, isForShadow);

            // 3. ציור תבליט הספירלה התלת-ממדי (3D Sculpted Spiral) בצד שמאל
            DrawSculptedSpiralFlank(-shellHalfWidth, shellRadiusY * 0.85f, primaryR, primaryG, primaryB, darkR, darkG, darkB, highlightR, highlightG, highlightB, true, isForShadow);

            // 4. ציור שפת הקונכייה (Aperture Rim) בחיבור לגוף
            DrawShellApertureRim(shellHalfWidth, shellRadiusY, darkR, darkG, darkB, isForShadow);

            // 5. ציור גלימת הקונכייה המחברת (Mantle Hood) מאחורי העיניים כפי שמופיע באיור
            DrawMantleHood(primaryR, primaryG, primaryB, isForShadow);

            GL.glPopMatrix();
        }

        /// <summary>
        /// ציור גוף הקונכייה העיקרי כמעטפת תלת-ממדית תפוחה ועבה (Bulbous Toroidal Ellipsoid)
        /// </summary>
        private void DrawMainShellBody(float halfW, float radY, float radZ, float pr, float pg, float pb, float dr, float dg, float db, bool isForShadow)
        {
            int stacks = 18;
            int slices = 36;

            for (int i = 0; i < stacks; i++)
            {
                // זווית לאורך ציר העובי X (מצד שמאל לצד ימין)
                double u0 = -Math.PI * 0.5 + Math.PI * (float)i / stacks;
                double u1 = -Math.PI * 0.5 + Math.PI * (float)(i + 1) / stacks;

                float sinU0 = (float)Math.Sin(u0);
                float cosU0 = (float)Math.Cos(u0);
                float sinU1 = (float)Math.Sin(u1);
                float cosU1 = (float)Math.Cos(u1);

                // שינוי עדין של הרדיוס במעטפת
                float x0 = halfW * sinU0;
                float x1 = halfW * sinU1;

                float profile0 = (float)Math.Max(0.08f, cosU0);
                float profile1 = (float)Math.Max(0.08f, cosU1);

                GL.glBegin(GL.GL_QUAD_STRIP);
                for (int j = 0; j <= slices; j++)
                {
                    double phi = j * 2.0 * Math.PI / slices;
                    float sinP = (float)Math.Sin(phi);
                    float cosP = (float)Math.Cos(phi);

                    // התרחבות ספירלית קלה בהיקף החיצוני
                    float spiralGrowth = 1.0f + 0.12f * sinP;

                    float y0 = radY * profile0 * cosP * spiralGrowth;
                    float z0 = radZ * profile0 * sinP * spiralGrowth;

                    float y1 = radY * profile1 * cosP * spiralGrowth;
                    float z1 = radZ * profile1 * sinP * spiralGrowth;

                    // חישוב נורמלים מוחלקים
                    float nx0 = sinU0 * 0.9f;
                    float ny0 = cosU0 * cosP;
                    float nz0 = cosU0 * sinP;

                    float nx1 = sinU1 * 0.9f;
                    float ny1 = cosU1 * cosP;
                    float nz1 = cosU1 * sinP;

                    // הצללת עומק טבעית
                    float shade0 = 0.82f + 0.28f * (float)Math.Max(0.0f, cosU0 * (sinP * 0.6f + 0.4f));
                    float shade1 = 0.82f + 0.28f * (float)Math.Max(0.0f, cosU1 * (sinP * 0.6f + 0.4f));

                    if (!isForShadow)
                    {
                        GL.glColor3f(pr * shade0, pg * shade0, pb * shade0);
                        GL.glNormal3f(nx0, ny0, nz0);
                    }
                    GL.glVertex3f(x0, y0, z0);

                    if (!isForShadow)
                    {
                        GL.glColor3f(pr * shade1, pg * shade1, pb * shade1);
                        GL.glNormal3f(nx1, ny1, nz1);
                    }
                    GL.glVertex3f(x1, y1, z1);
                }
                GL.glEnd();
            }
        }

        /// <summary>
        /// ציור תבליט ספירלה תלת-ממדי פיזי ומפוסל (Sculpted Spiral Whorl) על דופן הקונכייה
        /// עם רכס מוגבה, תעלות הצללה כהות וטבור מרכזי עגול
        /// </summary>
        private void DrawSculptedSpiralFlank(
            float flankX, float maxRadius,
            float pr, float pg, float pb,
            float dr, float dg, float db,
            float hr, float hg, float hb,
            bool flip, bool isForShadow)
        {
            int segments = 54;
            float totalAngle = (float)(Math.PI * 4.6); // שתי סיבובים וקצת של ספירלה
            float dir = flip ? -1.0f : 1.0f;
            float signX = flip ? -1.0f : 1.0f;

            // 1. ציור רכס הספירלה המפוסל בעל גובה ונפח ממשי
            GL.glBegin(GL.GL_QUAD_STRIP);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * totalAngle * dir;

                // רדיוס הספירלה הולך וקטן כלפי המרכז
                float radius = maxRadius * (1.0f - 0.78f * t);

                // רוחב הרכס הולך ונהיה צר כלפי המרכז
                float ribbonWidth = 0.085f * (1.0f - 0.50f * t);

                // גובה התבליט (בליטה כלפי חוץ בציר X)
                float ridgeOut = 0.038f * (1.0f - 0.35f * t);

                float sinA = (float)Math.Sin(angle);
                float cosA = (float)Math.Cos(angle);

                // קואורדינטות המרכז של הרכס
                float cy = radius * cosA;
                float cz = radius * sinA;

                // קואורדינטת בליטת הרכס
                float crestX = flankX + signX * ridgeOut;
                // קואורדינטת שקע התעלה
                float valleyX = flankX - signX * 0.012f;

                float vy0 = cy - ribbonWidth * 0.5f * cosA;
                float vz0 = cz - ribbonWidth * 0.5f * sinA;

                float vy1 = cy + ribbonWidth * 0.5f * cosA;
                float vz1 = cz + ribbonWidth * 0.5f * sinA;

                if (!isForShadow)
                {
                    // צד עליון של הרכס - מואר ובולט
                    GL.glColor3f(hr, hg, hb);
                    GL.glNormal3f(signX * 0.8f, cosA * 0.4f, sinA * 0.4f);
                }
                GL.glVertex3f(crestX, cy, cz);

                if (!isForShadow)
                {
                    // שקע התעלה הספירלית - כהה ומוצל
                    GL.glColor3f(dr, dg, db);
                    GL.glNormal3f(signX * 0.2f, -cosA * 0.5f, -sinA * 0.5f);
                }
                GL.glVertex3f(valleyX, vy0, vz0);
            }
            GL.glEnd();

            // 2. ציור פס הדגשה ספירלי נוסף לשפה הפנימית ליצירת עומק תלת-ממדי עשיר
            GL.glBegin(GL.GL_QUAD_STRIP);
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float angle = t * totalAngle * dir;

                float radius = maxRadius * (1.0f - 0.78f * t);
                float ribbonWidth = 0.085f * (1.0f - 0.50f * t);
                float ridgeOut = 0.038f * (1.0f - 0.35f * t);

                float sinA = (float)Math.Sin(angle);
                float cosA = (float)Math.Cos(angle);

                float cy = radius * cosA;
                float cz = radius * sinA;

                float crestX = flankX + signX * ridgeOut;
                float valleyX = flankX - signX * 0.012f;

                float vy1 = cy + ribbonWidth * 0.5f * cosA;
                float vz1 = cz + ribbonWidth * 0.5f * sinA;

                if (!isForShadow)
                {
                    GL.glColor3f(hr, hg, hb);
                    GL.glNormal3f(signX * 0.8f, cosA * 0.4f, sinA * 0.4f);
                }
                GL.glVertex3f(crestX, cy, cz);

                if (!isForShadow)
                {
                    GL.glColor3f(dr * 0.9f, dg * 0.9f, db * 0.9f);
                    GL.glNormal3f(signX * 0.2f, cosA * 0.5f, sinA * 0.5f);
                }
                GL.glVertex3f(valleyX, vy1, vz1);
            }
            GL.glEnd();

            // 3. כיפת הטבור המרכזית (Central Protoconch Button) בטבור הספירלה
            float hubRadius = 0.10f;
            float hubX = flankX + signX * 0.032f;
            int hubSlices = 16;

            GL.glBegin(GL.GL_TRIANGLE_FAN);
            if (!isForShadow)
            {
                GL.glColor3f(hr, hg, hb);
                GL.glNormal3f(signX, 0.0f, 0.0f);
            }
            GL.glVertex3f(hubX, 0.0f, 0.0f);

            for (int j = 0; j <= hubSlices; j++)
            {
                double a = j * 2.0 * Math.PI / hubSlices;
                float hy = hubRadius * (float)Math.Cos(a);
                float hz = hubRadius * (float)Math.Sin(a);

                if (!isForShadow)
                {
                    GL.glColor3f(pr, pg, pb);
                    GL.glNormal3f(signX * 0.7f, hy / hubRadius * 0.5f, hz / hubRadius * 0.5f);
                }
                GL.glVertex3f(flankX, hy, hz);
            }
            GL.glEnd();
        }

        /// <summary>
        /// ציור שפת הקונכייה (Aperture Collar Rim) בתחתית החזית
        /// </summary>
        private void DrawShellApertureRim(float halfW, float radY, float dr, float dg, float db, bool isForShadow)
        {
            int slices = 20;
            float rimRadius = 0.055f;
            float rimCenterY = -radY * 0.65f;
            float rimCenterZ = -0.32f;

            GL.glBegin(GL.GL_QUAD_STRIP);
            for (int i = 0; i <= slices; i++)
            {
                float t = (float)i / slices;
                float x = (t * 2.0f - 1.0f) * (halfW * 0.92f);

                if (!isForShadow)
                {
                    GL.glColor3f(dr, dg, db);
                    GL.glNormal3f(0.0f, -0.8f, -0.4f);
                }
                GL.glVertex3f(x, rimCenterY, rimCenterZ);

                if (!isForShadow)
                {
                    GL.glColor3f(dr * 1.3f, dg * 1.3f, db * 1.3f);
                    GL.glNormal3f(0.0f, -0.6f, 0.6f);
                }
                GL.glVertex3f(x, rimCenterY + rimRadius, rimCenterZ + rimRadius);
            }
            GL.glEnd();
        }

        /// <summary>
        /// גלימת הקונכייה (Mantle Hood) - המשולש הקדמי המחבר בין הקונכייה לעיניים
        /// </summary>
        private void DrawMantleHood(float pr, float pg, float pb, bool isForShadow)
        {
            // מעין כובע/משולש אדום (או כחול) בולט בדיוק מאחורי העיניים כפי שרואים באיור של החילזון
            GL.glBegin(GL.GL_TRIANGLES);

            if (!isForShadow)
            {
                GL.glColor3f(pr * 1.08f, pg * 1.08f, pb * 1.08f);
                GL.glNormal3f(0.0f, -0.6f, 0.8f);
            }
            GL.glVertex3f(0.0f, -0.44f, 0.16f); // קודקוד קדמי ליד העיניים

            if (!isForShadow)
            {
                GL.glColor3f(pr * 0.90f, pg * 0.90f, pb * 0.90f);
                GL.glNormal3f(-0.5f, 0.2f, 0.6f);
            }
            GL.glVertex3f(-0.24f, -0.22f, 0.32f);

            if (!isForShadow)
            {
                GL.glColor3f(pr * 0.90f, pg * 0.90f, pb * 0.90f);
                GL.glNormal3f(0.5f, 0.2f, 0.6f);
            }
            GL.glVertex3f(0.24f, -0.22f, 0.32f);

            GL.glEnd();
        }

        #endregion

        #region ציור העיניים, המחושים והעפעפיים (Eyestalks, Sleepy Eyes & Pupils)

        /// <summary>
        /// ציור צמד העיניים הגדולות עם מחושים תלת-ממדיים ועפעפיים רדומים
        /// </summary>
        private void DrawSnailEyes(bool isForShadow)
        {
            float eyeSpacingX = 0.20f;
            float eyeY = -0.28f;
            float eyeZ = 0.85f;

            // 1. מחוש עין שמאל
            DrawEyestalk(-0.16f, -0.24f, 0.66f, -eyeSpacingX, eyeY, eyeZ, 0.055f, isForShadow);
            // 2. מחוש עין ימין
            DrawEyestalk(0.16f, -0.24f, 0.66f, eyeSpacingX, eyeY, eyeZ, 0.055f, isForShadow);

            // 3. עין שמאל (כדור עין, עפעף רדום תלת-ממדי ואישון מבריק)
            DrawSingleEye(-eyeSpacingX, eyeY, eyeZ, true, isForShadow);

            // 4. עין ימין (כדור עין, עפעף רדום תלת-ממדי ואישון מבריק)
            DrawSingleEye(eyeSpacingX, eyeY, eyeZ, false, isForShadow);
        }

        /// <summary>
        /// ציור מחוש עין תלת-ממדי (גליל מקשר בין הראש לגלגל העין)
        /// </summary>
        private void DrawEyestalk(float x0, float y0, float z0, float x1, float y1, float z1, float radius, bool isForShadow)
        {
            int slices = 12;
            float skinR = 0.96f, skinG = 0.92f, skinB = 0.84f;

            GL.glBegin(GL.GL_QUAD_STRIP);
            for (int i = 0; i <= slices; i++)
            {
                double angle = i * 2.0 * Math.PI / slices;
                float sinA = (float)Math.Sin(angle);
                float cosA = (float)Math.Cos(angle);

                float dx0 = radius * sinA;
                float dy0 = radius * cosA * 0.7f;
                float dx1 = (radius * 0.85f) * sinA;
                float dy1 = (radius * 0.85f) * cosA * 0.7f;

                if (!isForShadow)
                {
                    GL.glColor3f(skinR * 0.92f, skinG * 0.92f, skinB * 0.92f);
                    GL.glNormal3f(sinA, cosA, 0.0f);
                }
                GL.glVertex3f(x0 + dx0, y0 + dy0, z0);

                if (!isForShadow)
                {
                    GL.glColor3f(skinR, skinG, skinB);
                    GL.glNormal3f(sinA, cosA, 0.0f);
                }
                GL.glVertex3f(x1 + dx1, y1 + dy1, z1);
            }
            GL.glEnd();
        }

        /// <summary>
        /// ציור עין בודדת תלת-ממדית מלאה: כדור עין לבן מבריק, עפעף תלת-ממדי עבה מנומנם ואישון
        /// </summary>
        private void DrawSingleEye(float posX, float posY, float posZ, bool isLeftEye, bool isForShadow)
        {
            float eyeRadius = 0.160f;

            GL.glPushMatrix();
            GL.glTranslatef(posX, posY, posZ);

            // הטיה עדינה של העין קדימה ולצדדים
            GL.glRotatef(12.0f, 1.0f, 0.0f, 0.0f);
            GL.glRotatef(isLeftEye ? -8.0f : 8.0f, 0.0f, 0.0f, 1.0f);

            int stacks = 16;
            int slices = 24;

            // 1. ציור כדור העין הלבן המבריק (White Eyeball Sphere)
            for (int i = 0; i < stacks; i++)
            {
                double lat0 = Math.PI * (-0.5 + (float)i / stacks);
                double lat1 = Math.PI * (-0.5 + (float)(i + 1) / stacks);

                float z0 = (float)Math.Sin(lat0) * eyeRadius;
                float z1 = (float)Math.Sin(lat1) * eyeRadius;

                float r0 = (float)Math.Cos(lat0) * eyeRadius;
                float r1 = (float)Math.Cos(lat1) * eyeRadius;

                GL.glBegin(GL.GL_QUAD_STRIP);
                for (int j = 0; j <= slices; j++)
                {
                    double lng = 2.0 * Math.PI * (float)j / slices;
                    float sinL = (float)Math.Sin(lng);
                    float cosL = (float)Math.Cos(lng);

                    float x0 = cosL * r0;
                    float y0 = sinL * r0;

                    float x1 = cosL * r1;
                    float y1 = sinL * r1;

                    if (!isForShadow)
                    {
                        // לבן עין מבריק וצחור
                        GL.glColor3f(0.99f, 0.99f, 0.99f);
                        GL.glNormal3f(x0 / eyeRadius, y0 / eyeRadius, z0 / eyeRadius);
                    }
                    GL.glVertex3f(x0, y0, z0);

                    if (!isForShadow)
                    {
                        GL.glNormal3f(x1 / eyeRadius, y1 / eyeRadius, z1 / eyeRadius);
                    }
                    GL.glVertex3f(x1, y1, z1);
                }
                GL.glEnd();
            }

            if (!isForShadow)
            {
                // 2. עפעף עליון תלת-ממדי רדום (Sleepy Eyelid Hood) המכסה כ-45% מהעין
                Draw3DSleepyEyelid(eyeRadius);

                // 3. אישון שחור עגול ומבריק עם נקודת ברק לבנה (Pupil & Specular Glint)
                DrawPupilAndHighlight(eyeRadius, isLeftEye);
            }

            GL.glPopMatrix();
        }

        /// <summary>
        /// ציור עפעף תלת-ממדי עבה מנומנם (Sleepy Eyelid) בעל נפח עוטף ומסגרת כהה
        /// </summary>
        private void Draw3DSleepyEyelid(float eyeRadius)
        {
            float lidRadius = eyeRadius * 1.055f; // מעט מעל גלגל העין כדי לעטוף אותו
            float lidR = 0.78f, lidG = 0.64f, lidB = 0.50f; // גוון חום-בז' עדין בדיוק כמו באיור של מייפל-סטורי

            int stacks = 8;
            int slices = 24;

            // כיפת עפעף המכסה מ-z=0 ומעלה עד הקודקוד
            for (int i = 0; i < stacks; i++)
            {
                // זווית מעל קו המשווה (0 עד 90 מעלות)
                double lat0 = (Math.PI * 0.5) * ((float)i / stacks);
                double lat1 = (Math.PI * 0.5) * ((float)(i + 1) / stacks);

                float z0 = (float)Math.Sin(lat0) * lidRadius;
                float z1 = (float)Math.Sin(lat1) * lidRadius;

                float r0 = (float)Math.Cos(lat0) * lidRadius;
                float r1 = (float)Math.Cos(lat1) * lidRadius;

                GL.glBegin(GL.GL_QUAD_STRIP);
                for (int j = 0; j <= slices; j++)
                {
                    // חצי כדור קדמי הפונה כלפי החזית (y <= 0)
                    double lng = Math.PI * (float)j / slices + Math.PI;
                    float sinL = (float)Math.Sin(lng);
                    float cosL = (float)Math.Cos(lng);

                    float x0 = cosL * r0;
                    float y0 = sinL * r0;

                    float x1 = cosL * r1;
                    float y1 = sinL * r1;

                    // הצללת עפעף עדינה
                    float shade0 = 0.90f + 0.10f * (float)Math.Sin(lat0);
                    float shade1 = 0.90f + 0.10f * (float)Math.Sin(lat1);

                    GL.glColor3f(lidR * shade0, lidG * shade0, lidB * shade0);
                    GL.glNormal3f(x0 / lidRadius, y0 / lidRadius, z0 / lidRadius);
                    GL.glVertex3f(x0, y0, z0);

                    GL.glColor3f(lidR * shade1, lidG * shade1, lidB * shade1);
                    GL.glNormal3f(x1 / lidRadius, y1 / lidRadius, z1 / lidRadius);
                    GL.glVertex3f(x1, y1, z1);
                }
                GL.glEnd();
            }

            // קו מסגרת שחור מודגש בתחתית העפעף (הריס / קפל העפעף המנומנם)
            GL.glDisable(GL.GL_LIGHTING);
            GL.glColor3f(0.12f, 0.08f, 0.08f);
            GL.glLineWidth(3.5f);

            GL.glBegin(GL.GL_LINE_STRIP);
            for (int j = 0; j <= slices; j++)
            {
                double lng = Math.PI * (float)j / slices + Math.PI;
                float x = (float)Math.Cos(lng) * lidRadius;
                float y = (float)Math.Sin(lng) * lidRadius;
                GL.glVertex3f(x, y, 0.006f);
            }
            GL.glEnd();

            GL.glLineWidth(1.0f);
            GL.glEnable(GL.GL_LIGHTING);
        }

        /// <summary>
        /// ציור אישון שחור עמוק מבריק עם נקודת ברק לבנה (Corneal Specular Highlight)
        /// </summary>
        private void DrawPupilAndHighlight(float eyeRadius, bool isLeftEye)
        {
            GL.glDisable(GL.GL_LIGHTING);

            float pupilRadius = 0.052f;
            float surfaceY = -eyeRadius - 0.005f;
            float pupilZ = -0.025f; // ממוקם מתחת לקו העפעף
            float pupilX = isLeftEye ? 0.015f : -0.015f; // מבט פוזל קלות קדימה

            // אישון שחור עמוק
            GL.glColor3f(0.06f, 0.06f, 0.06f);
            GL.glBegin(GL.GL_POLYGON);
            for (int i = 0; i < 18; i++)
            {
                double a = i * Math.PI * 2.0 / 18.0;
                float px = pupilX + (float)Math.Cos(a) * pupilRadius;
                float pz = pupilZ + (float)Math.Sin(a) * pupilRadius;
                GL.glVertex3f(px, surfaceY, pz);
            }
            GL.glEnd();

            // נקודת ברק לבנה קטנה בחלק העליון של האישון
            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glBegin(GL.GL_POLYGON);
            float hx = pupilX + 0.015f;
            float hz = pupilZ + 0.018f;
            float hr = pupilRadius * 0.38f;
            for (int i = 0; i < 12; i++)
            {
                double a = i * Math.PI * 2.0 / 12.0;
                float px = hx + (float)Math.Cos(a) * hr;
                float pz = hz + (float)Math.Sin(a) * hr;
                GL.glVertex3f(px, surfaceY - 0.003f, pz);
            }
            GL.glEnd();

            GL.glEnable(GL.GL_LIGHTING);
        }

        #endregion
    }
}
