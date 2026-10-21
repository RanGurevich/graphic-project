using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקת מפלצת פטריה כתומה (mushroom) בהשראת MapleStory.
    /// יורשת ממחלקת Monster (ו-ShadowObject).
    /// המחלקה מפוצלת לתתי-קבצים (partial class) לנוחות וקריאות מרבית:
    /// - mushroom.cs: מבנה ראשי, מאפיינים, בנאים, וציור כללי
    /// - mushroom.Textures.cs: טעינה וניהול טקסטורות
    /// - mushroom.Physics.cs: פיזיקת קפיצות, מעיכה/מתיחה וזמנים
    /// - mushroom.Body.cs: גאומטריית הגוף התלת-ממדי ותווי הפנים
    /// - mushroom.Cap.cs: גאומטריית כיפת הפטריה, זימים, שוליים ונקודות
    /// </summary>
    public partial class mushroom : Monster
    {
        #region מאפיינים כלליים

        /// <summary>
        /// האם להשתמש בטקסטורות בעת ציור המפלצת (כאשר אינו במצב היטל צל)
        /// </summary>
        public bool UseTexture { get; set; } = true;

        /// <summary>
        /// גובה רצפת העולם שעליה הפטריה עומדת
        /// </summary>
        public float BaseZ { get; set; } = WorldSkyBox.FloorHeight;

        /// <summary>
        /// האם לעדכן פיזיקה אוטומטית בכל פריים בעת קריאה ל-DrawMonster
        /// </summary>
        public bool AutoUpdate { get; set; } = true;

        /// <summary>
        /// האם לאפשר קפיצות אקראיות במרחב
        /// </summary>
        public bool CanJump { get; set; } = true;

        #endregion

        #region בנאים (Constructors)

        /// <summary>
        /// בנאי ראשי למפלצת הפטריה המאתחל חיים ומיקום התחלתי
        /// </summary>
        /// <param name="initialHealth">כמות חיים התחלתית</param>
        /// <param name="x">מיקום X</param>
        /// <param name="y">מיקום Y</param>
        /// <param name="z">מיקום Z (ברירת מחדל: גובה הרצפה)</param>
        public mushroom(int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(initialHealth, x, y, float.IsNaN(z) ? WorldSkyBox.FloorHeight : z)
        {
            _rand = new Random(Guid.NewGuid().GetHashCode());
            BaseZ = float.IsNaN(z) ? WorldSkyBox.FloorHeight : z;
            Z = BaseZ;

            // גובה מד החיים מעל ראש הפטריה
            HealthBarHeightOffset = 1.85f;

            // זמן המתנה אקראי ראשוני עד הקפיצה הראשונה
            _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.3);

            // טעינת הטקסטורות במידת הצורך
            EnsureTexturesLoaded();
        }

        /// <summary>
        /// בנאי ברירת מחדל
        /// </summary>
        public mushroom() : this(100, 0.0f, 0.0f)
        {
        }

        #endregion

        #region מתודות ציור ראשיות (Main Rendering Pipeline)

        /// <summary>
        /// מימוש ציור הגאומטריה התלת-ממדית של הפטריה.
        /// נקרא מתוך Monster.DrawGeometry(isForShadow) לאחר שכבר בוצעו טרנספורמציות המיקום, הסיבוב והקנה מידה.
        /// </summary>
        /// <param name="isForShadow">האם הציור מיועד להיטל צל (צל שחור שקוף ללא צבעים/טקסטורות)</param>
        protected override void DrawCustomGeometry(bool isForShadow)
        {
            EnsureTexturesLoaded();

            // חישוב מתיחה (Stretch) בקפיצה ומעיכה (Squish) בנחיתה / נשימה
            float scaleXY = 1.0f;
            float scaleZ = 1.0f;

            if (IsJumping)
            {
                float progress = _jumpTimer / JumpDuration;
                float arc = (float)Math.Sin(progress * Math.PI);
                scaleZ = 1.0f + 0.28f * arc;   // מתיחה לאורך
                scaleXY = 1.0f - 0.12f * arc;  // היצרות לרוחב
            }
            else if (_landingSquishTimer > 0.0f)
            {
                float ratio = _landingSquishTimer / 0.18f;
                scaleZ = 1.0f - 0.24f * ratio;  // מעיכה כלפי מטה
                scaleXY = 1.0f + 0.18f * ratio; // התרחבות לצדדים
            }
            else
            {
                // נשימה עדינה במצב עמידה
                float breathe = (float)Math.Sin(_idleTime * 3.5f) * 0.025f;
                scaleZ = 1.0f + breathe;
                scaleXY = 1.0f - breathe * 0.6f;
            }

            GL.glPushMatrix();
            GL.glScalef(scaleXY, scaleXY, scaleZ);

            // 1. ציור גוף הפטריה (Body / Stem) כולל הפנים
            DrawMushroomBody(isForShadow);

            // 2. ציור זימי הפטריה התלת-ממדיים (Gills) מתחת לכיפה
            DrawMushroomGills(isForShadow);

            // 3. ציור כיפת הפטריה הכתומה (Cap) עם נקודות צהובות ושוליים מעוגלים
            DrawMushroomCap(isForShadow);

            GL.glPopMatrix();
        }

        /// <summary>
        /// דריסת DrawMonster לעדכון אוטומטי של פיזיקת הקפיצות והאנימציה בעת הציור
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
    /// Alias נוח לתאימות כתיב (Mushroom)
    /// </summary>
    public class Mushroom : mushroom
    {
        public Mushroom(int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(initialHealth, x, y, z)
        {
        }

        public Mushroom() : base(100, 0.0f, 0.0f)
        {
        }
    }

    /// <summary>
    /// Alias נוח לתאימות כתיב (OrangeMushroom)
    /// </summary>
    public class OrangeMushroom : mushroom
    {
        public OrangeMushroom(int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(initialHealth, x, y, z)
        {
        }

        public OrangeMushroom() : base(100, 0.0f, 0.0f)
        {
        }
    }

    #endregion
}
