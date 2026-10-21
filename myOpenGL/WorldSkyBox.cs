using System;
using System.Collections.Generic;
using System.Drawing;
using OpenGL;

namespace OpenGL
{
    static class WorldSkyBox
    {
        public static Dictionary<string, uint> textureMap = new Dictionary<string, uint>();


        public static uint[] Textures = new uint[6];
        public static float[] TextureAspectRatios = new float[6];
        public static double angleTexture = 0;
        public static float SkyboxSize = 40.0f;
        public static float FloorHeight = -1.35f;
        public static float CeilingHeight = 40.0f;
        private static bool isInitialized = false;

        private static string ResolvePath(string path)
        {
            if (System.IO.File.Exists(path)) return path;
            string inBase = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            if (System.IO.File.Exists(inBase)) return inBase;
            string inPics = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\pics", System.IO.Path.GetFileName(path));
            if (System.IO.File.Exists(inPics)) return inPics;
            string directPics = System.IO.Path.Combine("pics", System.IO.Path.GetFileName(path));
            if (System.IO.File.Exists(directPics)) return directPics;
            return path;
        }

        public static void GenerateTextures()
        {
            if (isInitialized)
                return;

            GL.glBlendFunc(GL.GL_SRC_ALPHA, GL.GL_ONE_MINUS_SRC_ALPHA); 
            GL.glGenTextures(6, Textures);
            string[] imagesName = {
                @"..\..\pics\cube2-4.jpg", // 0: קיר קדמי
                @"..\..\pics\cube2-4.jpg", // 1: קיר אחורי
                @"..\..\pics\cube2-4.jpg", // 2: קיר שמאלי
                @"..\..\pics\cube2-4.jpg", // 3: קיר ימני
                @"..\..\pics\cube-6.jpg",  // 4: תקרה / שמיים (cube-6)
                @"..\..\pics\cube-6.jpg",  // 5: רצפה / דשא (cube-1)
            };
            for (int i = 0; i < 6; i++)
            {
                string resolved = ResolvePath(imagesName[i]);
                Bitmap image = new Bitmap(resolved);
                TextureAspectRatios[i] = image.Height > 0 ? (float)image.Width / image.Height : 1.0f;

                image.RotateFlip(RotateFlipType.RotateNoneFlipY); //Y axis in Windows is directed downwards, while in OpenGL-upwards
                System.Drawing.Imaging.BitmapData bitmapdata;
                Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

                bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                GL.glBindTexture(GL.GL_TEXTURE_2D, Textures[i]);
                //2D for XYZ
                GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, (int)GL.GL_RGB8, image.Width, image.Height,
                                                              0, GL.GL_BGR_EXT, GL.GL_UNSIGNED_byte, bitmapdata.Scan0);
                GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, (int)GL.GL_LINEAR);
                GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, (int)GL.GL_LINEAR);

