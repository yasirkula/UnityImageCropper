using UnityEngine;
using UnityEngine.EventSystems;
using Direction = ImageCropper.Direction;

namespace ImageCropperNamespace
{
	public class SelectionResizeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, ISelectionHandler
	{
		private const float SCROLL_DISTANCE = 70f;
		private const float SELECTION_MAX_DISTANCE_FOR_SCROLL = 50f;

#pragma warning disable 0649
		[SerializeField]
		private ImageCropper manager;

		[SerializeField]
		private Direction direction;

		[SerializeField]
		private Direction secondaryDirection = Direction.None;
#pragma warning restore 0649

		private Direction directions;
		private Direction pivot;

		private RectTransform selection;

		private Vector2 initialPosition;
		private Vector2 initialTouchPosition;

		private Vector2 initialSelectionPosition;
		private Vector2 initialSelectionSize;

		private int draggingPointer;
		private PointerEventData draggingPointerEventData;

		private void Awake()
		{
			selection = manager.Selection;

			if( direction == Direction.None )
			{
				Direction temp = direction;
				direction = secondaryDirection;
				secondaryDirection = temp;
			}

			directions = direction | secondaryDirection;

			pivot = Direction.None;
			if( ( directions & Direction.Left ) == Direction.Left )
				pivot |= Direction.Right;
			else if( ( directions & Direction.Right ) == Direction.Right )
				pivot |= Direction.Left;

			if( ( directions & Direction.Top ) == Direction.Top )
				pivot |= Direction.Bottom;
			else if( ( directions & Direction.Bottom ) == Direction.Bottom )
				pivot |= Direction.Top;
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
			draggingPointerEventData = eventData;

			if( ( directions & Direction.Left ) == Direction.Left )
				initialPosition.x = selection.anchoredPosition.x;
			else if( ( directions & Direction.Right ) == Direction.Right )
				initialPosition.x = selection.anchoredPosition.x + selection.sizeDelta.x;

			if( ( directions & Direction.Top ) == Direction.Top )
				initialPosition.y = selection.anchoredPosition.y + selection.sizeDelta.y;
			else if( ( directions & Direction.Bottom ) == Direction.Bottom )
				initialPosition.y = selection.anchoredPosition.y;

			initialTouchPosition = manager.GetTouchPosition( eventData.pressPosition, eventData.pressEventCamera );

			initialSelectionPosition = selection.anchoredPosition;
			initialSelectionSize = selection.sizeDelta;
		}

		public void OnDrag( PointerEventData eventData )
		{
			if( eventData.pointerId != draggingPointer )
			{
				eventData.pointerDrag = null;
				return;
			}

			draggingPointerEventData = eventData;

			Vector2 newPosition = initialPosition + manager.GetTouchPosition( eventData.position, eventData.pressEventCamera ) - initialTouchPosition;
			Vector2 selectionPosition = initialSelectionPosition;
			Vector2 selectionSize = initialSelectionSize;

			if( ( directions & Direction.Left ) == Direction.Left )
			{
				if( newPosition.x < manager.SelectionSnapToEdgeThreshold )
					newPosition.x = 0f;

				selectionSize.x -= newPosition.x - selectionPosition.x;
				selectionPosition.x = newPosition.x;
			}
			else if( ( directions & Direction.Right ) == Direction.Right )
			{
				if( newPosition.x > manager.OrientedImageSize.x - manager.SelectionSnapToEdgeThreshold )
					newPosition.x = manager.OrientedImageSize.x;

				selectionSize.x = newPosition.x - selectionPosition.x;
			}

			if( ( directions & Direction.Top ) == Direction.Top )
			{
				if( newPosition.y > manager.OrientedImageSize.y - manager.SelectionSnapToEdgeThreshold )
					newPosition.y = manager.OrientedImageSize.y;

				selectionSize.y = newPosition.y - selectionPosition.y;
			}
			else if( ( directions & Direction.Bottom ) == Direction.Bottom )
			{
				if( newPosition.y < manager.SelectionSnapToEdgeThreshold )
					newPosition.y = 0f;

				selectionSize.y -= newPosition.y - selectionPosition.y;
				selectionPosition.y = newPosition.y;
			}

			bool shouldExpand = false;
			if( secondaryDirection == Direction.None )
			{
				if( direction == Direction.Left || direction == Direction.Right )
				{
					if( selectionSize.x > initialSelectionSize.x )
						shouldExpand = true;
				}
				else
				{
					if( selectionSize.y > initialSelectionSize.y )
						shouldExpand = true;
				}
			}

			manager.UpdateSelection( selectionPosition, selectionSize, pivot, !shouldExpand );
		}

		public void OnEndDrag( PointerEventData eventData )
		{
			if( eventData.pointerId == draggingPointer )
			{
				draggingPointerEventData = null;
				manager.StopModifySelectionWith( this );
			}
		}

		public void OnUpdate()
		{
			if( draggingPointerEventData == null )
				return;

			Vector2 pointerLocalPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle( manager.Viewport, draggingPointerEventData.position, draggingPointerEventData.pressEventCamera, out pointerLocalPos );

			bool shouldUpdateViewport = false;
			float scale = manager.ImageHolder.localScale.z;

			Vector2 imagePosition = manager.ImageHolder.anchoredPosition;
			Vector2 selectionBottomLeft = imagePosition + selection.anchoredPosition * scale;
			Vector2 selectionTopRight = selectionBottomLeft + selection.sizeDelta * scale;

			Vector2 viewportSize = manager.ViewportSize;

			if( ( directions & Direction.Left ) == Direction.Left || ( directions & Direction.Right ) == Direction.Right )
			{
				if( pointerLocalPos.x <= SCROLL_DISTANCE && selectionBottomLeft.x <= SELECTION_MAX_DISTANCE_FOR_SCROLL )
				{
					imagePosition = manager.ScrollImage( imagePosition, Direction.Left );
					shouldUpdateViewport = true;
				}
				else if( pointerLocalPos.x >= viewportSize.x - SCROLL_DISTANCE && selectionTopRight.x >= viewportSize.x - SELECTION_MAX_DISTANCE_FOR_SCROLL )
				{
					imagePosition = manager.ScrollImage( imagePosition, Direction.Right );
					shouldUpdateViewport = true;
				}
			}

			if( ( directions & Direction.Bottom ) == Direction.Bottom || ( directions & Direction.Top ) == Direction.Top )
			{
				if( pointerLocalPos.y <= SCROLL_DISTANCE && selectionBottomLeft.y <= SELECTION_MAX_DISTANCE_FOR_SCROLL )
				{
					imagePosition = manager.ScrollImage( imagePosition, Direction.Bottom );
					shouldUpdateViewport = true;
				}
				else if( pointerLocalPos.y >= viewportSize.y - SCROLL_DISTANCE && selectionTopRight.y >= viewportSize.y - SELECTION_MAX_DISTANCE_FOR_SCROLL )
				{
					imagePosition = manager.ScrollImage( imagePosition, Direction.Top );
					shouldUpdateViewport = true;
				}
			}

			if( shouldUpdateViewport )
			{
				manager.ImageHolder.anchoredPosition = imagePosition;
				OnDrag( draggingPointerEventData );
			}
		}

		public void Stop()
		{
			draggingPointer--;
			draggingPointerEventData = null;
		}
	}
}