using myOpenGL;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenGL
{
    class DrawGame
    {
        #region Constants - Monster Amounts & Lake Configuration

        // כמות מפלצות (Monster Amounts)
        public const int RED_SNAIL_AMOUNT = 3;
        public const int BLUE_SNAIL_AMOUNT = 3;
        public const int SNAIL_AMOUNT = RED_SNAIL_AMOUNT + BLUE_SNAIL_AMOUNT;
        public const int SNAILS_AMOUNT = SNAIL_AMOUNT;

        public const int MUSHROOM_AMOUNT = 7;
        public const int MUSHROOMS_AMOUNT = MUSHROOM_AMOUNT;

        public const int TRIANGLE_AMOUNT = 5;
        public const int TRIANGLES_AMOUNT = TRIANGLE_AMOUNT;

        // סך כל המפלצות בעולם
        public const int TOTAL_MONSTERS_AMOUNT = SNAIL_AMOUNT + MUSHROOM_AMOUNT + TRIANGLE_AMOUNT;

        // הגדרות וגודל האגם (Lake Configuration & Size)
        public const float LAKE_RADIUS = 7.5f;
        public const float LAKE_SIZE = LAKE_RADIUS;
        public const float LAKE_CENTER_X = 10.0f;
        public const float LAKE_CENTER_Y = 10.0f;

        #endregion

        Control p;
        Character character = null;
        Grass grass = new Grass();
        public Lake lake;
        public Lake Lake
        {
            get { return lake; }
            set { lake = value; }
        }
        int Width;
        int Height;

        public List<TRINAGLE> Monsters = new List<TRINAGLE>();
        public List<mushroom> Mushrooms = new List<mushroom>();
        public mushroom MushroomMonster => Mushrooms.Count > 0 ? Mushrooms[0] : null;

        /// <summary>
        /// רשימת מפלצות חילזון (אדומים וכחולים) בעולם
        /// </summary>
        public List<snail> Snails = new List<snail>();

        private static readonly Random _spawnRand = new Random();

        public DrawGame(Control pb)
        {
            p = pb;
            Width = p.Width;
            Height = p.Height; 
            lake = new Lake(LAKE_CENTER_X, LAKE_CENTER_Y, LAKE_RADIUS, grass);
            lake.ShadowObjectsProvider = GetAllShadowObjects;
            grass.ShadowObjectsProvider = GetAllShadowObjects;
            InitializeGL();
            
            // פיזור רנדומלי חכם של כל המפלצות במרחב העולם באמצעות הפונקציה המודולרית InitializeMonster
            List<float[]> occupiedPositions = new List<float[]>();
            InitializeMonster(eGameMonster.MUSHROOM, MUSHROOM_AMOUNT, occupied: occupiedPositions);
            InitializeMonster(eGameMonster.SNAIL, RED_SNAIL_AMOUNT, eSnailColor.Red, occupiedPositions);
            InitializeMonster(eGameMonster.SNAIL, BLUE_SNAIL_AMOUNT, eSnailColor.Blue, occupiedPositions);
            InitializeMonster(eGameMonster.TRINAGLE, TRIANGLE_AMOUNT, occupied: occupiedPositions);

            DrawAll();
        }

        /// <summary>
        /// מחולל מיקום אקראי על היבשה שאינו בתוך האגם ושומר על מרחק ממפלצות אחרות
        /// </summary>
        private float[] GetRandomSpawnPosition(List<float[]> occupied, float minDistance = 2.8f)
        {
            float lakeX = lake != null ? lake.CenterX : LAKE_CENTER_X;
            float lakeY = lake != null ? lake.CenterY : LAKE_CENTER_Y;
            float lakeAvoidRadius = (lake != null ? lake.Radius : LAKE_RADIUS) + 1.0f;
            float maxBound = 15.0f;

            for (int attempt = 0; attempt < 250; attempt++)
            {
                // הגרלת מיקום במרחב הדשא
                float x = (float)(_spawnRand.NextDouble() * (maxBound * 2.0f) - maxBound);
                float y = (float)(_spawnRand.NextDouble() * (maxBound * 2.0f) - maxBound);

                // בדיקה שלא נוחת בתוך מי האגם
                float distToLake = (float)Math.Sqrt((x - lakeX) * (x - lakeX) + (y - lakeY) * (y - lakeY));
                if (distToLake < lakeAvoidRadius) continue;

                // בדיקה שאינו צמוד מדיי לנקודת ההתחלה של השחקן (0, 0)
                float distToSpawn = (float)Math.Sqrt(x * x + y * y);
                if (distToSpawn < 2.0f) continue;

                // בדיקה שלא קרוב מדיי למפלצת אחרת שכבר נוצרה
                bool tooClose = false;
                if (occupied != null)
                {
                    foreach (var pos in occupied)
                    {
                        float d = (float)Math.Sqrt((x - pos[0]) * (x - pos[0]) + (y - pos[1]) * (y - pos[1]));
                        if (d < minDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }

                if (!tooClose)
                {
                    var result = new float[] { x, y };
                    occupied?.Add(result);
                    return result;
                }
            }

            // גיבוי אם לא נמצא מיקום אידיאלי
            float fallbackX = (float)(_spawnRand.NextDouble() * 10.0f - 5.0f);
            float fallbackY = (float)(_spawnRand.NextDouble() * 10.0f - 5.0f);
            var fallback = new float[] { fallbackX, fallbackY };
            occupied?.Add(fallback);
            return fallback;
        }

        #region Modular Monster Initialization (eGameMonster)

        public void InitializeMonster(eGameMonster type, int count, eSnailColor snailColor = eSnailColor.Red, List<float[]> occupied = null, bool clearExisting = false)
        {
            if (clearExisting)
            {
                ClearMonsters(type);
            }

            if (count <= 0) return;

            float minDistance = (type == eGameMonster.SNAIL) ? 3.0f : 3.2f;

            for (int i = 0; i < count; i++)
            {
                float[] pos = GetRandomSpawnPosition(occupied, minDistance);
                Monster monster = CreateMonsterInstance(type, pos[0], pos[1], i, snailColor);
                if (monster != null)
                {
                    AddMonsterToList(monster);
                }
            }
        }


        /// <summary>
        /// מייצר מופע יחיד של מפלצת לפי סוג, מיקום, אינדקס וצבע חילזון.
        /// אורך החיים מוגרל באופן רנדומלי בין 90 ל-150 לכל מפלצת שנוצרת.
        /// </summary>
        private Monster CreateMonsterInstance(eGameMonster type, float x, float y, int index, eSnailColor snailColor)
        {
            float rotation = (float)(_spawnRand.NextDouble() * 360.0);
            int health = _spawnRand.Next(90, 151); // ערך חיים רנדומלי בין 90 ל-150 כולל

            switch (type)
            {
                case eGameMonster.MUSHROOM:
                {
                    float[] scales = { 1.05f, 0.95f, 1.0f, 1.10f, 0.85f, 1.25f, 1.0f };
                    return new mushroom(health, x, y)
                    {
                        Scale = scales[index % scales.Length],
                        RotationAngle = rotation
                    };
                }

                case eGameMonster.SNAIL:
                {
                    float scale = 0.95f + (float)_spawnRand.NextDouble() * 0.25f;
                    return new snail(snailColor, health, x, y)
                    {
                        Scale = scale,
                        RotationAngle = rotation
                    };
                }

                case eGameMonster.TRINAGLE:
                {
                    var configs = new[]
                    {
                        new { R = 0.95f, G = 0.25f, B = 0.20f },
                        new { R = 0.95f, G = 0.65f, B = 0.10f },
                        new { R = 0.70f, G = 0.20f, B = 0.90f },
                        new { R = 0.15f, G = 0.85f, B = 0.35f },
                        new { R = 0.20f, G = 0.75f, B = 0.95f }
                    };
                    var cfg = configs[index % configs.Length];
                    return new TRINAGLE(health, x, y, cfg.R, cfg.G, cfg.B)
                    {
                        RotationAngle = rotation
                    };
                }

                default:
                    return null;
            }
        }

        /// <summary>
        /// מוסיף את המפלצת לרשימה המתאימה לה לפי הסוג שלה
        /// </summary>
        private void AddMonsterToList(Monster monster)
        {
            if (monster is mushroom m)
            {
                Mushrooms.Add(m);
            }
            else if (monster is snail s)
            {
                Snails.Add(s);
            }
            else if (monster is TRINAGLE t)
            {
                Monsters.Add(t);
            }
        }

        /// <summary>
        /// מנקה את רשימת המפלצות לפי סוג
        /// </summary>
        public void ClearMonsters(eGameMonster type)
        {
            switch (type)
            {
                case eGameMonster.MUSHROOM:
                    Mushrooms.Clear();
                    break;
                case eGameMonster.SNAIL:
                    Snails.Clear();
                    break;
                case eGameMonster.TRINAGLE:
                    Monsters.Clear();
                    break;
            }
        }

        /// <summary>
        /// מנקה את כל המפלצות מכל הסוגים בעולם
        /// </summary>
        public void ClearAllMonsters()
        {
            Mushrooms.Clear();
            Snails.Clear();
            Monsters.Clear();
        }

        #endregion



        public void CreateLake(float centerX = LAKE_CENTER_X, float centerY = LAKE_CENTER_Y, float radius = LAKE_RADIUS)
        {
            lake = new Lake(centerX, centerY, radius, grass);
            lake.ShadowObjectsProvider = GetAllShadowObjects;
            if (character != null)
            {
                lake.CharacterRef = character;
            }
            DrawAll();
        }

        public void SetCharacter(Character character)
        {
            this.character = character;
            Monster.GlobalCharacter = character;

            // חיבור בדיקת התנגשויות תנועה של הדמות מול כל המפלצות בעולם
            character.CanMoveValidator = CanCharacterMoveTo;

            // חיבור ביצוע תקיפות (אגרוף ובעיטה) מול מפלצות בקרבת מקום
            character.AttackHandler = (charRef, damage) => HandleCharacterAttack(charRef, damage);

            if (lake != null)
            {
                lake.CharacterRef = character;
            }
            character.OnAnimationStep += () => 
            {
                DrawAll();
            };
        }

        /// <summary>
        /// מסיר מפלצת שמתה מהעולם ומשחרר את האובייקט שלה מהזיכרון
        /// </summary>
        public void RemoveMonster(Monster monster)
        {
            if (monster == null) return;

            monster.IsVisible = false;

            if (monster is mushroom m)
            {
                Mushrooms.Remove(m);
            }
            else if (monster is snail s)
            {
                Snails.Remove(s);
            }
            else if (monster is TRINAGLE t)
            {
                Monsters.Remove(t);
            }
        }

        /// <summary>
        /// מטפל בתקיפה (אגרוף או בעיטה) של הדמות מול מפלצת:
        /// בודק מפלצות קרובות בחזית, גורם 45-70 נזק, ומעלים מפלצת שמתה מהזיכרון
        /// </summary>
        public Monster HandleCharacterAttack(Character charRef, int damage)
        {
            if (charRef == null) return null;

            float charX = charRef.X;
            float charY = charRef.Y;
            float charAngle = charRef.CHARACTER_angle;

            // וקטור כיוון הפנים האמיתי של הדמות (חזית הדמות מול המפלצות - ציר מינוס Y במרחב המקומי)
            float rad = charAngle * (float)Math.PI / 180f;
            float forwardX = (float)Math.Sin(rad);
            float forwardY = -(float)Math.Cos(rad);

            Monster targetMonster = null;
            float minDistance = float.MaxValue;

            foreach (var monster in GetAllMonsters())
            {
                if (monster == null || !monster.IsAlive || !monster.IsVisible) continue;

                float dx = monster.X - charX;
                float dy = monster.Y - charY;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);

                // טווח פגיעה: מרחק מגע קרוב מאוד בחזית
                float hitRange = charRef.CollisionRadius + monster.EffectiveCollisionRadius + 0.85f;

                if (dist <= hitRange)
                {
                    // בדיקת זווית: המפלצת חייבת להיות מול פני הדמות (dot > 0.25f, חרוט של כ-75 מעלות) ולא מאחוריה!
                    float dot = (dist > 0.001f) ? (dx * forwardX + dy * forwardY) / dist : 1.0f;

                    if (dot > 0.25f)
                    {
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            targetMonster = monster;
                        }
                    }
                }
            }

            if (targetMonster != null)
            {
                targetMonster.TakeDamage(damage);

                // אם החיים ירדו ל-0 ומטה - המפלצת נעלמת ומשתחררת מהזיכרון
                if (!targetMonster.IsAlive || targetMonster.CurrentHealth <= 0)
                {
                    RemoveMonster(targetMonster);
                }

                DrawAll();
                return targetMonster;
            }

            return null;
        }

        /// <summary>
        /// מחזיר את כל המפלצות הפעילות והחיות בעולם (פירמידות, פטריות, חלזונות)
        /// </summary>
        public IEnumerable<Monster> GetAllMonsters()
        {
            if (Monsters != null)
            {
                for (int i = 0; i < Monsters.Count; i++)
                {
                    if (Monsters[i] != null && Monsters[i].IsAlive && Monsters[i].IsVisible)
                        yield return Monsters[i];
                }
            }
            if (Mushrooms != null)
            {
                for (int i = 0; i < Mushrooms.Count; i++)
                {
                    if (Mushrooms[i] != null && Mushrooms[i].IsAlive && Mushrooms[i].IsVisible)
                        yield return Mushrooms[i];
                }
            }
            if (Snails != null)
            {
                for (int i = 0; i < Snails.Count; i++)
                {
                    if (Snails[i] != null && Snails[i].IsAlive && Snails[i].IsVisible)
                        yield return Snails[i];
                }
            }
        }

        /// <summary>
        /// מחזיר את כל אובייקטי ה-ShadowObject בסצינה (הדמות וכל המפלצות שנוצרו)
        /// </summary>
        public IEnumerable<ShadowObject> GetAllShadowObjects()
        {
            if (character != null && character.IsVisible)
            {
                yield return character;
            }

            foreach (var monster in GetAllMonsters())
            {
                if (monster != null && monster.IsVisible)
                {
                    yield return monster;
                }
            }
        }

        /// <summary>
        /// בודק האם הדמות יכולה לנוע מ-(currX, currY) אל (nextX, nextY) מבלי לחדור לתחום של אף מפלצת
        /// </summary>
        public bool CanCharacterMoveTo(float currX, float currY, float nextX, float nextY, float charRadius)
        {
            foreach (var monster in GetAllMonsters())
            {
                float currDist = monster.DistanceTo(currX, currY);
                float nextDist = monster.DistanceTo(nextX, nextY);
                float minDist = charRadius + monster.EffectiveCollisionRadius;

                // אם המיקום החדש נמצא בתוך שטח המפלצת, והדמות מתקרבת או שוהה בתוכה - התנועה נחסמת!
                if (nextDist < minDist && nextDist <= currDist + 0.001f)
                {
                    return false;
                }
            }
            return true;
        }

        ~DrawGame()
        {
            WGL.wglDeleteContext(m_uint_RC);
        }

        uint m_uint_HWND = 0;
        public uint HWND
        {
            get { return m_uint_HWND; }
        }
        
        uint m_uint_DC = 0;
        public uint DC
        {
            get { return m_uint_DC; }
        }
        
        uint m_uint_RC = 0;
        public uint RC
        {
            get { return m_uint_RC; }
        }


       

        // ===== משתני מצלמה (מבט חופשי כמו במשחקי מחשב) =====
        public float CameraPitch = -25.0f;     // זווית הטיה מעלה/מטה (סביב ציר X)
        public float CameraYaw = -30.0f;       // זווית סיבוב ימינה/שמאלה (סביב ציר Z)
        public float CameraDistance = 10.0f;   // מרחק המצלמה מהדמות / מרכז
        public float MouseSensitivity = 0.2f;  // רגישות עכבר

        public void RotateCamera(float deltaX, float deltaY)
        {
            // סיבוב אופקי (Yaw) סביב ציר Z
            CameraYaw += deltaX * MouseSensitivity;
            while (CameraYaw >= 360.0f) CameraYaw -= 360.0f;
            while (CameraYaw < 0.0f) CameraYaw += 360.0f;

            // סיבוב הדמות בהתאמה ימינה ושמאלה (רק בציר האופקי ולא בגבהים)
            if (character != null)
            {
                character.CHARACTER_angle -= deltaX * MouseSensitivity;
                while (character.CHARACTER_angle >= 360.0f) character.CHARACTER_angle -= 360.0f;
                while (character.CHARACTER_angle < 0.0f) character.CHARACTER_angle += 360.0f;
            }

            // סיבוב אנכי (Pitch) - משנה רק את זווית המצלמה למעלה/למטה, ללא השפעה על גובה/הטיית הדמות
            CameraPitch -= deltaY * MouseSensitivity;

            // הגבלת זווית שלא תתהפך (בין 89- ל-89 מעלות)
            if (CameraPitch > 89.0f) CameraPitch = 89.0f;
            if (CameraPitch < -89.0f) CameraPitch = -89.0f;

            DrawAll();
        }

        public void ZoomCamera(int wheelDelta)
        {
            CameraDistance -= (wheelDelta / 120.0f) * 1.5f;
            if (CameraDistance < 1.0f) CameraDistance = 1.0f;
            if (CameraDistance > 45.0f) CameraDistance = 45.0f;
            DrawAll();
        }

        public void DrawAll()
        {
            if (m_uint_DC == 0 || m_uint_RC == 0)
                return;

            GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT | GL.GL_STENCIL_BUFFER_BIT);
            GL.glLoadIdentity();

            // 1. מיקום המצלמה (מרחק וזוויות מבט מעכבר)
            GL.glTranslatef(0.0f, 0.0f, -CameraDistance);
            GL.glRotatef(CameraPitch, 1.0f, 0.0f, 0.0f); // עלייה / ירידה של המבט (הסתכלות לשמיים או לרצפה)
            GL.glRotatef(CameraYaw, 0.0f, 0.0f, 1.0f);   // סיבוב ימינה / שמאלה

            // 2. תנועת המצלמה במעקב אחרי הדמות
            if (character != null)
            {
                GL.glTranslatef(-character.X, -character.Y, -character.Z);
            }

            // 3. ציור העולם - הקוביה (דשא, שמיים וקירות) והדמות באותה מערכת עולם!
            WorldSkyBox.DrawEarth();

            // ציור הדשא הדינמי על גבי cube-1 עם מעיכה של כל ShadowObject הדורך על הדשא
            if (grass != null)
            {
                grass.Draw(character, GetAllShadowObjects());
            }

            // ציור האגם על גבי משטח הרצפה עם השתקפות של כל ShadowObject
            if (lake != null)
            {
                lake.Draw();
            }

            if(character != null)
            {
                character.DrawCharacter();
            }

            // ציור מפלצות הפירמידה (TRINAGLE)
            if (Monsters != null)
            {
                for (int i = 0; i < Monsters.Count; i++)
                {
                    Monsters[i].DrawMonster();
                }
            }

            // ציור מפלצות הפטריה (mushroom)
            if (Mushrooms != null)
            {
                for (int i = 0; i < Mushrooms.Count; i++)
                {
                    Mushrooms[i].DrawMonster();
                }
            }

            // ציור מפלצות החילזון (snail)
            if (Snails != null)
            {
                for (int i = 0; i < Snails.Count; i++)
                {
                    Snails[i].DrawMonster();
                }
            }

            GL.glFlush();
            WGL.wglSwapBuffers(m_uint_DC);
        }

        protected virtual void InitializeGL()
        {
            m_uint_HWND = (uint)p.Handle.ToInt32();
            m_uint_DC = WGL.GetDC(m_uint_HWND);

            // Not doing the following WGL.wglSwapBuffers() on the DC will
            // result in a failure to subsequently create the RC.
            WGL.wglSwapBuffers(m_uint_DC);

            WGL.PIXELFORMATDESCRIPTOR pfd = new WGL.PIXELFORMATDESCRIPTOR();
            WGL.ZeroPixelDescriptor(ref pfd);
            pfd.nVersion = 1; 
            pfd.dwFlags = (WGL.PFD_DRAW_TO_WINDOW | WGL.PFD_SUPPORT_OPENGL | WGL.PFD_DOUBLEBUFFER); 
            pfd.iPixelType = (byte)(WGL.PFD_TYPE_RGBA);
            pfd.cColorBits = 32;
            pfd.cDepthBits = 32;
            pfd.cStencilBits = 32;
            pfd.iLayerType = (byte)(WGL.PFD_MAIN_PLANE);

            int pixelFormatIndex = 0;
            pixelFormatIndex = WGL.ChoosePixelFormat(m_uint_DC, ref pfd);
            if(pixelFormatIndex == 0)
            {
                MessageBox.Show("Unable to retrieve pixel format");
                return;
            }

            if(WGL.SetPixelFormat(m_uint_DC, pixelFormatIndex, ref pfd) == 0)
            {
                MessageBox.Show("Unable to set pixel format");
                return;
            }
            
            // Create rendering context
            m_uint_RC = WGL.wglCreateContext(m_uint_DC);
            if(m_uint_RC == 0)
            {
                MessageBox.Show("Unable to get rendering context");
                return;
            }
            if(WGL.wglMakeCurrent(m_uint_DC, m_uint_RC) == 0)
            {
                MessageBox.Show("Unable to make rendering context current");
                return;
            }

            initRenderingGL();
        }

        public void OnResize()
        {
            Width = p.Width;
            Height = p.Height;
            if (Height == 0) Height = 1;
            GL.glViewport(0, 0, Width, Height);
            GL.glMatrixMode(GL.GL_PROJECTION);
            GL.glLoadIdentity();
            GLUT.gluPerspective(45.0, ((double)Width) / Height, 1.0, 1000.0);
            GL.glMatrixMode(GL.GL_MODELVIEW);
            DrawAll();
        }




        protected virtual void initRenderingGL()
        {
            if(m_uint_DC == 0 || m_uint_RC == 0)
                return;
            if(this.Width == 0 || this.Height == 0)
                return;

            GL.glClearColor(105.0f / 255.0f, 175.0f / 255.0f, 232.0f / 255.0f, 0.2f); 
            GL.glEnable(GL.GL_DEPTH_TEST);
            GL.glDepthFunc(GL.GL_LEQUAL);

            GL.glViewport(0, 0, this.Width, this.Height);
            GL.glMatrixMode(GL.GL_PROJECTION);
            GL.glLoadIdentity();

            // Set up a 3D perspective projection instead of a 2D one
            GLUT.gluPerspective(45.0, ((double)Width) / Height, 1.0, 1000.0);
            
            GL.glMatrixMode(GL.GL_MODELVIEW);
            GL.glLoadIdentity();

            WorldSkyBox.GenerateTextures();
        }

        /// <summary>
        /// ממיר קואורדינטות מסך (עכבר) לקואורדינטות OpenGL תלת-מימדיות
        /// ומציג MessageBox עם התוצאה.
        /// </summary>
        /// <param name="mouseX">מיקום X של העכבר על הפאנל</param>
        /// <param name="mouseY">מיקום Y של העכבר על הפאנל</param>
        public void GetOpenGLCoords(int mouseX, int mouseY)
        {
            // שליפת המטריצות והViewport הנוכחיים מOpenGL
            double[] modelview = new double[16];
            double[] projection = new double[16];
            int[] viewport = new int[4];

            GL.glGetDoublev(GL.GL_MODELVIEW_MATRIX, modelview);
            GL.glGetDoublev(GL.GL_PROJECTION_MATRIX, projection);
            GL.glGetIntegerv(GL.GL_VIEWPORT, viewport);

            // הפיכת Y כי ב-Windows Y יורד למטה ובOpenGL Y עולה למעלה
            float winX = mouseX;
            float winY = viewport[3] - mouseY;

            // קריאת עומק (Z) מה-depth buffer בנקודה שלחצנו
            float[] depth = new float[1];
            GL.glReadPixels(mouseX, (int)winY, 1, 1, GL.GL_DEPTH_COMPONENT, GL.GL_FLOAT, depth);

            // המרה מקואורדינטות מסך לקואורדינטות עולם תלת-מימדיות
            double objX = 0, objY = 0, objZ = 0;
            GLUT.gluUnProject(winX, winY, depth[0],
                modelview, projection, viewport,
                ref objX, ref objY, ref objZ);

            MessageBox.Show(
                $"OpenGL Coordinates:\n" +
                $"X = {objX:F3}\n" +
                $"Y = {objY:F3}\n" +
                $"Z = {objZ:F3}\n\n" +
                $"Screen: ({mouseX}, {mouseY})\n" +
                $"Depth: {depth[0]:F6}",
                "OpenGL Coordinates");
        }
    }
}
