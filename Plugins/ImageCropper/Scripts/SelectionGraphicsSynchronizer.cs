using UnityEngine;

namespace ImageCropperNamespace
{
	public class SelectionGraphicsSynchronizer : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private ImageCropper manager;

		[SerializeField]
		private RectTransform selectionBottomLeft;

		[SerializeField]
		private RectTransform selectionTopRight;
#pragma warning restore 0649

		private RectTransform viewport;
		private RectTransform selectionGraphics;

		private Vector2 bottomLeftPrevPosition, topRightPrevPosition;

		private void Awake()
		{
			viewport = manager.Viewport;
			selectionGraphics = manager.SelectionGraphics;
		}

		private void Start()
		{
			bottomLeftPrevPosition = selectionBottomLeft.position;
			topRightPrevPosition = selectionTopRight.position;

			Synchronize( selectionBottomLeft.position, selectionTopRight.position );
		}

		public void Synchronize()
		{
			Vector2 bottomLeftPosition = selectionBottomLeft.position;
			Vector2 topRightPosition = selectionTopRight.position;

			if( bottomLeftPosition != bottomLeftPrevPosition || topRightPosition != topRightPrevPosition )
				Synchronize( bottomLeftPosition, topRightPosition );
		}

		private void Synchronize( Vector2 bottomLeft, Vector2 topRight )
		{
			Vector2 position = viewport.InverseTransformPoint( bottomLeft );
			Vector2 size = (Vector2) viewport.InverseTransformPoint( topRight ) - position;

			selectionGraphics.anchoredPosition = position;
			selectionGraphics.sizeDelta = size;

			bottomLeftPrevPosition = bottomLeft;
			topRightPrevPosition = topRight;
		}
	}
}