                image.UnlockBits(bitmapdata);
                image.Dispose();
            }

            isInitialized = true;
        }

        /// <summary>
        /// מציירת פאה של ה-Skybox עם פריסה מותאמת אישית לפי הרזולוציה ויחס הממדים של התמונה (למניעת מריחה).
        /// </summary>
        private static void DrawFace(int texIndex,
            float x1, float y1, float z1,
            float x2, float y2, float z2,
            float x3, float y3, float z3,
            float x4, float y4, float z4)
        {
            GL.glBindTexture(GL.GL_TEXTURE_2D, Textures[texIndex]);

            float aspect = (texIndex >= 0 && texIndex < TextureAspectRatios.Length) ? TextureAspectRatios[texIndex] : 1.0f;
            if (aspect <= 0.001f) aspect = 1.0f;

            float uMin, uMax, vMin, vMax;
            if (Math.Abs(aspect - 1.0f) < 0.02f)
            {
                // תמונה מרובעת (כמו תמונות קובייה 2048x2048) - פריסה רגילה מלאה
                uMin = 0.0f; uMax = 1.0f;
                vMin = 0.0f; vMax = 1.0f;
            }
            else if (aspect > 1.0f)
            {
                // תמונה רחבה (כמו grass.png 1600x1050) - התאמת פריסה לפי יחס הרזולוציה ללא מריחה
                float span = 0.5f / aspect;
                uMin = 0.5f - span;
                uMax = 0.5f + span;
                vMin = 0.0f;
                vMax = 1.0f;
            }
            else
            {
                // תמונה צרה/גבוהה
                float span = 0.5f * aspect;
                uMin = 0.0f;
                uMax = 1.0f;
                vMin = 0.5f - span;
                vMax = 0.5f + span;
            }

            GL.glBegin(GL.GL_QUADS);
            GL.glTexCoord2f(uMin, vMin); GL.glVertex3f(x1, y1, z1);
            GL.glTexCoord2f(uMax, vMin); GL.glVertex3f(x2, y2, z2);
            GL.glTexCoord2f(uMax, vMax); GL.glVertex3f(x3, y3, z3);
            GL.glTexCoord2f(uMin, vMax); GL.glVertex3f(x4, y4, z4);
            GL.glEnd();
        }

        public static void DrawEarth()
        {
            if (!isInitialized)
            {
                GenerateTextures();
            }

            // הגדרות Skybox: כיבוי תאורה, הפעלת בדיקת עומק אך ביטול כתיבה אליו (כדי שיהיה תמיד ברקע)
            GL.glDisable(GL.GL_LIGHTING);
            GL.glEnable(GL.GL_DEPTH_TEST);
            GL.glDepthMask(0);
            GL.glEnable(GL.GL_TEXTURE_2D);
            GL.glColor3f(1.0f, 1.0f, 1.0f);

            float s = SkyboxSize;
            float floorZ = FloorHeight;
            float ceilZ = CeilingHeight;

            // 1. קיר קדמי (Y = -s) - Textures[0]
            DrawFace(0, +s, -s, floorZ,  -s, -s, floorZ,  -s, -s, ceilZ,  +s, -s, ceilZ);

            // 2. קיר אחורי (Y = +s) - Textures[1]
            DrawFace(1, -s, +s, floorZ,  +s, +s, floorZ,  +s, +s, ceilZ,  -s, +s, ceilZ);

            // 3. קיר שמאלי (X = -s) - Textures[2]
            DrawFace(2, -s, -s, floorZ,  -s, +s, floorZ,  -s, +s, ceilZ,  -s, -s, ceilZ);

            // 4. קיר ימני (X = +s) - Textures[3]
            DrawFace(3, +s, +s, floorZ,  +s, -s, floorZ,  +s, -s, ceilZ,  +s, +s, ceilZ);

            // 5. תקרה / שמיים (Z = ceilZ) - Textures[4] (cube-6.jpg)
            if (angleTexture != 0)
            {
                GL.glMatrixMode(GL.GL_TEXTURE);
                GL.glPushMatrix();
                GL.glLoadIdentity();
                GL.glTranslated(0.5, 0.5, 0);
                GL.glRotated(angleTexture, 0, 0, 1);
                GL.glTranslated(-0.5, -0.5, 0);
                GL.glMatrixMode(GL.GL_MODELVIEW);
            }

            DrawFace(4, -s, -s, ceilZ,  +s, -s, ceilZ,  +s, +s, ceilZ,  -s, +s, ceilZ);

            if (angleTexture != 0)
            {
                GL.glMatrixMode(GL.GL_TEXTURE);
                GL.glPopMatrix();
                GL.glMatrixMode(GL.GL_MODELVIEW);
            }

            // 6. רצפה / דשא (Z = floorZ) - Textures[5] (cube-1.jpg)
            DrawFace(5, -s, -s, floorZ,  +s, -s, floorZ,  +s, +s, floorZ,  -s, +s, floorZ);

            // שחזור הגדרות OpenGL
            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glDepthMask(1);
        }
        
    }
}
