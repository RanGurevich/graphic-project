using System;
using OpenGL;
using myOpenGL;

namespace OpenGL
{
    /// <summary>
    /// מחלקת בסיס למפלצות (Monster) בסביבת OpenGL, היורשת ממחלקת ShadowObject.
    /// המחלקה מנהלת נקודות חיים (Health), מיקום במרחב (X, Y, Z), סיבוב, קנה מידה,
    /// ומספקת יכולת הטלת צל (Planar Shadow) וציור גאומטריה המיועדת להורשה על ידי מפלצות ספציפיות.
    /// </summary>
    public class Monster : ShadowObject
    {
        #region משתנים ומאפייני חיים (Health)

        /// <summary>
        /// כמות החיים המרבית של המפלצת
        /// </summary>
        public int MaxHealth { get; set; }

        private int _currentHealth;

        /// <summary>
        /// כמות החיים הנוכחית של המפלצת
        /// </summary>
        public int CurrentHealth
        {
            get => _currentHealth;
            set
            {
                int oldHealth = _currentHealth;
                _currentHealth = Math.Max(0, Math.Min(value, MaxHealth));

                if (_currentHealth != oldHealth)
                {
                    OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
                    if (_currentHealth == 0 && oldHealth > 0)
                    {
                        OnDeath?.Invoke();
                    }
                }
            }
        }

        /// <summary>
        /// האם המפלצת חיה (CurrentHealth > 0)
        /// </summary>
        public bool IsAlive => CurrentHealth > 0;

        /// <summary>
        /// אירוע המופעל כאשר כמות החיים משתנה (CurrentHealth, MaxHealth)
        /// </summary>
        public event Action<int, int> OnHealthChanged;

        /// <summary>
        /// אירוע המופעל כאשר כמות החיים מגיעה ל-0
        /// </summary>
        public event Action OnDeath;

        #endregion

        #region מאפייני מיקום ומרחב תלת-ממדי

        /// <summary>
        /// מיקום המפלצת בציר X
        /// </summary>
        public override float X { get; set; } = 0.0f;

        /// <summary>
        /// מיקום המפלצת בציר Y
        /// </summary>
        public override float Y { get; set; } = 0.0f;

        /// <summary>
        /// מיקום המפלצת בציר Z (גובה)
        /// </summary>
        public override float Z { get; set; } = 0.0f;

        /// <summary>
        /// זווית הסיבוב של המפלצת (במעלות) סביב ציר Z
        /// </summary>
        public float RotationAngle { get; set; } = 0.0f;

        /// <summary>
        /// קנה מידה (Scale) של המפלצת
        /// </summary>
        public float Scale { get; set; } = 1.0f;

        /// <summary>
        /// האם המפלצת נראית בסצינה
        /// </summary>
        public override bool IsVisible { get; set; } = true;

        /// <summary>
        /// רדיוס מעיכת הדשא של המפלצת בעת דריכה (מותאם לרדיוס ההתנגשות האפקטיבי)
        /// </summary>
        public override float TrampleRadius => EffectiveCollisionRadius > 0.1f ? EffectiveCollisionRadius * 1.5f : 1.35f;

        /// <summary>
        /// ציור המפלצת עבור השתקפות במים באגם (ללא צל קרקע וללא מד חיים)
        /// </summary>
        public override void DrawForReflection()
        {
            if (!IsVisible) return;
            bool prevHealthBar = ShowHealthBar;
            ShowHealthBar = false;
            DrawMonster(false);
            ShowHealthBar = prevHealthBar;
        }

        /// <summary>
        /// האם להציג מד חיים (Health Bar) גרפי מעל המפלצת
        /// </summary>
        public bool ShowHealthBar { get; set; } = true;

        /// <summary>
        /// גובה מד החיים ביחס למיקום המפלצת
        /// </summary>
        public float HealthBarHeightOffset { get; set; } = 3.0f;

        #endregion

        #region מערכת התנגשויות מול הדמות (Collision System with Character)

        /// <summary>
        /// הפניה סטטית גלובלית לדמות הראשית (Character) עבור חישובי התנגשות ומניעת חדירה
        /// </summary>
        public static Character GlobalCharacter { get; set; }

        /// <summary>
        /// רדיוס ההתנגשות הבסיסי של המפלצת במישור (X, Y)
        /// </summary>
        public virtual float CollisionRadius { get; set; } = 0.85f;

        /// <summary>
        /// רדיוס ההתנגשות האפקטיבי של המפלצת בהתחשב בקנה המידה (Scale) שלה
        /// </summary>
        public virtual float EffectiveCollisionRadius => CollisionRadius * Scale;

