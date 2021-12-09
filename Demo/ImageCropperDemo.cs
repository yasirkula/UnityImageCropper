using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ImageCropperNamespace
{
	public class ImageCropperDemo : MonoBehaviour
	{
		public RawImage croppedImageHolder;
		public Text croppedImageSize;

		public Toggle ovalSelectionInput, autoZoomInput;
		public InputField minAspectRatioInput, maxAspectRatioInput;

		public void Crop()
		{
			// If image cropper is already open, do nothing
			if( ImageCropper.Instance.IsOpen )
				return;

			StartCoroutine( TakeScreenshotAndCrop() );
		}

		private IEnumerator TakeScreenshotAndCrop()
		{
			yield return new WaitForEndOfFrame();

			bool ovalSelection = ovalSelectionInput.isOn;
			bool autoZoom = autoZoomInput.isOn;

			float minAspectRatio, maxAspectRatio;
			if( !float.TryParse( minAspectRatioInput.text, out minAspectRatio ) )
				minAspectRatio = 0f;
			if( !float.TryParse( maxAspectRatioInput.text, out maxAspectRatio ) )
				maxAspectRatio = 0f;

			Texture2D screenshot = new Texture2D( Screen.width, Screen.height, TextureFormat.RGB24, false );
			screenshot.ReadPixels( new Rect( 0, 0, Screen.width, Screen.height ), 0, 0 );
			screenshot.Apply();

			ImageCropper.Instance.Show( screenshot, ( bool result, Texture originalImage, Texture2D croppedImage ) =>
			{
				// Destroy previously cropped texture (if any) to free memory
				Destroy( croppedImageHolder.texture, 5f );

				// If screenshot was cropped successfully
				if( result )
				{
					// Assign cropped texture to the RawImage
					croppedImageHolder.enabled = true;
					croppedImageHolder.texture = croppedImage;

					Vector2 size = croppedImageHolder.rectTransform.sizeDelta;
					if( croppedImage.height <= croppedImage.width )
						size = new Vector2( 400f, 400f * ( croppedImage.height / (float) croppedImage.width ) );
					else
						size = new Vector2( 400f * ( croppedImage.width / (float) croppedImage.height ), 400f );
					croppedImageHolder.rectTransform.sizeDelta = size;

					croppedImageSize.enabled = true;
					croppedImageSize.text = "Image size: " + croppedImage.width + ", " + croppedImage.height;
				}
				else
				{
					croppedImageHolder.enabled = false;
					croppedImageSize.enabled = false;
				}

				// Destroy the screenshot as we no longer need it in this case
				Destroy( screenshot );
			},
			settings: new ImageCropper.Settings()
			{
				ovalSelection = ovalSelection,
				autoZoomEnabled = autoZoom,
				imageBackground = Color.clear, // transparent background
				selectionMinAspectRatio = minAspectRatio,
				selectionMaxAspectRatio = maxAspectRatio

			},
			croppedImageResizePolicy: ( ref int width, ref int height ) =>
			{
				// uncomment lines below to save cropped image at half resolution
				//width /= 2;
				//height /= 2;
			} );
		}
	}
}