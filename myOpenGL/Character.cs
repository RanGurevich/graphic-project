using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myOpenGL
{
    public class Character : ShadowObject
    {
        private float xPosition;
        private float yPosition;
        private float zPosition;

        public float X => xPosition;
        public float Y => yPosition;
        public float Z => zPosition;

        public bool IsMovingForward;
        public bool IsMovingBackward;
        public bool IsTurningLeft;
        public bool IsTurningRight;
        private bool isWalkingLoopRunning = false;

        /// <summary>
        /// רדיוס ההתנגשות של הדמות (Collision Radius) במישור X, Y
        /// </summary>
        public float CollisionRadius { get; set; } = 0.65f;

        /// <summary>
        /// פונקציית בדיקת תנועה מול מפלצות ומכשולים (currX, currY, targetX, targetY, charRadius)
        /// מחזירה true אם המיקום החדש מותר לתנועה, ו-false אם התנועה נחסמת עקב התנגשות עם מפלצת
        /// </summary>
        public Func<float, float, float, float, float, bool> CanMoveValidator { get; set; }

        /// <summary>
        /// מהירות ההליכה של הדמות (הוכפלה פי 2 מ-0.1f ל-0.2f לפי דרישה)
        /// </summary>
        public float WalkSpeed { get; set; } = 0.2f;

        public async void UpdateMovementLoop()
        {
            if (isWalkingLoopRunning) return;
            isWalkingLoopRunning = true;

            float walkSpeed = WalkSpeed;
            float turnSpeed = 5.0f;
            float swingMaxAngle = 45f;
            float swingSpeed = 0.45f; // התאמת קצב הנפת הידיים והרגליים למהירות הכפולה
            float time = 0;

            while (IsMovingForward || IsMovingBackward || IsTurningLeft || IsTurningRight)
            {
                // Turn
                if (IsTurningLeft) CHARACTER_angle += turnSpeed;
                if (IsTurningRight) CHARACTER_angle -= turnSpeed;

                // Move
                if (IsMovingForward || IsMovingBackward)
                {
                    float moveDir = IsMovingForward ? 1f : -1f;
                    float rad = CHARACTER_angle * (float)Math.PI / 180f;
                    float deltaX = (float)Math.Sin(rad) * walkSpeed * moveDir;
                    float deltaY = -(float)Math.Cos(rad) * walkSpeed * moveDir;

                    float proposedX = xPosition + deltaX;
                    float proposedY = yPosition + deltaY;

                    // בדיקה האם המיקום החדש מותר לתנועה או חסום עקב התנגשות עם מפלצת
                    bool canMoveFull = CanMoveValidator == null || CanMoveValidator(xPosition, yPosition, proposedX, proposedY, CollisionRadius);

                    if (canMoveFull)
                    {
                        xPosition = proposedX;
                        yPosition = proposedY;
                    }
                    else
                    {
                        // בדיקת החלקה (Wall Sliding): אם תנועה באלכסון נחסמת, בדיקה אם ציר X או ציר Y בודד פנוי
                        bool canMoveX = CanMoveValidator != null && CanMoveValidator(xPosition, yPosition, proposedX, yPosition, CollisionRadius);
                        bool canMoveY = CanMoveValidator != null && CanMoveValidator(xPosition, yPosition, xPosition, proposedY, CollisionRadius);

                        if (canMoveX)
                        {
                            xPosition = proposedX;
                        }
                        else if (canMoveY)
                        {
                            yPosition = proposedY;
                        }
                        // אם שני הצירים חסומים - הדמות נעצרת ולא יכולה ללכת קדימה!
                    }

                    // Animation (רק אם הדמות אינה מבצעת כעת אנימציית אגרוף או בעיטה)
                    if (!isAttacking)
                    {
                        float walkDir = IsMovingForward ? 1f : -1f;
                        time += swingSpeed * walkDir;
                        float swing = (float)Math.Sin(time) * swingMaxAngle;

                        // רגליים: הנפה קדימה (ערך שלילי מניף לעבר החזית)
                        LEFT_HIP_angle = -swing;
                        RIGHT_HIP_angle = swing;
                        
                        // כיפוף ברכיים: כשהרגל מונפת לאחור (ערך חיובי), הברך מתכופפת לאחור (40 מעלות)
                        LEFT_KNEE_angle = (-swing > 0) ? 40f : 10f;
                        RIGHT_KNEE_angle = (swing > 0) ? 40f : 10f;

                        // ידיים: תנועה נגדית מדויקת לרגליים (רגל שמאל קדימה -> יד ימין קדימה)
                        LEFT_SHOULDER_angle = swing;
                        RIGHT_SHOULDER_angle = -swing;

                        LEFT_ELBOW_angle = -30f;
                        RIGHT_ELBOW_angle = -30f;
                    }
                }
                else
                {
                    if (!isAttacking)
                    {
                        ResetPose();
                    }
                }

                OnAnimationStep?.Invoke();
                await Task.Delay(16); // ~60 FPS
            }

            if (!isAttacking)
            {
                ResetPose();
            }
            OnAnimationStep?.Invoke();
            isWalkingLoopRunning = false;
        }

        public void ResetPose()
        {
            LEFT_SHOULDER_angle = 10;
            RIGHT_SHOULDER_angle = -10;
            LEFT_HIP_angle = 5;
            RIGHT_HIP_angle = -5;
            LEFT_KNEE_angle = 10;
            RIGHT_KNEE_angle = 10;
            LEFT_ELBOW_angle = -30;
            RIGHT_ELBOW_angle = -30;
            HEAD_angle = 0;
        }

        #region פעולות לחימה ותקיפה (Combat: Punch & Kick)

        /// <summary>
        /// האם הדמות נמצאת כעת במהלך אנימציית תקיפה
        /// </summary>
        public bool IsAttacking => isAttacking;
        private bool isAttacking = false;

        private static readonly Random _attackRand = new Random();

        /// <summary>
        /// פונקציית טיפול בפגיעה במפלצת: (דמות, כמות נזק 15-40) -> מחזירה את המפלצת שנפגעה או null
        /// </summary>
        public Func<Character, int, Monster> AttackHandler { get; set; }

        /// <summary>
        /// ביצוע בעיטה (Kick) - מופעלת על ידי מקש ימני בעכבר.
        /// שילוח בעיטה קדימה לעבר החזית (מול פני הדמות) ופגיעה במפלצת שמולה.
        /// </summary>
        public async void PerformKick()
        {
            if (isAttacking) return;
            isAttacking = true;

            int damage = _attackRand.Next(15, 41);

            try
            {
                // שלב 1: כיווץ והרמת הברך קדימה לקראת שילוח הבעיטה (Windup / Chamber)
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_HIP, -60f, 12),
                    AnimateJoint(JointType.RIGHT_KNEE, 75f, 12),
                    AnimateJoint(JointType.LEFT_HIP, 10f, 12),
                    AnimateJoint(JointType.LEFT_KNEE, 10f, 12),
                    AnimateJoint(JointType.LEFT_SHOULDER, 20f, 12),
                    AnimateJoint(JointType.LEFT_ELBOW, -60f, 12),
                    AnimateJoint(JointType.RIGHT_SHOULDER, -20f, 12),
                    AnimateJoint(JointType.RIGHT_ELBOW, -45f, 12)
                );

                // שלב 2: שילוח הבעיטה ישר קדימה בעוצמה (Snap Kick Strike) לעבר המפלצת שמול הדמות
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_HIP, -85f, 10),
                    AnimateJoint(JointType.RIGHT_KNEE, 0f, 10),
                    AnimateJoint(JointType.LEFT_SHOULDER, 30f, 10),
                    AnimateJoint(JointType.RIGHT_SHOULDER, -30f, 10)
                );

                // שיא הפגיעה של הבעיטה: בדיקת פגיעה מול מפלצת והורדת 15-40 חיים
                AttackHandler?.Invoke(this, damage);

                // השהייה קצרצרה ביותר
                await Task.Delay(4);

                // שלב 3: החזרת הרגל ואיפוס חלק של כל המפרקים (Recovery)
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_HIP, -5f, 14),
                    AnimateJoint(JointType.RIGHT_KNEE, 10f, 14),
                    AnimateJoint(JointType.LEFT_HIP, 5f, 14),
                    AnimateJoint(JointType.LEFT_KNEE, 10f, 14),
                    AnimateJoint(JointType.LEFT_SHOULDER, 10f, 14),
                    AnimateJoint(JointType.LEFT_ELBOW, -30f, 14),
                    AnimateJoint(JointType.RIGHT_SHOULDER, -10f, 14),
                    AnimateJoint(JointType.RIGHT_ELBOW, -30f, 14),
                    AnimateJoint(JointType.HEAD, 0f, 14)
                );
            }
            finally
            {
                ResetPose();
                OnAnimationStep?.Invoke();
                isAttacking = false;
            }
        }

        /// <summary>
        /// ביצוע אגרוף (Punch) - מופעל על ידי מקש שמאלי בעכבר.
        /// שילוח אגרוף קדימה לעבר החזית (מול פני הדמות) ופגיעה במפלצת שמולה.
        /// </summary>
        public async void PerformPunch()
        {
            if (isAttacking) return;
            isAttacking = true;

            int damage = _attackRand.Next(15, 41);

            try
            {
                // שלב 1: דריכת האגרוף לאחור ועמידת קרב (Cock / Windup)
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_SHOULDER, 30f, 10),
                    AnimateJoint(JointType.RIGHT_ELBOW, -80f, 10),
                    AnimateJoint(JointType.LEFT_SHOULDER, -30f, 10),
                    AnimateJoint(JointType.LEFT_ELBOW, -45f, 10),
                    AnimateJoint(JointType.LEFT_HIP, -15f, 10),
                    AnimateJoint(JointType.LEFT_KNEE, 15f, 10),
                    AnimateJoint(JointType.RIGHT_HIP, 15f, 10),
                    AnimateJoint(JointType.RIGHT_KNEE, 15f, 10),
                    AnimateJoint(JointType.HEAD, -5f, 10)
                );

                // שלב 2: שילוח האגרוף הישר קדימה בעוצמה (Straight Punch Impact) לעבר המפלצת שמול הדמות
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_SHOULDER, -85f, 9),
                    AnimateJoint(JointType.RIGHT_ELBOW, -5f, 9),
                    AnimateJoint(JointType.LEFT_SHOULDER, 20f, 9),
                    AnimateJoint(JointType.LEFT_ELBOW, -75f, 9),
                    AnimateJoint(JointType.HEAD, 0f, 9)
                );

                // שיא הפגיעה של האגרוף: בדיקת פגיעה מול מפלצת והורדת 15-40 חיים
                AttackHandler?.Invoke(this, damage);

                // השהייה קצרצרה ביותר
                await Task.Delay(4);

                // שלב 3: החזרת היד ואיפוס כל המפרקים לעמידה רגילה (Recovery)
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_SHOULDER, -10f, 12),
                    AnimateJoint(JointType.RIGHT_ELBOW, -30f, 12),
                    AnimateJoint(JointType.LEFT_SHOULDER, 10f, 12),
                    AnimateJoint(JointType.LEFT_ELBOW, -30f, 12),
                    AnimateJoint(JointType.LEFT_HIP, 5f, 12),
                    AnimateJoint(JointType.LEFT_KNEE, 10f, 12),
                    AnimateJoint(JointType.RIGHT_HIP, -5f, 12),
                    AnimateJoint(JointType.RIGHT_KNEE, 10f, 12),
                    AnimateJoint(JointType.HEAD, 0f, 12)
                );
            }
            finally
            {
                ResetPose();
                OnAnimationStep?.Invoke();
                isAttacking = false;
            }
        }

        #endregion

        // ===== Joint angles =====
        public float CHARACTER_angle;        // whole body rotation around Z
        public float HEAD_angle;             // head turn around Z

        public float LEFT_SHOULDER_angle;    // left upper arm rotation
        public float RIGHT_SHOULDER_angle;   // right upper arm rotation
        public float LEFT_ELBOW_angle;       // left forearm rotation
        public float RIGHT_ELBOW_angle;      // right forearm rotation

        public float LEFT_HIP_angle;         // left thigh rotation
        public float RIGHT_HIP_angle;        // right thigh rotation
        public float LEFT_KNEE_angle;        // left shin rotation
        public float RIGHT_KNEE_angle;       // right shin rotation

        public float alpha;

        // ===== Display lists =====
        uint CHARACTER_LIST, HEAD_LIST;
        uint UPPER_ARM_LIST, LOWER_ARM_LIST;
        uint UPPER_LEG_LIST, LOWER_LEG_LIST;

        uint CHARACTER_SHADOW_LIST, HEAD_SHADOW_LIST;
        uint UPPER_ARM_SHADOW_LIST, LOWER_ARM_SHADOW_LIST;
        uint UPPER_LEG_SHADOW_LIST, LOWER_LEG_SHADOW_LIST;

        // ===== Body dimensions =====
        float bodyRadius, bodyLength;
        float headRadius;
        float neckRadius, neckLength;
        float upperArmRadius, upperArmLength;
        float lowerArmRadius, lowerArmLength;
        float upperLegRadius, upperLegLength;
        float lowerLegRadius, lowerLegLength;

        GLUquadric obj;

        public enum JointType
        {
            CHARACTER,
            HEAD,
            LEFT_SHOULDER,
            RIGHT_SHOULDER,
            LEFT_ELBOW,
            RIGHT_ELBOW,
            LEFT_HIP,
            RIGHT_HIP,
            LEFT_KNEE,
            RIGHT_KNEE
        }

        public event Action OnAnimationStep;

        public async Task AnimateJoint(JointType joint, float targetAngle, int durationMs)
        {
            float startAngle = GetJointAngle(joint);
            float angleDifference = targetAngle - startAngle;
            
            int frameDuration = 4; // אנימציה מהירה וחלקה ב-250 FPS
            int steps = Math.Max(1, durationMs / frameDuration);
            
            float angleStep = angleDifference / steps;

            for (int i = 0; i < steps; i++)
            {
                SetJointAngle(joint, GetJointAngle(joint) + angleStep);
                OnAnimationStep?.Invoke();
                if (steps > 1)
                {
                    await Task.Delay(frameDuration);
                }
            }
            
            // מוודא שהגענו בדיוק לזווית היעד בסוף
            SetJointAngle(joint, targetAngle);
            OnAnimationStep?.Invoke();
        }

        public async Task MoveJointByDegree(JointType joint, float degreeDelta, int durationMs)
        {
            float currentAngle = GetJointAngle(joint);
            float targetAngle = currentAngle + degreeDelta;
            await AnimateJoint(joint, targetAngle, durationMs);
        }

        private float GetJointAngle(JointType joint)
        {
            switch (joint)
            {
                case JointType.CHARACTER: return CHARACTER_angle;
                case JointType.HEAD: return HEAD_angle;
                case JointType.LEFT_SHOULDER: return LEFT_SHOULDER_angle;
                case JointType.RIGHT_SHOULDER: return RIGHT_SHOULDER_angle;
                case JointType.LEFT_ELBOW: return LEFT_ELBOW_angle;
                case JointType.RIGHT_ELBOW: return RIGHT_ELBOW_angle;
                case JointType.LEFT_HIP: return LEFT_HIP_angle;
                case JointType.RIGHT_HIP: return RIGHT_HIP_angle;
                case JointType.LEFT_KNEE: return LEFT_KNEE_angle;
                case JointType.RIGHT_KNEE: return RIGHT_KNEE_angle;
                default: return 0;
            }
        }

        private void SetJointAngle(JointType joint, float angle)
        {
            switch (joint)
            {
                case JointType.CHARACTER: CHARACTER_angle = angle; break;
                case JointType.HEAD: HEAD_angle = angle; break;
                case JointType.LEFT_SHOULDER: LEFT_SHOULDER_angle = angle; break;
                case JointType.RIGHT_SHOULDER: RIGHT_SHOULDER_angle = angle; break;
                case JointType.LEFT_ELBOW: LEFT_ELBOW_angle = angle; break;
                case JointType.RIGHT_ELBOW: RIGHT_ELBOW_angle = angle; break;
                case JointType.LEFT_HIP: LEFT_HIP_angle = angle; break;
                case JointType.RIGHT_HIP: RIGHT_HIP_angle = angle; break;
                case JointType.LEFT_KNEE: LEFT_KNEE_angle = angle; break;
                case JointType.RIGHT_KNEE: RIGHT_KNEE_angle = angle; break;
            }
        }

        public Character(float xPosition, float yPosition, float zPosition)
        {
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
        }

        bool isInitialized = false;

        public void PrepareLists()
        {
            if (!isInitialized)
            {
                isInitialized = true;

                // --- Body proportions (half scale) ---
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

                // --- Default joint angles ---
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

                // Allocate 12 consecutive display lists (6 for colored model, 6 for shadow)
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
                // head sphere (glowing ice-blue)
                GL.glColor3f(0.7f, 0.9f, 1.0f);
                GLU.gluSphere(obj, headRadius, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== UPPER ARM LIST ==========
                GL.glPushMatrix();
                GL.glNewList(UPPER_ARM_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.95f, 0.0f, 0.6f);              // neon magenta
                GLU.gluCylinder(obj, upperArmRadius, upperArmRadius, upperArmLength, 20, 20);
                GL.glTranslated(0, 0, upperArmLength);
                GL.glColor3f(0.0f, 1.0f, 0.4f);                // neon green joint
                GLU.gluSphere(obj, upperArmRadius * 1.3f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== LOWER ARM LIST ==========
                GL.glPushMatrix();
                GL.glNewList(LOWER_ARM_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.85f, 0.0f, 0.5f);              // deep magenta
                GLU.gluCylinder(obj, lowerArmRadius, lowerArmRadius, lowerArmLength, 20, 20);
                GL.glTranslated(0, 0, lowerArmLength);
                GL.glColor3f(1.0f, 0.5f, 0.0f);                // neon orange hand
                GLU.gluSphere(obj, lowerArmRadius * 1.5f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== UPPER LEG LIST ==========
                GL.glPushMatrix();
                GL.glNewList(UPPER_LEG_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.4f, 0.0f, 0.8f);               // electric purple
                GLU.gluCylinder(obj, upperLegRadius, upperLegRadius, upperLegLength, 20, 20);
                GL.glTranslated(0, 0, upperLegLength);
                GL.glColor3f(0.0f, 1.0f, 0.4f);                // neon green joint
                GLU.gluSphere(obj, upperLegRadius * 1.2f, 20, 20);
                GL.glEndList();
                GL.glPopMatrix();

                // ========== LOWER LEG LIST ==========
                GL.glPushMatrix();
                GL.glNewList(LOWER_LEG_LIST, GL.GL_COMPILE);
                GL.glColor3f(0.3f, 0.0f, 0.7f);               // deep purple
                GLU.gluCylinder(obj, lowerLegRadius, lowerLegRadius, lowerLegLength, 20, 20);
                GL.glTranslated(0, 0, lowerLegLength);
                GL.glColor3f(1.0f, 0.5f, 0.0f);                // neon orange boot
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

        public void DrawCube()
        {
            // cube
            GL.glBegin(GL.GL_QUADS);

            //1

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            //2

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);


            //3

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);


            //4

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);


            //5

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);


            //6

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);


            GL.glEnd();

        }

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

            // ===== Enable lighting for the character =====
            GL.glEnable(GL.GL_LIGHTING);
            GL.glEnable(GL.GL_LIGHT0);
            GL.glEnable(GL.GL_NORMALIZE);

            // Light position (above-right-front)
            float[] lightPos = LightPosition ?? GlobalLightPosition;
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_POSITION, lightPos);

            // Light properties
            float[] ambientLight = { 0.2f, 0.2f, 0.25f, 1.0f };
            float[] diffuseLight = { 0.9f, 0.9f, 1.0f, 1.0f };
            float[] specularLight = { 1.0f, 1.0f, 1.0f, 1.0f };
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_AMBIENT, ambientLight);
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_DIFFUSE, diffuseLight);
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_SPECULAR, specularLight);

            // Enable color material: glColor3f will set both ambient+diffuse material
            GL.glEnable(0x0B57); // GL_COLOR_MATERIAL = 0x0B57
            GL.glColorMaterial(GL.GL_FRONT_AND_BACK, GL.GL_AMBIENT_AND_DIFFUSE);

            // Add specular shininess to all surfaces
            float[] matSpecular = { 0.6f, 0.6f, 0.7f, 1.0f };
            GL.glMaterialfv(GL.GL_FRONT_AND_BACK, GL.GL_SPECULAR, matSpecular);
            GL.glMaterialf(GL.GL_FRONT_AND_BACK, GL.GL_SHININESS, 60.0f);

            // ===== Draw the character geometry =====
            DrawGeometry(false);

            // ===== Disable lighting so it doesn't affect the rest of the scene =====
            GL.glDisable(0x0B57); // GL_COLOR_MATERIAL
            GL.glDisable(GL.GL_LIGHT0);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_NORMALIZE);
        }
    }
}
