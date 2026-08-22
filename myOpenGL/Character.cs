using OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myOpenGL
{
    public class Character
    {
        private float xPosition;
        private float yPosition;
        private float zPosition;

        public float ARM_angle;
        public float SHOULDER_angle;
        public float ROBOT_angle;
        public float alpha;

        uint ROBOT_LIST, ARM_LIST, SHOULDER_LIST;
        float r;
        GLUquadric obj;


        public Character(float xPosition, float yPosition, float zPosition)
        {
            this.xPosition = xPosition;
            this.yPosition = yPosition;
            this.zPosition = zPosition;
        }

       public void PrepareLists()
        {
            float ARM_length, SHOULDER_length;
            ARM_length = 2;
            ARM_angle = -45;
            SHOULDER_length = 2.5f;
            SHOULDER_angle = 10;
            ROBOT_angle = 45;
            r = 0.3f;

            obj = GLU.gluNewQuadric();

            ROBOT_LIST = GL.glGenLists(3);
            ARM_LIST = ROBOT_LIST + 1;
            SHOULDER_LIST = ROBOT_LIST + 2;

            GL.glPushMatrix();
            GL.glNewList(ARM_LIST, GL.GL_COMPILE);
            //cone
            GL.glColor3f(0.5f, 0, 0);
            GLU.gluCylinder(obj, r, 0, ARM_length, 20, 20);
            GL.glTranslated(0, 0, ARM_length);
            //internal disk
            GL.glColor3f(1, 1, 0);
            GLU.gluDisk(obj, 0, r * 0.5, 20, 20);
            //external disk
            GL.glColor3f(1, 0, 0);
            GLU.gluDisk(obj, r * 0.5, r * 1.5, 20, 20);
            GL.glEndList();
            GL.glPopMatrix();

            GL.glPushMatrix();
            GL.glNewList(SHOULDER_LIST, GL.GL_COMPILE);
            GL.glColor3f(0, 0.5f, 0);
            GLU.gluCylinder(obj, r, r, SHOULDER_length, 20, 20);
            GL.glTranslated(0, 0, SHOULDER_length);
            GL.glColor3f(0, 1, 0);
            GLU.gluSphere(obj, r * 1.2, 20, 20);
            GL.glEndList();
            GL.glPopMatrix();

            CreateRobotList();
        }


        public void CreateRobotList()
        {

            GL.glPushMatrix();
            //
            // hierarchical list
            //
            GL.glNewList(ROBOT_LIST, GL.GL_COMPILE);

            // BASE : no rotations!!! Angle will be implemented in the CALL routine
            //                   before call to CreateRobotList()
            GL.glColor3f(0, 0, 0.5f);
            GLU.gluCylinder(obj, 3 * r, 3 * r, r * 1.2, 40, 20);
            GL.glTranslated(0, 0, r * 1.2);
            GLU.gluDisk(obj, 0, 3 * r, 40, 20);
            GL.glColor3f(0, 0, 1);
            GLU.gluSphere(obj, r * 1.2, 20, 20);
            // end base

            // transformations
            GL.glRotatef(SHOULDER_angle, 1, 0, 0);

            // call SHOULDER 
            GL.glCallList(SHOULDER_LIST);

            // transformations
            //no need in glTranslated 0, 0, SHOULDER_length
            //it is located properly now !!!
            GL.glRotatef(ARM_angle, 1, 0, 0);

            // call ARM  
            GL.glCallList(ARM_LIST);
            GL.glEndList();
            GL.glPopMatrix();
        }

        public void DrawCube()
        {
            // cube
            GL.glBegin(GL.GL_QUADS);

            //1

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            //2

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);


            //3

            GL.glColor3f(0.0f, 0.0f, 0.0f);
            GL.glVertex3f(0.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);


            //4

            GL.glColor3f(1.0f, 0.0f, 0.0f);
            GL.glVertex3f(1.0f, 0.0f, 0.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);


            //5

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(1.0f, 1.0f, 0.0f);
            GL.glVertex3f(1.0f, 1.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 0.0f);
            GL.glVertex3f(0.0f, 1.0f, 0.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);


            //6

            GL.glColor3f(1.0f, 1.0f, 1.0f);
            GL.glVertex3f(1.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 1.0f, 1.0f);
            GL.glVertex3f(0.0f, 1.0f, 1.0f);

            GL.glColor3f(0.0f, 0.0f, 1.0f);
            GL.glVertex3f(0.0f, 0.0f, 1.0f);

            GL.glColor3f(1.0f, 0.0f, 1.0f);
            GL.glVertex3f(1.0f, 0.0f, 1.0f);


            GL.glEnd();

        }

        public void DrawCharacter()
        {
            PrepareLists();
            GL.glRotatef(ROBOT_angle, 0, 0, 1);
            GL.glCallList(ROBOT_LIST);
        }
    }
}
