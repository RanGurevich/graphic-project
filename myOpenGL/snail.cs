using System;
using OpenGL;
using myOpenGL;

namespace OpenGL
{
    /// <summary>
    /// סוג צבע הקונכייה של החילזון (אדום או כחול)
    /// </summary>
    public enum SnailColor
    {
        Red,
        Blue
    }

    /// <summary>
    /// מחלקת מפלצת חילזון (Snail) בהשראת MapleStory (Red Snail / Blue Snail).
    /// יורשת ממחלקת Monster (ו-ShadowObject).
    /// מאפשרת הגדרת צבע קונכייה (אדום או כחול) בבנאי.
    /// המחלקה מפוצלת לתתי-קבצים (partial class) לקריאות וסדר מרבי:
    /// - snail.cs: הגדרה ראשית, מאפיינים, בנאים וצינור הציור
    /// - snail.Textures.cs: טעינה וניהול טקסטורות לחילזון אדום וכחול
    /// - snail.Physics.cs: תנועת זחילה רציפה, גלישת inchworm ואנימציה
    /// - snail.Rendering.cs: גאומטריית הגוף התלת-ממדי, קונכייה ספירלית ועיניים רדומות
    /// </summary>
    public partial class snail : Monster
    {
        #region מאפיינים ראשיים

        /// <summary>
        /// צבע קונכיית החילזון (אדום או כחול)
        /// </summary>
        public SnailColor ColorType { get; set; } = SnailColor.Red;

        /// <summary>
        /// האם החילזון בעל קונכייה אדומה
        /// </summary>
        public bool IsRed => ColorType == SnailColor.Red;

        /// <summary>
        /// האם להשתמש בטקסטורות בעת הציור
        /// </summary>
        public bool UseTexture { get; set; } = true;

        /// <summary>
        /// גובה רצפת העולם שעליה החילזון זוחל
        /// </summary>
        public float BaseZ { get; set; } = WorldSkyBox.FloorHeight;

        /// <summary>
        /// האם לעדכן פיזיקה ותנועה אוטומטית בכל פריים
        /// </summary>
        public bool AutoUpdate { get; set; } = true;

        /// <summary>
        /// האם החילזון מורשה לזחול במרחב
        /// </summary>
        public bool CanCrawl { get; set; } = true;

        #endregion

        #region בנאים (Constructors)

