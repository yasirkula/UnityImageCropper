using UnityEngine;
using Orientation = ImageCropper.Orientation;

namespace ImageCropperNamespace
{
	public static class ImageCropperUtils
	{
		public static Vector2 RoundToInt( this Vector2 vector )
		{
			vector.x = (int) ( vector.x + 0.5f );
			vector.y = (int) ( vector.y + 0.5f );

			return vector;
		}

		public static Vector2 CeilToInt( this Vector2 vector )
		{
			vector.x = (int) ( vector.x + 0.999f );
			vector.y = (int) ( vector.y + 0.999f );

			return vector;
		}

		public static Vector2 FloorToInt( this Vector2 vector )
		{
			vector.x = (int) vector.x;
			vector.y = (int) vector.y;

			return vector;
		}

		public static Vector2 ClampBetween( this Vector2 vector, Vector2 min, Vector2 max )
		{
			if( min.x < max.x )
			{
				if( vector.x < min.x )
					vector.x = min.x;
				else if( vector.x > max.x )
					vector.x = max.x;
			}
			else
			{
				if( vector.x < max.x )
					vector.x = max.x;
				else if( vector.x > min.x )
					vector.x = min.x;
			}

			if( min.y < max.y )
			{
				if( vector.y < min.y )
					vector.y = min.y;
				else if( vector.y > max.y )
					vector.y = max.y;
			}
			else
			{
				if( vector.y < max.y )
					vector.y = max.y;
				else if( vector.y > min.y )
					vector.y = min.y;
			}

			return vector;
		}

		public static Vector2 LerpTo( this Vector2 from, Vector2 to, float t )
		{
			return new Vector2( from.x + ( to.x - from.x ) * t, from.y + ( to.y - from.y ) * t );
		}

		public static Vector2 ScaleWith( this Vector2 vector, Vector2 scale )
		{
			return new Vector2( vector.x * scale.x, vector.y * scale.y );
		}

		public static Orientation GetExifFixOrientation( this Orientation exifOrientation )
		{
			if( exifOrientation == Orientation.Normal )
				return Orientation.Normal;
			else if( exifOrientation == Orientation.Rotate90 )
				return Orientation.Rotate270;
			else if( exifOrientation == Orientation.Rotate180 )
				return Orientation.Rotate180;
			else if( exifOrientation == Orientation.Rotate270 )
				return Orientation.Rotate90;
			else if( exifOrientation == Orientation.FlipHorizontal )
				return Orientation.FlipHorizontal;
			else if( exifOrientation == Orientation.Transpose )
				return Orientation.Transverse;
			else if( exifOrientation == Orientation.FlipVertical )
				return Orientation.FlipVertical;
			else
				return Orientation.Transpose;
		}
	}
}