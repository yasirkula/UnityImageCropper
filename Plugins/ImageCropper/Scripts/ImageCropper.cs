using UnityEngine;
using UnityEngine.UI;
using ImageCropperNamespace;
using System.Collections;

// ===
// Auto-zoom feature inspired from: Android Image Cropper https://github.com/ArthurHub/Android-Image-Cropper 
// Copyright 2016, Arthur Teplitzki, 2013, Edmodo, Inc.
// License (Apache License 2.0) https://github.com/ArthurHub/Android-Image-Cropper/blob/master/LICENSE.txt
// ===

public class ImageCropper : MonoBehaviour
{
	public class Settings
	{
		public bool autoZoomEnabled = true;
		public bool pixelPerfectSelection = false;
		public bool ovalSelection = false;
		public bool markTextureNonReadable = true;

		public Color imageBackground = Color.black;

		public Button visibleButtons = ~Button.None;
		public Visibility guidelinesVisibility = Visibility.AlwaysVisible;
		public Orientation initialOrientation = Orientation.Normal;

		public Vector2 selectionMinSize = Vector2.zero;
		public Vector2 selectionMaxSize = Vector2.zero;

		public float selectionMinAspectRatio = 0f;
		public float selectionMaxAspectRatio = 0f;

		public float selectionInitialPaddingLeft = 0.1f;
		public float selectionInitialPaddingTop = 0.1f;
		public float selectionInitialPaddingRight = 0.1f;
		public float selectionInitialPaddingBottom = 0.1f;
	}

	[System.Flags]
	public enum Direction { None = 0, Left = 1, Top = 2, Right = 4, Bottom = 8 };

	[System.Flags]
	public enum Button { None = 0, FlipHorizontal = 1, FlipVertical = 2, Rotate90Degrees = 4 };

	public enum Visibility { Hidden = 0, OnDrag = 1, AlwaysVisible = 2 };

	// EXIF orientation: http://sylvana.net/jpegcrop/exif_orientation.html (indices are reordered)
	public enum Orientation { Normal = 0, Rotate90 = 1, Rotate180 = 2, Rotate270 = 3, FlipHorizontal = 4, Transpose = 5, FlipVertical = 6, Transverse = 7 };

	public delegate void CropResult( bool result, Texture originalImage, Texture2D croppedImage );
	public delegate void ImageResizePolicy( ref int width, ref int height );

	private static ImageCropper m_instance = null;
	public static ImageCropper Instance
	{
		get
		{
			if( m_instance == null )
				m_instance = Instantiate( Resources.Load<ImageCropper>( "ImageCropper" ) );

			return m_instance;
		}
	}

#pragma warning disable 0649
	[Header( "Properties" )]
	[SerializeField]
	private float autoZoomInThreshold = 0.5f;

	[SerializeField]
	private float autoZoomOutThreshold = 0.65f;

	[SerializeField]
	private float autoZoomInFillAmount = 0.64f;

	[SerializeField]
	private float autoZoomOutFillAmount = 0.51f;

	[SerializeField]
	private AnimationCurve autoZoomCurve;

	[SerializeField]
	private float m_selectionSnapToEdgeThreshold = 5f;
	public float SelectionSnapToEdgeThreshold { get { return m_selectionSnapToEdgeThreshold; } }

	[SerializeField]
	private float viewportScrollSpeed = 512f;

	[Header( "Internal Variables" )]
	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private RectTransform m_viewport;
	public RectTransform Viewport { get { return m_viewport; } }

	[SerializeField]
	private RectTransform m_imageHolder;
	public RectTransform ImageHolder { get { return m_imageHolder; } }

	[SerializeField]
	private RawImage m_orientedImage;
	public RawImage OrientedImage { get { return m_orientedImage; } }

	[SerializeField]
	private RectTransform m_selection;
	public RectTransform Selection { get { return m_selection; } }

	[SerializeField]
	private RectTransform m_selectionGraphics;
	public RectTransform SelectionGraphics { get { return m_selectionGraphics; } }

	[SerializeField]
	private SizeChangeListener viewportSizeChangeListener;

	[SerializeField]
	private SelectionGraphicsSynchronizer selectionGraphicsSynchronizer;

	[SerializeField]
	private Behaviour[] ovalMaskElements;

	[SerializeField]
	private Behaviour[] guidelines;

	[Header( "User Interface" )]
	[SerializeField]
	private GameObject flipHorizontalButton;

	[SerializeField]
	private GameObject flipVerticalButton;

	[SerializeField]
	private GameObject rotate90DegreesButton;

	[SerializeField]
	private FontSizeSynchronizer textsSynchronizer;

	[Header( "Crop Helpers" )]
	[SerializeField]
	private Canvas cropRenderCanvas;

	[SerializeField]
	private RawImage cropRenderImage;

	[SerializeField]
	private RectTransform cropRenderSelection;

	[SerializeField]
	private Camera cropRenderCamera;
#pragma warning restore 0649

	public bool IsOpen { get { return gameObject.activeSelf; } }

