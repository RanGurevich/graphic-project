using System;
using System.Threading.Tasks;

namespace myOpenGL
{
    /// <summary>
    /// חלק מחלקת Character המטפל בתנועה במרחב (Locomotion & Movement Loop),
    /// בדיקת התנגשויות מול מפלצות ומכשולים, החלקת קירות (Wall Sliding),
    /// ואנימציית הנפת ידיים ורגליים בעת הליכה.
    /// </summary>
    public partial class Character
    {
        #region משתני ומאפייני תנועה (Movement State)

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

        #endregion

        #region לולאת תנועה והליכה (Movement Loop & Animation)

        /// <summary>
        /// מנהל את לולאת ההליכה הרציפה של הדמות כל עוד מקשי התנועה לחוצים:
        /// מעדכן סיבוב, מיקום במרחב, בדיקת התנגשויות והנפת איברים טבעית ב-60 FPS.
        /// </summary>
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
                // סיבוב ימינה ושמאלה
                if (IsTurningLeft) CHARACTER_angle += turnSpeed;
                if (IsTurningRight) CHARACTER_angle -= turnSpeed;

                // תנועה קדימה ואחורה
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
                        // אם שני הצירים חסומים - הדמות נעצרת ולא יכולה ללכת קדימה
                    }

                    // אנימציית הליכה (רק אם הדמות אינה מבצעת כעת אנימציית אגרוף או בעיטה)
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

        /// <summary>
        /// מאפס את כל מפרקי הדמות לעמידת מוצא רגילה
        /// </summary>
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

        #endregion
    }
}
