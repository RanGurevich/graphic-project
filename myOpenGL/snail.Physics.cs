using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על תנועת הזחילה האיטית והחלקה (Crawling & Wandering)
    /// </summary>
    public partial class snail
    {
        #region שדות פיזיקה ותנועה

        private readonly Random _rand;

        /// <summary>
        /// האם החילזון בתנועת זחילה כרגע
        /// </summary>
        public bool IsCrawling { get; private set; } = false;

        /// <summary>
        /// מהירות הזחילה של החילזון
        /// </summary>
        public float CrawlSpeed { get; set; } = 0.35f;

        private float _crawlTimer = 0.0f;
        private float _crawlDuration = 3.0f;
        private float _waitTimer = 0.0f;
        private float _crawlStartX = 0.0f;
        private float _crawlStartY = 0.0f;
        private float _crawlTargetX = 0.0f;
        private float _crawlTargetY = 0.0f;
        private float _crawlCycle = 0.0f;
        private float _idleTime = 0.0f;
        private DateTime _lastUpdateTime = DateTime.Now;

        #endregion

        #region עדכון פיזיקה וזחילה (Update & Crawling)

        /// <summary>
        /// עדכון מחזור התנועה של החילזון
        /// </summary>
        public void Update()
        {
            DateTime now = DateTime.Now;
            float dt = (float)(now - _lastUpdateTime).TotalSeconds;
            _lastUpdateTime = now;

            if (dt > 0.1f) dt = 0.1f;
            if (dt <= 0.0f) return;

            UpdatePhysics(dt);
        }

        /// <summary>
        /// עדכון פיזיקת הזחילה הרציפה וההתקדמות במרחב
        /// </summary>
        public void UpdatePhysics(float dt)
        {
            if (!IsAlive) return;

            _idleTime += dt;

            // בדיקת התנגשות ומגע עם הדמות הראשית:
            // אם החילזון נוגע בדמות, הוא עוצר לחלוטין במקום עד שהדמות תזוז!
            if (CheckAndResolveCharacterCollision())
            {
                if (IsCrawling)
                {
                    IsCrawling = false;
                    _crawlTimer = 0.0f;
                    _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.5);
                }
                return;
            }

            if (!CanCrawl) return;

            if (IsCrawling)
            {
                _crawlTimer += dt;
                _crawlCycle += dt * 1.5f;

                float progress = _crawlTimer / _crawlDuration;

                if (progress >= 1.0f)
                {
                    // סיום מקטע זחילה ומנוחה קצרה
                    IsCrawling = false;
                    _crawlTimer = 0.0f;
                    X = _crawlTargetX;
                    Y = _crawlTargetY;
                    _waitTimer = (float)(_rand.NextDouble() * 3.0 + 1.0);
                }
                else
                {
                    // חישוב מיקום מיועד ובדיקה שאינו חודר לדמות
                    float nextX = _crawlStartX + (_crawlTargetX - _crawlStartX) * progress;
                    float nextY = _crawlStartY + (_crawlTargetY - _crawlStartY) * progress;

                    if (GlobalCharacter != null)
                    {
                        float dx = nextX - GlobalCharacter.X;
                        float dy = nextY - GlobalCharacter.Y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        float minDist = EffectiveCollisionRadius + GlobalCharacter.CollisionRadius;

                        if (dist < minDist)
                        {
                            // עצירה מיידית מול הדמות ללא חדירה!
                            CheckAndResolveCharacterCollision();
                            IsCrawling = false;
                            _crawlTimer = 0.0f;
                            _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.5);
                            return;
                        }
                    }

                    // התקדמות ליניארית חלקה על הקרקע
                    X = nextX;
                    Y = nextY;
                }
            }
            else
            {
                // המתנה למקטע הזחילה הבא
                _waitTimer -= dt;
                if (_waitTimer <= 0.0f)
                {
                    StartRandomCrawl();
                }
            }
        }

        /// <summary>
        /// התחלת מקטע זחילה לכיוון אקראי חדש
        /// </summary>
        public void StartRandomCrawl()
        {
            if (!IsAlive) return;

            IsCrawling = true;
            _crawlTimer = 0.0f;
            _crawlStartX = X;
            _crawlStartY = Y;

            // זווית וכיוון אקראיים
            double angle = _rand.NextDouble() * Math.PI * 2.0;
            float distance = (float)(_rand.NextDouble() * 1.8 + 0.8);

            _crawlDuration = distance / Math.Max(0.1f, CrawlSpeed);

            float targetX = _crawlStartX + (float)Math.Cos(angle) * distance;
            float targetY = _crawlStartY + (float)Math.Sin(angle) * distance;

            // שמירה על גבולות העולם (הסייג של ה-Skybox)
            float bound = WorldSkyBox.SkyboxSize * 0.44f;
            _crawlTargetX = Math.Max(-bound, Math.Min(bound, targetX));
            _crawlTargetY = Math.Max(-bound, Math.Min(bound, targetY));

            // סיבוב כיוון הראש והקונכייה לעבר יעד הזחילה
            float dx = _crawlTargetX - _crawlStartX;
            float dy = _crawlTargetY - _crawlStartY;
            if (Math.Abs(dx) > 0.01f || Math.Abs(dy) > 0.01f)
            {
                RotationAngle = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
            }
        }

        #endregion
    }
}