	private Settings m_defaultSettings;
	private Settings DefaultSettings
	{
		get
		{
			if( m_defaultSettings == null )
				m_defaultSettings = new Settings();

			return m_defaultSettings;
		}
	}

	private bool m_autoZoomEnabled;
	public bool AutoZoomEnabled
	{
		get { return m_autoZoomEnabled; }
		set
		{
			m_autoZoomEnabled = value;
			if( m_autoZoomEnabled )
				StartAutoZoom( false );
		}
	}

	private bool m_pixelPerfectSelection;
	public bool PixelPerfectSelection
	{
		get { return m_pixelPerfectSelection; }
		set
		{
			m_pixelPerfectSelection = value;
			if( m_pixelPerfectSelection )
				MakePixelPerfectSelection();
		}
	}

	private bool m_ovalSelection;
	public bool OvalSelection
	{
		get { return m_ovalSelection; }
		set
		{
			m_ovalSelection = value;
			for( int i = 0; i < ovalMaskElements.Length; i++ )
				ovalMaskElements[i].enabled = m_ovalSelection;
		}
	}

	private Visibility m_guidelinesVisibility;
	public Visibility GuidelinesVisibility
	{
		get { return m_guidelinesVisibility; }
		set
		{
			m_guidelinesVisibility = value;

			bool visible = m_guidelinesVisibility == Visibility.AlwaysVisible;
			for( int i = 0; i < guidelines.Length; i++ )
				guidelines[i].enabled = visible;
		}
	}

	public bool MarkTextureNonReadable { get; set; }

	public Color ImageBackground { get; set; }

	private Vector2 m_viewportSize;
	public Vector2 ViewportSize { get { return m_viewportSize; } }

	private Vector2 m_originalImageSize;
	public Vector2 OriginalImageSize { get { return m_originalImageSize; } }

	private Vector2 m_orientedImageSize;
	public Vector2 OrientedImageSize { get { return m_orientedImageSize; } }

	public Vector2 SelectionSize { get { return m_selection.sizeDelta; } }

	private RectTransform orientedImageTransform;

	private IEnumerator autoZoomCoroutine;
	private ISelectionHandler currentSelectionHandler;

	private Orientation currentOrientation;

	private CropResult cropCallback;
	private ImageResizePolicy imageResizePolicy;

	private bool shouldRefreshViewport;

	private Vector2 minSize, maxSize;
	private Vector2 currMinSize, currMaxSize;

	private float minAspectRatio, maxAspectRatio;
	private float minImageScale;

	private void Awake()
	{
		if( m_instance == null )
			m_instance = this;
		else if( this != m_instance )
		{
			Destroy( gameObject );
			return;
		}

		orientedImageTransform = (RectTransform) m_orientedImage.transform;
		viewportSizeChangeListener.onSizeChanged = OnViewportDimensionsChange;

		cropRenderCanvas.gameObject.layer = 22; // some random layer that is hopefully not used by any other object
		cropRenderImage.gameObject.layer = 22;
		cropRenderCamera.cullingMask = 1 << 22;
		cropRenderCanvas.gameObject.SetActive( false );

		gameObject.SetActive( false );
	}

	private void OnDisable()
	{
		autoZoomCoroutine = null;
	}

	private void LateUpdate()
	{
		if( gameObject.activeInHierarchy )
		{
			if( currentSelectionHandler != null && m_imageHolder.localScale.z > minImageScale + 0.01f )
				currentSelectionHandler.OnUpdate();

			if( shouldRefreshViewport )
			{
				textsSynchronizer.Synchronize();
				ResetView( true );

				shouldRefreshViewport = false;
			}

			selectionGraphicsSynchronizer.Synchronize();
		}
	}

	private void OnViewportDimensionsChange( Vector2 size )
	{
		m_viewportSize = size;
		shouldRefreshViewport = true;
	}

	public void Show( Texture image, Settings settings = null )
	{
		Show( image, null, settings, null );
	}

