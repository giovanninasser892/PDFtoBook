using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Scripting;

namespace PDFtoBook
{
    public sealed class ReaderApp : MonoBehaviour
    {
        [SerializeField] private ReaderView view;
        private AndroidPdf pdf;
        private LibraryStore store;
        private BookRecord current;
        private string session, pendingRequest;
        private int desiredPage, desiredEdge = 1920;
        private bool rendering, opening, importing, initialized;
        private float nextDayCheck;
        private readonly Dictionary<int, CachedPage> cache = new Dictionary<int, CachedPage>();
        private readonly Queue<NativeEvent> events = new Queue<NativeEvent>();
        private const long CacheBudget = 48L * 1024 * 1024;
        private sealed class CachedPage { public Texture2D texture; public int edge; public long bytes; public float used; }
        public void Configure(ReaderView value) => view = value;
        private void Start()
        {
            Application.targetFrameRate=60;
            Screen.sleepTimeout=SleepTimeout.SystemSetting;
            try {
                pdf=new AndroidPdf(); store=new LibraryStore(pdf.Initialize(gameObject.name));
                Wire(); view.ShowLibrary(); RefreshLibrary(); RefreshMission(); initialized=true;
                if(!string.IsNullOrEmpty(store.RecoveryNotice)) view.Alert(store.RecoveryNotice);
            } catch(Exception error) { view.Alert("Não foi possível abrir os dados locais. "+error.Message); }
        }
        private void Wire()
        {
            view.importButton.onClick.AddListener(Import);
            view.missionButton.onClick.AddListener(()=>{
                RefreshMission();
                view.EditMission(store.State.mission.target, target=>{
                    store.State.mission.Refresh(DateTime.Now);
                    store.State.mission.target=target;
                    Save();view.UpdateMission(store.State.mission);
                });
            });
            view.pinButton.onClick.AddListener(TogglePinPlacement);
            view.gestures.PinPlaced+=PlacePin;
            view.backButton.onClick.AddListener(CloseBook);
            view.previousButton.onClick.AddListener(()=>Navigate(-1));
            view.nextButton.onClick.AddListener(()=>Navigate(1));
            view.jumpButton.onClick.AddListener(()=>{if(current!=null&&!opening){CancelPinPlacement();view.Jump(desiredPage,current.pageCount,RequestPage);}});
            view.fitButton.onClick.AddListener(()=>view.gestures.ResetView());
            view.bookmarkButton.onClick.AddListener(ToggleBookmark);
            view.bookmarksButton.onClick.AddListener(()=>{if(current!=null){CancelPinPlacement();view.Bookmarks(current,RequestPage);}});
            view.themeButton.onClick.AddListener(()=>{store.State.dark=!store.State.dark;view.SetTheme(store.State.dark);Save();});
            view.search.onValueChanged.AddListener(_=>RefreshLibrary());
            view.progress.onValueChanged.AddListener(value=>{if(current!=null&&!opening)RequestPage(Mathf.RoundToInt(value));});
            view.gestures.Navigate+=Navigate;
            view.gestures.ToggleControls+=()=>{if(view.dialog==null&&!view.gestures.PlacingPin&&!view.IsTransitioning)view.ToggleChrome();};
            view.gestures.ZoomFinished+=zoom=>{
                if(current==null||opening||zoom<1.4f) return;
                desiredEdge=2560; PumpRender();
            };
        }
        private void RefreshMission()
        {
            if(store==null)return;
            if(store.State.mission.Refresh(DateTime.Now))Save();
            view.UpdateMission(store.State.mission);
        }
        private void RefreshLibrary()
        {
            string filter=view.search.text.Trim();
            var books=store.SortedBooks.Where(book=>book.title.IndexOf(filter,StringComparison.OrdinalIgnoreCase)>=0);
            view.UpdateBookRows(books,OpenBook,book=>view.ConfirmRemove(book,()=>RemoveBook(book)));
            view.libraryStatus.text=importing?"Importando PDF…":$"{store.State.books.Count} livro(s)  ·  Apenas neste aparelho";
#if UNITY_EDITOR
            view.libraryStatus.text="Prévia · Leia seus PDFs no APK Android";
#endif
        }
        private void Import()
        {
            if(!initialized||importing)return;
            try { importing=true;view.importButton.interactable=false;RefreshLibrary();pdf.Pick(); }
            catch(Exception error){EndImport();view.Alert(error.Message);}
        }
        private void EndImport(){importing=false;view.importButton.interactable=true;if(store!=null)RefreshLibrary();}
        public void OpenBook(BookRecord book)
        {
            if(!initialized)return;
            if(!File.Exists(store.PdfPath(book))){view.Alert("O PDF não foi encontrado. Remova esta entrada e importe o arquivo novamente.");return;}
            if(current!=null)CloseBook();
            current=book;session=Guid.NewGuid().ToString("N");opening=true;desiredPage=book.page;
            view.ShowReader();view.readerTitle.text=book.title;view.readerStatus.text="Abrindo PDF…";
            view.pageImage.texture=null;view.SetTheme(store.State.dark);view.gestures.ResetView();view.SetPin(null,false);
            view.progress.wholeNumbers=true;view.progress.minValue=0;view.progress.maxValue=Math.Max(1,book.pageCount-1);view.progress.SetValueWithoutNotify(book.page);
            view.progress.interactable=false;UpdateControls();
            try{pdf.Open(store.PdfPath(book),session);}catch(Exception error){CloseBook();view.Alert(error.Message);}
        }
        private void Navigate(int direction){if(current!=null&&!opening&&view.dialog==null&&!view.IsTransitioning)RequestPage(desiredPage+direction);}
        public void RequestPage(int page)
        {
            if(current==null||opening)return;
            int target=Mathf.Clamp(page,0,current.pageCount-1);
            if(desiredPage!=target){
                CancelPinPlacement();
                if(current.ClearPinOnPageChange(target))Save();
                desiredPage=target;desiredEdge=1920;view.gestures.ResetView();
            }
            if(cache.TryGetValue(target,out var image))Display(target,image);
            else {view.pageImage.texture=null;view.readerStatus.text="Carregando página…";}
            UpdateControls();PumpRender();
        }
        private void PumpRender()
        {
            if(current==null||opening||rendering)return;
            int target=desiredPage,edge=desiredEdge;
            if(cache.TryGetValue(target,out var image)&&image.edge>=edge){
                target=desiredPage+1;edge=1920;
                if(target>=current.pageCount||cache.ContainsKey(target))return;
            }
            pendingRequest=Guid.NewGuid().ToString("N");rendering=true;
            try{pdf.Render(target,edge,pendingRequest,session);}catch(Exception error){rendering=false;view.readerStatus.text=error.Message;}
        }
        private void Display(int page,CachedPage image)
        {
            image.used=Time.unscaledTime;
            view.pageImage.texture=image.texture;
            FitPage(image.texture.width,image.texture.height);
            view.readerStatus.text=view.gestures.PlacingPin?"Toque na página para posicionar o pin.":"";
            bool changed=current.page!=page||current.lastRead==0;
            var mission=store.State.mission;
            var now=DateTime.Now;
            bool dayChanged=mission.Refresh(now);
            int before=mission.pages;bool wasComplete=mission.Complete;
            bool missionChanged=mission.RecordAdvance(current.page,page,now)||dayChanged;
            bool gained=mission.pages>before;
            bool completed=gained&&!wasComplete&&mission.TryCelebrate();
            current.page=page;current.lastRead=DateTime.UtcNow.Ticks;
            if(missionChanged)view.UpdateMission(mission,gained);
            if(changed||missionChanged||completed)Save();
            if(gained)view.feedbacks?.PageRead(completed);
            UpdateControls();
        }
        private void FitPage(int width,int height)
        {
            var space=view.pageViewport.rect.size;
            float fit=Mathf.Min(space.x/width,space.y/height);
            view.pageRect.sizeDelta=new Vector2(width*fit,height*fit);
        }
        private void UpdateControls()
        {
            if(current==null)return;
            view.pinButton.interactable=!opening&&view.pageImage.texture!=null;
            view.SetPin(view.pageImage.texture!=null&&current.page==desiredPage?current:null,view.gestures.PlacingPin);
            view.pageLabel.text=$"{desiredPage+1} / {current.pageCount}";
            view.progress.SetValueWithoutNotify(desiredPage);
            view.previousButton.interactable=!opening&&desiredPage>0;
            view.nextButton.interactable=!opening&&desiredPage<current.pageCount-1;
            view.bookmarkButton.interactable=!opening&&view.pageImage.texture!=null;
            view.bookmarkButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text=current.bookmarks.Contains(desiredPage)?"Marcado":"Marcar";
            view.StyleButton(view.bookmarkButton,current.bookmarks.Contains(desiredPage)?ButtonRole.Negative:ButtonRole.Positive);
        }
        private void TogglePinPlacement()
        {
            if(current==null||opening||view.pageImage.texture==null||view.dialog!=null)return;
            view.gestures.PlacingPin=!view.gestures.PlacingPin;
            view.readerStatus.text=view.gestures.PlacingPin?"Toque na página para posicionar o pin.":"";
            UpdateControls();
        }
        private void CancelPinPlacement()
        {
            if(!view.gestures.PlacingPin)return;
            view.gestures.PlacingPin=false;
            view.readerStatus.text="";
            view.SetPin(current,false);
        }
        private void PlacePin(Vector2 position)
        {
            if(current==null||view.pageImage.texture==null||current.page!=desiredPage||view.dialog!=null)return;
            current.SetPin(desiredPage,position);
            CancelPinPlacement();Save();UpdateControls();
        }
        private void ToggleBookmark()
        {
            if(current==null||view.pageImage.texture==null)return;
            if(!current.bookmarks.Remove(desiredPage))current.bookmarks.Add(desiredPage);
            current.bookmarks.Sort();Save();UpdateControls();
        }
        private void RemoveBook(BookRecord book)
        {
            store.State.books.Remove(book);
            if(!Save()){store.State.books.Add(book);return;}
            try{File.Delete(store.PdfPath(book));}catch(Exception){view.Alert("O livro foi removido da lista, mas sua cópia local não pôde ser apagada.");}
            RefreshLibrary();
        }
        public void CloseBook()
        {
            CancelPinPlacement();view.SetPin(null,false);
            if(store!=null)Save();
            session=null;pendingRequest=null;current=null;opening=rendering=false;
            pdf?.Close();view.pageImage.texture=null;ClearCache();view.ShowLibrary();
            if(store!=null)RefreshLibrary();
        }
        private void ClearCache(){foreach(var page in cache.Values)Destroy(page.texture);cache.Clear();}
        private void TrimCache()
        {
            long bytes=cache.Values.Sum(item=>item.bytes);
            while(cache.Count>3||bytes>CacheBudget){
                var victim=cache.Where(pair=>pair.Key!=desiredPage).OrderBy(pair=>pair.Value.used).FirstOrDefault();
                if(victim.Value==null)break;
                bytes-=victim.Value.bytes;Destroy(victim.Value.texture);cache.Remove(victim.Key);
            }
        }
        private bool Save()
        {
            try{store?.Save();return true;}catch(Exception){view.Alert("Não foi possível salvar o progresso. Verifique o espaço livre no aparelho.");return false;}
        }
        [Preserve]
        public void OnNativeEvent(string json)
        {
            try { events.Enqueue(JsonUtility.FromJson<NativeEvent>(json)); }
            catch(Exception){view.Alert("Resposta inválida do leitor de PDF.");}
        }
        private Vector2 viewportSize;
        private void Update()
        {
            if(initialized&&Time.unscaledTime>=nextDayCheck){nextDayCheck=Time.unscaledTime+1;RefreshMission();}
            while(events.Count>0)Handle(events.Dequeue());
            if(current!=null&&view.pageImage.texture!=null&&viewportSize!=view.pageViewport.rect.size){
                viewportSize=view.pageViewport.rect.size;FitPage(view.pageImage.texture.width,view.pageImage.texture.height);view.gestures.ResetView();
            }
            if(Keyboard.current!=null&&Keyboard.current.escapeKey.wasPressedThisFrame){
                if(view.dialog!=null)view.HideDialog();else if(view.gestures.PlacingPin)CancelPinPlacement();else if(current!=null)CloseBook();else Application.Quit();
            }
        }
        private void Handle(NativeEvent value)
        {
            if(value==null)return;
            if(value.type=="cancelled"){EndImport();return;}
            if(value.type=="imported"){
                EndImport();
                if(!Guid.TryParse(value.id,out _)||value.pageCount<1){view.Alert("PDF inválido.");return;}
                var book=new BookRecord{id=value.id,title=value.title,pageCount=value.pageCount};
                store.State.books.Add(book);
                if(!Save()){store.State.books.Remove(book);return;}
                RefreshLibrary();OpenBook(book);return;
            }
            if(value.type=="error"){
                if(value.request=="import"){EndImport();view.Alert(value.error);}
                else if(value.request==session){CloseBook();view.Alert(value.error);}
                else if(value.request==pendingRequest){rendering=false;view.readerStatus.text=value.error;}
                return;
            }
            if(value.type=="opened"&&value.request==session&&current!=null){
                current.pageCount=value.pageCount;opening=false;
                view.progress.maxValue=Math.Max(1,value.pageCount-1);view.progress.interactable=value.pageCount>1;
                RequestPage(Mathf.Clamp(current.page,0,value.pageCount-1));return;
            }
            if(value.type!="page")return;
            Texture2D texture=null;
            try{
                if(value.session!=session||current==null||value.request!=pendingRequest)return;
                rendering=false;
                texture=new Texture2D(2,2,TextureFormat.RGBA32,false);
                if(!texture.LoadImage(File.ReadAllBytes(value.path),true))throw new InvalidDataException();
                texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
                if(cache.TryGetValue(value.page,out var old))Destroy(old.texture);
                var cached=new CachedPage{texture=texture,edge=Math.Max(texture.width,texture.height),bytes=(long)texture.width*texture.height*4,used=Time.unscaledTime};
                cache[value.page]=cached;texture=null;
                if(value.page==desiredPage)Display(value.page,cached);
                TrimCache();PumpRender();
            }catch(Exception){rendering=false;view.readerStatus.text="Não foi possível exibir esta página. Tente outra página.";}
            finally{if(texture!=null)Destroy(texture);if(!string.IsNullOrEmpty(value.path)){try{File.Delete(value.path);}catch(Exception){}}}
        }
        private void OnApplicationPause(bool paused){if(!initialized)return;if(paused)Save();else RefreshMission();}
        private void OnApplicationFocus(bool focused){if(focused&&initialized)RefreshMission();}
        private void OnApplicationQuit(){if(initialized)Save();}
        private void OnDestroy(){ClearCache();pdf?.Dispose();}
    }
}