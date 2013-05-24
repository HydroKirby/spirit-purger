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
using SFML.Window;
using SFML.Graphics;

namespace TestSFMLDotNet
{
	/// <summary>
	/// Common calculations and functions.
	/// </summary>
	public static class Physics
	{
		/// <summary>
		/// Does a rectangle-rectangle collision test.
		/// </summary>
		/// <param name="e1">An Entity.</param>
		/// <param name="e2">Another Entity.</param>
		/// <returns>True if they least touch each other.</returns>
		public static bool Touches(Entity e1, Entity e2)
		{
			return e1.Location.X <= e2.Location.X + e2.Size.X &&
				e1.Location.X + e1.Size.X >= e2.Location.X &&
				e1.Location.Y <= e2.Location.Y + e2.Size.Y &&
				e1.Location.Y + e1.Size.Y >= e2.Location.Y;
		}

		/// <summary>
		/// Does a circle-circle collision test.
		/// </summary>
		/// <param name="b1">A Bullet.</param>
		/// <param name="b2">Another Bullet.</param>
		/// <returns>True if they least touch each other.</returns>
		public static bool Touches(Bullet b1, Bullet b2)
		{
			uint a = (b1.Radius + b2.Radius) * (b1.Radius + b2.Radius);
			double dx = b1.location.X - b2.location.X;
			double dy = b1.location.Y - b2.location.Y;
			return a > (dx * dx) + (dy * dy);
		}

		/// <summary>
		/// Does a rectangle-circle collision test.
		/// </summary>
		/// <param name="entity">An Entity.</param>
		/// <param name="bullet">A Bullet.</param>
		/// <returns>True if they least touch each other.</returns>
		public static bool Touches(Entity entity, Bullet bullet)
		{
			// Get the center of the circle relative to the center of this.
			// Actually, the sprite is centered, so let's just copy the location.
			Vector2f rectCenter = new Vector2f(entity.Location.X, entity.Location.Y);
			Vector2f circleCenterRelRect = bullet.location - rectCenter;

			// Get the point on the surface of the square that's closest to
			// the bullet.
			Vector2f rectPoint = new Vector2f();
			// Check circle against rect on the x-axis alone. If the circle
			// is to the left of the rect, then the left edge is closest.
			// Vice versa is true as well. When the circle is between the
			// rect's edges, the circle's distance from the rect is 0.
			if (circleCenterRelRect.X < -entity.HalfSize.X)
				rectPoint.X = -entity.HalfSize.X;
			else if (circleCenterRelRect.X > entity.HalfSize.X)
				rectPoint.X = entity.HalfSize.X;
			else
				rectPoint.X = circleCenterRelRect.X;

			// Do the same check for the y-axis.
			if (circleCenterRelRect.Y < -entity.HalfSize.Y)
				rectPoint.Y = -entity.HalfSize.Y;
			else if (circleCenterRelRect.Y > entity.HalfSize.Y)
				rectPoint.Y = entity.HalfSize.Y;
			else
				rectPoint.Y = circleCenterRelRect.Y;

			// See if the distance from the closest point on the rect to the
			// circle is less than the radius.
			Vector2f dist = circleCenterRelRect - rectPoint;
			return dist.X * dist.X + dist.Y * dist.Y <
				bullet.Radius * bullet.Radius;
		}
	}

	/// <summary>
	/// Provides logic for dealing with vectors.
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
		
		public Vector2D(Vector2D vector) {
			X = vector.X;
			Y = vector.Y;
		}
		
		public static Vector2D operator+(Vector2D v1, Vector2D v2) {
			return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
		}
		
		public static Vector2D operator+(Vector2u p, Vector2D v) {
			return new Vector2D(p.X + v.X, p.Y + v.Y);
		}
		
		public static Vector2D operator+(Vector2D v, Vector2u p) {
			return new Vector2D(v.X + p.X, v.Y + p.Y);
		}
		
		public static Vector2D operator-(Vector2D v1, Vector2D v2) {
			return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
		}
		
		public static Vector2D operator-(Vector2u p, Vector2D v) {
			return new Vector2D(p.X - v.X, p.Y - v.Y);
		}
		
		public static Vector2D operator-(Vector2D v, Vector2u p) {
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
		public static double GetDirection(Vector2f fromPt, Vector2f destPt) {
			double distance = GetDistance(fromPt, destPt);
			double radians = 0;
			if (distance != 0)
				radians = Math.Acos((fromPt.X - destPt.X) / distance);
			if (fromPt.Y < destPt.Y)
				radians = -radians;
			return radians;
		}

        public static Vector2f GetDirectionVector(Vector2f fromPt,
                                                   Vector2f destPt)
        {
            double radians = GetDirection(fromPt, destPt);
            return new Vector2f((float)Math.Cos(radians), (float)Math.Sin(radians));
        }
		
		public static double GetDistance(Vector2u pt1, Vector2u pt2) {
			return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) +
			                 (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
		}
		
		public static double GetDistance(Vector2f pt1, Vector2f pt2) {
			return Math.Sqrt((pt1.X - pt2.X) * (pt1.X - pt2.X) +
			                 (pt1.Y - pt2.Y) * (pt1.Y - pt2.Y));
		}
		
		public void SetAngle(double radians) {
			X = Math.Cos(radians);
			Y = Math.Sin(radians);
		}

        public static Vector2f VectorFromAngle(double radians)
        {
            return new Vector2f((float)Math.Cos(radians), (float)Math.Sin(radians));
        }
		
		public static double GetAngle(Vector2f v) {
			return Math.Atan2(v.Y, v.X);
		}

        public static void Normalize(Vector2f v)
        {
            double magnitude = Math.Sqrt(v.X * v.X + v.Y * v.Y);
            if (magnitude >= -0.00001 || magnitude <= 0.00001)
                // It is dangerously close to 0. Do not try division by 0.
                return;
            v.X /= (float)magnitude;
            v.Y /= (float)magnitude;
        }
		
		/// <summary>
		/// Returns this vector as a Point with integer coordinates.
		/// </summary>
		/// <returns>A Point representing this vector.</returns>
		public Vector2u AsPoint() {
			return new Vector2u((uint) X, (uint) Y);
		}
	}
}