	public void Show( Texture image, CropResult onCrop, Settings settings = null, ImageResizePolicy croppedImageResizePolicy = null )
	{
		if( image == null )
		{
			Debug.LogError( "Image is null!" );
			return;
		}

		if( !gameObject.activeSelf )
			gameObject.SetActive( true );

		cropCallback = onCrop;
		imageResizePolicy = croppedImageResizePolicy;

		if( settings == null )
			settings = DefaultSettings;

		m_orientedImage.texture = image;

		m_originalImageSize = new Vector2( image.width, image.height );
		orientedImageTransform.sizeDelta = m_originalImageSize;

		MarkTextureNonReadable = settings.markTextureNonReadable;
		OvalSelection = settings.ovalSelection;
		GuidelinesVisibility = settings.guidelinesVisibility;
		ImageBackground = settings.imageBackground;

		minAspectRatio = settings.selectionMinAspectRatio;
		maxAspectRatio = settings.selectionMaxAspectRatio;

		if( minAspectRatio <= 0f )
			minAspectRatio = 1E-6f;
		if( maxAspectRatio <= 0f )
			maxAspectRatio = 1E6f;

		if( minAspectRatio > maxAspectRatio )
		{
			float temp = minAspectRatio;
			minAspectRatio = maxAspectRatio;
			maxAspectRatio = temp;
		}

		minSize = settings.selectionMinSize;
		maxSize = settings.selectionMaxSize;

		Vector2 maxSizeDefault = new Vector2( 2f, 2f ) * Mathf.Max( m_originalImageSize.x, m_originalImageSize.y );
		if( minSize.x < 1f || minSize.y < 1f )
			minSize = new Vector2( 0.1f, 0.1f ) * Mathf.Min( m_originalImageSize.x, m_originalImageSize.y );
		if( maxSize.x < 1f || maxSize.y < 1f )
			maxSize = maxSizeDefault;

		minSize = minSize.ClampBetween( Vector2.one, Vector2.one * Mathf.Max( m_originalImageSize.x, m_originalImageSize.y ) );
		maxSize = maxSize.ClampBetween( minSize, maxSizeDefault );

		m_autoZoomEnabled = false;
		SetOrientation( settings.initialOrientation );

		m_autoZoomEnabled = settings.autoZoomEnabled;
		m_pixelPerfectSelection = settings.pixelPerfectSelection;

		flipHorizontalButton.SetActive( ( settings.visibleButtons & Button.FlipHorizontal ) == Button.FlipHorizontal );
		flipVerticalButton.SetActive( ( settings.visibleButtons & Button.FlipVertical ) == Button.FlipVertical );
		rotate90DegreesButton.SetActive( ( settings.visibleButtons & Button.Rotate90Degrees ) == Button.Rotate90Degrees );

		Vector2 paddingMax = new Vector2( 1f - Mathf.Clamp01( settings.selectionInitialPaddingRight ), 1f - Mathf.Clamp01( settings.selectionInitialPaddingTop ) );
		Vector2 paddingMin = new Vector2( Mathf.Clamp( settings.selectionInitialPaddingLeft, 0f, paddingMax.x ), Mathf.Clamp( settings.selectionInitialPaddingBottom, 0f, paddingMax.y ) );

		UpdateSelection( Vector2.zero, m_orientedImageSize.ScaleWith( paddingMax - paddingMin ) );
		m_selection.anchoredPosition = ( m_orientedImageSize - m_selection.sizeDelta ) * 0.5f;
		if( m_pixelPerfectSelection )
			MakePixelPerfectSelection();

		ResetView( false );
	}

	public void Hide()
	{
		gameObject.SetActive( false );

		m_orientedImage.texture = null;
		cropCallback = null;
		imageResizePolicy = null;
	}

	public void ResetView( bool frameSelection )
	{
		if( m_orientedImageSize.x <= 0f || m_orientedImageSize.y <= 0f )
			return;

		StopAutoZoom();

		minImageScale = Mathf.Min( m_viewportSize.x / m_orientedImageSize.x, m_viewportSize.y / m_orientedImageSize.y );

		m_imageHolder.anchoredPosition = RestrictImageToViewport( m_imageHolder.anchoredPosition, minImageScale );
		m_imageHolder.localScale = new Vector3( minImageScale, minImageScale, minImageScale );

		if( frameSelection && m_autoZoomEnabled )
			StartAutoZoom( true );
	}

	public void Cancel()
	{
		if( cropCallback != null )
			cropCallback( false, m_orientedImage.texture, null );

		Hide();
	}

	public void Crop()
	{
		if( cropCallback != null )
		{
			Texture2D result = CropSelection();
			cropCallback( result != null, m_orientedImage.texture, result );
		}

		Hide();
	}

	public Texture2D CropSelection()
	{
		Vector2 selectionSize = m_selection.sizeDelta;
		int width = Mathf.Clamp( (int) selectionSize.x, 1, (int) m_orientedImageSize.x );
		int height = Mathf.Clamp( (int) selectionSize.y, 1, (int) m_orientedImageSize.y );
		if( imageResizePolicy != null )
			imageResizePolicy( ref width, ref height );

		if( width < 0 )
			width = 1;
		if( height < 0 )
			height = 1;

		return CropSelection( width, height );
	}