        /// <summary>
        /// האם המפלצת במצב עצירה עקב מגע/התנגשות עם הדמות הראשית
        /// </summary>
        public bool IsStoppedByCollision { get; set; } = false;

        /// <summary>
        /// חישוב המרחק האוקלידי הדו-ממדי (X, Y) בין המפלצת לנקודה
        /// </summary>
        public float DistanceTo(float targetX, float targetY)
        {
            float dx = X - targetX;
            float dy = Y - targetY;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// חישוב המרחק בין המפלצת לדמות הראשית
        /// </summary>
        public float DistanceToCharacter()
        {
            if (GlobalCharacter == null) return float.MaxValue;
            return DistanceTo(GlobalCharacter.X, GlobalCharacter.Y);
        }

        /// <summary>
        /// בודק האם המפלצת נמצאת במגע/התנגשות עם הדמות הראשית,
        /// ומונע חדירה הדדית (Penetration Resolution) על ידי דחיפת המפלצת אל קו המגע המדויק.
        /// </summary>
        /// <returns>true אם יש מגע/התנגשות (המפלצת צריכה לעצור), אחרת false</returns>
        public bool CheckAndResolveCharacterCollision()
        {
            if (GlobalCharacter == null || !IsAlive || !IsVisible)
            {
                IsStoppedByCollision = false;
                return false;
            }

            float charX = GlobalCharacter.X;
            float charY = GlobalCharacter.Y;
            float charRadius = GlobalCharacter.CollisionRadius;

            float dx = X - charX;
            float dy = Y - charY;
            float dist = (float)Math.Sqrt(dx * dx + dy * dy);
            float minDist = EffectiveCollisionRadius + charRadius;

            if (dist < minDist)
            {
                IsStoppedByCollision = true;

                // הפרדת גופים מדויקת (Penetration Resolution) - מוודא שהמפלצת לא נכנסת בתוך הדמות
                float overlap = minDist - dist;
                if (dist > 0.0001f)
                {
                    X += (dx / dist) * overlap;
                    Y += (dy / dist) * overlap;
                }
                else
                {
                    // במקרה קיצוני של מיקום זהה בדיוק
                    X += minDist;
                }

                return true;
            }
            else if (dist > minDist + 0.15f)
            {
                // הדמות התרחקה מספיק - שחרור העצירה
                IsStoppedByCollision = false;
            }

            return IsStoppedByCollision;
        }

        #endregion

        #region בנאים (Constructors)

        /// <summary>
        /// בנאי ראשי המאתחל את כמות החיים של המפלצת
        /// </summary>
        /// <param name="initialHealth">כמות החיים ההתחלתית והמרבית של המפלצת</param>
        public Monster(int initialHealth) : base()
        {
            if (initialHealth <= 0)
            {
                initialHealth = 100;
            }

            MaxHealth = initialHealth;
            _currentHealth = initialHealth;
        }

        /// <summary>
        /// בנאי המאתחל כמות חיים ומיקום התחלתי במרחב
        /// </summary>
        /// <param name="initialHealth">כמות חיים התחלתית</param>
        /// <param name="x">מיקום בציר X</param>
        /// <param name="y">מיקום בציר Y</param>
        /// <param name="z">מיקום בציר Z</param>
        public Monster(int initialHealth, float x, float y, float z) : this(initialHealth)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// בנאי ברירת מחדל עם 100 נקודות חיים
        /// </summary>
        public Monster() : this(100)
        {
        }

        #endregion

        #region ניהול חיים ופגיעות (Health Operations)

        /// <summary>
        /// טיימר רטט והירתעות בעת פגיעה (Hit Reaction)
        /// </summary>
        public float HitFlinchTimer { get; set; } = 0.0f;

        /// <summary>
        /// פגיעה במפלצת והורדת כמות חיים (במידה והחיים יורדים ל-0, המפלצת נעלמת ומוסרת)
        /// </summary>
        /// <param name="damage">כמות הנזק (למשל 15-40 בבעיטה או אגרוף)</param>
        public virtual void TakeDamage(int damage)
        {
            if (damage > 0 && IsAlive)
            {
                CurrentHealth -= damage;
                HitFlinchTimer = 0.25f;

                if (CurrentHealth <= 0)
                {
                    IsVisible = false;
                }
            }
        }

        /// <summary>
        /// ריפוי המפלצת והעלאת כמות חיים (עד ל-MaxHealth)
        /// </summary>
        /// <param name="healAmount">כמות הריפוי</param>
        public virtual void Heal(int healAmount)
        {
            if (healAmount > 0 && IsAlive)
            {
                CurrentHealth += healAmount;
            }
        }

        /// <summary>
        /// איפוס החיים למקסימום
        /// </summary>
        public virtual void ResetHealth()
        {
            CurrentHealth = MaxHealth;
        }

        #endregion

        #region ציור OpenGL והורשה

        /// <summary>
        /// ציור הגאומטריה של המפלצת (מימוש פונקציית הבסיס מ-ShadowObject).
        /// מחלקות יורשות ידרסו (override) פונקציה זו כדי לצייר את המודל התלת-ממדי של המפלצת.
        /// </summary>
        /// <param name="isForShadow">האם הציור מיועד להיטל צל (true) או לציור רגיל (false)</param>
        public override void DrawGeometry(bool isForShadow)
        {
            // מחלקות יורשות יממשו כאן את הגאומטריה המפורטת שלהן.
            // כברירת מחדל, אם לא נדרסה ע"י מחלקה יורשת, נבצע טרנספורמציה בסיסית של מיקום, סיבוב וקנה מידה:
            GL.glPushMatrix();
            GL.glTranslatef(X, Y, Z);
            GL.glRotatef(RotationAngle, 0, 0, 1);
            GL.glScalef(Scale, Scale, Scale);

            DrawCustomGeometry(isForShadow);

            GL.glPopMatrix();
        }

        /// <summary>
        /// פונקציה וירטואלית נוחה למחלקות יורשות לציור הגאומטריה ללא צורך לבצע מחדש glPushMatrix / glTranslate / glRotate / glScale.
        /// </summary>
        /// <param name="isForShadow">האם הציור מתבצע עבור היטל צל</param>
        protected virtual void DrawCustomGeometry(bool isForShadow)
        {
            // ברירת מחדל: מיועד לדריסה במחלקות היורשות (למשל: ציור גוף, ראש, רגליים וכדומה)
        }

        /// <summary>
        /// ציור מלא של המפלצת כולל תאורה, צל (DrawShadow) ומד חיים.
        /// </summary>
        /// <param name="drawShadow">האם להטיל צל על הקרקע</param>
        public virtual void DrawMonster(bool drawShadow = true)
        {
            if (!IsVisible) return;

            // 1. ציור הצל במידה ומאופשר
            if (drawShadow && CastShadow)
            {
                DrawShadow();
            }

            // 2. שמירת הגדרות תאורה ו-OpenGL
            GL.glPushAttrib(GL.GL_ALL_ATTRIB_BITS);

            // הפעלת תאורה וצבעי חומר
            GL.glEnable(GL.GL_LIGHTING);
            GL.glEnable(GL.GL_LIGHT0);
            GL.glEnable(GL.GL_NORMALIZE);

            float[] lightPos = LightPosition ?? GlobalLightPosition;
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_POSITION, lightPos);

            GL.glEnable(0x0B57); // GL_COLOR_MATERIAL
            GL.glColorMaterial(GL.GL_FRONT_AND_BACK, GL.GL_AMBIENT_AND_DIFFUSE);

            // 3. ציור גאומטריית המפלצת
            DrawGeometry(false);

            // שחזור הגדרות קודמות
            GL.glPopAttrib();

            // 4. ציור מד חיים גרפי במידה ומאופשר
            if (ShowHealthBar)
            {
                DrawHealthBar();
            }
        }