        /// <summary>
        /// בנאי המקבל טיפוס eSnailColor מתוך eSnailColor.cs
        /// </summary>
        public snail(eSnailColor color, int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : this(color == eSnailColor.Red ? SnailColor.Red : SnailColor.Blue, initialHealth, x, y, z)
        {
        }

        /// <summary>
        /// בנאי ראשי המקבל סוג צבע (SnailColor.Red או SnailColor.Blue), נקודות חיים ומיקום התחלתי
        /// </summary>
        /// <param name="color">צבע הקונכייה (Red או Blue)</param>
        /// <param name="initialHealth">כמות חיים התחלתית</param>
        /// <param name="x">מיקום X</param>
        /// <param name="y">מיקום Y</param>
        /// <param name="z">מיקום Z (ברירת מחדל: גובה הרצפה)</param>
        public snail(SnailColor color, int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(initialHealth, x, y, float.IsNaN(z) ? WorldSkyBox.FloorHeight : z)
        {
            ColorType = color;
            _rand = new Random(Guid.NewGuid().GetHashCode());
            BaseZ = float.IsNaN(z) ? WorldSkyBox.FloorHeight : z;
            Z = BaseZ;

            HealthBarHeightOffset = 1.55f;

            // זמן המתנה ראשוני אקראי לתחילת זחילה
            _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.2);

            EnsureTexturesLoaded();
        }

        /// <summary>
        /// בנאי נוח המקבל בוליאני (true = אדום, false = כחול)
        /// </summary>
        public snail(bool isRed, int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : this(isRed ? SnailColor.Red : SnailColor.Blue, initialHealth, x, y, z)
        {
        }

        /// <summary>
        /// בנאי המקבל מחרוזת צבע ("red" / "blue" / "אדום" / "כחול")
        /// </summary>
        public snail(string colorName, int initialHealth = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : this(ParseColorName(colorName), initialHealth, x, y, z)
        {
        }

        /// <summary>
        /// בנאי ברירת מחדל (חילזון אדום עם 100 חיים)
        /// </summary>
        public snail() : this(SnailColor.Red, 100, 0.0f, 0.0f)
        {
        }

        private static SnailColor ParseColorName(string name)
        {
            if (string.IsNullOrEmpty(name)) return SnailColor.Red;
            name = name.Trim().ToLowerInvariant();
            if (name.Contains("blue") || name.Contains("כחול")) return SnailColor.Blue;
            return SnailColor.Red;
        }

        #endregion

        #region צינור ציור ראשי (Rendering Pipeline)

        /// <summary>
        /// מימוש ציור הגאומטריה התלת-ממדית של החילזון מתוך Monster.DrawGeometry
        /// </summary>
        /// <param name="isForShadow">האם הציור מיועד להיטל צל</param>
        protected override void DrawCustomGeometry(bool isForShadow)
        {
            EnsureTexturesLoaded();

            // אפקט מעיכה ומתיחה (Squash and Stretch) עדין של זחילה איטית (Inchworm / Slug glide)
            float crawlScaleY = 1.0f;
            float crawlScaleZ = 1.0f;
            float crawlScaleX = 1.0f;

            if (IsCrawling)
            {
                float cycle = (float)Math.Sin(_crawlCycle * Math.PI * 2.0);
                crawlScaleY = 1.0f + 0.08f * cycle;  // התארכות והתכווצות לאורך הגוף
                crawlScaleZ = 1.0f - 0.04f * cycle;  // הנמכה והגבהה קלה
                crawlScaleX = 1.0f - 0.05f * cycle;  // היצרות והתרחבות
            }
            else
            {
                // נשימה עדינה במקום
                float breathe = (float)Math.Sin(_idleTime * 3.0f) * 0.02f;
                crawlScaleZ = 1.0f + breathe;
                crawlScaleX = 1.0f - breathe * 0.5f;
            }

            GL.glPushMatrix();
            GL.glScalef(crawlScaleX, crawlScaleY, crawlScaleZ);

            // 1. ציור גוף החילזון התלת-ממדי (Foot & Snout)
            DrawSnailBody(isForShadow);

            // 2. ציור הקונכייה הספירלית התלת-ממדית (Spiral Shell) בצבע אדום או כחול
            DrawSnailShell(isForShadow);

            // 3. ציור העיניים התלת-ממדיות עם העפעפיים הרדומים (Eyestalks, Sleepy Eyes & Pupils)
            DrawSnailEyes(isForShadow);

            GL.glPopMatrix();
        }

        /// <summary>
        /// דריסת DrawMonster לעדכון אוטומטי של תנועת הזחילה בעת הציור
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
    /// Alias נוח לתאימות כתיב (Snail)
    /// </summary>
    public class Snail : snail
    {
        public Snail(SnailColor color, int health = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(color, health, x, y, z) { }

        public Snail(eSnailColor color, int health = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(color, health, x, y, z) { }

        public Snail(bool isRed, int health = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(isRed, health, x, y, z) { }

        public Snail() : base(SnailColor.Red, 100, 0.0f, 0.0f) { }
    }

    /// <summary>
    /// חילזון אדום (RedSnail)
    /// </summary>
    public class RedSnail : snail
    {
        public RedSnail(int health = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(SnailColor.Red, health, x, y, z) { }

        public RedSnail() : base(SnailColor.Red, 100, 0.0f, 0.0f) { }
    }

    /// <summary>
    /// חילזון כחול (BlueSnail)
    /// </summary>
    public class BlueSnail : snail
    {
        public BlueSnail(int health = 100, float x = 0.0f, float y = 0.0f, float z = float.NaN)
            : base(SnailColor.Blue, health, x, y, z) { }

        public BlueSnail() : base(SnailColor.Blue, 100, 0.0f, 0.0f) { }
    }

    #endregion
}
