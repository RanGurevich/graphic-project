using System;
using System.Collections.Generic;
using System.Drawing;

namespace OpenGL
{
    static class Earth
    {
        public static Dictionary<string, uint> textureMap = new Dictionary<string, uint>();

        enum SquareEdge
        {
            Top,
            Bottom,
            Left,
            Right
        }

        public static void DrawEarth()
        {
            GL.glEnable(GL.GL_TEXTURE_2D);
            GL.glBindTexture(GL.GL_TEXTURE_2D, GetTexture(@"..\..\pics\grass.png"));
            //DrawTexturedSquare(-10f, -10.0f, 1.0f,
            //    -10f, 10.0f, 1.0f, SquareEdge.Top, @"..\..\pics\grass.png");
            float num = 2.5f;
            float floor = -1.5f;
            float resizeFloor = 1f;
            DrawTexturedQuad(
                -num, -resizeFloor * num, floor,   // שמאל למטה
                num, -resizeFloor * num, floor,   // ימין למטה
                num, resizeFloor*num, floor,   // ימין למעלה
                -num, resizeFloor * num, floor,   // שמאל למעלה
                @"..\..\pics\grass.png"
            );

        }

        private static void DrawTexturedSquare(float x1, float y1, float z1,
                                       float x2, float y2, float z2, SquareEdge edge, string texturePath)
        {
            float dx = x2 - x1;
            float dy = y2 - y1;
            float dz = z2 - z1;
            float edgeLen = (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);

            float px, py, pz;
            float crossLen;

            if (edge == SquareEdge.Top || edge == SquareEdge.Bottom)
            {
                px = 0; py = dz; pz = -dy;
                crossLen = (float)Math.Sqrt(px * px + py * py + pz * pz);
                if (crossLen < 0.0001f)
                {
                    px = -dz; py = 0; pz = dx;
                    crossLen = (float)Math.Sqrt(px * px + py * py + pz * pz);
                }
                px = px / crossLen * edgeLen;
                py = py / crossLen * edgeLen;
                pz = pz / crossLen * edgeLen;
                if (edge == SquareEdge.Top && pz > 0) { px = -px; py = -py; pz = -pz; }
                if (edge == SquareEdge.Bottom && pz < 0) { px = -px; py = -py; pz = -pz; }
            }
            else
            {
                px = dy; py = -dx; pz = 0;
                crossLen = (float)Math.Sqrt(px * px + py * py + pz * pz);
                if (crossLen < 0.0001f)
                {
                    px = 0; py = dz; pz = -dy;
                    crossLen = (float)Math.Sqrt(px * px + py * py + pz * pz);
                }
                px = px / crossLen * edgeLen;
                py = py / crossLen * edgeLen;
                pz = pz / crossLen * edgeLen;
                if (edge == SquareEdge.Right) { px = -px; py = -py; pz = -pz; }
            }

            float x3 = x1 + px, y3 = y1 + py, z3 = z1 + pz;
            float x4 = x2 + px, y4 = y2 + py, z4 = z2 + pz;

            uint texId = GetTexture(texturePath);

            GL.glEnable(GL.GL_TEXTURE_2D);
            GL.glBindTexture(GL.GL_TEXTURE_2D, texId);
            GL.glColor3f(1.0f, 1.0f, 1.0f);

            GL.glBegin(GL.GL_QUADS);
            GL.glTexCoord2f(0.0f, 0.0f); GL.glVertex3f(x1, y1, z1);
            GL.glTexCoord2f(1.0f, 0.0f); GL.glVertex3f(x2, y2, z2);
            GL.glTexCoord2f(1.0f, 1.0f); GL.glVertex3f(x4, y4, z4);
            GL.glTexCoord2f(0.0f, 1.0f); GL.glVertex3f(x3, y3, z3);
            GL.glEnd();

            GL.glDisable(GL.GL_TEXTURE_2D);
        }

        /// <summary>
        /// מציירת מלבן עם טקסטורה (תמונה) על פי 4 פינות בתלת-מימד.
        /// הפינות צריכות להיות בסדר נגד כיוון השעון.
        /// </summary>
        /// <param name="imagePath">נתיב לקובץ התמונה (png/bmp)</param>
        private static void DrawTexturedQuad(
            float x1, float y1, float z1,  // פינה שמאלית תחתונה
            float x2, float y2, float z2,  // פינה ימנית תחתונה
            float x3, float y3, float z3,  // פינה ימנית עליונה
            float x4, float y4, float z4,  // פינה שמאלית עליונה
            string imagePath)
        {
            uint texId = GetTexture(imagePath);

            GL.glEnable(GL.GL_TEXTURE_2D);
            GL.glBindTexture(GL.GL_TEXTURE_2D, texId);
            GL.glColor3f(1.0f, 1.0f, 1.0f); // צבע לבן כדי שהתמונה תוצג כמו שהיא

            GL.glBegin(GL.GL_QUADS);
            GL.glTexCoord2f(0.0f, 0.0f); GL.glVertex3f(x1, y1, z1); // שמאל למטה
            GL.glTexCoord2f(1.0f, 0.0f); GL.glVertex3f(x2, y2, z2); // ימין למטה
            GL.glTexCoord2f(1.0f, 1.0f); GL.glVertex3f(x3, y3, z3); // ימין למעלה
            GL.glTexCoord2f(0.0f, 1.0f); GL.glVertex3f(x4, y4, z4); // שמאל למעלה
            GL.glEnd();

            GL.glDisable(GL.GL_TEXTURE_2D);
        }

        public static uint GetTexture(string imageBMPfile)
        {
            if (textureMap.ContainsKey(imageBMPfile))
                return textureMap[imageBMPfile];

            GL.glEnable(GL.GL_TEXTURE_2D);

            uint[] tex = new uint[1];

            Bitmap image = new Bitmap(imageBMPfile);
            image.RotateFlip(RotateFlipType.RotateNoneFlipY); //Y axis in Windows is directed downwards, while in OpenGL-upwards
            System.Drawing.Imaging.BitmapData bitmapdata;
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);

            bitmapdata = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly,
                System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            GL.glGenTextures(1, tex);
            GL.glBindTexture(GL.GL_TEXTURE_2D, tex[0]);

            GL.glTexImage2D(GL.GL_TEXTURE_2D, 0, (int)GL.GL_RGB8, image.Width, image.Height,
                0, GL.GL_BGR_EXT, GL.GL_UNSIGNED_byte, bitmapdata.Scan0);

            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MIN_FILTER, (int)GL.GL_LINEAR);		// Linear Filtering
            GL.glTexParameteri(GL.GL_TEXTURE_2D, GL.GL_TEXTURE_MAG_FILTER, (int)GL.GL_LINEAR);		// Linear Filtering

            image.UnlockBits(bitmapdata);
            image.Dispose();

            textureMap[imageBMPfile] = tex[0];
            return tex[0];
        }
    }
}
