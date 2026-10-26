using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace PDFtoBook
{
    public enum ButtonRole { Neutral, Positive, Negative }

    /// <summary>Scene references and small UI factories; no PDF or persistence responsibilities.</summary>
    public sealed class ReaderView : MonoBehaviour
    {
        public TMP_FontAsset font;
        public Sprite buttonSprite, sliderSprite;
        public GameObject libraryPanel, readerPanel, topBar, bottomBar, dialog;
        public RectTransform bookList, pageRect, pageViewport;
        public TextMeshProUGUI libraryStatus, readerStatus, readerTitle, pageLabel;
        public UnityEngine.UI.RawImage pageImage;
        public UnityEngine.UI.Button importButton, backButton, previousButton, nextButton, bookmarkButton, bookmarksButton, jumpButton, fitButton, themeButton;
        public UnityEngine.UI.Slider progress;
        public TMP_InputField search;
        public PageGestures gestures;
        private bool dark = true;
        public static Color Background => Color.black;
        public static Color Surface => new Color32(71, 114, 56, 255);
        public static Color Ink => Color.white;
        public static Color Muted => Color.white;
        public static Color Accent => new Color32(97, 165, 63, 255);

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform; rect.SetParent(parent, false); return rect;
        }
        public static void Fill(RectTransform rect, float left=0, float bottom=0, float right=0, float top=0)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom); rect.offsetMax = new Vector2(-right, -top);
        }
        public static UnityEngine.UI.Image Panel(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent); Fill(rect);
            var image = rect.gameObject.AddComponent<UnityEngine.UI.Image>(); image.color = color; return image;
        }
        public TextMeshProUGUI Text(string name, Transform parent, string value, float size, Color color)
        {
            var rect = Rect(name, parent); Fill(rect);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = Mathf.Max(13,Mathf.Round(size/13)*13); text.color = color;
            text.enableAutoSizing = false; text.raycastTarget = false; text.richText = false;
            text.alignment = TextAlignmentOptions.MidlineLeft; text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }
        public UnityEngine.UI.Button Button(string name, Transform parent, string label, UnityAction action=null, ButtonRole? role=null)
        {
            var image = Panel(name, parent, Surface);
            var button = image.gameObject.AddComponent<UnityEngine.UI.Button>(); button.targetGraphic = image;

            var colors = button.colors; colors.highlightedColor = new Color(1.2f,1.2f,1.2f); colors.pressedColor = new Color(0.7f,0.7f,0.7f); button.colors = colors;
            var text = Text("Label", image.transform, label, 23, Ink);
            text.alignment = TextAlignmentOptions.Center; Fill(text.rectTransform, 8, 0, 8, 0);
            StyleButton(button,role??RoleFor(name));
            if (action != null) button.onClick.AddListener(action);
            return button;
        }
        public static ButtonRole RoleFor(string name)
        {
            switch(name) {
                case "ImportPdf": case "Go": case "Bookmark": return ButtonRole.Positive;
                case "Back": case "Remove": case "Cancel": case "Close": return ButtonRole.Negative;
                default: return ButtonRole.Neutral;
            }
        }
        public void StyleButton(UnityEngine.UI.Button button,ButtonRole role)
        {
            var image=button.GetComponent<UnityEngine.UI.Image>();
            image.color=role==ButtonRole.Positive?Accent:role==ButtonRole.Negative?Surface:Color.white;
            if(buttonSprite!=null){image.sprite=buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.fillCenter=true;image.pixelsPerUnitMultiplier=0.5f;}
            var outline=image.GetComponent<UnityEngine.UI.Outline>();
            if(outline!=null){if(Application.isPlaying)Destroy(outline);else DestroyImmediate(outline);}
            foreach(var text in button.GetComponentsInChildren<TMP_Text>(true))text.color=role==ButtonRole.Negative?Color.white:Color.black;
        }
        public static void Height(Transform item, float height, float width=-1)
        {
            var layout = item.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
            layout.minHeight = layout.preferredHeight = height;
            if (width >= 0) layout.minWidth = layout.preferredWidth = width;
        }
        public void ShowLibrary() { libraryPanel.SetActive(true); readerPanel.SetActive(false); HideDialog(); }
        public void ShowReader() { libraryPanel.SetActive(false); readerPanel.SetActive(true); topBar.SetActive(true); bottomBar.SetActive(true); }
        public void ToggleChrome() { bool value = !topBar.activeSelf; topBar.SetActive(value); bottomBar.SetActive(value); }
        public void SetTheme(bool value)
        {
            dark = value;
            pageViewport.GetComponent<UnityEngine.UI.Image>().color = dark ? Background : Color.white;
            pageImage.color = Color.white; // PDF colors remain faithful; theme affects the surrounding UI.
        }
        public void UpdateBookRows(IEnumerable<BookRecord> books, Action<BookRecord> open, Action<BookRecord> remove)
        {
            for (int i = bookList.childCount - 1; i >= 0; i--) { var child=bookList.GetChild(i); child.gameObject.SetActive(false); Destroy(child.gameObject); }
            int count = 0;
            foreach (var book in books) {
                count++;
                var row = Panel("Book", bookList, Background); Height(row.transform, 124);
                var openButton = Button("Open", row.transform, "", () => open(book));
                Fill((RectTransform)openButton.transform, 0, 0, 144, 0);
                var title = Text("Title", openButton.transform, book.title, 27, Color.black);
                Fill(title.rectTransform, 20, 56, 14, 12);
                var info = Text("Progress", openButton.transform, book.lastRead == 0 ? $"{book.pageCount} páginas  ·  Novo" : book.Progress, 20, Color.black);
                Fill(info.rectTransform, 20, 14, 14, 65);
                var delete = Button("Remove", row.transform, "Excluir", () => remove(book));
                var dr = (RectTransform)delete.transform; dr.anchorMin = new Vector2(1,0); dr.anchorMax = Vector2.one; dr.pivot = new Vector2(1,0.5f);
                dr.sizeDelta = new Vector2(140,0); dr.anchoredPosition = Vector2.zero;
                var line = Panel("ProgressLine", row.transform, Color.black); line.raycastTarget = false;
                var lr = (RectTransform)line.transform; lr.anchorMin = Vector2.zero; lr.anchorMax = new Vector2(book.lastRead == 0 ? 0 : (book.page+1f)/book.pageCount, 0);
                lr.pivot = Vector2.zero; lr.sizeDelta = new Vector2(0,3); lr.anchoredPosition = Vector2.zero;
            }
            if (count == 0) {
                var text = Text("Empty", bookList, string.IsNullOrWhiteSpace(search.text) ? "Sua próxima leitura começa aqui.\nImporte um PDF para abrir seu primeiro livro." : "Nenhum livro encontrado.", 26, Muted);
                text.alignment = TextAlignmentOptions.Center; text.textWrappingMode = TextWrappingModes.Normal; Height(text.transform, 230);
            }
        }
        public void HideDialog() { if (dialog != null) { Destroy(dialog); dialog = null; } }
        public RectTransform Dialog(string title, float height=340)
        {
            HideDialog();
            dialog = Panel("DialogOverlay", transform, new Color(Background.r,Background.g,Background.b,0.82f)).gameObject;
            var box = Panel("Dialog", dialog.transform, Background);
            var rect = (RectTransform)box.transform; rect.anchorMin = rect.anchorMax = new Vector2(0.5f,0.5f); rect.pivot=new Vector2(0.5f,0.5f); rect.sizeDelta=new Vector2(610,height); rect.anchoredPosition=Vector2.zero;
            var heading = Text("Heading", rect, title, 29, Ink); Fill(heading.rectTransform,24,height-76,24,18);
            return rect;
        }
        public void Alert(string message)
        {
            var box = Dialog("Não foi possível concluir", 360);
            var body=Text("Message",box,message,25,Muted); Fill(body.rectTransform,24,90,24,80); body.textWrappingMode=TextWrappingModes.Normal;
            var close=Button("Close",box,"Entendi",HideDialog); Fill((RectTransform)close.transform,24,22,24,278);
        }
        public TMP_InputField Input(string name, Transform parent, string placeholder, TMP_InputField.ContentType type=TMP_InputField.ContentType.Standard)
        {
            var panel=Panel(name,parent,Color.white);
            if(buttonSprite!=null){panel.sprite=buttonSprite;panel.type=UnityEngine.UI.Image.Type.Sliced;panel.pixelsPerUnitMultiplier=0.5f;}
            var viewport=Rect("TextArea",panel.transform); Fill(viewport,16,8,16,8); viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
            var text=Text("Text",viewport,"",25,Color.black);
            var hint=Text("Placeholder",viewport,placeholder,25,new Color(0,0,0,0.65f));
            var input=panel.gameObject.AddComponent<TMP_InputField>(); input.textViewport=viewport; input.textComponent=text; input.placeholder=hint;
            input.customCaretColor=true; input.caretColor=Color.black; input.selectionColor=new Color(0,0,0,0.25f);
            input.fontAsset=font; input.contentType=type; input.lineType=TMP_InputField.LineType.SingleLine;
            return input;
        }
        public void ConfirmRemove(BookRecord book, Action confirm)
        {
            var box=Dialog("Remover da biblioteca?",360);
            var body=Text("Message",box,book.title+"\nA cópia original do PDF não será alterada.",25,Muted); Fill(body.rectTransform,24,100,24,80); body.textWrappingMode=TextWrappingModes.Normal;
            var cancel=Button("Cancel",box,"Cancelar",HideDialog); Fill((RectTransform)cancel.transform,24,24,320,276);
            var remove=Button("Remove",box,"Remover",()=>{HideDialog();confirm();}); Fill((RectTransform)remove.transform,320,24,24,276);
        }
        public void Jump(int current, int total, Action<int> go)
        {
            var box=Dialog($"Ir para página (1–{total})",340);
            var input=Input("PageNumber",box,"Número da página",TMP_InputField.ContentType.IntegerNumber); Fill((RectTransform)input.transform,24,145,24,90); input.text=(current+1).ToString();
            var cancel=Button("Cancel",box,"Cancelar",HideDialog); Fill((RectTransform)cancel.transform,24,24,320,256);
            UnityAction submit=()=>{ if(int.TryParse(input.text,out int number)&&number>=1&&number<=total){HideDialog();go(number-1);} else input.text=""; };
            var ok=Button("Go",box,"Ir",submit); Fill((RectTransform)ok.transform,320,24,24,256);
            input.onSubmit.AddListener(_=>submit()); input.ActivateInputField();
        }
        public void Bookmarks(BookRecord book, Action<int> go)
        {
            var box=Dialog("Marcadores",600);
            var list=CreateScroll(box,"Bookmarks",out var content); Fill(list,24,100,24,90);
            foreach(int page in book.bookmarks) { int target=page; var button=Button("Bookmark",content,$"Página {page+1}",()=>{HideDialog();go(target);}); Height(button.transform,64); }
            if(book.bookmarks.Count==0){var text=Text("Empty",content,"Nenhuma página marcada.",25,Muted);Height(text.transform,90);}
            var close=Button("Close",box,"Fechar",HideDialog);Fill((RectTransform)close.transform,24,24,24,516);
        }
        public static RectTransform CreateScroll(Transform parent,string name,out RectTransform content)
        {
            var rect=Rect(name,parent); Fill(rect);
            var scroll=rect.gameObject.AddComponent<UnityEngine.UI.ScrollRect>(); scroll.horizontal=false; scroll.movementType=UnityEngine.UI.ScrollRect.MovementType.Clamped;
            var viewportImage=Panel("Viewport",rect,new Color(1,1,1,0.01f)); var mask=viewportImage.gameObject.AddComponent<UnityEngine.UI.Mask>(); mask.showMaskGraphic=false;
            var viewport=(RectTransform)viewportImage.transform;
            content=Rect("Content",viewport); content.anchorMin=new Vector2(0,1);content.anchorMax=Vector2.one;content.pivot=new Vector2(0.5f,1);content.sizeDelta=Vector2.zero;
            var layout=content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();layout.spacing=12;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandWidth=true;layout.childForceExpandHeight=false;
            var fitter=content.gameObject.AddComponent<UnityEngine.UI.ContentSizeFitter>();fitter.verticalFit=UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport=viewport;scroll.content=content;return rect;
        }
    }
}