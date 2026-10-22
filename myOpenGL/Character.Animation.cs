using System;
using System.Threading.Tasks;

namespace myOpenGL
{
    /// <summary>
    /// חלק מחלקת Character המטפל במערכת המפרקים והאנימציה (Joint Hierarchy & Keyframe Interpolation):
    /// הגדרת זוויות המפרקים של הראש, הזרועות, האמות, הירכיים והשוקיים,
    /// אינטרפולציה חלקה של זוויות המפרקים (AnimateJoint) ושיגור אירועי אנימציה.
    /// </summary>
    public partial class Character
    {
        #region זוויות מפרקים (Joint Angles)

        public float CHARACTER_angle;        // סיבוב הגוף כולו סביב ציר Z
        public float HEAD_angle;             // סיבוב הראש סביב ציר Z

        public float LEFT_SHOULDER_angle;    // סיבוב זרוע שמאל
        public float RIGHT_SHOULDER_angle;   // סיבוב זרוע ימין
        public float LEFT_ELBOW_angle;       // כיפוף אמת יד שמאל
        public float RIGHT_ELBOW_angle;      // כיפוף אמת יד ימין

        public float LEFT_HIP_angle;         // סיבוב ירך שמאל
        public float RIGHT_HIP_angle;        // סיבוב ירך ימין
        public float LEFT_KNEE_angle;        // כיפוף שוק שמאל (ברך)
        public float RIGHT_KNEE_angle;       // כיפוף שוק ימין (ברך)

        public float alpha;

        #endregion

        #region הגדרת טיפוסי מפרקים (JointType Enum)

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

        #endregion

        #region אירועי אנימציה (Animation Events)

        /// <summary>
        /// אירוע המופעל בכל פריים של אנימציית מפרקים או הליכה (משמש לרענון הציור ב-DrawGame)
        /// </summary>
        public event Action OnAnimationStep;

        #endregion

        #region מתודות אנימציית מפרקים (Joint Animation Methods)

        /// <summary>
        /// מנפיש מפרק מסוים לעבר זווית יעד באופן רציף וחלק לאורך משך זמן נתון במילישניות
        /// </summary>
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

        /// <summary>
        /// מזיז מפרק בשינוי מעלות יחסי (Delta) לאורך זמן נתון
        /// </summary>
        public async Task MoveJointByDegree(JointType joint, float degreeDelta, int durationMs)
        {
            float currentAngle = GetJointAngle(joint);
            float targetAngle = currentAngle + degreeDelta;
            await AnimateJoint(joint, targetAngle, durationMs);
        }

        /// <summary>
        /// קבלת הזווית הנוכחית של מפרק נבחר
        /// </summary>
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

        /// <summary>
        /// הגדרת הזווית של מפרק נבחר
        /// </summary>
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

        #endregion
    }
}