	public Texture2D CropSelection( int width, int height )
	{
		if( !gameObject.activeInHierarchy )
		{
			Debug.LogError( "Cropper is not visible!" );
			return null;
		}

		if( m_orientedImage.texture == null )
		{
			Debug.LogError( "Cropper is not initialized!" );
			return null;
		}

		// Make sure that cropped image dimensions do not exceed maximum supported texture size
		int maxTextureSize = SystemInfo.maxTextureSize;
		if( width > maxTextureSize || height > maxTextureSize )
		{
			int preferredWidth = (int) ( maxTextureSize * width / (float) height );
			int preferredHeight = (int) ( maxTextureSize * height / (float) width );

			if( preferredWidth <= maxTextureSize )
				preferredHeight = maxTextureSize;
			else
				preferredWidth = maxTextureSize;

			Debug.Log( string.Concat( width, "x", height, " is too large, using ", preferredWidth, "x", preferredHeight, " instead..." ) );

			width = preferredWidth;
			height = preferredHeight;
		}

		RectTransform cropRenderCanvasTR = (RectTransform) cropRenderCanvas.transform;
		RectTransform cropRenderImageTR = (RectTransform) cropRenderImage.transform;
		Transform cropRenderCameraTR = cropRenderCamera.transform;

		cropRenderImage.texture = m_orientedImage.texture;

		Vector2 selectionSize = m_selection.sizeDelta;
		Vector2 selectionCenter = m_selection.anchoredPosition + selectionSize * 0.5f;

		cropRenderCanvasTR.sizeDelta = m_orientedImageSize;

		cropRenderSelection.anchoredPosition = m_selection.anchoredPosition;
		cropRenderSelection.sizeDelta = selectionSize;

		cropRenderImageTR.position = cropRenderCanvasTR.position;
		cropRenderImageTR.sizeDelta = m_originalImageSize;
		cropRenderImageTR.localScale = orientedImageTransform.localScale;
		cropRenderImageTR.localEulerAngles = orientedImageTransform.localEulerAngles;

		cropRenderCameraTR.eulerAngles = cropRenderCanvasTR.eulerAngles;
		cropRenderCameraTR.position = cropRenderCanvasTR.TransformPoint( new Vector3( selectionCenter.x - m_orientedImageSize.x * 0.5f, selectionCenter.y - m_orientedImageSize.y * 0.5f, -5f ) );

		cropRenderCamera.aspect = selectionSize.x / selectionSize.y;
		cropRenderCamera.orthographicSize = selectionSize.y * 0.5f;

		Texture2D result = null;
		RenderTexture temp = RenderTexture.active;
		RenderTexture renderTex = RenderTexture.GetTemporary( width, height, 24 );
		try
		{
			cropRenderCanvas.gameObject.SetActive( true );
			LayoutRebuilder.ForceRebuildLayoutImmediate( cropRenderCanvasTR );

			RenderTexture.active = renderTex;

			bool transparentBackground = ImageBackground.a < 1f;
			if( transparentBackground )
			{
				cropRenderCamera.clearFlags = CameraClearFlags.Depth;
				GL.Clear( false, true, ImageBackground );
			}
			else
				cropRenderCamera.clearFlags = CameraClearFlags.Color;

			cropRenderCamera.backgroundColor = ImageBackground;
			cropRenderCamera.targetTexture = renderTex;
			cropRenderCamera.Render();

			result = new Texture2D( width, height, transparentBackground ? TextureFormat.RGBA32 : TextureFormat.RGB24, false );
			result.ReadPixels( new Rect( 0, 0, width, height ), 0, 0, false );
			result.Apply( false, MarkTextureNonReadable );
		}
		catch( System.Exception e )
		{
			Debug.LogException( e );

			if( result != null )
			{
				DestroyImmediate( result );
				result = null;
			}
		}
		finally
		{
			RenderTexture.active = temp;
			RenderTexture.ReleaseTemporary( renderTex );

			cropRenderCanvas.gameObject.SetActive( false );
			cropRenderImage.texture = null;
			cropRenderCamera.targetTexture = null;
		}

		return result;
	}

	public void Rotate90Clockwise()
	{
		RotateClockwise( 1 );
	}

	public void Rotate180Clockwise()
	{
		RotateClockwise( 2 );
	}

	public void Rotate270Clockwise()
	{
		RotateClockwise( 3 );
	}

	private void RotateClockwise( int amount )
	{
		while( amount < 0 )
			amount += 4;
		while( amount > 3 )
			amount -= 4;

		if( amount == 0 )
			return;

		int orientationInt = (int) currentOrientation;
		if( orientationInt < 4 )
		{
			orientationInt -= amount;
			if( orientationInt < 0 )
				orientationInt += 4;
		}
		else
		{
			orientationInt -= amount;
			if( orientationInt < 4 )
				orientationInt += 4;
		}

		SetOrientation( (Orientation) orientationInt );
	}

	public void FlipHorizontal()
	{
		if( currentOrientation == Orientation.Normal )
			SetOrientation( Orientation.FlipHorizontal );
		else if( currentOrientation == Orientation.FlipHorizontal )
			SetOrientation( Orientation.Normal );
		else if( currentOrientation == Orientation.Rotate90 )
			SetOrientation( Orientation.Transverse );
		else if( currentOrientation == Orientation.Transverse )
			SetOrientation( Orientation.Rotate90 );
		else if( currentOrientation == Orientation.Rotate180 )
			SetOrientation( Orientation.FlipVertical );
		else if( currentOrientation == Orientation.FlipVertical )
			SetOrientation( Orientation.Rotate180 );
		else if( currentOrientation == Orientation.Rotate270 )
			SetOrientation( Orientation.Transpose );
		else
			SetOrientation( Orientation.Rotate270 );
	}

