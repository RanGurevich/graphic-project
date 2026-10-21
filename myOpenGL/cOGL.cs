using myOpenGL;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenGL
{
    class cOGL
    {
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

        /// <summary>
        /// רשימת 5 מפלצות הפירמידה (TRINAGLE) בעולם
        /// </summary>
        public List<TRINAGLE> Monsters = new List<TRINAGLE>();

        /// <summary>
        /// רשימת 7 מפלצות פטריה כתומה (mushroom) בעולם
        /// </summary>
        public List<mushroom> Mushrooms = new List<mushroom>();

        /// <summary>
        /// תאימות לגישה לפטריה בודדת (מחזירה את הפטריה הראשונה ברשימה)
        /// </summary>
        public mushroom MushroomMonster => Mushrooms.Count > 0 ? Mushrooms[0] : null;

        /// <summary>
        /// רשימת 6 מפלצות חילזון (3 אדומים ו-3 כחולים) בעולם
        /// </summary>
        public List<snail> Snails = new List<snail>();

        private static readonly Random _spawnRand = new Random();

        public cOGL(Control pb)
        {
            p = pb;
            Width = p.Width;
            Height = p.Height; 
            lake = new Lake(10.0f, 10.0f, 7.5f, grass);
            InitializeGL();
            
            // פיזור רנדומלי חכם של כל המפלצות במרחב העולם
            List<float[]> occupiedPositions = new List<float[]>();
            Initialize7Mushrooms(occupiedPositions);
            Initialize6Snails(occupiedPositions);
            Initialize5Monsters(occupiedPositions);

            DrawAll();
        }

        /// <summary>
        /// מחולל מיקום אקראי על היבשה שאינו בתוך האגם ושומר על מרחק ממפלצות אחרות
        /// </summary>
        private float[] GetRandomSpawnPosition(List<float[]> occupied, float minDistance = 2.8f)
        {
            float lakeX = 10.0f;
            float lakeY = 10.0f;
            float lakeAvoidRadius = 8.5f;
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

        /// <summary>
        /// מאתחל 7 מפלצות פטריה (mushroom) בפיזור אקראי רחב וטבעי בעולם
        /// </summary>
        public void Initialize7Mushrooms(List<float[]> occupied = null)
        {
            Mushrooms.Clear();
            int[] healths = { 120, 100, 110, 130, 80, 150, 100 };
            float[] scales = { 1.05f, 0.95f, 1.0f, 1.10f, 0.85f, 1.25f, 1.0f };

            for (int i = 0; i < 7; i++)
            {
                float[] pos = GetRandomSpawnPosition(occupied, 3.2f);
                Mushrooms.Add(new mushroom(healths[i], pos[0], pos[1])
                {
                    Scale = scales[i],
                    RotationAngle = (float)(_spawnRand.NextDouble() * 360.0)
                });
            }
        }

        /// <summary>
        /// מאתחל 6 חלזונות (3 אדומים ו-3 כחולים) בפיזור אקראי במרחב העולם
        /// </summary>
        public void Initialize6Snails(List<float[]> occupied = null)
        {
            Snails.Clear();

            // 3 חלזונות אדומים (Red Snails)
            for (int i = 0; i < 3; i++)
            {
                float[] pos = GetRandomSpawnPosition(occupied, 3.0f);
                int health = 90 + i * 15;
                float scale = 0.95f + (float)_spawnRand.NextDouble() * 0.25f;

                Snails.Add(new snail(SnailColor.Red, health, pos[0], pos[1])
                {
                    Scale = scale,
                    RotationAngle = (float)(_spawnRand.NextDouble() * 360.0)
                });
            }

            // 3 חלזונות כחולים (Blue Snails)
            for (int i = 0; i < 3; i++)
            {
                float[] pos = GetRandomSpawnPosition(occupied, 3.0f);
                int health = 80 + i * 20;
                float scale = 0.95f + (float)_spawnRand.NextDouble() * 0.25f;

                Snails.Add(new snail(SnailColor.Blue, health, pos[0], pos[1])
                {
                    Scale = scale,
                    RotationAngle = (float)(_spawnRand.NextDouble() * 360.0)
                });
            }
        }

        /// <summary>
        /// מאתחל 5 מפלצות TRINAGLE בפיזור אקראי רחב בעולם
        /// </summary>
        public void Initialize5Monsters(List<float[]> occupied = null)
        {
            Monsters.Clear();
            var configs = new[]
            {
                new { Health = 100, R = 0.95f, G = 0.25f, B = 0.20f },
                new { Health = 120, R = 0.95f, G = 0.65f, B = 0.10f },
                new { Health = 150, R = 0.70f, G = 0.20f, B = 0.90f },
                new { Health =  80, R = 0.15f, G = 0.85f, B = 0.35f },
                new { Health = 200, R = 0.20f, G = 0.75f, B = 0.95f }
            };

            foreach (var cfg in configs)
            {
                float[] pos = GetRandomSpawnPosition(occupied, 3.2f);
                Monsters.Add(new TRINAGLE(cfg.Health, pos[0], pos[1], cfg.R, cfg.G, cfg.B)
                {
                    RotationAngle = (float)(_spawnRand.NextDouble() * 360.0)
                });
            }
        }

        public void CreateLake(float centerX, float centerY, float radius)
        {
            lake = new Lake(centerX, centerY, radius, grass);
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
        /// בודק מפלצות קרובות בחזית, גורם 15-40 נזק, ומעלים מפלצת שמתה מהזיכרון
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

        ~cOGL()
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

        void DrawOldAxes()
        {
            //for this time
            //Lights positioning is here!!!
            float[] pos = new float[4];
            pos[0] = 10; pos[1] = 10; pos[2] = 10; pos[3] = 1;
            GL.glLightfv(GL.GL_LIGHT0, GL.GL_POSITION, pos);
            GL.glDisable(GL.GL_LIGHTING);

            //INITIAL axes
            GL.glEnable(GL.GL_LINE_STIPPLE);
            GL.glLineStipple(1, 0xFF00);  //  dotted   
            GL.glBegin(GL.GL_LINES);
            //x  RED
            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(-3.0f, 0.0f, 0.0f);
            GL.glVertex3f(3.0f, 0.0f, 0.0f);
            //y  GREEN 
            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, -3.0f, 0.0f);
            GL.glVertex3f(0.0f, 3.0f, 0.0f);
            //z  BLUE
            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, -3.0f);
            GL.glVertex3f(0.0f, 0.0f, 3.0f);
            GL.glEnd();
            GL.glDisable(GL.GL_LINE_STIPPLE);
        }
        void DrawAxes()
        {
            GL.glBegin(GL.GL_LINES);
            //x  RED
            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(-10.0f, 0.0f, 0.0f);
            GL.glVertex3f(10.0f, 0.0f, 0.0f);
            //y  GREEN 
            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, -3.0f, 0.0f);
            GL.glVertex3f(0.0f, 3.0f, 0.0f);
            //z  BLUE
            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, -3.0f);
            GL.glVertex3f(0.0f, 0.0f, 3.0f);
            GL.glEnd();
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

            // ציור הדשא הדינמי על גבי cube-1
            if (grass != null)
            {
                grass.Draw(character);
            }

            // ציור האגם על גבי משטח הרצפה
            if (lake != null)
            {
                lake.Draw();
            }

            DrawAxes();
            if(character != null)
            {
                character.DrawCharacter();
            }

            // ציור 5 מפלצות הפירמידה (TRINAGLE)
            if (Monsters != null)
            {
                for (int i = 0; i < Monsters.Count; i++)
                {
                    Monsters[i].DrawMonster();
                }
            }

            // ציור 7 מפלצות הפטריה (mushroom)
            if (Mushrooms != null)
            {
                for (int i = 0; i < Mushrooms.Count; i++)
                {
                    Mushrooms[i].DrawMonster();
                }
            }

            // ציור 6 מפלצות החילזון (3 אדומים ו-3 כחולים)
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
