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
                @"..\..\pics\cube2-4.jpg",
                @"..\..\pics\cube2-4.jpg",
                @"..\..\pics\cube2-4.jpg",
                @"..\..\pics\cube2-4.jpg",
                @"..\..\pics\cube-6.jpg",
                @"..\..\pics\cube-6.jpg",
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
                uMin = 0.0f; uMax = 1.0f;
                vMin = 0.0f; vMax = 1.0f;
            }
            else if (aspect > 1.0f)
            {
                float span = 0.5f / aspect;
                uMin = 0.5f - span;
                uMax = 0.5f + span;
                vMin = 0.0f;
                vMax = 1.0f;
            }
            else
            {
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

            GL.glDisable(GL.GL_LIGHTING);
            GL.glEnable(GL.GL_DEPTH_TEST);
            GL.glDepthMask(0);
            GL.glEnable(GL.GL_TEXTURE_2D);
            GL.glColor3f(1.0f, 1.0f, 1.0f);

            float s = SkyboxSize;
            float floorZ = FloorHeight;
            float ceilZ = CeilingHeight;

            DrawFace(0, +s, -s, floorZ,  -s, -s, floorZ,  -s, -s, ceilZ,  +s, -s, ceilZ);
            DrawFace(1, -s, +s, floorZ,  +s, +s, floorZ,  +s, +s, ceilZ,  -s, +s, ceilZ);
            DrawFace(2, -s, -s, floorZ,  -s, +s, floorZ,  -s, +s, ceilZ,  -s, -s, ceilZ);
            DrawFace(3, +s, +s, floorZ,  +s, -s, floorZ,  +s, -s, ceilZ,  +s, +s, ceilZ);

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

            DrawFace(5, -s, -s, floorZ,  +s, -s, floorZ,  +s, +s, floorZ,  -s, +s, floorZ);

            GL.glDisable(GL.GL_TEXTURE_2D);
            GL.glDepthMask(1);
        }
        
    }
}