	public void FlipVertical()
	{
		if( currentOrientation == Orientation.Normal )
			SetOrientation( Orientation.FlipVertical );
		else if( currentOrientation == Orientation.FlipVertical )
			SetOrientation( Orientation.Normal );
		else if( currentOrientation == Orientation.Rotate90 )
			SetOrientation( Orientation.Transpose );
		else if( currentOrientation == Orientation.Transpose )
			SetOrientation( Orientation.Rotate90 );
		else if( currentOrientation == Orientation.Rotate180 )
			SetOrientation( Orientation.FlipHorizontal );
		else if( currentOrientation == Orientation.FlipHorizontal )
			SetOrientation( Orientation.Rotate180 );
		else if( currentOrientation == Orientation.Rotate270 )
			SetOrientation( Orientation.Transverse );
		else
			SetOrientation( Orientation.Rotate270 );
	}

	public void SetOrientation( Orientation orientation )
	{
		Vector2 selectionPosition = m_selection.anchoredPosition;
		Vector2 selectionSize = m_selection.sizeDelta;

		if( currentOrientation == Orientation.Normal )
		{
		}
		else if( currentOrientation == Orientation.Rotate90 )
		{
			selectionPosition = new Vector2( selectionPosition.y, m_orientedImageSize.x - selectionPosition.x - selectionSize.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}
		else if( currentOrientation == Orientation.Rotate180 )
			selectionPosition = new Vector2( m_orientedImageSize.x - selectionPosition.x - selectionSize.x, m_orientedImageSize.y - selectionPosition.y - selectionSize.y );
		else if( currentOrientation == Orientation.Rotate270 )
		{
			selectionPosition = new Vector2( m_orientedImageSize.y - selectionPosition.y - selectionSize.y, selectionPosition.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}
		else if( currentOrientation == Orientation.FlipHorizontal )
			selectionPosition = new Vector2( m_orientedImageSize.x - selectionPosition.x - selectionSize.x, selectionPosition.y );
		else if( currentOrientation == Orientation.Transpose )
		{
			selectionPosition = new Vector2( m_orientedImageSize.y - selectionPosition.y - selectionSize.y, m_orientedImageSize.x - selectionPosition.x - selectionSize.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}
		else if( currentOrientation == Orientation.FlipVertical )
			selectionPosition = new Vector2( selectionPosition.x, m_orientedImageSize.y - selectionPosition.y - selectionSize.y );
		else
		{
			selectionPosition = new Vector2( selectionPosition.y, selectionPosition.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}

		if( orientation == Orientation.Normal )
		{
			m_orientedImageSize = m_originalImageSize;

			orientedImageTransform.localScale = Vector3.one;
			orientedImageTransform.localEulerAngles = Vector3.zero;
		}
		else if( orientation == Orientation.Rotate90 )
		{
			m_orientedImageSize = new Vector2( m_originalImageSize.y, m_originalImageSize.x );

			orientedImageTransform.localScale = Vector3.one;
			orientedImageTransform.localEulerAngles = new Vector3( 0f, 0f, 90f );

			selectionPosition = new Vector2( m_originalImageSize.y - selectionPosition.y - selectionSize.y, selectionPosition.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}
		else if( orientation == Orientation.Rotate180 )
		{
			m_orientedImageSize = m_originalImageSize;

			orientedImageTransform.localScale = Vector3.one;
			orientedImageTransform.localEulerAngles = new Vector3( 0f, 0f, 180f );

			selectionPosition = new Vector2( m_originalImageSize.x - selectionPosition.x - selectionSize.x, m_originalImageSize.y - selectionPosition.y - selectionSize.y );
		}
		else if( orientation == Orientation.Rotate270 )
		{
			m_orientedImageSize = new Vector2( m_originalImageSize.y, m_originalImageSize.x );

			orientedImageTransform.localScale = Vector3.one;
			orientedImageTransform.localEulerAngles = new Vector3( 0f, 0f, 270f );

			selectionPosition = new Vector2( selectionPosition.y, m_originalImageSize.x - selectionPosition.x - selectionSize.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}
		else if( orientation == Orientation.FlipHorizontal )
		{
			m_orientedImageSize = m_originalImageSize;

			orientedImageTransform.localScale = new Vector3( -1f, 1f, 1f );
			orientedImageTransform.localEulerAngles = Vector3.zero;

			selectionPosition = new Vector2( m_originalImageSize.x - selectionPosition.x - selectionSize.x, selectionPosition.y );
		}
		else if( orientation == Orientation.Transpose )
		{
			m_orientedImageSize = new Vector2( m_originalImageSize.y, m_originalImageSize.x );

			orientedImageTransform.localScale = new Vector3( -1f, 1f, 1f );
			orientedImageTransform.localEulerAngles = new Vector3( 0f, 0f, 90f );

			selectionPosition = new Vector2( m_originalImageSize.y - selectionPosition.y - selectionSize.y, m_originalImageSize.x - selectionPosition.x - selectionSize.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}
		else if( orientation == Orientation.FlipVertical )
		{
			m_orientedImageSize = m_originalImageSize;

			orientedImageTransform.localScale = new Vector3( -1f, 1f, 1f );
			orientedImageTransform.localEulerAngles = new Vector3( 0f, 0f, 180f );

			selectionPosition = new Vector2( selectionPosition.x, m_originalImageSize.y - selectionPosition.y - selectionSize.y );
		}
		else
		{
			m_orientedImageSize = new Vector2( m_originalImageSize.y, m_originalImageSize.x );

			orientedImageTransform.localScale = new Vector3( -1f, 1f, 1f );
			orientedImageTransform.localEulerAngles = new Vector3( 0f, 0f, 270f );

			selectionPosition = new Vector2( selectionPosition.y, selectionPosition.x );
			selectionSize = new Vector2( selectionSize.y, selectionSize.x );
		}

		m_imageHolder.sizeDelta = m_orientedImageSize;

		currMinSize = minSize.ClampBetween( Vector2.one, m_orientedImageSize );
		currMaxSize = maxSize.ClampBetween( currMinSize, m_orientedImageSize );

		// If new selection doesn't comply with limiting aspect ratios, keep original selection
		float aspectRatio = selectionSize.x / selectionSize.y;
		if( aspectRatio < minAspectRatio - 1E-4f ) // consider floating-point precision
		{
			// Pixel-perfect selection may not always comply with limiting aspect ratios
			if( !m_pixelPerfectSelection || ( selectionSize.x + 1 ) / ( selectionSize.y - 1 ) < minAspectRatio - 1E-4f )
				selectionSize = m_selection.sizeDelta;
		}
		else if( aspectRatio > maxAspectRatio + 1E-4f )
		{
			if( !m_pixelPerfectSelection || ( selectionSize.x - 1 ) / ( selectionSize.y + 1 ) > maxAspectRatio + 1E-4f )
				selectionSize = m_selection.sizeDelta;
		}

		UpdateSelection( selectionPosition, selectionSize );
		if( m_pixelPerfectSelection )
			MakePixelPerfectSelection();

		currentOrientation = orientation;
		ResetView( true );
	}

	public void StartAutoZoom( bool instantZoom )
	{
		if( !gameObject.activeInHierarchy )
			return;

		StopAutoZoom();

		Vector2 selectionSize = m_selection.sizeDelta;
		Vector2 selectionSizeScaled = selectionSize * m_imageHolder.localScale.z;

		float zoomAmount = -1f;
		float scale = m_imageHolder.localScale.z;
		float fillRate = Mathf.Max( selectionSizeScaled.x / m_viewportSize.x, selectionSizeScaled.y / m_viewportSize.y );
		if( fillRate <= autoZoomInThreshold )
		{
			float scaleX = m_viewportSize.x * autoZoomInFillAmount / selectionSize.x;
			float scaleY = m_viewportSize.y * autoZoomInFillAmount / selectionSize.y;

			zoomAmount = Mathf.Min( scaleX, scaleY );
		}
		else if( fillRate >= autoZoomOutThreshold )
		{
			float scaleX = m_viewportSize.x * autoZoomOutFillAmount / selectionSize.x;
			float scaleY = m_viewportSize.y * autoZoomOutFillAmount / selectionSize.y;

			zoomAmount = Mathf.Min( scaleX, scaleY );
		}
		else
		{
			Vector2 selectionBottomLeft = m_imageHolder.anchoredPosition + m_selection.anchoredPosition * scale;
			Vector2 selectionTopRight = selectionBottomLeft + m_selection.sizeDelta * scale;

			// If part of the selection rests outside of the viewport, engage auto-zoom
			if( selectionBottomLeft.x < -1E-4f || selectionBottomLeft.y < -1E-4f || selectionTopRight.x > m_viewportSize.x + 1E-4f || selectionTopRight.y > m_viewportSize.y + 1E-4f )
				zoomAmount = scale;
		}

		if( zoomAmount < 0f )
			return;

		if( zoomAmount < minImageScale )
			zoomAmount = minImageScale;

		if( Mathf.Abs( zoomAmount - scale ) < 0.001f )
			instantZoom = true;

		autoZoomCoroutine = AutoZoom( zoomAmount, instantZoom );
		StartCoroutine( autoZoomCoroutine );
	}

	private IEnumerator AutoZoom( float zoomAmount, bool instantZoom )
	{
		float elapsed = 0f;
		float length = autoZoomCurve.length == 0 ? 0f : autoZoomCurve[autoZoomCurve.length - 1].time;

		Vector3 initialScale = m_imageHolder.localScale;
		Vector2 initialPosition = m_imageHolder.anchoredPosition;

		Vector3 finalScale = new Vector3( zoomAmount, zoomAmount, zoomAmount );
		Vector2 finalPosition = m_viewportSize * 0.5f - ( m_selection.anchoredPosition + m_selection.sizeDelta * 0.5f ) * finalScale.z;

		finalPosition = RestrictImageToViewport( finalPosition, zoomAmount );

		if( !instantZoom && elapsed < length )
		{
			Vector2 deltaPosition = finalPosition - initialPosition;
			Vector3 deltaScale = finalScale - initialScale;

			while( elapsed < length )
			{
				yield return null;
				elapsed += Time.unscaledDeltaTime;
				if( elapsed >= length )
					break;

				float modifier = autoZoomCurve.Evaluate( elapsed );

				m_imageHolder.anchoredPosition = initialPosition + deltaPosition * modifier;
				m_imageHolder.localScale = initialScale + deltaScale * modifier;
			}
		}

		m_imageHolder.anchoredPosition = finalPosition;
		m_imageHolder.localScale = finalScale;

		autoZoomCoroutine = null;
	}

	private void StopAutoZoom()
	{
		if( autoZoomCoroutine != null )
		{
			StopCoroutine( autoZoomCoroutine );
			autoZoomCoroutine = null;
		}

		if( currentSelectionHandler != null )
		{
			currentSelectionHandler.Stop();
			currentSelectionHandler = null;
		}
	}

	public bool CanModifySelectionWith( ISelectionHandler handler )
	{
		if( autoZoomCoroutine != null )
			return false;

		if( handler != currentSelectionHandler )
		{
			if( currentSelectionHandler != null )
				currentSelectionHandler.Stop();

			currentSelectionHandler = handler;
		}

		if( m_guidelinesVisibility == Visibility.OnDrag )
		{
			for( int i = 0; i < guidelines.Length; i++ )
				guidelines[i].enabled = true;
		}

		return true;
	}

	public void StopModifySelectionWith( ISelectionHandler handler )
	{
		if( currentSelectionHandler == handler )
		{
			currentSelectionHandler = null;

			if( m_guidelinesVisibility == Visibility.OnDrag )
			{
				for( int i = 0; i < guidelines.Length; i++ )
					guidelines[i].enabled = false;
			}

			if( m_pixelPerfectSelection )
				MakePixelPerfectSelection();

			if( m_autoZoomEnabled )
				StartAutoZoom( false );
		}
	}

	public void MakePixelPerfectSelection()
	{
		Vector2 size = m_selection.sizeDelta.RoundToInt().ClampBetween( currMinSize.CeilToInt(), m_orientedImageSize );
		float aspectRatio = size.x / size.y;
		if( aspectRatio < minAspectRatio )
		{
			bool foundMatchingSize = false;
			bool canExpandWidth = size.x < m_orientedImageSize.x;
			bool canShrinkHeight = size.y > Mathf.CeilToInt( currMinSize.y );

			if( canExpandWidth )
			{
				aspectRatio = ( size.x + 1 ) / size.y;
				if( aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio )
				{
					size.x = size.x + 1;
					foundMatchingSize = true;
				}
			}

			if( !foundMatchingSize && canShrinkHeight )
			{
				aspectRatio = size.x / ( size.y - 1 );
				if( aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio )
				{
					size.y = size.y - 1;
					foundMatchingSize = true;
				}
			}

			if( !foundMatchingSize && canExpandWidth && canShrinkHeight )
			{
				aspectRatio = ( size.x + 1 ) / ( size.y - 1 );
				if( aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio )
				{
					size.x = size.x + 1;
					size.y = size.y - 1;
				}
			}
		}
		else if( aspectRatio > maxAspectRatio )
		{
			bool foundMatchingSize = false;
			bool canShrinkWidth = size.x > Mathf.CeilToInt( currMinSize.x );
			bool canExpandHeight = size.y < m_orientedImageSize.y;

			if( !foundMatchingSize && canShrinkWidth )
			{
				aspectRatio = ( size.x - 1 ) / size.y;
				if( aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio )
				{
					size.x = size.x - 1;
					foundMatchingSize = true;
				}
			}

			if( canExpandHeight )
			{
				aspectRatio = size.x / ( size.y + 1 );
				if( aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio )
				{
					size.y = size.y + 1;
					foundMatchingSize = true;
				}
			}

			if( !foundMatchingSize && canShrinkWidth && canExpandHeight )
			{
				aspectRatio = ( size.x - 1 ) / ( size.y + 1 );
				if( aspectRatio >= minAspectRatio && aspectRatio <= maxAspectRatio )
				{
					size.x = size.x - 1;
					size.y = size.y + 1;
				}
			}
		}

		m_selection.anchoredPosition = m_selection.anchoredPosition.RoundToInt().ClampBetween( Vector2.zero, m_orientedImageSize - size );
		m_selection.sizeDelta = size;
	}

	public void UpdateSelection( Vector2 position )
	{
		m_selection.anchoredPosition = position.ClampBetween( Vector2.zero, m_orientedImageSize - m_selection.sizeDelta );
	}

	public void UpdateSelection( Vector2 position, Vector2 size, Direction pivot = Direction.None, bool shrinkToFit = true )
	{
		Vector2 newSize = size.ClampBetween( currMinSize, currMaxSize );

		float aspectRatio = newSize.x / newSize.y;
		if( aspectRatio < minAspectRatio )
		{
			if( shrinkToFit )
			{
				float requiredHeight = newSize.x / minAspectRatio;
				if( requiredHeight >= currMinSize.y )
					newSize.y = requiredHeight;
				else
				{
					float requiredWidth = currMinSize.y * minAspectRatio;
					if( requiredWidth <= currMaxSize.x )
						newSize = new Vector2( requiredWidth, currMinSize.y );
					else
						newSize = new Vector2( currMaxSize.x, currMinSize.y );
				}
			}
			else
			{
				float requiredWidth = newSize.y * minAspectRatio;
				if( requiredWidth <= currMaxSize.x )
					newSize.x = requiredWidth;
				else
				{
					float requiredHeight = currMaxSize.x / minAspectRatio;
					if( requiredHeight <= currMaxSize.y )
						newSize = new Vector2( currMaxSize.x, requiredHeight );
					else
						newSize = currMaxSize;
				}
			}
		}
		else if( aspectRatio > maxAspectRatio )
		{
			if( shrinkToFit )
			{
				float requiredWidth = newSize.y * maxAspectRatio;
				if( requiredWidth >= currMinSize.x )
					newSize.x = requiredWidth;
				else
				{
					float requiredHeight = currMinSize.x / maxAspectRatio;
					if( requiredHeight <= currMaxSize.y )
						newSize = new Vector2( currMinSize.x, requiredHeight );
					else
						newSize = new Vector2( currMinSize.x, currMaxSize.y );
				}
			}
			else
			{
				float requiredHeight = newSize.x / maxAspectRatio;
				if( requiredHeight <= currMaxSize.y )
					newSize.y = requiredHeight;
				else
				{
					float requiredWidth = currMaxSize.y * maxAspectRatio;
					if( requiredWidth <= currMaxSize.x )
						newSize = new Vector2( requiredWidth, currMaxSize.y );
					else
						newSize = currMaxSize;
				}
			}
		}

		if( size.x != newSize.x )
		{
			if( ( pivot & Direction.Right ) == Direction.Right )
				position.x -= newSize.x - size.x;
			else if( ( pivot & Direction.Left ) != Direction.Left )
				position.x -= ( newSize.x - size.x ) * 0.5f;

			size.x = newSize.x;
		}
		if( size.y != newSize.y )
		{
			if( ( pivot & Direction.Top ) == Direction.Top )
				position.y -= newSize.y - size.y;
			else if( ( pivot & Direction.Bottom ) != Direction.Bottom )
				position.y -= ( newSize.y - size.y ) * 0.5f;

			size.y = newSize.y;
		}

		m_selection.anchoredPosition = position.ClampBetween( Vector2.zero, m_orientedImageSize - size );
		m_selection.sizeDelta = size;
	}

	private Vector2 RestrictImageToViewport( Vector2 position, float scale )
	{
		Vector2 sizeScaled = m_imageHolder.sizeDelta * scale;

		if( sizeScaled.x < m_viewportSize.x )
			position.x = ( m_viewportSize.x - sizeScaled.x ) * 0.5f;
		else
			position.x = Mathf.Clamp( position.x, m_viewportSize.x - sizeScaled.x, 0f );

		if( sizeScaled.y < m_viewportSize.y )
			position.y = ( m_viewportSize.y - sizeScaled.y ) * 0.5f;
		else
			position.y = Mathf.Clamp( position.y, m_viewportSize.y - sizeScaled.y, 0f );

		return position;
	}

	public Vector2 ScrollImage( Vector2 imagePosition, Direction direction )
	{
		if( direction == Direction.Left )
		{
			imagePosition.x += viewportScrollSpeed * Time.unscaledDeltaTime;
			if( imagePosition.x > 0f )
				imagePosition.x = 0f;
		}
		else if( direction == Direction.Top )
		{
			float imageHeight = m_imageHolder.sizeDelta.y * m_imageHolder.localScale.z;
			imagePosition.y -= viewportScrollSpeed * Time.unscaledDeltaTime;
			if( imagePosition.y + imageHeight < m_viewportSize.y )
				imagePosition.y = m_viewportSize.y - imageHeight;
		}
		else if( direction == Direction.Right )
		{
			float imageWidth = m_imageHolder.sizeDelta.x * m_imageHolder.localScale.z;
			imagePosition.x -= viewportScrollSpeed * Time.unscaledDeltaTime;
			if( imagePosition.x + imageWidth < m_viewportSize.x )
				imagePosition.x = m_viewportSize.x - imageWidth;
		}
		else
		{
			imagePosition.y += viewportScrollSpeed * Time.unscaledDeltaTime;
			if( imagePosition.y > 0f )
				imagePosition.y = 0f;
		}

		return imagePosition;
	}

	public Vector2 GetTouchPosition( Vector2 screenPos, Camera cam )
	{
		Vector2 localPos;
		RectTransformUtility.ScreenPointToLocalPointInRectangle( m_imageHolder, screenPos, cam, out localPos );

		return localPos;
	}
}