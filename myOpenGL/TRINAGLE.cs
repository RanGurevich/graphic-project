using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// מפלצת פירמידה (TRINAGLE) היורשת ממחלקת Monster (ו-ShadowObject).
    /// המפלצת מיוצגת על ידי פירמידה תלת-ממדית פשוטה ונקייה של OpenGL,
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

        #region מאפייני טקסטורה (Texture Support via TextureManager)

        /// <summary>
        /// האם להשתמש בטקסטורה בעת ציור הפירמידה (במידה ונטענה)
        /// </summary>
        public bool UseTexture { get; set; } = false;

        /// <summary>
        /// מזהה הטקסטורה של הפירמידה ב-OpenGL (נטענת באמצעות TextureManager)
        /// </summary>
        public uint TextureId { get; set; } = 0;

        /// <summary>
        /// טוען טקסטורה מותאמת לפירמידה מתוך קובץ באמצעות מחלקת התשתית TextureManager
        /// </summary>
        /// <param name="texturePath">נתיב קובץ התמונה (למשל: pics\pyramid.png)</param>
        public void LoadTexture(string texturePath)
        {
            TextureId = TextureManager.LoadTexture(texturePath);
            UseTexture = (TextureId != 0);
        }

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
            if (!MonsterPhysics.TryGetDeltaTime(ref lastUpdateTime, out float dt)) return;
            UpdatePhysics(dt);
        }

        /// <summary>
        /// מעדכן את מיקום הקפיצה של הפירמידה בזמן אמת
        /// </summary>
        public void UpdatePhysics(float dt)
        {
            if (!IsAlive) return;

            MonsterPhysics.UpdateTimer(ref landingSquishTime, dt);

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
                    // חישוב מיקום מיועד ובדיקה שאינו חודר לדמות לאורך מסלול הקפיצה
                    float nextX = MonsterPhysics.Lerp(jumpStartX, jumpTargetX, progress);
                    float nextY = MonsterPhysics.Lerp(jumpStartY, jumpTargetY, progress);

                    if (MonsterPhysics.WillCollideAlongPath(nextX, nextY, EffectiveCollisionRadius))
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

                    // אינטרפולציה ליניארית במישור X, Y ותנועה פרבולית בציר Z (גובה הקפיצה)
                    X = nextX;
                    Y = nextY;
                    Z = BaseZ + MonsterPhysics.CalculateJumpArc(progress, CurrentJumpHeight);
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

            // בחירת יעד אקראי במרחק 1.2 - 3.0 יחידות תוך שמירה על גבולות ה-Skybox
            MonsterPhysics.PickRandomWaypoint(
                jumpStartX, jumpStartY,
                1.2f, 3.0f,
                rand,
                out jumpTargetX, out jumpTargetY,
                0.45f);

            // סיבוב הפירמידה לכיוון הקפיצה (חזית הפירמידה פונה לכיוון התנועה)
            if (MonsterPhysics.TryCalculateFaceAngle(jumpStartX, jumpStartY, jumpTargetX, jumpTargetY, out float angle))
            {
                RotationAngle = angle;
            }
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
                float arc = MonsterPhysics.CalculateJumpArc(progress, 1.0f);
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

            bool bindTex = UseTexture && TextureId != 0 && !isForShadow;
            if (bindTex)
            {
                TextureManager.Bind(TextureId);
            }

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
            if (bindTex) GL.glTexCoord2f(0.5f, 1.0f);
            GL.glVertex3f(apexX, apexY, apexZ);
            if (bindTex) GL.glTexCoord2f(0.0f, 0.0f);
            GL.glVertex3f(v0X, v0Y, v0Z);
            if (bindTex) GL.glTexCoord2f(1.0f, 0.0f);
            GL.glVertex3f(v1X, v1Y, v1Z);

            // --- פאה ימנית ---
            if (!isForShadow)
            {
                GL.glNormal3f(ny, 0.0f, nz);
                GL.glColor3f(ColorR * 0.85f, ColorG * 0.85f, ColorB * 0.85f);
            }
            if (bindTex) GL.glTexCoord2f(0.5f, 1.0f);
            GL.glVertex3f(apexX, apexY, apexZ);
            if (bindTex) GL.glTexCoord2f(0.0f, 0.0f);
            GL.glVertex3f(v1X, v1Y, v1Z);
            if (bindTex) GL.glTexCoord2f(1.0f, 0.0f);
            GL.glVertex3f(v2X, v2Y, v2Z);

            // --- פאה אחורית ---
            if (!isForShadow)
            {
                GL.glNormal3f(0.0f, ny, nz);
                GL.glColor3f(ColorR * 0.70f, ColorG * 0.70f, ColorB * 0.70f);
            }
            if (bindTex) GL.glTexCoord2f(0.5f, 1.0f);
            GL.glVertex3f(apexX, apexY, apexZ);
            if (bindTex) GL.glTexCoord2f(0.0f, 0.0f);
            GL.glVertex3f(v2X, v2Y, v2Z);
            if (bindTex) GL.glTexCoord2f(1.0f, 0.0f);
            GL.glVertex3f(v3X, v3Y, v3Z);

            // --- פאה שמאלית ---
            if (!isForShadow)
            {
                GL.glNormal3f(-ny, 0.0f, nz);
                GL.glColor3f(ColorR * 0.80f, ColorG * 0.80f, ColorB * 0.80f);
            }
            if (bindTex) GL.glTexCoord2f(0.5f, 1.0f);
            GL.glVertex3f(apexX, apexY, apexZ);
            if (bindTex) GL.glTexCoord2f(0.0f, 0.0f);
            GL.glVertex3f(v3X, v3Y, v3Z);
            if (bindTex) GL.glTexCoord2f(1.0f, 0.0f);
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
            if (bindTex) GL.glTexCoord2f(0.0f, 0.0f);
            GL.glVertex3f(v0X, v0Y, v0Z);
            if (bindTex) GL.glTexCoord2f(0.0f, 1.0f);
            GL.glVertex3f(v3X, v3Y, v3Z);
            if (bindTex) GL.glTexCoord2f(1.0f, 1.0f);
            GL.glVertex3f(v2X, v2Y, v2Z);
            if (bindTex) GL.glTexCoord2f(1.0f, 0.0f);
            GL.glVertex3f(v1X, v1Y, v1Z);
            GL.glEnd();

            if (bindTex)
            {
                TextureManager.Unbind();
            }

            GL.glPopMatrix();
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
