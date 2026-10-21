using System;
using System.Collections.Generic;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקת בסיס לאובייקטים בעלי צל (ShadowObject).
    /// כל אובייקט שיורש ממחלקה זו מקבל יכולת להטיל צל שטוח (Planar Shadow)
    /// בצבע שחור מעט שקוף על גבי מישור הרצפה (או כל מישור אחר) באמצעות
    /// מטריצת היטל צל (Shadow Projection Matrix), וכן תמיכה בהשתקפות באגם (Lake)
    /// ומעיכת דשא (Grass) בעת דריכה.
    /// </summary>
    public abstract class ShadowObject
    {
        #region רישום מופעים כללי (Global Instances Registry)

        /// <summary>
        /// רשימת כל מופעי ה-ShadowObject הפעילים בסצינה (למעט הדשא עצמו)
        /// </summary>
        public static List<ShadowObject> AllInstances { get; } = new List<ShadowObject>();

        protected ShadowObject()
        {
            if (!(this is Grass))
            {
                AllInstances.Add(this);
            }
        }

        #endregion

        #region מאפייני מרחב, דריכה והשתקפות (Spatial, Trample & Reflection)

        /// <summary>
        /// מיקום האובייקט בציר X
        /// </summary>
        public virtual float X { get; set; } = 0.0f;

        /// <summary>
        /// מיקום האובייקט בציר Y
        /// </summary>
        public virtual float Y { get; set; } = 0.0f;

        /// <summary>
        /// מיקום האובייקט בציר Z
        /// </summary>
        public virtual float Z { get; set; } = 0.0f;

        /// <summary>
        /// האם האובייקט נראה ופעיל בסצינה
        /// </summary>
        public virtual bool IsVisible { get; set; } = true;

        /// <summary>
        /// רדיוס הדריכה/מעיכה של האובייקט על גבי הדשא (ברירת מחדל: 1.35f, כפי שמבוצע עבור הדמות)
        /// </summary>
        public virtual float TrampleRadius { get; set; } = 1.35f;

        /// <summary>
        /// האם האובייקט דורך כעת על הקרקע/דשא.
        /// תומך הן באובייקטים שבסיסם בגובה הרצפה (FloorHeight = -1.35f) והן באובייקטים שבסיסם במרכז/אגן (Z = 0.0f כגון הדמות)
        /// </summary>
        public virtual bool IsSteppingOnGround
        {
            get
            {
                if (!IsVisible) return false;
                float floorZ = WorldSkyBox.FloorHeight;
                return Z <= floorZ + 0.85f || (Z >= -0.5f && Z <= 0.85f);
            }
        }

        /// <summary>
        /// ציור האובייקט עבור השתקפות במים באגם (Lake.cs).
        /// מבוצע ללא ציור הצל המוטל על הקרקע (DrawShadow), שכן אין להטיל צל הפוך בתוך המים.
        /// מחלקות יורשות דורסות מתודה זו כדי לצייר את המודל שלהן באופן אופטימלי להשתקפות.
        /// </summary>
        public virtual void DrawForReflection()
        {
            if (!IsVisible) return;
            DrawGeometry(false);
        }

        #endregion

        #region משתנים ומאפייני צל ותאורה

        /// <summary>
        /// מיקום מקור האור הכללי בסצינה [X, Y, Z, W]
        /// </summary>
        public static float[] GlobalLightPosition = new float[4] { 5.0f, 5.0f, 10.0f, 1.0f };

        /// <summary>
        /// מיקום מקור אור ספציפי לאובייקט זה (אם null, נעשה שימוש ב-GlobalLightPosition)
        /// </summary>
        public float[] LightPosition { get; set; } = null;

        /// <summary>
        /// 3 נקודות המגדירות את מישור הרצפה ברירת מחדל (נגד כיוון השעון)
        /// מוגבה מעט מעל FloorHeight כדי למנוע הבהוב Z-fighting
        /// </summary>
        public static float[,] DefaultGroundPlane
        {
            get
            {
                float floorZ = WorldSkyBox.FloorHeight + 0.003f;
                return new float[3, 3]
                {
                    { 1.0f, 1.0f, floorZ },
                    { 0.0f, 1.0f, floorZ },
                    { 1.0f, 0.0f, floorZ }
                };
            }
        }

        /// <summary>
        /// מישור יעד להטלת הצל (אם null, נעשה שימוש ב-DefaultGroundPlane)
        /// </summary>
        public float[,] TargetPlane { get; set; } = null;

        /// <summary>
        /// מטריצת טרנספורמציית הצל (4x4)
        /// </summary>
        public float[] cubeXform = new float[16];

        /// <summary>
        /// האם האובייקט מטיל צל
        /// </summary>
        public bool CastShadow { get; set; } = true;

        /// <summary>
        /// רכיבי צבע הצל: שחור מעט שקוף (Semi-transparent black)
        /// </summary>
        public float ShadowR { get; set; } = 0.0f;
        public float ShadowG { get; set; } = 0.0f;
        public float ShadowB { get; set; } = 0.0f;
        public float ShadowAlpha { get; set; } = 0.40f; // 40% אטימות = שחור שקוף ועדין

        #endregion

        #region פונקציות חישוב מטריצת הצל (על פי הטכניקה הנתונה)

        private const int x = 0;
        private const int y = 1;
        private const int z = 2;

        /// <summary>
        /// מנרמל וקטור נורמל לאורך 1
        /// </summary>
        public void ReduceToUnit(float[] vector)
        {
            float length = (float)Math.Sqrt((vector[0] * vector[0]) +
                                            (vector[1] * vector[1]) +
                                            (vector[2] * vector[2]));

            if (length == 0.0f)
                length = 1.0f;

            vector[0] /= length;
            vector[1] /= length;
            vector[2] /= length;
        }

        /// <summary>
        /// מחשב וקטור נורמל מתוך 3 נקודות המוגדרות נגד כיוון השעון
        /// </summary>
        public void calcNormal(float[,] v, float[] outp)
        {
            float[] v1 = new float[3];
            float[] v2 = new float[3];

            // וקטורים משלוש הנקודות
            v1[x] = v[0, x] - v[1, x];
            v1[y] = v[0, y] - v[1, y];
            v1[z] = v[0, z] - v[1, z];

            v2[x] = v[1, x] - v[2, x];
            v2[y] = v[1, y] - v[2, y];
            v2[z] = v[1, z] - v[2, z];

            // מכפלה וקטורית (Cross Product)
            outp[x] = v1[y] * v2[z] - v1[z] * v2[y];
            outp[y] = v1[z] * v2[x] - v1[x] * v2[z];
            outp[z] = v1[x] * v2[y] - v1[y] * v2[x];

            // נירמול הוקטור
            ReduceToUnit(outp);
        }

        /// <summary>
        /// מייצר מטריצת היטל צל (Shadow projection matrix) מתוך נקודות המישור ומקור האור.
        /// התוצאה נשמרת במערך cubeXform.
        /// </summary>
        public void MakeShadowMatrix(float[,] points)
        {
            MakeShadowMatrix(points, LightPosition ?? GlobalLightPosition);
        }

        /// <summary>
        /// מייצר מטריצת היטל צל מתוך נקודות המישור ומקור אור נתון.
        /// </summary>
        public void MakeShadowMatrix(float[,] points, float[] lightPos)
        {
            if (lightPos == null) lightPos = GlobalLightPosition;

            float[] planeCoeff = new float[4];
            float dot;

            // חישוב מקדמי משוואת המישור (הנורמל הוא A, B, C)
            calcNormal(points, planeCoeff);

            // מציאת D ע"י הצבת נקודה במשוואת המישור
            planeCoeff[3] = -(
                (planeCoeff[0] * points[2, 0]) +
                (planeCoeff[1] * points[2, 1]) +
                (planeCoeff[2] * points[2, 2]));

            // מכפלה סקלרית של מקדמי המישור עם מיקום מקור האור
            dot = planeCoeff[0] * lightPos[0] +
                  planeCoeff[1] * lightPos[1] +
                  planeCoeff[2] * lightPos[2] +
                  planeCoeff[3];

            // עמודה ראשונה
            cubeXform[0] = dot - lightPos[0] * planeCoeff[0];
            cubeXform[4] = 0.0f - lightPos[0] * planeCoeff[1];
            cubeXform[8] = 0.0f - lightPos[0] * planeCoeff[2];
            cubeXform[12] = 0.0f - lightPos[0] * planeCoeff[3];

            // עמודה שנייה
            cubeXform[1] = 0.0f - lightPos[1] * planeCoeff[0];
            cubeXform[5] = dot - lightPos[1] * planeCoeff[1];
            cubeXform[9] = 0.0f - lightPos[1] * planeCoeff[2];
            cubeXform[13] = 0.0f - lightPos[1] * planeCoeff[3];

            // עמודה שלישית
            cubeXform[2] = 0.0f - lightPos[2] * planeCoeff[0];
            cubeXform[6] = 0.0f - lightPos[2] * planeCoeff[1];
            cubeXform[10] = dot - lightPos[2] * planeCoeff[2];
            cubeXform[14] = 0.0f - lightPos[2] * planeCoeff[3];

            // עמודה רביעית
            cubeXform[3] = 0.0f - lightPos[3] * planeCoeff[0];
            cubeXform[7] = 0.0f - lightPos[3] * planeCoeff[1];
            cubeXform[11] = 0.0f - lightPos[3] * planeCoeff[2];
            cubeXform[15] = dot - lightPos[3] * planeCoeff[3];
        }

        #endregion

        #region פונקציות ציור צל והיררכיית ציור

        /// <summary>
        /// פונקציה אבסטרקטית שכל מחלקה יורשת מממשת לציור הגאומטריה שלה.
        /// כאשר isForShadow == true, הגאומטריה מצוירת עבור הצל (ללא צבעים/טקסטורות מקוריות).
        /// </summary>
        public abstract void DrawGeometry(bool isForShadow);

        /// <summary>
        /// מטיל ומצייר את צל האובייקט על גבי מישור הרצפה
        /// </summary>
        public virtual void DrawShadow()
        {
            DrawShadow(TargetPlane ?? DefaultGroundPlane, LightPosition ?? GlobalLightPosition);
        }

        /// <summary>
        /// מטיל ומצייר את צל האובייקט על גבי מישור יעד נתון
        /// </summary>
        public virtual void DrawShadow(float[,] targetPlane)
        {
            DrawShadow(targetPlane, LightPosition ?? GlobalLightPosition);
        }

        /// <summary>
        /// מטיל ומצייר את צל האובייקט על גבי מישור יעד נתון ומקור אור נתון
        /// </summary>
        public virtual void DrawShadow(float[,] targetPlane, float[] lightPos)
        {
            if (!CastShadow) return;

            if (targetPlane == null) targetPlane = DefaultGroundPlane;
            if (lightPos == null) lightPos = GlobalLightPosition;

            // 1. חישוב מטריצת היטל הצל לתוך cubeXform
            MakeShadowMatrix(targetPlane, lightPos);

            // 2. שמירת הגדרות OpenGL הנוכחיות
            GL.glPushAttrib(GL.GL_ALL_ATTRIB_BITS);

            // 3. כיבוי תאורה וטקסטורות כדי שהצל ייצבע בצבע הצל בלבד
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_TEXTURE_2D);

            // 4. הפעלת ערבוב אלפא (Alpha Blending) לצבע שחור שקוף
            GL.glEnable(GL.GL_BLEND);
            GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA);

            // 5. קביעת צבע הצל: שחור מעט שקוף
            GL.glColor4f(ShadowR, ShadowG, ShadowB, ShadowAlpha);

            // 6. ביטול כתיבה לעומק והפעלת היסט פוליגונים למניעת הבהובי Z-fighting מול המישור
            GL.glEnable(GL.GL_POLYGON_OFFSET_FILL);
            GL.glPolygonOffset(-1.0f, -1.0f);
            GL.glDepthMask(0);

            // 7. הכפלת המטריצה במטריצת הצל וציור גאומטריית האובייקט
            GL.glPushMatrix();
            GL.glMultMatrixf(cubeXform);

            DrawGeometry(true);

            GL.glPopMatrix();

            // 8. שחזור מצב OpenGL
            GL.glDepthMask(1);
            GL.glDisable(GL.GL_POLYGON_OFFSET_FILL);
            GL.glPopAttrib();
        }

        /// <summary>
        /// מצייר את האובייקט יחד עם הצל שלו
        /// </summary>
        public virtual void DrawWithShadow()
        {
            DrawGeometry(false);
            if (CastShadow)
            {
                DrawShadow();
            }
        }

        #endregion
    }
}
