using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Clawbyrinth
{
    public class Camera
    {
        private float x, y;
        private float targetX, targetY;
        private float followSpeed;
        private int viewportWidth, viewportHeight;
        private float zoom;
        
        public float X => x;
        public float Y => y;
        public float Zoom => zoom;
        public Rectangle ViewBounds => new Rectangle(
            (int)(x - viewportWidth / 2), 
            (int)(y - viewportHeight / 2), 
            viewportWidth, 
            viewportHeight
        );

        public Camera(int viewportWidth, int viewportHeight, float followSpeed = 8.0f)
        {
            this.viewportWidth = viewportWidth;
            this.viewportHeight = viewportHeight;
            this.followSpeed = followSpeed;
            this.zoom = 3.0f;
            
            x = targetX = viewportWidth / 2;
            y = targetY = viewportHeight / 2;
        }

        public void Update(float deltaTime)
        {
            // Smoothly move camera towards target
            float lerpFactor = Math.Min(1.0f, followSpeed * deltaTime);
            
            x = Lerp(x, targetX, lerpFactor);
            y = Lerp(y, targetY, lerpFactor);
        }

        public void FollowTarget(PointF targetPosition)
        {
            targetX = targetPosition.X;
            targetY = targetPosition.Y;
        }

        public void SetPosition(PointF position)
        {
            x = targetX = position.X;
            y = targetY = position.Y;
        }

        public void ApplyTransform(Graphics g)
        {
            // Apply camera transformation
            // First translate to center the viewport
            g.TranslateTransform(viewportWidth / 2, viewportHeight / 2);
            // Then apply zoom
            g.ScaleTransform(zoom, zoom);
            // Finally translate by camera position (negated to move world opposite to camera)
            g.TranslateTransform(-x, -y);
        }

        public void RemoveTransform(Graphics g)
        {
            // Reset transformation matrix
            g.ResetTransform();
        }

        public PointF ScreenToWorld(PointF screenPoint)
        {
            return new PointF(
                (screenPoint.X - viewportWidth / 2) / zoom + x,
                (screenPoint.Y - viewportHeight / 2) / zoom + y
            );
        }

        public PointF WorldToScreen(PointF worldPoint)
        {
            return new PointF(
                (worldPoint.X - x) * zoom + viewportWidth / 2,
                (worldPoint.Y - y) * zoom + viewportHeight / 2
            );
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        public void SetZoom(float newZoom)
        {
            zoom = Math.Max(0.1f, Math.Min(5.0f, newZoom)); // Clamp zoom between 0.1x and 5x
        }

        public void AdjustZoom(float zoomDelta)
        {
            SetZoom(zoom + zoomDelta);
        }
    }
}
