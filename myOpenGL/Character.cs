using System;
using OpenGL;

namespace myOpenGL
{
    /// <summary>
    /// מחלקת הדמות הראשית (Character) במשחק, היורשת ממחלקת ShadowObject.
    /// הדמות נשלטת באמצעות מקלדת ועכבר, מטילה צל על הקרקע, משתקפת במים,
    /// מועכת דשא בעת הליכה, ונלחמת במפלצות בעזרת אגרופים ובעיטות.
    /// 
    /// המחלקה מפוצלת לתתי-קבצים (partial class) לנוחות וקריאות מרבית:
    /// - Character.cs: מבנה ראשי, מאפייני מרחב ודריכה, ובנאי
    /// - Character.Movement.cs: לולאת תנועה והליכה (WASD), בדיקת התנגשויות והחלקה, אנימציית צעדים
    /// - Character.Combat.cs: פעולות לחימה ותקיפה (אגרוף ובעיטה), מהירות מוגברת וחישוב נזק
    /// - Character.Animation.cs: זוויות מפרקים (ראש, ידיים, רגליים), JointType ואינטרפולציית אנימציה
    /// - Character.Rendering.cs: רשימות ציור (Display Lists), היררכיית חלקי הגוף, תאורה, צל וציור באגם
    /// </summary>
    public partial class Character : ShadowObject
    {
        #region שדות ומאפייני מיקום במרחב (Spatial Properties)

        private float xPosition;
        private float yPosition;
        private float zPosition;

        /// <summary>
        /// מיקום הדמות בציר X
        /// </summary>
        public override float X
        {
            get => xPosition;
            set => xPosition = value;
        }

        /// <summary>
        /// מיקום הדמות בציר Y
        /// </summary>
        public override float Y
        {
            get => yPosition;
            set => yPosition = value;
        }

        /// <summary>
        /// מיקום הדמות בציר Z (גובה אגן/מרכז גוף הדמות)
        /// </summary>
        public override float Z
        {
            get => zPosition;
            set => zPosition = value;
        }

        /// <summary>
        /// רדיוס מעיכת הדשא של הדמות בעת דריכה
        /// </summary>
        public override float TrampleRadius { get; set; } = 1.35f;

        /// <summary>
        /// הדמות דורכת על הקרקע כאשר היא נראית וגובהה סביב גובה ההליכה
        /// (בסיס הדמות zPosition = 0.0f כאשר הרגליים מגיעות לרצפה בגובה FloorHeight = -1.35f)
        /// </summary>
        public override bool IsSteppingOnGround => IsVisible && zPosition <= 0.85f;

        #endregion

        #region בנאים (Constructors)

        /// <summary>
        /// בנאי ראשי המאתחל את מיקום הדמות במרחב התלת-ממדי
        /// </summary>
        /// <param name="xPosition">מיקום התחלתי בציר X</param>
        /// <param name="yPosition">מיקום התחלתי בציר Y</param>
        /// <param name="zPosition">מיקום התחלתי בציר Z (בדרך כלל 0.0f)</param>
        public Character(float xPosition, float yPosition, float zPosition)
        {
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
        }

        #endregion
    }
}
