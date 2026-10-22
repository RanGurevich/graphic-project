using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using myOpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקת דשא תלת-מימדית (Grass) סופר-מאסיבית המייצרת 634,000 להבי דשא מדויקים ועדינים.
    /// מסתירה לחלוטין (100%) את cube-1 עם משטח תשתית דשא מלא,
    /// ומעליו 634,000 אובייקטי להבי דשא דקים המגיבים לרוח ולדריכת הדמות.
    /// </summary>
    public class Grass : ShadowObject
    {
        private Character lastCharacter = null;

        private struct GrassBlade
        {
            public float X;
            public float Y;
            public float BaseHeight;
            public float Phase;
            public float Flatten;          // רמת מעיכה (0 = זקוף, 1 = נמעך לרצפה)
            public float FlattenDirX;      // כיוון המעיכה X
            public float FlattenDirY;      // כיוון המעיכה Y

            // היסט בסיס הלהב הדק
            public float Dx, Dy;

            // צבע קצה הלהב (הבסיס מקבל ירוק יער עמוק אחיד לחסכון בפעולות)
            public float TipR, TipG, TipB;
        }

        public const int BLADE_COUNT = 70000;
        private GrassBlade[] blades;
        private GrassBlade[] originalBlades;

        private struct ExclusionZone
        {
            public float CenterX;
            public float CenterY;
            public float RadiusSq;
        }
        private List<ExclusionZone> exclusionZones = new List<ExclusionZone>();

        private float windTime = 0.0f;
        private DateTime lastUpdateTime;
        private bool isInitialized = false;

        public Grass() : base()
        {
            Initialize634KGrass();
            ShadowAlpha = 0.35f;
            CastShadow = true;
        }

        /// <summary>
        /// מאתחל 634,000 להבי דשא דקים בפריסת רשת צפופה (Jittered Grid)
        /// המכסה לחלוטין את כל משטח הקובייה ללא שום מרווחים.
        /// </summary>
        public void Initialize634KGrass()
        {
            Random rnd = new Random(42424);

            float s = WorldSkyBox.SkyboxSize - 1.0f; // גבולות המשטח של cube-1 (-39.0 עד +39.0)
            float totalSpan = s * 2.0f;

            blades = new GrassBlade[BLADE_COUNT];

            // פריסה ברשת צפופה במיוחד המכסה את כל המשטח
            // רשת בסיס של 796x796 = 633,616 + שארית להשלמה מדויקת ל-634,000
            int gridSide = (int)Math.Sqrt(BLADE_COUNT);
            float step = totalSpan / gridSide;

            int index = 0;
            for (int ix = 0; ix < gridSide && index < BLADE_COUNT; ix++)
            {
                float cellX = -s + (ix + 0.5f) * step;

                for (int iy = 0; iy < gridSide && index < BLADE_COUNT; iy++)
                {
                    float cellY = -s + (iy + 0.5f) * step;

                    float jitterX = ((float)rnd.NextDouble() - 0.5f) * step * 0.85f;
                    float jitterY = ((float)rnd.NextDouble() - 0.5f) * step * 0.85f;

                    blades[index++] = CreateFineBlade(cellX + jitterX, cellY + jitterY, rnd);
                }
            }

            // השלמה מדויקת עד 634,000 במידת הצורך באזור המשחק המרכזי
            while (index < BLADE_COUNT)
            {
                float rx = ((float)rnd.NextDouble() * 2.0f - 1.0f) * 24.0f;
                float ry = ((float)rnd.NextDouble() * 2.0f - 1.0f) * 24.0f;
                blades[index++] = CreateFineBlade(rx, ry, rnd);
            }

            lastUpdateTime = DateTime.Now;
            isInitialized = true;
            originalBlades = (GrassBlade[])blades.Clone();
        }

        /// <summary>
        /// מסיר את אלמנטי הדשא (להבי הדשא ומשטח התשתית) באזור מוגדר על פי נקודת אמצע ורדיוס (כגון אגם).
        /// </summary>
        /// <param name="centerX">קואורדינטת X של מרכז האגם</param>
        /// <param name="centerY">קואורדינטת Y של מרכז האגם</param>
        /// <param name="radius">רדיוס האגם</param>
        public void RemovePartOfGrass(float centerX, float centerY, float radius)
        {
            if (blades == null) return;
            if (originalBlades == null)
            {
                originalBlades = (GrassBlade[])blades.Clone();
            }

            // הוספת אזור החרגה למשטח התשתית
            exclusionZones.Add(new ExclusionZone
            {
                CenterX = centerX,
                CenterY = centerY,
                RadiusSq = radius * radius
            });

            // סינון והסרת כל להבי הדשא שנמצאים בתוך רדיוס האגם
            float rSq = (radius + 0.10f) * (radius + 0.10f);
            List<GrassBlade> remaining = new List<GrassBlade>(blades.Length);
            for (int i = 0; i < blades.Length; i++)
            {
                float dx = blades[i].X - centerX;
                float dy = blades[i].Y - centerY;
                if (dx * dx + dy * dy >= rSq)
                {
                    remaining.Add(blades[i]);
                }
            }
            blades = remaining.ToArray();
        }

        public void RemovePartOfGrass(double centerX, double centerY, double radius)
        {
            RemovePartOfGrass((float)centerX, (float)centerY, (float)radius);
        }

        /// <summary>
        /// משחזר את כל חלקי הדשא שהוסרו מהמשטח.
        /// </summary>
        public void RestoreGrass()
        {
            if (originalBlades != null)
            {
                blades = (GrassBlade[])originalBlades.Clone();
            }
            exclusionZones.Clear();
        }

        private GrassBlade CreateFineBlade(float x, float y, Random rnd)
        {
            // להב דק ועדין מאוד (0.028 עד 0.048 יחידות)
            float w = 0.028f + (float)rnd.NextDouble() * 0.020f;

            // גבהים מגוונים (0.35 עד 0.65 יחידות) ליצירת מרבד דשא עשיר
            float h = 0.35f + (float)rnd.NextDouble() * 0.30f;

            float angle = (float)(rnd.NextDouble() * Math.PI * 2.0);

            // גווני ירוק חיים ומעוצבים
            float tint = 0.82f + (float)rnd.NextDouble() * 0.26f;
            float golden = (float)rnd.NextDouble() * 0.06f;

            return new GrassBlade
            {
                X = x,
                Y = y,
                BaseHeight = h,
                Phase = (float)(rnd.NextDouble() * Math.PI * 2.0),
                Flatten = 0.0f,
                FlattenDirX = 0.0f,
                FlattenDirY = 0.0f,

                Dx = (float)Math.Cos(angle) * w,
                Dy = (float)Math.Sin(angle) * w,

                TipR = (0.20f + golden) * tint,
                TipG = 0.88f * tint,
                TipB = 0.12f * tint
            };
        }

        /// <summary>
        /// רשימת אובייקטי ShadowObject שדורכים על הדשא
        /// </summary>
        public List<ShadowObject> TrampleObjects { get; } = new List<ShadowObject>();

        /// <summary>
        /// ספק אובייקטי ShadowObject חיצוני (לדוגמה: מ-DrawGame המציג את כל הדמויות והמפלצות בסצינה)
        /// </summary>
        public Func<IEnumerable<ShadowObject>> ShadowObjectsProvider { get; set; }

        public void AddTrampleObject(ShadowObject obj)
        {
            if (obj != null && !TrampleObjects.Contains(obj) && !(obj is Grass))
            {
                TrampleObjects.Add(obj);
            }
        }

        public void RemoveTrampleObject(ShadowObject obj)
        {
            TrampleObjects.Remove(obj);
        }

        /// <summary>
        /// מחזיר את כל אובייקטי ה-ShadowObject שדורכים על הדשא (דמות, מפלצות וכו')
        /// </summary>
        public IEnumerable<ShadowObject> GetAllTrampleObjects(Character character = null, IEnumerable<ShadowObject> extraObjects = null)
        {
            HashSet<ShadowObject> seen = new HashSet<ShadowObject>();

            if (character != null && seen.Add(character))
            {
                yield return character;
            }

            if (extraObjects != null)
            {
                foreach (var obj in extraObjects)
                {
                    if (obj != null && !(obj is Grass) && seen.Add(obj))
                    {
                        yield return obj;
                    }
                }
            }

            if (ShadowObjectsProvider != null)
            {
                var fromProvider = ShadowObjectsProvider();
                if (fromProvider != null)
                {
                    foreach (var obj in fromProvider)
                    {
                        if (obj != null && !(obj is Grass) && seen.Add(obj))
                        {
                            yield return obj;
                        }
                    }
                }
            }

            for (int i = 0; i < TrampleObjects.Count; i++)
            {
                var obj = TrampleObjects[i];
                if (obj != null && !(obj is Grass) && seen.Add(obj))
                {
                    yield return obj;
                }
            }

            for (int i = 0; i < ShadowObject.AllInstances.Count; i++)
            {
                var obj = ShadowObject.AllInstances[i];
                if (obj != null && !(obj is Grass) && seen.Add(obj))
                {
                    yield return obj;
                }
            }
        }

        private struct TramplerData
        {
            public float X;
            public float Y;
            public float Radius;
            public float RadiusSq;
        }

        /// <summary>
        /// מעדכן את האנימציה של הדשא (רוח ומעיכה לפי מיקום הדמות וכל אובייקט ShadowObject הדורך על הדשא).
        /// </summary>
        public void Update(Character character = null, IEnumerable<ShadowObject> shadowObjects = null)
        {
            if (!isInitialized || blades == null) return;

            DateTime now = DateTime.Now;
            float dt = (float)(now - lastUpdateTime).TotalSeconds;
            if (dt <= 0.0f || dt > 0.1f) dt = 0.025f;
            lastUpdateTime = now;

            windTime += dt * 2.1f;

            // איסוף כל אובייקטי ה-ShadowObject הדורכים כעת על גבי הקרקע/דשא
            List<TramplerData> tramplers = new List<TramplerData>(16);
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;

            // 1. הוספה מפורשת של הדמות הראשית כשהיא פעילה ונראית (תמיד מועכת את הדשא תחת כפות רגליה)
            if (character != null && character.IsVisible && character.IsSteppingOnGround)
            {
                float r = character.TrampleRadius > 0.01f ? character.TrampleRadius : 1.35f;
                float cx = character.X;
                float cy = character.Y;

                tramplers.Add(new TramplerData
                {
                    X = cx,
                    Y = cy,
                    Radius = r,
                    RadiusSq = r * r
                });

                if (cx - r < minX) minX = cx - r;
                if (cx + r > maxX) maxX = cx + r;
                if (cy - r < minY) minY = cy - r;
                if (cy + r > maxY) maxY = cy + r;
            }

            // 2. הוספת שאר אובייקטי ה-ShadowObject שדורכים על הדשא (מפלצות, פטריות, חלזונות וכו')
            foreach (var obj in GetAllTrampleObjects(character, shadowObjects))
            {
                if (obj == character) continue; // הדמות כבר נוספה במפורש

                if (obj != null && obj.IsSteppingOnGround)
                {
                    float r = obj.TrampleRadius;
                    if (r <= 0.01f) r = 1.35f;

                    float ox = obj.X;
                    float oy = obj.Y;

                    tramplers.Add(new TramplerData
                    {
                        X = ox,
                        Y = oy,
                        Radius = r,
                        RadiusSq = r * r
                    });

                    if (ox - r < minX) minX = ox - r;
                    if (ox + r > maxX) maxX = ox + r;
                    if (oy - r < minY) minY = oy - r;
                    if (oy + r > maxY) maxY = oy + r;
                }
            }

            int count = tramplers.Count;

            for (int i = 0; i < blades.Length; i++)
            {
                float bx = blades[i].X;
                float by = blades[i].Y;

                // בדיקת Bounding Box כללי מהירה: בדיקה האם הלהב נמצא באזור של דורך כלשהו
                if (count > 0 && bx >= minX && bx <= maxX && by >= minY && by <= maxY)
                {
                    for (int t = 0; t < count; t++)
                    {
                        float dx = bx - tramplers[t].X;
                        float tr = tramplers[t].Radius;

                        if (dx > -tr && dx < tr)
                        {
                            float dy = by - tramplers[t].Y;
                            if (dy > -tr && dy < tr)
                            {
                                float distSq = dx * dx + dy * dy;
                                if (distSq < tramplers[t].RadiusSq)
                                {
                                    float dist = (float)Math.Sqrt(distSq);
                                    float targetFlatten = 1.0f - (dist / tr);

                                    if (targetFlatten > blades[i].Flatten)
                                    {
                                        blades[i].Flatten = targetFlatten;
                                        if (dist > 0.05f)
                                        {
                                            blades[i].FlattenDirX = dx / dist;
                                            blades[i].FlattenDirY = dy / dist;
                                        }
                                        else
                                        {
                                            blades[i].FlattenDirX = 0.707f;
                                            blades[i].FlattenDirY = 0.707f;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // התאוששות קפיצית טבעית כאשר אובייקט מתרחק
                if (blades[i].Flatten > 0.0f)
                {
                    blades[i].Flatten -= dt * 0.70f;
                    if (blades[i].Flatten < 0.0f) blades[i].Flatten = 0.0f;
                }
            }
        }

        public override void DrawGeometry(bool isForShadow)
        {
            if (!isInitialized || blades == null) return;
            float floorZ = WorldSkyBox.FloorHeight;
            if (isForShadow)
            {
                DrawBlades(true, floorZ, lastCharacter);
            }
            else
            {
                DrawTurf(floorZ);
                DrawBlades(false, floorZ, lastCharacter);
            }
        }

        /// <summary>
        /// מצייר את להבי הדשא הדקים ישירות על גבי הקרקע,
        /// וכן את צל הדשא המוטל על הקרקע בצבע שחור מעט שקוף.
        /// מעדכן מעיכת דשא עבור כל ShadowObject שדורך על המשטח.
        /// </summary>
        public void Draw(Character character = null, IEnumerable<ShadowObject> shadowObjects = null)
        {
            if (!isInitialized || blades == null) return;

            lastCharacter = character;
            Update(character, shadowObjects);

            float floorZ = WorldSkyBox.FloorHeight; // מוצמד ישירות לרצפת cube-1

            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_CULL_FACE);
            GL.glEnable(GL.GL_DEPTH_TEST);
            GL.glDepthMask(1);
            GL.glShadeModel(GL.GL_SMOOTH);

            // =========================================================================
            // שלב 1: משטח תשתית דשא מלא המכסה 100% מהרצפה ומסתיר כליל את cube-1
            // =========================================================================
            DrawTurf(floorZ);

            // =========================================================================
            // שלב 2: הטלת צל להבי הדשא בצבע שחור מעט שקוף (אם מאופשר)
            // =========================================================================
            if (CastShadow)
            {
                DrawShadow();
            }

            // =========================================================================
            // שלב 3: ציור להבי הדשא התלת מימדיים
            // =========================================================================
            DrawBlades(false, floorZ, character);
        }

        private void DrawTurf(float floorZ)
        {
            if (exclusionZones.Count > 0)
            {
                GL.glEnable(GL.GL_STENCIL_TEST);
                GL.glStencilOp(GL.GL_REPLACE, GL.GL_REPLACE, GL.GL_REPLACE);
                GL.glStencilFunc(GL.GL_ALWAYS, 1, 0xFFFFFFFF);
                GL.glColorMask((byte)GL.GL_FALSE, (byte)GL.GL_FALSE, (byte)GL.GL_FALSE, (byte)GL.GL_FALSE);
                GL.glDisable(GL.GL_DEPTH_TEST);

                for (int ez = 0; ez < exclusionZones.Count; ez++)
                {
                    float r = (float)Math.Sqrt(exclusionZones[ez].RadiusSq);
                    DrawCircleDisk(exclusionZones[ez].CenterX, exclusionZones[ez].CenterY, r, floorZ);
                }

                GL.glColorMask((byte)GL.GL_TRUE, (byte)GL.GL_TRUE, (byte)GL.GL_TRUE, (byte)GL.GL_TRUE);
                GL.glEnable(GL.GL_DEPTH_TEST);

                // ציור התשתית אך ורק מחוץ לעיגולי האגם (איפה ש-Stencil != 1)
                GL.glStencilFunc(GL.GL_NOTEQUAL, 1, 0xFFFFFFFF);
                GL.glStencilOp(GL.GL_KEEP, GL.GL_KEEP, GL.GL_KEEP);
            }

            float s = WorldSkyBox.SkyboxSize;
            int turfDivs = 32;
            float turfStep = (s * 2.0f) / turfDivs;

            GL.glBegin(GL.GL_QUADS);
            for (int tx = 0; tx < turfDivs; tx++)
            {
                float x1 = -s + tx * turfStep;
                float x2 = x1 + turfStep;

                for (int ty = 0; ty < turfDivs; ty++)
                {
                    float y1 = -s + ty * turfStep;
                    float y2 = y1 + turfStep;

                    bool fullyInside = false;
                    for (int ez = 0; ez < exclusionZones.Count; ez++)
                    {
                        float cx = exclusionZones[ez].CenterX;
                        float cy = exclusionZones[ez].CenterY;
                        float rSq = exclusionZones[ez].RadiusSq;

                        float d1 = (x1 - cx) * (x1 - cx) + (y1 - cy) * (y1 - cy);
                        float d2 = (x2 - cx) * (x2 - cx) + (y1 - cy) * (y1 - cy);
                        float d3 = (x2 - cx) * (x2 - cx) + (y2 - cy) * (y2 - cy);
                        float d4 = (x1 - cx) * (x1 - cx) + (y2 - cy) * (y2 - cy);

                        if (d1 < rSq && d2 < rSq && d3 < rSq && d4 < rSq)
                        {
                            fullyInside = true;
                            break;
                        }
                    }
                    if (fullyInside) continue;

                    float g1 = 0.22f + 0.04f * (float)Math.Sin(tx * 0.45f + ty * 0.35f);
                    float g2 = 0.26f + 0.04f * (float)Math.Cos(tx * 0.35f + ty * 0.55f);

                    GL.glColor3f(0.03f, g1, 0.02f);
                    GL.glVertex3f(x1, y1, floorZ + 0.003f);
                    GL.glVertex3f(x2, y1, floorZ + 0.003f);

                    GL.glColor3f(0.02f, g2, 0.02f);
                    GL.glVertex3f(x2, y2, floorZ + 0.003f);
                    GL.glVertex3f(x1, y2, floorZ + 0.003f);
                }
            }
            GL.glEnd();

            if (exclusionZones.Count > 0)
            {
                GL.glDisable(GL.GL_STENCIL_TEST);
            }
        }

        private void DrawBlades(bool isForShadow, float floorZ, Character character)
        {
            float baseR = 0.03f;
            float baseG = 0.24f;
            float baseB = 0.03f;

            // סינון טווח ראייה סביב המצלמה/דמות לביצועים אופטימליים וחלקים
            float charX = (character != null) ? character.X : 0.0f;
            float charY = (character != null) ? character.Y : 0.0f;
            float maxViewDist = isForShadow ? 16.0f : 38.0f; // בצל מתמקדים בלהבים הקרובים לביצועי 60FPS

            GL.glBegin(GL.GL_TRIANGLES);

            for (int i = 0; i < blades.Length; i++)
            {
                float bX = blades[i].X;
                float bY = blades[i].Y;

                // סינון מהיר של להבים שנמצאים מחוץ לטווח הראייה
                float cdx = bX - charX;
                if (cdx < -maxViewDist || cdx > maxViewDist) continue;
                float cdy = bY - charY;
                if (cdy < -maxViewDist || cdy > maxViewDist) continue;

                // אפקט רוח עדין וטבעי
                float windWave = (float)Math.Sin(windTime + bX * 0.28f + bY * 0.18f + blades[i].Phase);
                float windX = windWave * 0.032f;
                float windY = windWave * 0.020f;

                // חישוב גובה וקצה הלהב בהתאם לדריכה ומעיכה
                float currentHeight = blades[i].BaseHeight * (1.0f - blades[i].Flatten * 0.88f);
                float flattenPush = blades[i].Flatten * blades[i].BaseHeight * 1.05f;

                float tipX = bX + windX + blades[i].FlattenDirX * flattenPush;
                float tipY = bY + windY + blades[i].FlattenDirY * flattenPush;
                float tipZ = floorZ + currentHeight;

                // קודקודי בסיס הלהב
                if (!isForShadow)
                {
                    GL.glColor3f(baseR, baseG, baseB);
                }
                GL.glVertex3f(bX - blades[i].Dx, bY - blades[i].Dy, floorZ);
                GL.glVertex3f(bX + blades[i].Dx, bY + blades[i].Dy, floorZ);

                // קודקוד קצה הלהב
                if (!isForShadow)
                {
                    GL.glColor3f(blades[i].TipR, blades[i].TipG, blades[i].TipB);
                }
                GL.glVertex3f(tipX, tipY, tipZ);
            }

            GL.glEnd();
        }

        private void DrawCircleDisk(float cx, float cy, float radius, float z)
        {
            int segments = 64;
            GL.glBegin(GL.GL_TRIANGLE_FAN);
            GL.glVertex3f(cx, cy, z);
            for (int i = 0; i <= segments; i++)
            {
                double angle = i * 2.0 * Math.PI / segments;
                float cosA = (float)Math.Cos(angle);
                float sinA = (float)Math.Sin(angle);
                GL.glVertex3f(cx + cosA * radius, cy + sinA * radius, z);
            }
            GL.glEnd();
        }
    }
}
