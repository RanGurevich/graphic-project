using myOpenGL;
using System;
using System.Windows.Forms;

namespace OpenGL
{
    class cOGL
    {
        Control p;
        Character character = null;
        int Width;
        int Height;

        public cOGL(Control pb)
        {
            p = pb;
            Width = p.Width;
            Height = p.Height; 
            InitializeGL();
            DrawAll();
        }

        public void SetCharacter(Character character)
        {
            this.character = character;
            character.OnAnimationStep += () => 
            {
                DrawAll();
            };
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

            GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT);
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

            DrawAxes();
            if(character != null)
            {
                character.DrawCharacter();
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
