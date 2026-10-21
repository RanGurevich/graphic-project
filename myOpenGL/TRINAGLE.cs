using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// מפלצת פירמידה (TRINAGLE) היורשת ממחלקת Monster (ו-ShadowObject).
    /// המפלצת מיוצגת על ידי פירמידה תלת-ממדית עם עין/ליבה מיסטית,
    /// נעה בקפיצות אקראיות על גבי רצפת העולם (FloorHeight),
    /// ומטילה צל מדויק על הקרקע בהתאם לתנועת הקפיצה.
    /// </summary>
    public class TRINAGLE : Monster
    {
        #region שדות ומאפייני פירמידה וצבע

        private static readonly Random globalRand = new Random();
        private readonly Random rand;

        /// <summary>
        /// רוחב בסיס הפירמידה
        /// </summary>
        public float BaseWidth { get; set; } = 1.2f;

        /// <summary>
        /// גובה הפירמידה
        /// </summary>
        public float PyramidHeight { get; set; } = 1.4f;

        /// <summary>
        /// גוון צבע אדום
        /// </summary>
        public float ColorR { get; set; } = 0.95f;

        /// <summary>
        /// גוון צבע ירוק
        /// </summary>
        public float ColorG { get; set; } = 0.35f;

        /// <summary>
        /// גוון צבע כחול
        /// </summary>
        public float ColorB { get; set; } = 0.15f;

        #endregion

        #region שדות ומאפייני פיזיקת קפיצות (Hopping Physics)

        /// <summary>
        /// גובה הרצפה שמעליו המפלצת קופצת
        /// </summary>
        public float BaseZ { get; set; } = WorldSkyBox.FloorHeight;

        /// <summary>
        /// האם המפלצת נמצאת כעת באמצע קפיצה באוויר
        /// </summary>
        public bool IsJumping { get; private set; } = false;

        /// <summary>
        /// גובה שיא של הקפיצה הנוכחית
        /// </summary>
        public float CurrentJumpHeight { get; set; } = 1.8f;

        /// <summary>
        /// משך הקפיצה בשניות
        /// </summary>
        public float JumpDuration { get; set; } = 0.65f;

        private float jumpTimer = 0.0f;
        private float waitTimer = 0.0f;
        private float jumpStartX = 0.0f;
        private float jumpStartY = 0.0f;
        private float jumpTargetX = 0.0f;
        private float jumpTargetY = 0.0f;
        private float landingSquishTime = 0.0f;
        private DateTime lastUpdateTime = DateTime.Now;

        /// <summary>
        /// האם לעדכן פיזיקה אוטומטית בעת קריאה ל-DrawMonster
        /// </summary>
        public bool AutoUpdate { get; set; } = true;

        #endregion

        #region בנאים (Constructors)

        /// <summary>
        /// בנאי ראשי למפלצת TRINAGLE המאתחל כמות חיים ומיקום התחלתי
        /// </summary>
        /// <param name="initialHealth">כמות חיים התחלתית המועברת לבנאי של Monster</param>
        /// <param name="x">מיקום X התחלתי</param>
        /// <param name="y">מיקום Y התחלתי</param>
        public TRINAGLE(int initialHealth, float x = 0.0f, float y = 0.0f) 
            : base(initialHealth, x, y, WorldSkyBox.FloorHeight)
        {
            rand = new Random(Guid.NewGuid().GetHashCode());
            BaseZ = WorldSkyBox.FloorHeight;
            Z = BaseZ;
            HealthBarHeightOffset = PyramidHeight + 0.4f;

            // זמן המתנה ראשוני רנדומלי כדי ש-5 המפלצות לא יקפצו באותו זמן בדיוק
            waitTimer = (float)(rand.NextDouble() * 1.5 + 0.2);
        }

        /// <summary>
        /// בנאי המאפשר גם הגדרת צבע אישי למפלצת
        /// </summary>
        public TRINAGLE(int initialHealth, float x, float y, float r, float g, float b)
            : this(initialHealth, x, y)
        {
            ColorR = r;
            ColorG = g;
            ColorB = b;
        }

        /// <summary>
        /// בנאי ברירת מחדל
        /// </summary>
        public TRINAGLE() : this(100, 0.0f, 0.0f)
        {
        }

        #endregion

        #region מנגנון קפיצות אקראיות (Random Jumping Logic)

        /// <summary>
        /// מעדכן את מצב הפיזיקה והקפיצה של המפלצת לפי הזמן שחלף
        /// </summary>
        public void Update()
        {
            DateTime now = DateTime.Now;
            float dt = (float)(now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            if (dt > 0.1f) dt = 0.1f;
            if (dt <= 0.0f) return;

            UpdatePhysics(dt);
        }

        /// <summary>
        /// מעדכן את מיקום הקפיצה של הפירמידה בזמן אמת
        /// </summary>
        public void UpdatePhysics(float dt)
        {
            if (!IsAlive) return;

            if (landingSquishTime > 0.0f)
            {
                landingSquishTime -= dt;
                if (landingSquishTime < 0.0f) landingSquishTime = 0.0f;
            }

            // בדיקת התנגשות ומגע עם הדמות הראשית:
            // אם הפירמידה נוגעת בדמות, היא עוצרת לחלוטין במקום עד שהדמות תזוז!
            if (CheckAndResolveCharacterCollision())
            {
                if (IsJumping)
                {
                    IsJumping = false;
                    jumpTimer = 0.0f;
                    Z = BaseZ;
                    landingSquishTime = 0.18f;
                    waitTimer = (float)(rand.NextDouble() * 1.5 + 0.5);
                }
                return;
            }

            if (IsJumping)
            {
                jumpTimer += dt;
                float progress = jumpTimer / JumpDuration;

                if (progress >= 1.0f)
                {
                    // נחיתה על הרצפה
                    IsJumping = false;
                    X = jumpTargetX;
                    Y = jumpTargetY;
                    Z = BaseZ;
                    jumpTimer = 0.0f;
                    landingSquishTime = 0.18f; // אפקט מעיכה רגעי בנחיתה

                    // הגרלת זמן מנוחה עד הקפיצה הבאה (בין 0.4 ל-1.4 שניות)
                    waitTimer = (float)(rand.NextDouble() * 1.0 + 0.4);
                }
                else
                {
                    float nextX = jumpStartX + (jumpTargetX - jumpStartX) * progress;
                    float nextY = jumpStartY + (jumpTargetY - jumpStartY) * progress;

                    if (GlobalCharacter != null)
                    {
                        float dx = nextX - GlobalCharacter.X;
                        float dy = nextY - GlobalCharacter.Y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        float minDist = EffectiveCollisionRadius + GlobalCharacter.CollisionRadius;

                        if (dist < minDist)
                        {
                            // עצירה מיידית מול הדמות ונחיתה על הרצפה ללא חדירה!
                            CheckAndResolveCharacterCollision();
                            IsJumping = false;
                            jumpTimer = 0.0f;
                            Z = BaseZ;
                            landingSquishTime = 0.18f;
                            waitTimer = (float)(rand.NextDouble() * 1.5 + 0.5);
                            return;
                        }
                    }

                    // אינטרפולציה ליניארית במישור X, Y
                    X = nextX;
                    Y = nextY;

                    // תנועה פרבולית בציר Z (גובה הקפיצה)
                    float arc = (float)Math.Sin(progress * Math.PI);
                    Z = BaseZ + arc * CurrentJumpHeight;
                }
            }
            else
            {
                // המפלצת על הרצפה וממתינה לקפיצה הבאה
                waitTimer -= dt;
                if (waitTimer <= 0.0f)
                {
                    StartRandomJump();
                }
            }
        }

        /// <summary>
        /// מתחיל קפיצה חדשה לכיוון אקראי
        /// </summary>
        private void StartRandomJump()
        {
            IsJumping = true;
            jumpTimer = 0.0f;
            jumpStartX = X;
            jumpStartY = Y;

            // משך קפיצה וגובה אקראיים מעט לשונות טבעית
            JumpDuration = (float)(rand.NextDouble() * 0.25 + 0.55); // 0.55 - 0.80 שניות
            CurrentJumpHeight = (float)(rand.NextDouble() * 1.0 + 1.4); // 1.4 - 2.4 יחידות גובה
            float jumpDistance = (float)(rand.NextDouble() * 1.8 + 1.2); // 1.2 - 3.0 יחידות מרחק

            // חישוב כיוון קפיצה אקראי (זווית 0 עד 360 מעלות)
            float angleRad = (float)(rand.NextDouble() * 2.0 * Math.PI);

            // בדיקת גבולות העולם: אם המפלצת קרובה לקצה (מעל 28 יחידות), נכוון אותה חזרה למרכז
            float distFromCenter = (float)Math.Sqrt(X * X + Y * Y);
            if (distFromCenter > 25.0f)
            {
                angleRad = (float)Math.Atan2(-Y, -X) + (float)((rand.NextDouble() - 0.5) * 0.8);
            }

            // חישוב יעד הקפיצה
            jumpTargetX = X + (float)Math.Cos(angleRad) * jumpDistance;
            jumpTargetY = Y + (float)Math.Sin(angleRad) * jumpDistance;

            // סיבוב המפלצת לכיוון הקפיצה (עם חזית הפירמידה קדימה)
            RotationAngle = (float)(angleRad * 180.0 / Math.PI) - 90.0f;
        }

        #endregion

        #region ציור גאומטריית הפירמידה (OpenGL Pyramid Rendering)

        /// <summary>
        /// ציור מודל הפירמידה ב-OpenGL.
        /// הפונקציה ממומשת על פי ההורשה מ-Monster ומטפלת גם בהיטל צל מדויק (isForShadow).
        /// </summary>
        /// <param name="isForShadow">האם הציור מיועד להיטל צל</param>
        protected override void DrawCustomGeometry(bool isForShadow)
        {
            float halfW = BaseWidth / 2.0f;
            float h = PyramidHeight;

            // אפקט מתיחה (Stretch) באוויר ומעיכה (Squish) בנחיתה כדי להחיות את המפלצת
            float scaleZ = 1.0f;
            float scaleXY = 1.0f;

            if (IsJumping)
            {
                float progress = jumpTimer / JumpDuration;
                float arc = (float)Math.Sin(progress * Math.PI);
                scaleZ = 1.0f + 0.30f * arc;    // מתיחה לגובה בעת קפיצה
                scaleXY = 1.0f - 0.12f * arc;   // היצרות בבסיס
            }
            else if (landingSquishTime > 0.0f)
            {
                float squishRatio = landingSquishTime / 0.18f;
                scaleZ = 1.0f - 0.25f * squishRatio;  // מעיכה כלפי מטה בנחיתה
                scaleXY = 1.0f + 0.20f * squishRatio; // התרחבות לצדדים
            }

            GL.glPushMatrix();
            GL.glScalef(scaleXY, scaleXY, scaleZ);

            // 1. קודקודי הפירמידה
            // קודקוד עליון (Apex)
            float apexX = 0.0f, apexY = 0.0f, apexZ = h;

            // 4 קודקודי הבסיס
            float v0X = -halfW, v0Y = -halfW, v0Z = 0.0f; // קדמי-שמאלי
            float v1X =  halfW, v1Y = -halfW, v1Z = 0.0f; // קדמי-ימני
            float v2X =  halfW, v2Y =  halfW, v2Z = 0.0f; // אחורי-ימני
            float v3X = -halfW, v3Y =  halfW, v3Z = 0.0f; // אחורי-שמאלי

            // חישוב נורמלים לפאות
            float len = (float)Math.Sqrt(h * h + halfW * halfW);
            float ny = h / len;
            float nz = halfW / len;

            // ==========================================
            // ציור פאות הפירמידה (GL_TRIANGLES)
            // ==========================================
            GL.glBegin(GL.GL_TRIANGLES);

            // --- פאה קדמית ---
            if (!isForShadow)
            {
                GL.glNormal3f(0.0f, -ny, nz);
                GL.glColor3f(ColorR, ColorG, ColorB);
            }
            GL.glVertex3f(apexX, apexY, apexZ);
            GL.glVertex3f(v0X, v0Y, v0Z);
            GL.glVertex3f(v1X, v1Y, v1Z);

            // --- פאה ימנית ---
            if (!isForShadow)
            {
                GL.glNormal3f(ny, 0.0f, nz);
                GL.glColor3f(ColorR * 0.85f, ColorG * 0.85f, ColorB * 0.85f);
            }
            GL.glVertex3f(apexX, apexY, apexZ);
            GL.glVertex3f(v1X, v1Y, v1Z);
            GL.glVertex3f(v2X, v2Y, v2Z);

            // --- פאה אחורית ---
            if (!isForShadow)
            {
                GL.glNormal3f(0.0f, ny, nz);
                GL.glColor3f(ColorR * 0.70f, ColorG * 0.70f, ColorB * 0.70f);
            }
            GL.glVertex3f(apexX, apexY, apexZ);
            GL.glVertex3f(v2X, v2Y, v2Z);
            GL.glVertex3f(v3X, v3Y, v3Z);

            // --- פאה שמאלית ---
            if (!isForShadow)
            {
                GL.glNormal3f(-ny, 0.0f, nz);
                GL.glColor3f(ColorR * 0.80f, ColorG * 0.80f, ColorB * 0.80f);
            }
            GL.glVertex3f(apexX, apexY, apexZ);
            GL.glVertex3f(v3X, v3Y, v3Z);
            GL.glVertex3f(v0X, v0Y, v0Z);

            GL.glEnd();

            // ==========================================
            // בסיס הפירמידה (GL_QUADS)
            // ==========================================
            GL.glBegin(GL.GL_QUADS);
            if (!isForShadow)
            {
                GL.glNormal3f(0.0f, 0.0f, -1.0f);
                GL.glColor3f(ColorR * 0.45f, ColorG * 0.45f, ColorB * 0.45f);
            }
            GL.glVertex3f(v0X, v0Y, v0Z);
            GL.glVertex3f(v3X, v3Y, v3Z);
            GL.glVertex3f(v2X, v2Y, v2Z);
            GL.glVertex3f(v1X, v1Y, v1Z);
            GL.glEnd();

            // ==========================================
            // עין מפלצת מיסטית בחזית הפירמידה
            // ==========================================
            if (!isForShadow)
            {
                DrawEye(halfW, h);
            }

            GL.glPopMatrix();
        }

        /// <summary>
        /// ציור עין מיסטית בחזית הפירמידה
        /// </summary>
        private void DrawEye(float halfW, float h)
        {
            float eyeY = -halfW * 0.52f;
            float eyeZ = h * 0.42f;
            float eyeRadius = 0.16f;

            // לבן העין / רקע זהוב
            GL.glDisable(GL.GL_LIGHTING);
            GL.glColor3f(1.0f, 0.95f, 0.4f);
            GL.glBegin(GL.GL_POLYGON);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI * 2.0 / 8.0;
                GL.glVertex3f((float)Math.Cos(a) * eyeRadius, eyeY, eyeZ + (float)Math.Sin(a) * eyeRadius);
            }
            GL.glEnd();

            // אישון כהה במרכז
            GL.glColor3f(0.1f, 0.05f, 0.05f);
            GL.glBegin(GL.GL_POLYGON);
            for (int i = 0; i < 8; i++)
            {
                double a = i * Math.PI * 2.0 / 8.0;
                GL.glVertex3f((float)Math.Cos(a) * (eyeRadius * 0.48f), eyeY - 0.005f, eyeZ + (float)Math.Sin(a) * (eyeRadius * 0.48f));
            }
            GL.glEnd();
            GL.glEnable(GL.GL_LIGHTING);
        }

        /// <summary>
        /// דריסת DrawMonster לעדכון אוטומטי של תנועת הקפיצות בעת הציור
        /// </summary>
        /// <param name="drawShadow">האם להטיל צל</param>
        public override void DrawMonster(bool drawShadow = true)
        {
            if (AutoUpdate)
            {
                Update();
            }

            base.DrawMonster(drawShadow);
        }

        #endregion
    }

    #region מחלקות עזר ותאימות שמות (Aliases)

    /// <summary>
    /// Alias נוח לתאימות כתיב (TriangleMonster)
    /// </summary>
    public class TriangleMonster : TRINAGLE
    {
        public TriangleMonster(int health, float x = 0, float y = 0) : base(health, x, y) { }
        public TriangleMonster(int health, float x, float y, float r, float g, float b) : base(health, x, y, r, g, b) { }
    }

    /// <summary>
    /// Alias נוח לתאימות כתיב (Triangle)
    /// </summary>
    public class Triangle : TRINAGLE
    {
        public Triangle(int health, float x = 0, float y = 0) : base(health, x, y) { }
        public Triangle(int health, float x, float y, float r, float g, float b) : base(health, x, y, r, g, b) { }
    }

    #endregion
}
