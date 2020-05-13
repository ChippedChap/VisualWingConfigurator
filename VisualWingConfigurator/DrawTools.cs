using UnityEngine;

namespace VisualWingConfigurator
{
    // This is a stripped down version of a class written by Sarbian (https://github.com/sarbian)
    public static class DrawTools
    {
        private static Material _material;

        private static int glDepth = 0;

        private static Material material
        {
            get
            {
                if (_material == null) _material = new Material(Shader.Find("Hidden/Internal-Colored"));
                return _material;
            }
        }

        // Ok that's cheap but I did not want to add a bunch 
        // of try catch to make sure the GL calls ends.
        public static void NewFrame()
        {
            if (glDepth > 0)
                MonoBehaviour.print(glDepth);
            glDepth = 0;
        }

        private static void GLStart()
        {
            if (glDepth == 0)
            {
                GL.PushMatrix();
                material.SetPass(0);
                GL.LoadPixelMatrix();
                GL.Begin(GL.LINES);
            }
            glDepth++;
        }

        private static void GLEnd()
        {
            glDepth--;

            if (glDepth == 0)
            {
                GL.End();
                GL.PopMatrix();
            }
        }

        private static Camera GetActiveCam()
        {
            Camera cam;
            if (HighLogic.LoadedSceneIsEditor)
                cam = EditorLogic.fetch.editorCamera;
            else if (HighLogic.LoadedSceneIsFlight)
                cam = MapView.MapIsEnabled ? PlanetariumCamera.Camera : FlightCamera.fetch.mainCamera;
            else
                cam = Camera.main;
            return cam;
        }

        private static void Line(Vector3 origin, Vector3 destination, Color color)
        {
            Camera cam = GetActiveCam();

            Vector3 screenPoint1 = cam.WorldToScreenPoint(origin);
            Vector3 screenPoint2 = cam.WorldToScreenPoint(destination);

            GL.Color(color);
            GL.Vertex3(screenPoint1.x, screenPoint1.y, 0);
            GL.Vertex3(screenPoint2.x, screenPoint2.y, 0);
        }

        private static void Ray(Vector3 origin, Vector3 direction, Color color)
        {
            Camera cam = GetActiveCam();

            Vector3 screenPoint1 = cam.WorldToScreenPoint(origin);
            Vector3 screenPoint2 = cam.WorldToScreenPoint(origin + direction);

            GL.Color(color);
            GL.Vertex3(screenPoint1.x, screenPoint1.y, 0);
            GL.Vertex3(screenPoint2.x, screenPoint2.y, 0);
        }

        public static void DrawLine(Vector3 from, Vector3 to, Color color)
        {
            GLStart();
            GL.Color(color);
            Line(from, to, color);
            GLEnd();
        }

        public static void DrawPoint(Vector3 position, Color color, float scale = 1.0f)
        {
            GLStart();
            GL.Color(color);

            Ray(position + Vector3.up * (scale * 0.5f), -Vector3.up * scale, color);
            Ray(position + Vector3.right * (scale * 0.5f), -Vector3.right * scale, color);
            Ray(position + Vector3.forward * (scale * 0.5f), -Vector3.forward * scale, color);

            GLEnd();
        }

        public static void DrawTransform(Transform t, float scale = 1.0f)
        {
            GLStart();

            Ray(t.position, t.up * scale, Color.green);
            Ray(t.position, t.right * scale, Color.red);
            Ray(t.position, t.forward * scale, Color.blue);

            GLEnd();
        }
    }
}