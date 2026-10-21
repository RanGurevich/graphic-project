using System;
using myOpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקה מרכזית לטיפול בפיזיקה, תנועה, קפיצות, ואינטרפולציה של מפלצות (Monster) בעולם ה-OpenGL.
    /// מרכזת ומאחדת קוד משותף שהיה משוכפל בין snail.Physics, mushroom.Physics ו-TRINAGLE:
    /// 1. חישוב זמן דלתא (Delta Time) יציב ומוגן מקפיצות זמן.
    /// 2. חישוב נקודות יעד אקראיות (Random Waypoints) עם הגבלת גבולות עולם (Skybox Bounds).
    /// 3. חישוב זווית כיוון וסיבוב (Heading / Face Angle) אל היעד.
    /// 4. בדיקת התנגשות חזויה מול הדמות הראשית לאורך מסלול התנועה (Path Collision Probing).
    /// 5. אינטרפולציה ליניארית (Lerp) במישור X, Y.
    /// 6. חישוב מסלול קשתי/פרבולי לקפיצות (Parabolic Jump Arc).
    /// 7. עדכון טיימרים (מעיכת נחיתה, זמני המתנה וכו').
    /// </summary>
    public static class MonsterPhysics
    {
        #region חישוב זמן דלתא (Delta Time Management)

        /// <summary>
        /// מחשב את הפרש הזמן (Delta Time) בשניות מהפריים הקודם ומעדכן את זמן הפריים האחרון.
        /// כולל סינון ערכים לא תקינים והגבלת מקסימום למניעת קפיצות פיזיקה (Lag spikes).
        /// </summary>
        /// <param name="lastUpdateTime">משתנה הזמן של הפריים הקודם (מתעדכן לזמן הנוכחי)</param>
        /// <param name="dt">ערך הדלתא המחושב בשניות</param>
        /// <param name="maxDt">הגבלת זמן דלתא מרבית (ברירת מחדל: 0.1 שניות)</param>
        /// <returns>true אם חלף זמן חיובי ויש לעדכן פיזיקה, אחרת false</returns>
        public static bool TryGetDeltaTime(ref DateTime lastUpdateTime, out float dt, float maxDt = 0.1f)
        {
            DateTime now = DateTime.Now;
            dt = (float)(now - lastUpdateTime).TotalSeconds;
            lastUpdateTime = now;

            if (dt > maxDt) dt = maxDt;
            if (dt <= 0.0f)
            {
                dt = 0.0f;
                return false;
            }

            return true;
        }

        #endregion

        #region ניווט וחישוב נקודות יעד (Waypoints & Navigation)

        /// <summary>
        /// מחולל נקודת יעד אקראית במרחק נתון מנקודת המוצא, תוך שמירה הדוקה על גבולות העולם (Skybox Bounds).
        /// </summary>
        /// <param name="startX">מיקום X התחלתי</param>
        /// <param name="startY">מיקום Y התחלתי</param>
        /// <param name="minDistance">מרחק תנועה מינימלי</param>
        /// <param name="maxDistance">מרחק תנועה מקסימלי</param>
        /// <param name="rand">מחולל מספרים אקראיים</param>
        /// <param name="targetX">מיקום X ביעד (מוגבל לגבולות העולם)</param>
        /// <param name="targetY">מיקום Y ביעד (מוגבל לגבולות העולם)</param>
        /// <param name="boundFactor">אחוז מגודל ה-Skybox המשמש כגבול מותר (ברירת מחדל: 0.44)</param>
        /// <returns>מרחק התנועה בפועל אל היעד</returns>
        public static float PickRandomWaypoint(
            float startX, float startY,
            float minDistance, float maxDistance,
            Random rand,
            out float targetX, out float targetY,
            float boundFactor = 0.44f)
        {
            if (rand == null) rand = new Random();

            double angle = rand.NextDouble() * Math.PI * 2.0;
            float distance = (float)(rand.NextDouble() * (maxDistance - minDistance) + minDistance);

            float rawTargetX = startX + (float)Math.Cos(angle) * distance;
            float rawTargetY = startY + (float)Math.Sin(angle) * distance;

            float bound = WorldSkyBox.SkyboxSize * boundFactor;
            targetX = Math.Max(-bound, Math.Min(bound, rawTargetX));
            targetY = Math.Max(-bound, Math.Min(bound, rawTargetY));

            float dx = targetX - startX;
            float dy = targetY - startY;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// מחשב את זווית הסיבוב (Yaw במעלות סביב ציר Z) הדרושה כדי להפנות את חזית המפלצת לעבר נקודת היעד.
        /// חזית המפלצות במערכת הצירים מכוונת לעבר מינוס Y (Atan2(dx, -dy)).
        /// </summary>
        /// <param name="fromX">מיקום X התחלתי</param>
        /// <param name="fromY">מיקום Y התחלתי</param>
        /// <param name="toX">מיקום X ביעד</param>
        /// <param name="toY">מיקום Y ביעד</param>
        /// <param name="angle">זווית הסיבוב במעלות</param>
        /// <returns>true אם יש תנועה משמעותית (dx/dy > 0.01), אחרת false</returns>
        public static bool TryCalculateFaceAngle(float fromX, float fromY, float toX, float toY, out float angle)
        {
            float dx = toX - fromX;
            float dy = toY - fromY;

            if (Math.Abs(dx) > 0.01f || Math.Abs(dy) > 0.01f)
            {
                angle = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
                return true;
            }

            angle = 0.0f;
            return false;
        }

        #endregion

        #region בדיקות התנגשות לאורך המסלול (Collision Probing)

        /// <summary>
        /// בודק האם מיקום מתוכנן הבא של המפלצת יוביל להתנגשות וחדירה בדמות הראשית.
        /// מונע מקרים שבהם המפלצת קופצת או זוחלת ישירות אל תוך השחקן.
        /// </summary>
        /// <param name="nextX">המיקום הבא המתוכנן בציר X</param>
        /// <param name="nextY">המיקום הבא המתוכנן בציר Y</param>
        /// <param name="monsterEffectiveRadius">רדיוס ההתנגשות האפקטיבי של המפלצת</param>
        /// <param name="character">הפניה לדמות הראשית (אם null, נעשה שימוש ב-Monster.GlobalCharacter)</param>
        /// <returns>true אם המיקום יגרום לחדירה/התנגשות בדמות הראשית</returns>
        public static bool WillCollideAlongPath(
            float nextX, float nextY,
            float monsterEffectiveRadius,
            Character character = null)
        {
            Character targetChar = character ?? Monster.GlobalCharacter;
            if (targetChar == null) return false;

            float dx = nextX - targetChar.X;
            float dy = nextY - targetChar.Y;
            float distSq = dx * dx + dy * dy;
            float minDist = monsterEffectiveRadius + targetChar.CollisionRadius;

            return distSq < (minDist * minDist);
        }

        #endregion

        #region אינטרפולציה ופיזיקת קפיצות (Interpolation & Jump Arc)

        /// <summary>
        /// אינטרפולציה ליניארית (Lerp) בין נקודת מוצא לנקודת יעד לפי התקדמות
        /// </summary>
        public static float Lerp(float start, float target, float progress)
        {
            return start + (target - start) * progress;
        }

        /// <summary>
        /// מחשב את גובה הקשת הפרבולית (Parabolic Arc) בקפיצה בהתאם להתקדמות (progress מ-0 ל-1)
        /// </summary>
        /// <param name="progress">התקדמות הקפיצה מ-0.0f (המראה) ועד 1.0f (נחיתה)</param>
        /// <param name="peakHeight">גובה שיא הקפיצה מעל גובה הבסיס</param>
        /// <returns>תוספת הגובה בציר Z</returns>
        public static float CalculateJumpArc(float progress, float peakHeight)
        {
            if (progress <= 0.0f || progress >= 1.0f) return 0.0f;
            return (float)Math.Sin(progress * Math.PI) * peakHeight;
        }

        /// <summary>
        /// מעדכן טיימר לאחור עם חסימה ל-0.0f (שימושי לטיימרי מעיכה, זמני המתנה וכו')
        /// </summary>
        public static void UpdateTimer(ref float timer, float dt)
        {
            if (timer > 0.0f)
            {
                timer -= dt;
                if (timer < 0.0f) timer = 0.0f;
            }
        }

        #endregion
    }

    /// <summary>
    /// מבנה נתונים מודולרי לניהול מסלול תנועה או קפיצה של מפלצת במרחב.
    /// מנהל נקודת מוצא, נקודת יעד, זמן התקדמות ומשך התנועה.
    /// </summary>
    public struct MonsterTrajectory
    {
        public bool IsActive { get; private set; }
        public float StartX { get; private set; }
        public float StartY { get; private set; }
        public float TargetX { get; private set; }
        public float TargetY { get; private set; }
        public float Timer { get; private set; }
        public float Duration { get; set; }

        public float Progress => Duration > 0.0f ? Math.Min(1.0f, Timer / Duration) : 1.0f;

        public void Start(float startX, float startY, float targetX, float targetY, float duration)
        {
            StartX = startX;
            StartY = startY;
            TargetX = targetX;
            TargetY = targetY;
            Duration = Math.Max(0.01f, duration);
            Timer = 0.0f;
            IsActive = true;
        }

        public void Stop()
        {
            IsActive = false;
            Timer = 0.0f;
        }

        /// <summary>
        /// מקדם את הטיימר ומחזיר את המיקום המחושב הבא (nextX, nextY).
        /// </summary>
        /// <returns>true אם מקטע התנועה הגיע לסיומו (progress >= 1.0f), אחרת false</returns>
        public bool Step(float dt, out float currentX, out float currentY)
        {
            if (!IsActive)
            {
                currentX = TargetX;
                currentY = TargetY;
                return true;
            }

            Timer += dt;
            float p = Progress;

            if (p >= 1.0f)
            {
                currentX = TargetX;
                currentY = TargetY;
                IsActive = false;
                return true;
            }

            currentX = MonsterPhysics.Lerp(StartX, TargetX, p);
            currentY = MonsterPhysics.Lerp(StartY, TargetY, p);
            return false;
        }
    }
}
