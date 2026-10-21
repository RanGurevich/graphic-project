using System;
using System.Collections.Generic;
using System.Drawing;
using myOpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקת אגם (Lake) המייצגת גוף מים טבעי ומעוגל על משטח הרצפה.
    /// האגם מוגדר על ידי מיקום מרכז (CenterX, CenterY) ורדיוס (Radius).
    /// שטח האגם מעוגל לחלוטין וכולל שקיפות מים מלאה על פני כל העיגול,
    /// עם מנגנון השתקפות (Reflection) עבור כל אובייקט OpenGL הנמצא באגם או נמסר למחלקה.
    /// </summary>
    public class Lake
    {
        public float CenterX { get; set; }
        public float CenterY { get; set; }
        public float Radius { get; set; }
        public Grass GrassRef { get; set; }
        public Character CharacterRef { get; set; }

        public bool IsVisible { get; private set; } = true;
        public bool bDepthTest = true;

        /// <summary>
        /// רמת השקיפות של מי האגם על פני כל העיגול (0 = שקוף לחלוטין, 1 = אטום).
        /// מוגדר לשקיפות צלולה כדי שההשתקפויות תיראינה בבירור בכל חלקי העיגול.
        /// </summary>
        public float WaterAlpha { get; set; } = 0.38f;

        /// <summary>
        /// רשימת פעולות ציור של אובייקטים שנרשמו להשתקפות באגם
        /// </summary>
        public List<Action> ReflectedObjects { get; } = new List<Action>();

        private DateTime startTime;

        /// <summary>
        /// בנאי ראשי למחלקת Lake המקבל קואורדינטות X, Y, רדיוס והפניה למחלקת Grass.
        /// </summary>
        public Lake(float centerX, float centerY, float radius, Grass grass = null)
        {
            this.CenterX = centerX;
            this.CenterY = centerY;
            this.Radius = radius;
            this.GrassRef = grass;
            this.startTime = DateTime.Now;

            displayLake();
        }

        /// <summary>
        /// בנאי נוסף המקבל קואורדינטות ורדיוס בלבד.
        /// </summary>
        public Lake(float centerX, float centerY, float radius) : this(centerX, centerY, radius, null)
        {
        }

        /// <summary>
        /// מוסיף אובייקט (פעולת ציור של OpenGL) לרשימת ההשתקפויות באגם.
        /// </summary>
        public void AddObject(Action drawAction)
        {
            if (drawAction != null && !ReflectedObjects.Contains(drawAction))
            {
                ReflectedObjects.Add(drawAction);
            }
        }

        /// <summary>
        /// מסיר אובייקט מרשימת ההשתקפויות של האגם.
        /// </summary>
        public void RemoveObject(Action drawAction)
        {
            ReflectedObjects.Remove(drawAction);
        }

        /// <summary>
        /// בודק האם מיקום מסוים (X, Y) נמצא בתוך השטח המעוגל של האגם.
        /// </summary>
        public bool IsInsideLake(float x, float y, float margin = 0f)
        {
            float dx = x - CenterX;
            float dy = y - CenterY;
            float r = Radius + margin;
            return (dx * dx + dy * dy) <= (r * r);
        }

        /// <summary>
        /// מציג את האגם ומסיר את הדשא באזור המעוגל שבו הוא ממוקם.
        /// </summary>
        public void displayLake()
        {
            IsVisible = true;

            if (GrassRef != null)
            {
                GrassRef.RemovePartOfGrass(CenterX, CenterY, Radius);
            }
        }

        /// <summary>
        /// מסתיר את האגם ומשחזר את הדשא באזור.
        /// </summary>
        public void hideLake()
        {
            IsVisible = false;

            if (GrassRef != null)
            {
                GrassRef.RestoreGrass();
            }
        }

        /// <summary>
        /// מצייר עיגול מלא של פני המים בגובה Z הנתון עבור מסכת ה-Stencil או שטח המים.
        /// </summary>
        public void DrawCircularWaterDisk(float radius, float z)
        {
            int segments = 64;
            GL.glBegin(GL.GL_TRIANGLE_FAN);
            GL.glVertex3f(CenterX, CenterY, z);

            for (int i = 0; i <= segments; i++)
            {
                double angle = i * 2.0 * Math.PI / segments;
                float cosA = (float)Math.Cos(angle);
                float sinA = (float)Math.Sin(angle);
                GL.glVertex3f(CenterX + cosA * radius, CenterY + sinA * radius, z);
            }
            GL.glEnd();
        }

        /// <summary>
        /// פונקציה ראשית להשתקפות: מקבלת כל אובייקט OpenGL (פעולת ציור)
        /// ומציגה את ההשתקפות שלו במים של האגם המעוגל, תוך שימוש במיזוג שקיפות
        /// וסינון פאות (Two-Pass Face Culling) בדיוק לפי הטכניקה שהוצגה בקוד.
        /// </summary>
        /// <param name="drawObjectAction">פעולת ציור של האובייקט אותו רוצים לשקף</param>
        /// <param name="drawRealObject">האם לצייר גם את האובייקט האמיתי מעל המים</param>
        public void DrawWithReflection(Action drawObjectAction, bool drawRealObject = true)
        {
            if (!IsVisible || drawObjectAction == null) return;

            float floorZ = WorldSkyBox.FloorHeight;
            float waterZ = floorZ + 0.007f;

            // =========================================================================
            // שלב 1: מסכת Stencil על פי צורת העיגול המדויקת של האגם
            // מבטיח שההשתקפות תיראה אך ורק בתוך גבולות העיגול ולא תזלוג החוצה
            // =========================================================================
            GL.glEnable(GL.GL_STENCIL_TEST);
            GL.glStencilOp(GL.GL_REPLACE, GL.GL_REPLACE, GL.GL_REPLACE);
            GL.glStencilFunc(GL.GL_ALWAYS, 1, 0xFFFFFFFF);
            GL.glColorMask((byte)GL.GL_FALSE, (byte)GL.GL_FALSE, (byte)GL.GL_FALSE, (byte)GL.GL_FALSE);
            GL.glDisable(GL.GL_DEPTH_TEST);

            // רישום העיגול ל-Stencil
            DrawCircularWaterDisk(Radius, waterZ);

            // שחזור כתיבת צבע
            GL.glColorMask((byte)GL.GL_TRUE, (byte)GL.GL_TRUE, (byte)GL.GL_TRUE, (byte)GL.GL_TRUE);

            if (bDepthTest)
                GL.glEnable(GL.GL_DEPTH_TEST);
            else
                GL.glDisable(GL.GL_DEPTH_TEST);

            // =========================================================================
            // שלב 2: ציור ההשתקפות אך ורק בתוך העיגול של האגם (Stencil == 1)
            // =========================================================================
            GL.glStencilFunc(GL.GL_EQUAL, 1, 0xFFFFFFFF);
            GL.glStencilOp(GL.GL_KEEP, GL.GL_KEEP, GL.GL_KEEP);
            GL.glEnable(GL.GL_STENCIL_TEST);

            // שיקוף המודל סביב מישור פני המים בציר Z
            GL.glPushMatrix();
            GL.glTranslatef(0.0f, 0.0f, waterZ);
            GL.glScalef(1.0f, 1.0f, -1.0f); // היפוך גובה בציר Z
            GL.glTranslatef(0.0f, 0.0f, -waterZ);

            // הפעלת מיזוג (Blending) וסינון פאות (Cull Face) בשני מעברים
            // כפי שהוגדר בקוד: FRONT ואז BACK (או BACK ואז FRONT בשיקוף Z)
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);
            GL.glEnable(GL.GL_CULL_FACE);

            GL.glCullFace(GL.GL_BACK);
            drawObjectAction();

            GL.glCullFace(GL.GL_FRONT);
            drawObjectAction();

            GL.glDisable(GL.GL_CULL_FACE);
            GL.glDisable(GL.GL_BLEND);

            GL.glPopMatrix();

            // =========================================================================
            // שלב 3: ציור פני המים המעוגלים עם שקיפות מלאה מעל ההשתקפות
            // =========================================================================
            GL.glDepthMask((byte)GL.GL_FALSE);
            DrawTransparentWater(waterZ);
            GL.glDepthMask((byte)GL.GL_TRUE);

            // סיום השימוש ב-Stencil
            GL.glDisable(GL.GL_STENCIL_TEST);

            // =========================================================================
            // שלב 4: ציור האובייקט האמיתי במקומו המקורי מעל המים (אם נדרש)
            // =========================================================================
            if (drawRealObject)
            {
                drawObjectAction();
            }
        }

        /// <summary>
        /// ציור רצועת החוף הטבעית המקיפה את העיגול ומחברת אותו לדשא שסביבו.
        /// </summary>
        private void DrawShore(float baseZ)
        {
            int segments = 64;
            float shoreInnerR = Radius * 0.98f;
            float shoreOuterR = Radius * 1.06f;

            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_CULL_FACE);
            GL.glEnable(GL.GL_DEPTH_TEST);
            GL.glDepthMask(1);
            GL.glShadeModel(GL.GL_SMOOTH);
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);

            GL.glBegin(GL.GL_QUAD_STRIP);
            for (int i = 0; i <= segments; i++)
            {
                double angle = i * 2.0 * Math.PI / segments;
                float cosA = (float)Math.Cos(angle);
                float sinA = (float)Math.Sin(angle);

                // גוון חול/בוץ רטוב בגבול המים
                GL.glColor4f(0.24f, 0.20f, 0.14f, 0.90f);
                GL.glVertex3f(CenterX + cosA * shoreInnerR, CenterY + sinA * shoreInnerR, baseZ);

                // גוון חול יבש במפגש עם הדשא
                GL.glColor4f(0.38f, 0.32f, 0.20f, 0.85f);
                GL.glVertex3f(CenterX + cosA * shoreOuterR, CenterY + sinA * shoreOuterR, baseZ);
            }
            GL.glEnd();
        }

        /// <summary>
        /// ציור פני המים המעוגלים עם שקיפות מלאה בכל חלקי העיגול, כולל אדוות עדינות ונצנוץ שמש.
        /// שקיפות המים מאפשרת לראות את ההשתקפות באופן צלול ונקי בכל נקודה בעיגול.
        /// </summary>
        private void DrawTransparentWater(float waterZ)
        {
            int segments = 64;
            float time = (float)(DateTime.Now - startTime).TotalSeconds;

            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_CULL_FACE);
            GL.glEnable(GL.GL_DEPTH_TEST);
            GL.glShadeModel(GL.GL_SMOOTH);

            // הפעלת מיזוג (Blending) לשקיפות מלאה בכל שטח העיגול
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);

            // =========================================================================
            // עיגול המים השקוף (Transparent Circular Water Surface)
            // מעבר צבע מים עדין וצלול עם שקיפות על פני כל העיגול
            // =========================================================================
            GL.glBegin(GL.GL_TRIANGLE_FAN);
            // מרכז העיגול - גוון מים תכול-טורקיז צלול ושקוף
            GL.glColor4f(0.06f, 0.42f, 0.74f, WaterAlpha);
            GL.glVertex3f(CenterX, CenterY, waterZ);

            for (int i = 0; i <= segments; i++)
            {
                double angle = i * 2.0 * Math.PI / segments;
                float cosA = (float)Math.Cos(angle);
                float sinA = (float)Math.Sin(angle);

                // קצה העיגול - שקיפות רכה במפגש עם החוף
                GL.glColor4f(0.12f, 0.58f, 0.82f, WaterAlpha * 0.75f);
                GL.glVertex3f(CenterX + cosA * Radius, CenterY + sinA * Radius, waterZ);
            }
            GL.glEnd();

            // =========================================================================
            // אדוות מים דינמיות מתפשטות (Animated Ripples)
            // =========================================================================
            int rippleCount = 3;
            float rippleSpeed = 1.15f;

            for (int r = 0; r < rippleCount; r++)
            {
                float phase = (time * rippleSpeed + r * (Radius / rippleCount)) % Radius;
                if (phase <= 0.2f) continue;

                float progress = phase / Radius;
                float rippleAlpha = (1.0f - progress) * 0.28f;

                float rInner = Math.Max(0.0f, phase - 0.15f);
                float rOuter = phase;

                GL.glBegin(GL.GL_QUAD_STRIP);
                for (int i = 0; i <= segments; i++)
                {
                    double angle = i * 2.0 * Math.PI / segments;
                    float cosA = (float)Math.Cos(angle);
                    float sinA = (float)Math.Sin(angle);

                    GL.glColor4f(0.70f, 0.90f, 1.0f, 0.0f);
                    GL.glVertex3f(CenterX + cosA * rInner, CenterY + sinA * rInner, waterZ + 0.001f);

                    GL.glColor4f(0.85f, 0.96f, 1.0f, rippleAlpha);
                    GL.glVertex3f(CenterX + cosA * rOuter, CenterY + sinA * rOuter, waterZ + 0.001f);
                }
                GL.glEnd();
            }

            // =========================================================================
            // נצנוץ ספקולרי עדין של השמש על פני המים השקופים
            // =========================================================================
            float glintCenterX = CenterX - Radius * 0.22f;
            float glintCenterY = CenterY + Radius * 0.22f;
            float glintR = Radius * 0.38f;

            GL.glBegin(GL.GL_TRIANGLE_FAN);
            GL.glColor4f(1.0f, 1.0f, 1.0f, 0.20f + 0.05f * (float)Math.Sin(time * 3.0f));
            GL.glVertex3f(glintCenterX, glintCenterY, waterZ + 0.002f);

            for (int i = 0; i <= 32; i++)
            {
                double angle = i * 2.0 * Math.PI / 32;
                float cosA = (float)Math.Cos(angle);
                float sinA = (float)Math.Sin(angle);

                GL.glColor4f(0.60f, 0.85f, 1.0f, 0.0f);
                GL.glVertex3f(glintCenterX + cosA * glintR, glintCenterY + sinA * glintR, waterZ + 0.002f);
            }
            GL.glEnd();

            GL.glDisable(GL.GL_BLEND);
            GL.glColor3f(1.0f, 1.0f, 1.0f);
        }

        /// <summary>
        /// ציור אגם מעוגל בסיסי עם מים שקופים.
        /// </summary>
        public void DrawLake()
        {
            if (!IsVisible) return;

            float floorZ = WorldSkyBox.FloorHeight;
            float baseZ = floorZ + 0.004f;
            float waterZ = floorZ + 0.007f;

            DrawShore(baseZ);
            DrawTransparentWater(waterZ);
        }

        /// <summary>
        /// ציור האגם המעוגל וכל ההשתקפויות של האובייקטים שנמצאים באזור האגם.
        /// </summary>
        public void Draw()
        {
            if (!IsVisible) return;

            float floorZ = WorldSkyBox.FloorHeight;
            float baseZ = floorZ + 0.004f;
            float waterZ = floorZ + 0.007f;

            // 1. ציור רצועת החוף המעגלית
            DrawShore(baseZ);

            // 2. בדיקה האם יש אובייקטים שנדרשת עבורם השתקפות במים
            bool hasCharacterNear = CharacterRef != null && IsInsideLake(CharacterRef.X, CharacterRef.Y, Radius * 0.8f);
            bool hasObjectsToReflect = ReflectedObjects.Count > 0 || hasCharacterNear;

            if (hasObjectsToReflect)
            {
                // ציור השתקפות המים עבור כל אובייקט שנרשם או נמצא באגם
                DrawWithReflection(() =>
                {
                    // השתקפות אובייקטים שנרשמו לאגם
                    for (int i = 0; i < ReflectedObjects.Count; i++)
                    {
                        ReflectedObjects[i]?.Invoke();
                    }

                    // השתקפות הדמות כאשר היא נמצאת בשטח המעוגל של האגם
                    if (hasCharacterNear)
                    {
                        CharacterRef.DrawCharacter(false);
                    }
                }, drawRealObject: false);

                // ציור האובייקטים האמיתיים שנרשמו מעל המים (הדמות מצוירת ב-cOGL.DrawAll)
                for (int i = 0; i < ReflectedObjects.Count; i++)
                {
                    ReflectedObjects[i]?.Invoke();
                }
            }
            else
            {
                // אם אין אובייקטים להשתקפות - ציור המים המעוגלים עם שקיפות מלאה
                DrawTransparentWater(waterZ);
            }
        }
    }
}
