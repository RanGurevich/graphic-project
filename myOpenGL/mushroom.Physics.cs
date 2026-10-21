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
            if (!MonsterPhysics.TryGetDeltaTime(ref _lastUpdateTime, out float dt)) return;
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
            MonsterPhysics.UpdateTimer(ref _landingSquishTimer, dt);

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
                    // חישוב מיקום מיועד ובדיקה שאינו חודר לדמות לאורך מסלול הקפיצה
                    float nextX = MonsterPhysics.Lerp(_jumpStartX, _jumpTargetX, progress);
                    float nextY = MonsterPhysics.Lerp(_jumpStartY, _jumpTargetY, progress);

                    if (MonsterPhysics.WillCollideAlongPath(nextX, nextY, EffectiveCollisionRadius))
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

                    // תנועה במסלול קשתי (Parabolic Jump Arc)
                    X = nextX;
                    Y = nextY;
                    Z = BaseZ + MonsterPhysics.CalculateJumpArc(progress, JumpHeight);
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

            // בחירת יעד אקראי בגבולות ה-Skybox
            MonsterPhysics.PickRandomWaypoint(
                _jumpStartX, _jumpStartY,
                0.5f, 2.0f,
                _rand,
                out _jumpTargetX, out _jumpTargetY,
                0.45f);

            // סיבוב הפטריה לכיוון הקפיצה
            if (MonsterPhysics.TryCalculateFaceAngle(_jumpStartX, _jumpStartY, _jumpTargetX, _jumpTargetY, out float angle))
            {
                RotationAngle = angle;
            }
        }

        #endregion
    }
}
