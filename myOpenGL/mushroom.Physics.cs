using System;
using OpenGL;

namespace OpenGL
{
    /// <summary>
    /// חלק המחלקה האחראי על תנועה, פיזיקת קפיצות (Parabolic Arc) ומעיכה/מתיחה (Squash and Stretch)
    /// </summary>
    public partial class mushroom
    {
        #region שדות פיזיקה ותנועה

        private static readonly Random _globalRand = new Random();
        private readonly Random _rand;

        /// <summary>
        /// האם הפטריה כרגע באמצע קפיצה
        /// </summary>
        public bool IsJumping { get; private set; } = false;

        /// <summary>
        /// גובה שיא של הקפיצה
        /// </summary>
        public float JumpHeight { get; set; } = 1.35f;

        /// <summary>
        /// משך הקפיצה בשניות
        /// </summary>
        public float JumpDuration { get; set; } = 0.65f;

        private float _jumpTimer = 0.0f;
        private float _waitTimer = 0.0f;
        private float _jumpStartX = 0.0f;
        private float _jumpStartY = 0.0f;
        private float _jumpTargetX = 0.0f;
        private float _jumpTargetY = 0.0f;
        private float _landingSquishTimer = 0.0f;
        private float _idleTime = 0.0f;
        private DateTime _lastUpdateTime = DateTime.Now;

        #endregion

        #region עדכון פיזיקה ואנימציה (Update & Physics)

        /// <summary>
        /// עדכון מחזור החיים והתנועה של הפטריה
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
        /// עדכון תנועת הקפיצות, המעיכה בעת נחיתה והנשימה במקום
        /// </summary>
        public void UpdatePhysics(float dt)
        {
            if (!IsAlive) return;

            _idleTime += dt;

            // עדכון מעיכת נחיתה
            if (_landingSquishTimer > 0.0f)
            {
                _landingSquishTimer -= dt;
                if (_landingSquishTimer < 0.0f) _landingSquishTimer = 0.0f;
            }

            // בדיקת התנגשות ומגע עם הדמות הראשית:
            // אם הפטריה נוגעת בדמות, היא עוצרת לחלוטין במקום עד שהדמות תזוז!
            if (CheckAndResolveCharacterCollision())
            {
                if (IsJumping)
                {
                    IsJumping = false;
                    _jumpTimer = 0.0f;
                    Z = BaseZ;
                    _landingSquishTimer = 0.18f;
                    _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.5);
                }
                return;
            }

            if (!CanJump) return;

            if (IsJumping)
            {
                _jumpTimer += dt;
                float progress = _jumpTimer / JumpDuration;

                if (progress >= 1.0f)
                {
                    // סיום קפיצה ונחיתה על הרצפה
                    IsJumping = false;
                    _jumpTimer = 0.0f;
                    X = _jumpTargetX;
                    Y = _jumpTargetY;
                    Z = BaseZ;
                    _landingSquishTimer = 0.18f; // הפעלת אפקט מעיכה רך בנחיתה

                    // קביעת זמן המתנה אקראי לקפיצה הבאה
                    _waitTimer = (float)(_rand.NextDouble() * 2.0 + 0.8);
                }
                else
                {
                    float nextX = _jumpStartX + (_jumpTargetX - _jumpStartX) * progress;
                    float nextY = _jumpStartY + (_jumpTargetY - _jumpStartY) * progress;

                    if (GlobalCharacter != null)
                    {
                        float dx = nextX - GlobalCharacter.X;
                        float dy = nextY - GlobalCharacter.Y;
                        float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                        float minDist = EffectiveCollisionRadius + GlobalCharacter.CollisionRadius;

                        if (dist < minDist)
                        {
                            // עצירה מיידית מול הדמות ונחיתה על הרצפה ללא חדירה!
                            CheckAndResolveCharacterCollision();
                            IsJumping = false;
                            _jumpTimer = 0.0f;
                            Z = BaseZ;
                            _landingSquishTimer = 0.18f;
                            _waitTimer = (float)(_rand.NextDouble() * 1.5 + 0.5);
                            return;
                        }
                    }

                    // תנועה במסלול קשתי (Parabolic Jump Arc)
                    X = nextX;
                    Y = nextY;

                    float arc = (float)Math.Sin(progress * Math.PI);
                    Z = BaseZ + JumpHeight * arc;
                }
            }
            else
            {
                // המתנה לקפיצה הבאה
                _waitTimer -= dt;
                if (_waitTimer <= 0.0f)
                {
                    StartRandomJump();
                }
            }
        }

        /// <summary>
        /// התחלת קפיצה אקראית לכיוון סמוך
        /// </summary>
        public void StartRandomJump()
        {
            if (!IsAlive) return;

            IsJumping = true;
            _jumpTimer = 0.0f;
            _jumpStartX = X;
            _jumpStartY = Y;

            // זווית קפיצה אקראית במרחב
            double angle = _rand.NextDouble() * Math.PI * 2.0;
            float distance = (float)(_rand.NextDouble() * 1.5 + 0.5);

            float targetX = _jumpStartX + (float)Math.Cos(angle) * distance;
            float targetY = _jumpStartY + (float)Math.Sin(angle) * distance;

            // שמירה על גבולות העולם (הסייג של ה-Skybox)
            float bound = WorldSkyBox.SkyboxSize * 0.45f;
            _jumpTargetX = Math.Max(-bound, Math.Min(bound, targetX));
            _jumpTargetY = Math.Max(-bound, Math.Min(bound, targetY));

            // סיבוב הפטריה לכיוון הקפיצה
            float dx = _jumpTargetX - _jumpStartX;
            float dy = _jumpTargetY - _jumpStartY;
            if (Math.Abs(dx) > 0.01f || Math.Abs(dy) > 0.01f)
            {
                RotationAngle = (float)(Math.Atan2(dx, -dy) * 180.0 / Math.PI);
            }
        }

        #endregion
    }
}
