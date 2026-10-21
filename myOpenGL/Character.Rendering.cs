using OpenGL;
using System;

namespace myOpenGL
{
    /// <summary>
    /// חלק מחלקת Character המטפל בציור ורינדור הדמות (Rendering & Display Lists):
    /// מידות הגוף, יצירת והרכבת היררכיית מודל הדמות (Torso, Head, Arms, Legs),
    /// ציור גאומטריה רגילה, ציור עבור השתקפות באגם, הטלת צל (Shadow Projections),
    /// והגדרת תאורה וחומרים (GL_COLOR_MATERIAL, Specular Shininess).
    /// </summary>
    public partial class Character
    {
        #region שדות גאומטריה ורשימות ציור (Display Lists & Geometry)

        // רשימות ציור רגילות (מודל צבעוני)
        private uint CHARACTER_LIST, HEAD_LIST;
        private uint UPPER_ARM_LIST, LOWER_ARM_LIST;
        private uint UPPER_LEG_LIST, LOWER_LEG_LIST;

        // רשימות ציור לצל (גאומטריה נקייה ללא צבעים)
        private uint CHARACTER_SHADOW_LIST, HEAD_SHADOW_LIST;
        private uint UPPER_ARM_SHADOW_LIST, LOWER_ARM_SHADOW_LIST;
        private uint UPPER_LEG_SHADOW_LIST, LOWER_LEG_SHADOW_LIST;

        // ממדי איברי הגוף
        private float bodyRadius, bodyLength;
        private float headRadius;
        private float neckRadius, neckLength;
        private float upperArmRadius, upperArmLength;
        private float lowerArmRadius, lowerArmLength;
        private float upperLegRadius, upperLegLength;
        private float lowerLegRadius, lowerLegLength;

        private GLUquadric obj;
        private bool isInitialized = false;

        #endregion

        #region יצירת והקצאת רשימות ציור (Display List Initialization)

