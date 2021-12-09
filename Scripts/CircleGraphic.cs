using UnityEngine;
using UnityEngine.Sprites;
using UnityEngine.UI;

namespace ImageCropperNamespace
{
    [RequireComponent(typeof(CanvasRenderer))]
    public class CircleGraphic : MaskableGraphic
    {
        public enum Mode
        {
            FillInside = 0,
            FillOutside = 1,
            Edge = 2
        }

        private Color32 color32;
        private float deltaRadians;
        private float deltaWidth, deltaHeight;

        private Vector2 uv;

        private float width, height;

        public override Texture mainTexture => renderSprite != null ? renderSprite.texture : s_WhiteTexture;

        protected override void Awake()
        {
            base.Awake();

            if (renderSprite != null)
            {
                var packedUv = DataUtility.GetOuterUV(renderSprite);
                uv = new Vector2(packedUv.x + packedUv.z, packedUv.y + packedUv.w) * 0.5f; // uv center point
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            var r = GetPixelAdjustedRect();

            color32 = color;
            width = r.width * 0.5f;
            height = r.height * 0.5f;

            vh.Clear();

            var pivot = rectTransform.pivot;
            deltaWidth = r.width * (0.5f - pivot.x);
            deltaHeight = r.height * (0.5f - pivot.y);

            if (mode == Mode.FillInside)
            {
                deltaRadians = 360f / detail * Mathf.Deg2Rad;
                FillInside(vh);
            }
            else if (mode == Mode.FillOutside)
            {
                var quarterDetail = (detail + 3) / 4;
                deltaRadians = 360f / (quarterDetail * 4) * Mathf.Deg2Rad;

                vh.AddVert(new Vector3(width + deltaWidth, height + deltaHeight, 0f), color32, uv);
                vh.AddVert(new Vector3(-width + deltaWidth, height + deltaHeight, 0f), color32, uv);
                vh.AddVert(new Vector3(-width + deltaWidth, -height + deltaHeight, 0f), color32, uv);
                vh.AddVert(new Vector3(width + deltaWidth, -height + deltaHeight, 0f), color32, uv);

                var triangleIndex = 4;
                FillOutside(vh, new Vector3(width + deltaWidth, deltaHeight, 0f), 0, quarterDetail, ref triangleIndex);
                FillOutside(vh, new Vector3(deltaWidth, height + deltaHeight, 0f), 1, quarterDetail, ref triangleIndex);
                FillOutside(vh, new Vector3(-width + deltaWidth, deltaHeight, 0f), 2, quarterDetail, ref triangleIndex);
                FillOutside(vh, new Vector3(deltaWidth, -height + deltaHeight, 0f), 3, quarterDetail,
                    ref triangleIndex);
            }
            else
            {
                deltaRadians = 360f / detail * Mathf.Deg2Rad;
                GenerateEdges(vh);
            }
        }

        public override void Cull(Rect clipRect, bool validRect)
        {
            canvasRenderer.cull = false;
        }

        private void FillInside(VertexHelper vh)
        {
            vh.AddVert(new Vector3(deltaWidth, deltaHeight, 0f), color32, uv);
            vh.AddVert(new Vector3(width + deltaWidth, deltaHeight, 0f), color32, uv);

            var triangleIndex = 2;
            for (var i = 1; i < detail; i++, triangleIndex++)
            {
                var radians = i * deltaRadians;

                vh.AddVert(
                    new Vector3(Mathf.Cos(radians) * width + deltaWidth, Mathf.Sin(radians) * height + deltaHeight, 0f),
                    color32, uv);
                vh.AddTriangle(triangleIndex, triangleIndex - 1, 0);
            }

            vh.AddTriangle(1, triangleIndex - 1, 0);
        }

        private void FillOutside(VertexHelper vh, Vector3 initialPoint, int quarterIndex, int detail,
            ref int triangleIndex)
        {
            var startIndex = quarterIndex * detail;
            var endIndex = (quarterIndex + 1) * detail;

            vh.AddVert(initialPoint, color32, uv);
            triangleIndex++;

            for (var i = startIndex + 1; i <= endIndex; i++, triangleIndex++)
            {
                var radians = i * deltaRadians;

                vh.AddVert(
                    new Vector3(Mathf.Cos(radians) * width + deltaWidth, Mathf.Sin(radians) * height + deltaHeight, 0f),
                    color32, uv);
                vh.AddTriangle(quarterIndex, triangleIndex - 1, triangleIndex);
            }
        }

        private void GenerateEdges(VertexHelper vh)
        {
            var innerWidth = width - edgeThickness;
            var innerHeight = height - edgeThickness;

            vh.AddVert(new Vector3(width + deltaWidth, deltaHeight, 0f), color32, uv);
            vh.AddVert(new Vector3(innerWidth + deltaWidth, deltaHeight, 0f), color32, uv);

            var triangleIndex = 2;
            for (var i = 1; i < detail; i++, triangleIndex += 2)
            {
                var radians = i * deltaRadians;
                var cos = Mathf.Cos(radians);
                var sin = Mathf.Sin(radians);

                vh.AddVert(new Vector3(cos * width + deltaWidth, sin * height + deltaHeight, 0f), color32, uv);
                vh.AddVert(new Vector3(cos * innerWidth + deltaWidth, sin * innerHeight + deltaHeight, 0f), color32,
                    uv);

                vh.AddTriangle(triangleIndex, triangleIndex - 2, triangleIndex - 1);
                vh.AddTriangle(triangleIndex, triangleIndex - 1, triangleIndex + 1);
            }

            vh.AddTriangle(0, triangleIndex - 2, triangleIndex - 1);
            vh.AddTriangle(0, triangleIndex - 1, 1);
        }

#pragma warning disable 0649
        [Header("-> The Real Deal <- <- <-")] [SerializeField]
        private Sprite renderSprite;

        [SerializeField] private int detail = 64;

        [SerializeField] private Mode mode;

        [SerializeField] private float edgeThickness = 1;
#pragma warning restore 0649
    }
}