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
            if (!MonsterPhysics.TryGetDeltaTime(ref _lastUpdateTime, out float dt)) return;
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
                    // חישוב מיקום מיועד ובדיקה שאינו חודר לדמות לאורך מסלול התנועה
                    float nextX = MonsterPhysics.Lerp(_crawlStartX, _crawlTargetX, progress);
                    float nextY = MonsterPhysics.Lerp(_crawlStartY, _crawlTargetY, progress);

                    if (MonsterPhysics.WillCollideAlongPath(nextX, nextY, EffectiveCollisionRadius))
                    {
                        // עצירה מיידית מול הדמות ללא חדירה!
                        CheckAndResolveCharacterCollision();
                        IsCrawling = false;
                        _crawlTimer = 0.0f;
                        _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.5);
                        return;
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

            // בחירת יעד אקראי בגבולות ה-Skybox וחישוב משך הזחילה
            float distance = MonsterPhysics.PickRandomWaypoint(
                _crawlStartX, _crawlStartY,
                0.8f, 2.6f,
                _rand,
                out _crawlTargetX, out _crawlTargetY,
                0.44f);

            _crawlDuration = distance / Math.Max(0.1f, CrawlSpeed);

            // סיבוב כיוון הראש והקונכייה לעבר יעד הזחילה
            if (MonsterPhysics.TryCalculateFaceAngle(_crawlStartX, _crawlStartY, _crawlTargetX, _crawlTargetY, out float angle))
            {
                RotationAngle = angle;
            }
        }

        #endregion
    }
}
