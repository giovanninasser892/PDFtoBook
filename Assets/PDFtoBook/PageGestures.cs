using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace PDFtoBook
{
    public sealed class PageGestures : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        [SerializeField] private RectTransform page;
        public event Action<int> Navigate;
        public event Action<Vector2> PinPlaced;
        public bool PlacingPin { get; set; }
        public event Action ToggleControls;
        public event Action<float> ZoomFinished;
        private RectTransform viewport;
        private Vector2 start;
        private float startTime, pinchDistance, pinchZoom, zoom = 1;
        private bool pinching, usedPinch;
        public float Zoom => zoom;
        public void Configure(RectTransform image) { page = image; viewport = (RectTransform)transform; }
        private void Awake() { viewport = (RectTransform)transform; }
        public void ResetView() { zoom = 1; page.localScale = Vector3.one; page.anchoredPosition = Vector2.zero; }
        public void OnPointerDown(PointerEventData e) { start = e.position; startTime = Time.unscaledTime; usedPinch = false; }
        public void OnDrag(PointerEventData e)
        {
            if (PlacingPin || pinching || zoom <= 1.01f) return;
            float scale = GetComponentInParent<Canvas>().scaleFactor;
            page.anchoredPosition += e.delta / scale;
            ClampPan();
        }
        public void OnPointerUp(PointerEventData e)
        {
            if (usedPinch || pinching) return;
            Vector2 delta = e.position - start;
            if (PlacingPin) {
                if (delta.magnitude < 25 && Time.unscaledTime - startTime < 0.6f &&
                    RectTransformUtility.ScreenPointToLocalPointInRectangle(page, e.position, e.pressEventCamera, out var point) &&
                    page.rect.Contains(point)) {
                    PinPlaced?.Invoke(new Vector2((point.x - page.rect.xMin) / page.rect.width,
                        (point.y - page.rect.yMin) / page.rect.height));
                }
                return;
            }
            if (zoom > 1.01f) { if (delta.magnitude < 25 && Time.unscaledTime - startTime < 0.6f) ToggleControls?.Invoke(); return; }
            if (Mathf.Abs(delta.x) > 70 && Mathf.Abs(delta.x) > Mathf.Abs(delta.y) * 1.3f) Navigate?.Invoke(delta.x < 0 ? 1 : -1);
            else if (delta.magnitude < 25 && Time.unscaledTime - startTime < 0.6f) {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(viewport, e.position, e.pressEventCamera, out var local);
                float fraction = (local.x - viewport.rect.xMin) / viewport.rect.width;
                if (fraction < 0.22f) Navigate?.Invoke(-1);
                else if (fraction > 0.78f) Navigate?.Invoke(1);
                else ToggleControls?.Invoke();
            }
        }
        public void OnScroll(PointerEventData e) { if (PlacingPin) return; SetZoom(zoom + e.scrollDelta.y * 0.15f); ZoomFinished?.Invoke(zoom); }
        private void Update()
        {
            var screen = Touchscreen.current;
            if (screen == null) return;
            int count = 0;
            Vector2 first = default, second = default;
            foreach (var touch in screen.touches) if (touch.press.isPressed) {
                if (count == 0) first = touch.position.ReadValue();
                else if (count == 1) second = touch.position.ReadValue();
                count++;
            }
            if (PlacingPin) {
                if (count >= 2) usedPinch = true;
                pinching = false;
                return;
            }
            if (count >= 2) {
                float distance = Vector2.Distance(first, second);
                if (!pinching) { pinching = usedPinch = true; pinchDistance = Mathf.Max(1, distance); pinchZoom = zoom; }
                SetZoom(pinchZoom * distance / pinchDistance);
            } else if (pinching) { pinching = false; ZoomFinished?.Invoke(zoom); }
        }
        private void SetZoom(float value) { zoom = Mathf.Clamp(value, 1, 4); page.localScale = Vector3.one * zoom; ClampPan(); }
        private void ClampPan()
        {
            var max = Vector2.Max(Vector2.zero, (page.rect.size * zoom - viewport.rect.size) * 0.5f);
            page.anchoredPosition = new Vector2(Mathf.Clamp(page.anchoredPosition.x, -max.x, max.x), Mathf.Clamp(page.anchoredPosition.y, -max.y, max.y));
        }
    }

}
