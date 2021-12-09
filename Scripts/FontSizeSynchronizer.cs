using UnityEngine;
using UnityEngine.UI;

namespace ImageCropperNamespace
{
	[SerializeField]
	public class FontSizeSynchronizer : MonoBehaviour
	{
#pragma warning disable 0649
		[SerializeField]
		private Text[] texts;
#pragma warning restore 0649

		private int[] initialBestFitSizes;
		private Canvas canvas;

		private void Awake()
		{
			if( texts.Length == 0 )
				return;

			canvas = texts[0].canvas;

			initialBestFitSizes = new int[texts.Length];
			for( int i = 0; i < texts.Length; i++ )
				initialBestFitSizes[i] = texts[i].resizeTextMaxSize;
		}

		public void Synchronize()
		{
			if( canvas == null || !gameObject.activeInHierarchy )
				return;

			int minSize = int.MaxValue;
			for( int i = 0; i < texts.Length; i++ )
			{
				Text text = texts[i];

				text.resizeTextMaxSize = initialBestFitSizes[i];
				text.resizeTextForBestFit = true;
				text.cachedTextGenerator.Populate( text.text, text.GetGenerationSettings( text.rectTransform.rect.size ) );

				int fontSize = text.cachedTextGenerator.fontSizeUsedForBestFit;
				if( fontSize < minSize )
					minSize = fontSize;
			}

			int fontSizeScaled = (int) ( minSize / canvas.scaleFactor );
			for( int i = 0; i < texts.Length; i++ )
			{
				texts[i].fontSize = fontSizeScaled;
				texts[i].resizeTextForBestFit = false;
			}
		}
	}
}