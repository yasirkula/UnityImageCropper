using UnityEngine;

namespace ImageCropperNamespace
{
	public class SelectionCornersFitter : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private RectTransform selection;

		[SerializeField]
		private RectTransform bottomLeft;

		[SerializeField]
		private RectTransform bottomRight;

		[SerializeField]
		private RectTransform topLeft;

		[SerializeField]
		private RectTransform topRight;

		[SerializeField]
		private float preferredCornerSize = 30f;

		[SerializeField]
		private float cornerSizeMaxRatio = 0.3f;
#pragma warning restore 0649

		private Vector2 inset;

		private void OnEnable()
		{
			inset = ( (RectTransform) transform ).sizeDelta * 0.5f;
			OnRectTransformDimensionsChange();
		}

		private void OnRectTransformDimensionsChange()
		{
			if( !gameObject.activeInHierarchy )
				return;

			Vector2 cornerSize;
			Vector2 maxCornerSize = selection.rect.size * cornerSizeMaxRatio + inset;
			if( preferredCornerSize <= maxCornerSize.x && preferredCornerSize <= maxCornerSize.y )
				cornerSize = new Vector2( preferredCornerSize, preferredCornerSize );
			else
				cornerSize = Vector2.one * Mathf.Min( maxCornerSize.x, maxCornerSize.y );

			float halfCornerSize = cornerSize.x * 0.5f;

			bottomLeft.anchoredPosition = new Vector2( halfCornerSize, halfCornerSize );
			bottomLeft.sizeDelta = cornerSize;

			bottomRight.anchoredPosition = new Vector2( -halfCornerSize, halfCornerSize );
			bottomRight.sizeDelta = cornerSize;

			topLeft.anchoredPosition = new Vector2( halfCornerSize, -halfCornerSize );
			topLeft.sizeDelta = cornerSize;

			topRight.anchoredPosition = new Vector2( -halfCornerSize, -halfCornerSize );
			topRight.sizeDelta = cornerSize;
		}
	}
}