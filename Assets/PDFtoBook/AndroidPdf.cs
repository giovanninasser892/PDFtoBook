using System;
using UnityEngine;

namespace PDFtoBook
{
    [Serializable]
    public sealed class NativeEvent
    {
        public string type, request, session, id, title, path, error;
        public int page, pageCount;
    }

    /// <summary>Java owns document descriptors, bitmaps and its serial rendering worker.</summary>
    public sealed class AndroidPdf : IDisposable
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private readonly AndroidJavaClass bridge = new AndroidJavaClass("com.pdftobook.reader.PdfBridge");
        public string Initialize(string receiver) => bridge.CallStatic<string>("initialize", receiver);
        public void Pick() => bridge.CallStatic("pickDocument");
        public void Open(string path, string session) => bridge.CallStatic("openDocument", path, session);
        public void Render(int page, int edge, string request, string session) => bridge.CallStatic("renderPage", page, edge, request, session);
        public void Close() => bridge.CallStatic("closeDocument");
        public void Dispose() { Close(); bridge.Dispose(); }
#else
        public string Initialize(string receiver) => System.IO.Path.Combine(Application.persistentDataPath, "EditorPreview");
        public void Pick() => throw new PlatformNotSupportedException("A importação e a renderização de PDFs estão disponíveis no APK Android.");
        public void Open(string path, string session) => throw new PlatformNotSupportedException("Abra o APK Android para ler este PDF.");
        public void Render(int page, int edge, string request, string session) { }
        public void Close() { }
        public void Dispose() { }
#endif
    }
}