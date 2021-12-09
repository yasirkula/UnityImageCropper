using UnityEngine;
using UnityEngine.EventSystems;
using Direction = ImageCropper.Direction;

namespace ImageCropperNamespace
{
	public class SelectionMovementHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ISelectionHandler
	{
		private const float SCROLL_DISTANCE = 5f;

#pragma warning disable 0649
		[SerializeField]
		private ImageCropper manager;
#pragma warning restore 0649

		private RectTransform selection;

		private Vector2 initialPosition;
		private Vector2 initialTouchPosition;

		private int draggingPointer;

		private void Awake()
		{
			selection = manager.Selection;
		}

		private void OnDisable()
		{
			manager.StopModifySelectionWith( this );
		}

		public void OnBeginDrag( PointerEventData eventData )
		{
			if( !manager.CanModifySelectionWith( this ) )
			{
				eventData.pointerDrag = null;
				return;
			}

			draggingPointer = eventData.pointerId;

			initialPosition = selection.anchoredPosition;
			initialTouchPosition = manager.GetTouchPosition( eventData.pressPosition, eventData.pressEventCamera );
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != draggingPointer )
			{
				eventData.pointerDrag = null;
				return;
			}

			manager.UpdateSelection( initialPosition + manager.GetTouchPosition( eventData.position, eventData.pressEventCamera ) - initialTouchPosition );
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId == draggingPointer )
				manager.StopModifySelectionWith( this );
		}

		public void OnUpdate()
		{
			bool shouldUpdateViewport = false;
			float scale = manager.ImageHolder.localScale.z;

			Vector2 imagePosition = manager.ImageHolder.anchoredPosition;
			Vector2 selectionBottomLeft = imagePosition + selection.anchoredPosition * scale;
			Vector2 selectionTopRight = selectionBottomLeft + selection.sizeDelta * scale;
			Vector2 selectionSize = selectionTopRight - selectionBottomLeft;

			Vector2 viewportSize = manager.ViewportSize;

			if( selectionBottomLeft.x <= SCROLL_DISTANCE )
			{
				imagePosition = manager.ScrollImage( imagePosition, Direction.Left );
				selectionBottomLeft.x = 0f;

				shouldUpdateViewport = true;
			}
			else if( selectionTopRight.x >= viewportSize.x - SCROLL_DISTANCE )
			{
				imagePosition = manager.ScrollImage( imagePosition, Direction.Right );
				selectionBottomLeft.x = viewportSize.x - selectionSize.x;

				shouldUpdateViewport = true;
			}

			if( selectionBottomLeft.y <= SCROLL_DISTANCE )
			{
				imagePosition = manager.ScrollImage( imagePosition, Direction.Bottom );
				selectionBottomLeft.y = 0f;

				shouldUpdateViewport = true;
			}
			else if( selectionTopRight.y >= viewportSize.y - SCROLL_DISTANCE )
			{
				imagePosition = manager.ScrollImage( imagePosition, Direction.Top );
				selectionBottomLeft.y = viewportSize.y - selectionSize.y;

				shouldUpdateViewport = true;
			}

			if( shouldUpdateViewport )
			{
				manager.ImageHolder.anchoredPosition = imagePosition;
				manager.UpdateSelection( ( selectionBottomLeft - imagePosition ) / scale );
			}
		}

		public void Stop()
		{
			draggingPointer--;
		}
	}
}