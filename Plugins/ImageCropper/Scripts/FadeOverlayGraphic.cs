using UnityEngine;
using UnityEngine.UI;

namespace ImageCropperNamespace
{
	[RequireComponent( typeof( CanvasRenderer ) )]
	public class FadeOverlayGraphic : Graphic
	{
		private const float OFFSET = 20000f;

#pragma warning disable 0649
		[SerializeField]
		private Sprite renderSprite;
#pragma warning restore 0649

		private Vector2 uv = Vector2.zero;
		private Color32 color32;

		public override Texture mainTexture { get { return renderSprite != null ? renderSprite.texture : s_WhiteTexture; } }

		protected override void Awake()
		{
			base.Awake();

			if( renderSprite != null )
			{
				Vector4 packedUv = UnityEngine.Sprites.DataUtility.GetOuterUV( renderSprite );
				uv = new Vector2( packedUv.x + packedUv.z, packedUv.y + packedUv.w ) * 0.5f; // uv center point
			}
		}

		protected override void OnPopulateMesh( VertexHelper vh )
		{
			Rect r = GetPixelAdjustedRect();

			float xMin = r.x, xMax = r.x + r.width;
			float yMin = r.y, yMax = r.y + r.height;

			color32 = color;
			vh.Clear();

			GenerateMesh( vh, -OFFSET, yMax, OFFSET, OFFSET, 0 );
			GenerateMesh( vh, -OFFSET, -OFFSET, OFFSET, yMin, 4 );
			GenerateMesh( vh, -OFFSET, yMin, xMin, yMax, 8 );
			GenerateMesh( vh, xMax, yMin, OFFSET, yMax, 12 );
		}

		private void GenerateMesh( VertexHelper vh, float xMin, float yMin, float xMax, float yMax, int triangleIndex )
		{
			vh.AddVert( new Vector3( xMin, yMin ), color32, uv );
			vh.AddVert( new Vector3( xMin, yMax ), color32, uv );
			vh.AddVert( new Vector3( xMax, yMax ), color32, uv );
			vh.AddVert( new Vector3( xMax, yMin ), color32, uv );

			vh.AddTriangle( triangleIndex, triangleIndex + 1, triangleIndex + 2 );
			vh.AddTriangle( triangleIndex + 2, triangleIndex + 3, triangleIndex );
		}
	}
}