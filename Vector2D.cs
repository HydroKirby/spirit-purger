/*
 * Larry Resnik          Pgm 9-1           Game (Shmup)
 * 
 * Created by SharpDevelop.
 * SharpDevelop Version : 3.0.0.3800
 * .NET Version         : 2.0.50727.4200
 * 
 * Date: 11/19/2009
 * Time: 3:53 PM
 */

using System;

namespace TestSFMLDotNet
{
    /// <summary>
    /// Holds integer coordinates.
    /// This is merely a shell of a replacement for what is offered in System.Drawing.
    /// I do not want to include GDI+ elements, so System.Drawing is banned.
    /// </summary>
    public class Point
    {
        protected int x = 0;
        protected int y = 0;

        public int X
        {
            get { return x; }
            set { x = value; }
        }

        public int Y
        {
            get { return y; }
            set { y = value; }
        }

        public Point(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    /// <summary>
    /// Holds rectangular extremities.
    /// This is another hastily composed stand-in for a System.Drawing class.
    /// </summary>
    public class Rectangle
    {
        public int Top;
        public int Bottom;
        public int Left;
        public int Right;
    }

	/// <summary>
	/// Vector made for 2-dimensions. It is set up for radians.
	/// </summary>
	public class Vector2D {
		protected double x = 0.0;
		protected double y = 0.0;
		
		public double X {
			get { return x; }
			set { x = value; }
		}
		
		public double Y {
			get { return y; }
			set { y = value; }
		}
		
		public Vector2D() {}
		
		public Vector2D(double radians) {
			SetAngle(radians);
		}
		
		public Vector2D(double x, double y) {
			X = x;
			Y = y;
		}
		
		public Vector2D(Point pt) {
			X = pt.X;
			Y = pt.Y;
		}
		
        // TODO: Can I remove this?
        /*
		public Vector2D(PointF pt) {
			X = pt.X;
			Y = pt.Y;
		}
         */
		
		public Vector2D(Vector2D vector) {
			X = vector.X;
			Y = vector.Y;
		}
		
		public static Vector2D operator+(Vector2D v1, Vector2D v2) {
			return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
		}
		
		public static Vector2D operator+(Point p, Vector2D v) {
			return new Vector2D(p.X + v.X, p.Y + v.Y);
		}
		
		public static Vector2D operator+(Vector2D v, Point p) {
			return new Vector2D(v.X + p.X, v.Y + p.Y);
		}
		
		public static Vector2D operator-(Vector2D v1, Vector2D v2) {
			return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
		}
		
		public static Vector2D operator-(Point p, Vector2D v) {
			return new Vector2D(p.X - v.X, p.Y - v.Y);
		}
		
		public static Vector2D operator-(Vector2D v, Point p) {
			return new Vector2D(v.X - p.X, v.Y - p.Y);
		}
		
		public static double RadiansToDegrees(double radians) {
			return radians * 180.0 / Math.PI;
		}
		
		public static double DegreesToRadians(double degrees) {
			return Math.PI * degrees / 180.0;
		}
		
		/// <summary>
		/// Returns an angle which decribes the exact angle in radians from
		/// one point to another. If the points are coincident, a setting of
		/// 0 radians is used.
		/// </summary>
		/// <param name="fromPt">The point to begin from.</param>
		/// <param name="destPt">The destination point.</param>
		/// <returns>A angle between dest to fromPt.</returns>
		public static double GetDirection(Vector2D fromPt, Vector2D destPt) {
			double distance = GetDistance(fromPt, destPt);
			double radians = 0;
			if (distance != 0)
				radians = Math.Acos((fromPt.X - destPt.X) / distance);
			if (fromPt.Y < destPt.Y)
				radians = -radians;
			return radians;
		}
		
		public static Vector2D GetDirectionVector(Vector2D fromPt,
		                                           Vector2D destPt) {
			double radians = GetDirection(fromPt, destPt);
			return new Vector2D(Math.Cos(radians), Math.Sin(radians));
		}
		
		public static double GetDistance(Point pt1, Point pt2) {
			return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) +
			                 (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
		}
		
		public static double GetDistance(Vector2D pt1, Vector2D pt2) {
			return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) +
			                 (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
		}
		
		public void SetAngle(double radians) {
			X = Math.Cos(radians);
			Y = Math.Sin(radians);
		}
		
		public double GetAngle() {
			return Math.Atan2(Y, X);
		}
		
		public void Normalize() {
			double magnitude = Math.Sqrt(X * X + Y * Y);
			if (magnitude >= -0.00001 || magnitude <= 0.00001)
				// It is dangerously close to 0. Do not try division by 0.
				return;
			X /= magnitude;
			Y /= magnitude;
		}
		
		/// <summary>
		/// Returns this vector as a Point with integer coordinates.
		/// </summary>
		/// <returns>A Point representing this vector.</returns>
		public Point AsPoint() {
			return new Point((int) X, (int) Y);
		}
	}
}
