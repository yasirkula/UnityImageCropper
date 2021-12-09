using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR && UNITY_2021_1_OR_NEWER
using Screen = UnityEngine.Device.Screen; // To support Device Simulator on Unity 2021.1+
#endif

namespace ImageCropperNamespace
{
	public class NotchCompensator : MonoBehaviour
	{
#if UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS
#pragma warning disable 0649
		[SerializeField]
		[Tooltip( "If enabled, on Android and iOS devices with notch screens, top buttons will be repositioned so that the cutout(s) don't obscure them" )]
		private bool avoidScreenCutout = true;

		[SerializeField]
		private RectTransform buttons;
		[SerializeField]
		private RectTransform viewport;
		[SerializeField]
		private Image notchBackground;
#pragma warning restore 0649

#pragma warning disable 0414
		private RectTransform canvasTR;
#pragma warning restore 0414

		private bool screenDimensionsChanged = true;

		private void Awake()
		{
			canvasTR = (RectTransform) transform;
		}

		// Window is resized, update the list
		private void OnRectTransformDimensionsChange()
		{
			screenDimensionsChanged = true;
		}

		private void LateUpdate()
		{
			if( screenDimensionsChanged )
			{
				CheckScreenCutout();
				screenDimensionsChanged = false;
			}
		}

		// If a cutout is intersecting with the buttons at the top on notch screens, shift these buttons downwards
		private void CheckScreenCutout()
		{
			if( !avoidScreenCutout )
				return;

#if UNITY_2017_2_OR_NEWER && ( UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS )
			// Check if there is a cutout at the top of the screen
			int screenHeight = Screen.height;
			float safeYMax = Screen.safeArea.yMax;
			if( safeYMax < screenHeight - 1 ) // 1: a small threshold
			{
				// There is a cutout, shift the top buttons downwards
				float cutoutPercentage = ( screenHeight - safeYMax ) / Screen.height;
				float cutoutLocalSize = cutoutPercentage * canvasTR.rect.height;

				buttons.anchoredPosition = new Vector2( 0f, -cutoutLocalSize );
				viewport.sizeDelta = new Vector2( 0f, -cutoutLocalSize );
				notchBackground.rectTransform.sizeDelta = new Vector2( 0f, cutoutLocalSize + 5f ); // 5f: to prevent a thin black line from appearing when canvas is scaled with screen size

				if( !notchBackground.enabled )
					notchBackground.enabled = true;
			}
			else
			{
				buttons.anchoredPosition = Vector2.zero;
				viewport.sizeDelta = Vector2.zero;

				if( notchBackground.enabled )
					notchBackground.enabled = false;
			}
#endif
		}
#endif
	}
}