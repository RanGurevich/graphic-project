using System;
using System.Threading.Tasks;
using OpenGL;

namespace myOpenGL
{
    /// <summary>
    /// חלק מחלקת Character המטפל בפעולות לחימה ותקיפה (Combat System):
    /// ביצוע בעיטה (Kick) ואגרוף (Punch), מהירות מוגברת פי 4,
    /// פגיעה במפלצות וגרימת 45-70 נזק תוך חסימת פעולות תקיפה חופפות.
    /// </summary>
    public partial class Character
    {
        #region משתנים ומאפייני לחימה (Combat State)

        /// <summary>
        /// האם הדמות נמצאת כעת במהלך אנימציית תקיפה פעילה
        /// </summary>
        public bool IsAttacking => isAttacking;
        private bool isAttacking = false;

        private static readonly Random _attackRand = new Random();

        /// <summary>
        /// פונקציית טיפול בפגיעה במפלצת: (דמות, כמות נזק 45-70) -> מחזירה את המפלצת שנפגעה או null
        /// </summary>
        public Func<Character, int, Monster> AttackHandler { get; set; }

        #endregion

        #region מתודות תקיפה: בעיטה ואגרוף (Kick & Punch)

        /// <summary>
        /// ביצוע בעיטה (Kick) - מופעלת על ידי מקש ימני בעכבר.
        /// מהירות מוגברת פי 4, נזק מוגבר 45-70.
        /// שילוח בעיטה קדימה לעבר החזית (מול פני הדמות) ופגיעה במפלצת שמולה.
        /// </summary>
        public async void PerformKick()
        {
            if (isAttacking) return;
            isAttacking = true;

            int damage = _attackRand.Next(45, 71);

            try
            {
                // שלב 1: כיווץ והרמת הברך קדימה לקראת שילוח הבעיטה (Windup / Chamber) - מהיר פי 4
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_HIP, -60f, 3),
                    AnimateJoint(JointType.RIGHT_KNEE, 75f, 3),
                    AnimateJoint(JointType.LEFT_HIP, 10f, 3),
                    AnimateJoint(JointType.LEFT_KNEE, 10f, 3),
                    AnimateJoint(JointType.LEFT_SHOULDER, 20f, 3),
                    AnimateJoint(JointType.LEFT_ELBOW, -60f, 3),
                    AnimateJoint(JointType.RIGHT_SHOULDER, -20f, 3),
                    AnimateJoint(JointType.RIGHT_ELBOW, -45f, 3)
                );
                await Task.Delay(1);

                // שלב 2: שילוח הבעיטה ישר קדימה בעוצמה (Snap Kick Strike) לעבר המפלצת שמול הדמות - מהיר פי 4
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_HIP, -85f, 3),
                    AnimateJoint(JointType.RIGHT_KNEE, 0f, 3),
                    AnimateJoint(JointType.LEFT_SHOULDER, 30f, 3),
                    AnimateJoint(JointType.RIGHT_SHOULDER, -30f, 3)
                );

                // שיא הפגיעה של הבעיטה: בדיקת פגיעה מול מפלצת והורדת 45-70 חיים
                AttackHandler?.Invoke(this, damage);

                // השהיית פגיעה קצרצרה ביותר
                await Task.Delay(1);

                // שלב 3: החזרת הרגל ואיפוס חלק של כל המפרקים (Recovery) - מהיר פי 4
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_HIP, -5f, 3),
                    AnimateJoint(JointType.RIGHT_KNEE, 10f, 3),
                    AnimateJoint(JointType.LEFT_HIP, 5f, 3),
                    AnimateJoint(JointType.LEFT_KNEE, 10f, 3),
                    AnimateJoint(JointType.LEFT_SHOULDER, 10f, 3),
                    AnimateJoint(JointType.LEFT_ELBOW, -30f, 3),
                    AnimateJoint(JointType.RIGHT_SHOULDER, -10f, 3),
                    AnimateJoint(JointType.RIGHT_ELBOW, -30f, 3),
                    AnimateJoint(JointType.HEAD, 0f, 3)
                );
                await Task.Delay(1);
            }
            finally
            {
                if (!IsMovingForward && !IsMovingBackward && !IsTurningLeft && !IsTurningRight)
                {
                    ResetPose();
                }
                OnAnimationStep?.Invoke();
                isAttacking = false;
            }
        }

        /// <summary>
        /// ביצוע אגרוף (Punch) - מופעל על ידי מקש שמאלי בעכבר.
        /// מהירות מוגברת פי 4, נזק מוגבר 45-70.
        /// שילוח אגרוף קדימה לעבר החזית (מול פני הדמות) ופגיעה במפלצת שמולה.
        /// </summary>
        public async void PerformPunch()
        {
            if (isAttacking) return;
            isAttacking = true;

            int damage = _attackRand.Next(45, 71);

            try
            {
                // שלב 1: דריכת האגרוף לאחור ועמידת קרב (Cock / Windup) - מהיר פי 4
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_SHOULDER, 30f, 3),
                    AnimateJoint(JointType.RIGHT_ELBOW, -80f, 3),
                    AnimateJoint(JointType.LEFT_SHOULDER, -30f, 3),
                    AnimateJoint(JointType.LEFT_ELBOW, -45f, 3),
                    AnimateJoint(JointType.LEFT_HIP, -15f, 3),
                    AnimateJoint(JointType.LEFT_KNEE, 15f, 3),
                    AnimateJoint(JointType.RIGHT_HIP, 15f, 3),
                    AnimateJoint(JointType.RIGHT_KNEE, 15f, 3),
                    AnimateJoint(JointType.HEAD, -5f, 3)
                );
                await Task.Delay(1);

                // שלב 2: שילוח האגרוף הישר קדימה בעוצמה (Straight Punch Impact) לעבר המפלצת שמול הדמות - מהיר פי 4
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_SHOULDER, -85f, 2),
                    AnimateJoint(JointType.RIGHT_ELBOW, -5f, 2),
                    AnimateJoint(JointType.LEFT_SHOULDER, 20f, 2),
                    AnimateJoint(JointType.LEFT_ELBOW, -75f, 2),
                    AnimateJoint(JointType.HEAD, 0f, 2)
                );

                // שיא הפגיעה של האגרוף: בדיקת פגיעה מול מפלצת והורדת 45-70 חיים
                AttackHandler?.Invoke(this, damage);

                // השהיית פגיעה קצרצרה ביותר
                await Task.Delay(1);

                // שלב 3: החזרת היד ואיפוס כל המפרקים לעמידה רגילה (Recovery) - מהיר פי 4
                await Task.WhenAll(
                    AnimateJoint(JointType.RIGHT_SHOULDER, -10f, 3),
                    AnimateJoint(JointType.RIGHT_ELBOW, -30f, 3),
                    AnimateJoint(JointType.LEFT_SHOULDER, 10f, 3),
                    AnimateJoint(JointType.LEFT_ELBOW, -30f, 3),
                    AnimateJoint(JointType.LEFT_HIP, 5f, 3),
                    AnimateJoint(JointType.LEFT_KNEE, 10f, 3),
                    AnimateJoint(JointType.RIGHT_HIP, -5f, 3),
                    AnimateJoint(JointType.RIGHT_KNEE, 10f, 3),
                    AnimateJoint(JointType.HEAD, 0f, 3)
                );
                await Task.Delay(1);
            }
            finally
            {
                if (!IsMovingForward && !IsMovingBackward && !IsTurningLeft && !IsTurningRight)
                {
                    ResetPose();
                }
                OnAnimationStep?.Invoke();
                isAttacking = false;
            }
        }

        #endregion
    }
}
