using UnityEngine;
namespace PDFtoBook
{
    public sealed class SafeArea : MonoBehaviour
    {
        private Rect previous;
        private Vector2Int size;
        private void Update()
        {
            Rect area = Screen.safeArea;
            if (area == previous && size.x == Screen.width && size.y == Screen.height) return;
            previous = area; size = new Vector2Int(Screen.width, Screen.height);
            var rect = (RectTransform)transform;
            rect.anchorMin = new Vector2(area.xMin / Screen.width, area.yMin / Screen.height);
            rect.anchorMax = new Vector2(area.xMax / Screen.width, area.yMax / Screen.height);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}