        /// <summary>
        /// מאתחל ומקצה את רשימות הציור (Display Lists) עבור כל חלקי הגוף והצל
        /// </summary>
        public void PrepareLists()
        {
            if (!isInitialized)
            {
                isInitialized = true;

                // --- ממדי הגוף (קנה מידה חצי) ---
                bodyRadius = 0.2f;
                bodyLength = 1.0f;
                headRadius = 0.175f;
                neckRadius = 0.06f;
                neckLength = 0.1f;
                upperArmRadius = 0.06f;
                upperArmLength = 0.6f;
                lowerArmRadius = 0.05f;
                lowerArmLength = 0.5f;
                upperLegRadius = 0.075f;
                upperLegLength = 0.65f;
                lowerLegRadius = 0.06f;
                lowerLegLength = 0.6f;

                // --- זוויות מפרקים ברירת מחדל ---
                CHARACTER_angle = 0;
                HEAD_angle = 0;
                LEFT_SHOULDER_angle = 10;
                RIGHT_SHOULDER_angle = -10;
                LEFT_ELBOW_angle = -30;
                RIGHT_ELBOW_angle = -30;
                LEFT_HIP_angle = 5;
                RIGHT_HIP_angle = -5;
                LEFT_KNEE_angle = 10;
                RIGHT_KNEE_angle = 10;

                obj = GLU.gluNewQuadric();

                // הקצאת 12 רשימות ציור עוקבות (6 למודל צבעוני, 6 לצל)
                CHARACTER_LIST = GL.glGenLists(12);
                HEAD_LIST = CHARACTER_LIST + 1;
                UPPER_ARM_LIST = CHARACTER_LIST + 2;
                LOWER_ARM_LIST = CHARACTER_LIST + 3;
                UPPER_LEG_LIST = CHARACTER_LIST + 4;
                LOWER_LEG_LIST = CHARACTER_LIST + 5;

                CHARACTER_SHADOW_LIST = CHARACTER_LIST + 6;
                HEAD_SHADOW_LIST = CHARACTER_LIST + 7;
                UPPER_ARM_SHADOW_LIST = CHARACTER_LIST + 8;
                LOWER_ARM_SHADOW_LIST = CHARACTER_LIST + 9;
                UPPER_LEG_SHADOW_LIST = CHARACTER_LIST + 10;
                LOWER_LEG_SHADOW_LIST = CHARACTER_LIST + 11;

                // ========== HEAD LIST ==========
                GL.glPushMatrix();
                GL.glNewList(HEAD_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.7f, 0.9f, 1.0f); // head sphere (glowing ice-blue)
                GLU.gluSphere(obj, headRadius, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== UPPER ARM LIST ==========
                GL.glPushMatrix();
                GL.glNewList(UPPER_ARM_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.95f, 0.0f, 0.6f); // neon magenta
                GLU.gluCylinder(obj, upperArmRadius, upperArmRadius, upperArmLength, 20, 20);
                GL.glTranslated(0, 0, upperArmLength);
                GL.glColor3f(0.0f, 1.0f, 0.4f); // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== LOWER ARM LIST ==========
                GL.glPushMatrix();
                GL.glNewList(LOWER_ARM_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.85f, 0.0f, 0.5f); // deep magenta
                GLU.gluCylinder(obj, lowerArmRadius, lowerArmRadius, lowerArmLength, 20, 20);
                GL.glTranslated(0, 0, lowerArmLength);
                GL.glColor3f(1.0f, 0.5f, 0.0f); // neon orange hand
                GLU.gluSphere(obj, lowerArmRadius * 1.5f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== UPPER LEG LIST ==========
                GL.glPushMatrix();
                GL.glNewList(UPPER_LEG_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.4f, 0.0f, 0.8f); // electric purple
                GLU.gluCylinder(obj, upperLegRadius, upperLegRadius, upperLegLength, 20, 20);
                GL.glTranslated(0, 0, upperLegLength);
                GL.glColor3f(0.0f, 1.0f, 0.4f); // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.2f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== LOWER LEG LIST ==========
                GL.glPushMatrix();
                GL.glNewList(LOWER_LEG_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.3f, 0.0f, 0.7f); // deep purple
                GLU.gluCylinder(obj, lowerLegRadius, lowerLegRadius, lowerLegLength, 20, 20);
                GL.glTranslated(0, 0, lowerLegLength);
                GL.glColor3f(1.0f, 0.5f, 0.0f); // neon orange boot
                GL.glPushMatrix();
                GL.glScalef(1.0f, 0.6f, 1.5f);
                GLU.gluSphere(obj, lowerLegRadius * 1.3f, 20, 20);
                GL.glPopMatrix();
                GL.glEndList();
                GL.glPopMatrix();

                // ========================================================
                // רשימות צל ייעודיות - גאומטריה נקייה לחלוטין ללא קריאות glColor
                // מאפשר הטלת צל שחור שקוף אחיד על פני כל חלקי הגוף
                // ========================================================
                GL.glPushMatrix();
                GL.glNewList(HEAD_SHADOW_LIST, GL.GL_COMPILE);
                GLU.gluSphere(obj, headRadius, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                GL.glPushMatrix();
                GL.glNewList(UPPER_ARM_SHADOW_LIST, GL.GL_COMPILE);
                GLU.gluCylinder(obj, upperArmRadius, upperArmRadius, upperArmLength, 20, 20);
                GL.glTranslated(0, 0, upperArmLength);
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                GL.glPushMatrix();
                GL.glNewList(LOWER_ARM_SHADOW_LIST, GL.GL_COMPILE);
                GLU.gluCylinder(obj, lowerArmRadius, lowerArmRadius, lowerArmLength, 20, 20);
                GL.glTranslated(0, 0, lowerArmLength);
                GLU.gluSphere(obj, lowerArmRadius * 1.5f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                GL.glPushMatrix();
                GL.glNewList(UPPER_LEG_SHADOW_LIST, GL.GL_COMPILE);
                GLU.gluCylinder(obj, upperLegRadius, upperLegRadius, upperLegLength, 20, 20);
                GL.glTranslated(0, 0, upperLegLength);
                GLU.gluSphere(obj, upperLegRadius * 1.2f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                GL.glPushMatrix();
                GL.glNewList(LOWER_LEG_SHADOW_LIST, GL.GL_COMPILE);
                GLU.gluCylinder(obj, lowerLegRadius, lowerLegRadius, lowerLegLength, 20, 20);
                GL.glTranslated(0, 0, lowerLegLength);
                GL.glPushMatrix();
                GL.glScalef(1.0f, 0.6f, 1.5f);
                GLU.gluSphere(obj, lowerLegRadius * 1.3f, 20, 20);
                GL.glPopMatrix();
                GL.glEndList();
                GL.glPopMatrix();
            }

            CreateCharacterList();
        }

        /// <summary>
        /// בונה מחדש את רשימת העל של הדמות (מודל וצל) בהתאם לזוויות המפרקים הנוכחיות
        /// </summary>
        public void CreateCharacterList()
        {
            // 1. רשימת הציור הרגילה עם צבעים
            GL.glPushMatrix();
            GL.glNewList(CHARACTER_LIST, GL.GL_COMPILE);
            BuildCharacterHierarchy(false);
            GL.glEndList();
            GL.glPopMatrix();

            // 2. רשימת הציור לצל - אותה גאומטריה ללא צבעים קשיחים
            GL.glPushMatrix();
            GL.glNewList(CHARACTER_SHADOW_LIST, GL.GL_COMPILE);
            BuildCharacterHierarchy(true);
            GL.glEndList();
            GL.glPopMatrix();
        }

        #endregion

        #region הרכבת היררכיית חלקי הגוף (Hierarchical Body Tree)

        private void BuildCharacterHierarchy(bool isForShadow)
        {
            // ==========================================
            //  BODY (TORSO) — root of the hierarchy
            // ==========================================
            if (!isForShadow) GL.glColor3f(0.0f, 0.9f, 0.9f);               // neon cyan armor
            GLU.gluCylinder(obj, bodyRadius, bodyRadius * 0.85f, bodyLength, 20, 20);
            if (!isForShadow) GL.glColor3f(0.0f, 0.7f, 0.7f);               // darker cyan belt
            GLU.gluDisk(obj, 0, bodyRadius, 20, 20);

            // ==========================================
            //  HEAD  (child of body)
            // ==========================================
            GL.glPushMatrix();
                GL.glTranslated(0, 0, bodyLength);
                if (!isForShadow) GL.glColor3f(0.0f, 0.9f, 0.9f);           // cyan top
                GLU.gluDisk(obj, 0, bodyRadius * 0.85f, 20, 20);
                if (!isForShadow) GL.glColor3f(0.5f, 0.5f, 0.6f);           // metallic silver neck
                GLU.gluCylinder(obj, neckRadius, neckRadius, neckLength, 20, 20);
                GL.glTranslated(0, 0, neckLength + headRadius * 0.7f);
                GL.glRotatef(HEAD_angle, 0, 0, 1);
                GL.glCallList(isForShadow ? HEAD_SHADOW_LIST : HEAD_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  LEFT ARM  (child of body)
            //    └── left forearm  (child of left arm)
            // ==========================================
            GL.glPushMatrix();
                GL.glTranslated(bodyRadius * 0.85f + upperArmRadius, 0, bodyLength - 0.1f);
                if (!isForShadow) GL.glColor3f(0.0f, 1.0f, 0.4f);           // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);

                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(LEFT_SHOULDER_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? UPPER_ARM_SHADOW_LIST : UPPER_ARM_LIST);

                GL.glRotatef(LEFT_ELBOW_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? LOWER_ARM_SHADOW_LIST : LOWER_ARM_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  RIGHT ARM  (child of body)
            //    └── right forearm  (child of right arm)
            // ==========================================
            GL.glPushMatrix();
                GL.glTranslated(-(bodyRadius * 0.85f + upperArmRadius), 0, bodyLength - 0.1f);
                if (!isForShadow) GL.glColor3f(0.0f, 1.0f, 0.4f);           // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);

                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(RIGHT_SHOULDER_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? UPPER_ARM_SHADOW_LIST : UPPER_ARM_LIST);

                GL.glRotatef(RIGHT_ELBOW_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? LOWER_ARM_SHADOW_LIST : LOWER_ARM_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  LEFT LEG  (child of body)
            //    └── left shin  (child of left leg)
            // ==========================================
            GL.glPushMatrix();
                GL.glTranslated(bodyRadius * 0.4f, 0, 0);
                if (!isForShadow) GL.glColor3f(0.0f, 1.0f, 0.4f);           // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.1f, 20, 20);

                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(LEFT_HIP_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? UPPER_LEG_SHADOW_LIST : UPPER_LEG_LIST);

                GL.glRotatef(LEFT_KNEE_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? LOWER_LEG_SHADOW_LIST : LOWER_LEG_LIST);
            GL.glPopMatrix();

            // ==========================================
            //  RIGHT LEG  (child of body)
            //    └── right shin  (child of right leg)
            // ==========================================
            GL.glPushMatrix();
                GL.glTranslated(-(bodyRadius * 0.4f), 0, 0);
                if (!isForShadow) GL.glColor3f(0.0f, 1.0f, 0.4f);           // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.1f, 20, 20);

                GL.glRotatef(180, 1, 0, 0);
                GL.glRotatef(RIGHT_HIP_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? UPPER_LEG_SHADOW_LIST : UPPER_LEG_LIST);

                GL.glRotatef(RIGHT_KNEE_angle, 1, 0, 0);
                GL.glCallList(isForShadow ? LOWER_LEG_SHADOW_LIST : LOWER_LEG_LIST);
            GL.glPopMatrix();
        }

        #endregion

        #region ציור הדמות וגאומטריה (Rendering Pipeline)

        public override void DrawGeometry(bool isForShadow)
        {
            GL.glPushMatrix();
            GL.glTranslatef(xPosition, yPosition, zPosition);
            GL.glRotatef(CHARACTER_angle, 0, 0, 1);
            if (isForShadow)
            {
                GL.glCallList(CHARACTER_SHADOW_LIST);
            }
            else
            {
                GL.glCallList(CHARACTER_LIST);
            }
            GL.glPopMatrix();
        }

        /// <summary>
        /// ציור מלא של הדמות, כולל תאורה ממוקדת, חומרים ספקולריים והטלת צל
        /// </summary>
        public void DrawCharacter(bool drawShadow = true)
        {
            PrepareLists();

            // מעדכן את רשימת הציור כדי להכיל את הזוויות החדשות
            CreateCharacterList();

            // 1. ציור הצל המוטל על הקרקע בצבע שחור מעט שקוף
            if (drawShadow && CastShadow)
            {
                DrawShadow();
            }

            // ===== תאורה מותאמת לדמות =====
            GL.glEnable(GL.GL_LIGHTING);
            GL.glEnable(GL.GL_LIGHT0);
            GL.glEnable(GL.GL_NORMALIZE);

            // מיקום מקור האור
            float[] lightPos = LightPosition ?? GlobalLightPosition;
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_POSITION, lightPos);

            // תכונות האור
            float[] ambientLight = { 0.2f, 0.2f, 0.25f, 1.0f };
            float[] diffuseLight = { 0.9f, 0.9f, 1.0f, 1.0f };
            float[] specularLight = { 1.0f, 1.0f, 1.0f, 1.0f };
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_AMBIENT, ambientLight);
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_DIFFUSE, diffuseLight);
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_SPECULAR, specularLight);

            // הפעלת חומרי צבע
            GL.glEnable(0x0B57); // GL_COLOR_MATERIAL
            GL.glColorMaterial(GL.GL_FRONT_AND_BACK, GL.GL_AMBIENT_AND_DIFFUSE);

            // הוספת ברק ספקולרי למשטחי הדמות
            float[] matSpecular = { 0.6f, 0.6f, 0.7f, 1.0f };
            GL.glMaterialfv(GL.GL_FRONT_AND_BACK, GL.GL_SPECULAR, matSpecular);
            GL.glMaterialf(GL.GL_FRONT_AND_BACK, GL.GL_SHININESS, 60.0f);

            // ===== ציור גאומטריית הדמות =====
            DrawGeometry(false);

            // ===== כיבוי התאורה כדי לא להשפיע על שאר הסצינה =====
            GL.glDisable(0x0B57); // GL_COLOR_MATERIAL
            GL.glDisable(GL.GL_LIGHT0);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_NORMALIZE);
        }

        /// <summary>
        /// ציור הדמות עבור השתקפות במים באגם (Lake.cs)
        /// </summary>
        public override void DrawForReflection()
        {
            if (!IsVisible) return;
            DrawCharacter(false);
        }

        /// <summary>
        /// ציור קוביה רב-צבעונית (עזר)
        /// </summary>
        public void DrawCube()
        {
            GL.glBegin(GL.GL_QUADS);

            // 1
            GL.glColor3f(0.0f, 0.0f, 0.0f); GL.glVertex3f(0.0f, 0.0f, 0.0f);
            GL.glColor3f(0.0f, 1.0f, 0.0f); GL.glVertex3f(0.0f, 1.0f, 0.0f);
            GL.glColor3f(1.0f, 1.0f, 0.0f); GL.glVertex3f(1.0f, 1.0f, 0.0f);
            GL.glColor3f(1.0f, 0.0f, 0.0f); GL.glVertex3f(1.0f, 0.0f, 0.0f);

            // 2
            GL.glColor3f(0.0f, 0.0f, 0.0f); GL.glVertex3f(0.0f, 0.0f, 0.0f);
            GL.glColor3f(0.0f, 0.0f, 1.0f); GL.glVertex3f(0.0f, 0.0f, 1.0f);
            GL.glColor3f(0.0f, 1.0f, 1.0f); GL.glVertex3f(0.0f, 1.0f, 1.0f);
            GL.glColor3f(0.0f, 1.0f, 0.0f); GL.glVertex3f(0.0f, 1.0f, 0.0f);

            // 3
            GL.glColor3f(0.0f, 0.0f, 0.0f); GL.glVertex3f(0.0f, 0.0f, 0.0f);
            GL.glColor3f(1.0f, 0.0f, 0.0f); GL.glVertex3f(1.0f, 0.0f, 0.0f);
            GL.glColor3f(1.0f, 0.0f, 1.0f); GL.glVertex3f(1.0f, 0.0f, 1.0f);
            GL.glColor3f(0.0f, 0.0f, 1.0f); GL.glVertex3f(0.0f, 0.0f, 1.0f);

            // 4
            GL.glColor3f(1.0f, 0.0f, 0.0f); GL.glVertex3f(1.0f, 0.0f, 0.0f);
            GL.glColor3f(1.0f, 0.0f, 1.0f); GL.glVertex3f(1.0f, 0.0f, 1.0f);
            GL.glColor3f(1.0f, 1.0f, 1.0f); GL.glVertex3f(1.0f, 1.0f, 1.0f);
            GL.glColor3f(1.0f, 1.0f, 0.0f); GL.glVertex3f(1.0f, 1.0f, 0.0f);

            // 5
            GL.glColor3f(1.0f, 1.0f, 1.0f); GL.glVertex3f(1.0f, 1.0f, 1.0f);
            GL.glColor3f(1.0f, 1.0f, 0.0f); GL.glVertex3f(1.0f, 1.0f, 0.0f);
            GL.glColor3f(0.0f, 1.0f, 0.0f); GL.glVertex3f(0.0f, 1.0f, 0.0f);
            GL.glColor3f(0.0f, 1.0f, 1.0f); GL.glVertex3f(0.0f, 1.0f, 1.0f);

            // 6
            GL.glColor3f(1.0f, 1.0f, 1.0f); GL.glVertex3f(1.0f, 1.0f, 1.0f);
            GL.glColor3f(0.0f, 1.0f, 1.0f); GL.glVertex3f(0.0f, 1.0f, 1.0f);
            GL.glColor3f(0.0f, 0.0f, 1.0f); GL.glVertex3f(0.0f, 0.0f, 1.0f);
            GL.glColor3f(1.0f, 0.0f, 1.0f); GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glEnd();
        }

        #endregion
    }
}