        /// <summary>
        /// ציור מד חיים (Health Bar) תלת-ממדי מעל המפלצת.
        /// כחול משני צדדיו בחיים מלאים, ומתמלא באדום ככל שהחיים יורדים.
        /// בנוי בגאומטריה תלת-ממדית נקייה וללא חפיפת משטחים, מה שמונע Z-Fighting (חלקים שחורים שבוקעים פנימה).
        /// </summary>
        public virtual void DrawHealthBar()
        {
            if (MaxHealth <= 0) return;

            GL.glPushAttrib(GL.GL_ALL_ATTRIB_BITS);
            GL.glDisable(GL.GL_LIGHTING);
            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glEnable(GL.GL_CULL_FACE);
            GL.glCullFace(GL.GL_BACK);

            GL.glPushMatrix();
            GL.glTranslatef(X, Y, Z + HealthBarHeightOffset);

            // סיבוב מד החיים שיתאים לזווית של המפלצת
            GL.glRotatef(RotationAngle, 0, 0, 1);

            float barWidth = 1.6f * Scale;
            float barHeight = 0.22f * Scale;
            float border = 0.035f * Scale;
            float halfThickness = 0.02f * Scale;
            float healthRatio = Math.Max(0.0f, Math.Min(1.0f, (float)CurrentHealth / MaxHealth));

            float xLeft = -barWidth / 2.0f;
            float xRight = barWidth / 2.0f;
            float xSplit = xLeft + barWidth * healthRatio;
            float zBottom = 0.0f;
            float zTop = barHeight;

            // צבע כחול חיים, אדום נזק, ומסגרת כהה אלגנטית
            float[] blueColor = { 0.05f, 0.55f, 1.0f };
            float[] redColor = { 0.95f, 0.12f, 0.12f };
            float[] frameColor = { 0.08f, 0.08f, 0.12f };

            // פונקציית עזר לציור מלבן חזית (+Y)
            void DrawFrontQuad(float x1, float x2, float z1, float z2)
            {
                GL.glVertex3f(x1, halfThickness, z1);
                GL.glVertex3f(x2, halfThickness, z1);
                GL.glVertex3f(x2, halfThickness, z2);
                GL.glVertex3f(x1, halfThickness, z2);
            }

            // פונקציית עזר לציור מלבן גב (-Y) עם כיוון קודקודים תקין
            void DrawBackQuad(float x1, float x2, float z1, float z2)
            {
                GL.glVertex3f(x2, -halfThickness, z1);
                GL.glVertex3f(x1, -halfThickness, z1);
                GL.glVertex3f(x1, -halfThickness, z2);
                GL.glVertex3f(x2, -halfThickness, z2);
            }

            // ==========================================
            // 1. חלק כחול (החיים הקיימים) - משני צדדי המד
            // ==========================================
            if (healthRatio > 0.0f)
            {
                GL.glColor3fv(blueColor);
                GL.glBegin(GL.GL_QUADS);
                DrawFrontQuad(xLeft, xSplit, zBottom, zTop);
                DrawBackQuad(xLeft, xSplit, zBottom, zTop);
                GL.glEnd();
            }

            // ==========================================
            // 2. חלק אדום שמתמלא ככל שהחיים יורדים - משני צדדי המד
            // ==========================================
            if (healthRatio < 1.0f)
            {
                GL.glColor3fv(redColor);
                GL.glBegin(GL.GL_QUADS);
                DrawFrontQuad(xSplit, xRight, zBottom, zTop);
                DrawBackQuad(xSplit, xRight, zBottom, zTop);
                GL.glEnd();
            }

            // ==========================================
            // 3. מסגרת כהה היקפית בלבד (ללא שום פוליגון מאחורי הפנים, מונע Z-Fighting לחלוטין)
            // ==========================================
            GL.glColor3fv(frameColor);
            GL.glBegin(GL.GL_QUADS);

            // מסגרת עליונה ותחתונה בחזית ובגב
            DrawFrontQuad(xLeft - border, xRight + border, zTop, zTop + border);
            DrawBackQuad(xLeft - border, xRight + border, zTop, zTop + border);

            DrawFrontQuad(xLeft - border, xRight + border, zBottom - border, zBottom);
            DrawBackQuad(xLeft - border, xRight + border, zBottom - border, zBottom);

            // מסגרת צדדית שמאלית וימנית בחזית ובגב
            DrawFrontQuad(xLeft - border, xLeft, zBottom, zTop);
            DrawBackQuad(xLeft - border, xLeft, zBottom, zTop);

            DrawFrontQuad(xRight, xRight + border, zBottom, zTop);
            DrawBackQuad(xRight, xRight + border, zBottom, zTop);

            // דפנות עליונות ותחתונות לסגירת התיבה התלת-ממדית
            GL.glVertex3f(xLeft - border, -halfThickness, zTop + border);
            GL.glVertex3f(xRight + border, -halfThickness, zTop + border);
            GL.glVertex3f(xRight + border, halfThickness, zTop + border);
            GL.glVertex3f(xLeft - border, halfThickness, zTop + border);

            GL.glVertex3f(xLeft - border, halfThickness, zBottom - border);
            GL.glVertex3f(xRight + border, halfThickness, zBottom - border);
            GL.glVertex3f(xRight + border, -halfThickness, zBottom - border);
            GL.glVertex3f(xLeft - border, -halfThickness, zBottom - border);

            // דפנות צדדיות לסגירת התיבה התלת-ממדית
            GL.glVertex3f(xLeft - border, -halfThickness, zBottom - border);
            GL.glVertex3f(xLeft - border, halfThickness, zBottom - border);
            GL.glVertex3f(xLeft - border, halfThickness, zTop + border);
            GL.glVertex3f(xLeft - border, -halfThickness, zTop + border);

            GL.glVertex3f(xRight + border, halfThickness, zBottom - border);
            GL.glVertex3f(xRight + border, -halfThickness, zBottom - border);
            GL.glVertex3f(xRight + border, -halfThickness, zTop + border);
            GL.glVertex3f(xRight + border, halfThickness, zTop + border);

            GL.glEnd();

            GL.glPopMatrix();
            GL.glPopAttrib();
        }

        #endregion
    }
}
