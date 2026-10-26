using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using PDFtoBook;

public static class ReaderProjectSetup
{
    [MenuItem("PDFtoBook/Create reader scene")]
    public static void CreateScene()
    {
        if(EditorSceneManager.GetActiveScene().isDirty)throw new InvalidOperationException("Salve a cena atual antes de criar a cena do leitor.");
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var camera=new GameObject("Main Camera",typeof(Camera));camera.tag="MainCamera";
        var cam=camera.GetComponent<Camera>();cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=ReaderView.Background;cam.cullingMask=0;
        var canvasObject=new GameObject("ReaderCanvas",typeof(RectTransform),typeof(Canvas),typeof(UnityEngine.UI.CanvasScaler),typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();scaler.uiScaleMode=UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(720,1280);scaler.matchWidthOrHeight=0;
        var safe=ReaderView.Rect("SafeArea",canvasObject.transform);ReaderView.Fill(safe);safe.gameObject.AddComponent<SafeArea>();
        var view=safe.gameObject.AddComponent<ReaderView>();
        view.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Branding/Fonts/Not Jam Signature 21 SDF.asset");
        if(view.font==null)throw new InvalidOperationException("Importe a fonte Not Jam Signature antes de criar a cena.");
        var lib=ReaderView.Panel("Library",safe,ReaderView.Background);view.libraryPanel=lib.gameObject;
        var eyebrow=view.Text("Eyebrow",lib.transform,"PDFtoBook  /  LEITOR LOCAL",20,ReaderView.Accent);ReaderView.Fill(eyebrow.rectTransform,28,1200,28,28);Top(eyebrow.rectTransform,28,32,664,36);
        var heading=view.Text("Title",lib.transform,"Biblioteca",52,ReaderView.Ink);Top(heading.rectTransform,28,76,664,76);
        var subtitle=view.Text("Subtitle",lib.transform,"Seus livros. No seu tempo.",25,ReaderView.Muted);Top(subtitle.rectTransform,28,155,664,44);
        view.importButton=view.Button("ImportPdf",lib.transform,"+  Importar PDF");Top((RectTransform)view.importButton.transform,28,224,664,70);

        view.search=view.Input("Search",lib.transform,"Buscar na biblioteca");Top((RectTransform)view.search.transform,28,310,664,64);
        var list=ReaderView.CreateScroll(lib.transform,"Books",out view.bookList);ReaderView.Fill(list,28,88,28,400);
        view.libraryStatus=view.Text("Status",lib.transform,"Biblioteca local",20,ReaderView.Muted);Bottom(view.libraryStatus.rectTransform,28,24,664,44);
        var reader=ReaderView.Panel("Reader",safe,ReaderView.Background);view.readerPanel=reader.gameObject;
        var viewport=ReaderView.Panel("PageViewport",reader.transform,ReaderView.Background);view.pageViewport=(RectTransform)viewport.transform;ReaderView.Fill(view.pageViewport,0,180,0,110);viewport.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        view.pageRect=ReaderView.Rect("PdfPage",viewport.transform);view.pageRect.anchorMin=view.pageRect.anchorMax=new Vector2(0.5f,0.5f);view.pageRect.sizeDelta=new Vector2(660,920);
        view.pageImage=view.pageRect.gameObject.AddComponent<UnityEngine.UI.RawImage>();view.pageImage.raycastTarget=false;
        view.gestures=viewport.gameObject.AddComponent<PageGestures>();view.gestures.Configure(view.pageRect);
        view.readerStatus=view.Text("Loading",viewport.transform,"",25,ReaderView.Muted);view.readerStatus.alignment=TextAlignmentOptions.Center;ReaderView.Fill(view.readerStatus.rectTransform,30,30,30,30);
        var top=ReaderView.Panel("TopBar",reader.transform,ReaderView.Background);view.topBar=top.gameObject;AnchorBar((RectTransform)top.transform,true,110);
        view.backButton=view.Button("Back",top.transform,"Voltar");Top((RectTransform)view.backButton.transform,16,20,100,64);
        view.readerTitle=view.Text("BookTitle",top.transform,"Leitura",26,ReaderView.Ink);ReaderView.Fill(view.readerTitle.rectTransform,136,18,150,18);
        view.themeButton=view.Button("Theme",top.transform,"Tema");var tr=(RectTransform)view.themeButton.transform;tr.anchorMin=tr.anchorMax=new Vector2(1,0.5f);tr.pivot=new Vector2(1,0.5f);tr.sizeDelta=new Vector2(110,64);tr.anchoredPosition=new Vector2(-16,0);
        var bottom=ReaderView.Panel("BottomBar",reader.transform,ReaderView.Background);view.bottomBar=bottom.gameObject;AnchorBar((RectTransform)bottom.transform,false,180);
        view.previousButton=view.Button("Previous",bottom.transform,"Anterior");Bottom((RectTransform)view.previousButton.transform,16,94,130,62);
        view.nextButton=view.Button("Next",bottom.transform,"Próxima");Bottom((RectTransform)view.nextButton.transform,574,94,130,62);
        view.pageLabel=view.Text("PageLabel",bottom.transform,"1 / 1",24,ReaderView.Ink);Bottom(view.pageLabel.rectTransform,164,102,220,44);view.pageLabel.alignment=TextAlignmentOptions.Center;
        view.jumpButton=view.Button("Jump",bottom.transform,"Ir para");Bottom((RectTransform)view.jumpButton.transform,400,94,150,62);
        var sliderRect=ReaderView.Rect("Progress",bottom.transform);Bottom(sliderRect,28,75,664,7);
        var track=sliderRect.gameObject.AddComponent<UnityEngine.UI.Image>();track.color=ReaderView.Surface;
        view.progress=sliderRect.gameObject.AddComponent<UnityEngine.UI.Slider>();
        var fill=ReaderView.Panel("Fill",sliderRect,ReaderView.Accent);fill.raycastTarget=false;view.progress.fillRect=(RectTransform)fill.transform;
        var handle=ReaderView.Panel("Handle",sliderRect,ReaderView.Accent);var hr=(RectTransform)handle.transform;hr.sizeDelta=new Vector2(26,26);view.progress.handleRect=hr;view.progress.targetGraphic=handle;
        view.bookmarkButton=view.Button("Bookmark",bottom.transform,"Marcar");Bottom((RectTransform)view.bookmarkButton.transform,16,14,190,48);
        view.bookmarksButton=view.Button("Bookmarks",bottom.transform,"Marcadores");Bottom((RectTransform)view.bookmarksButton.transform,222,14,250,48);
        view.fitButton=view.Button("Fit",bottom.transform,"Ajustar");Bottom((RectTransform)view.fitButton.transform,488,14,216,48);
        var events=new GameObject("EventSystem",typeof(UnityEngine.EventSystems.EventSystem),typeof(InputSystemUIInputModule));
        events.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
        var app=new GameObject("ReaderApp").AddComponent<ReaderApp>();app.Configure(view);
        reader.gameObject.SetActive(false);
        ReaderBranding.ApplyScene();
        Directory.CreateDirectory("Assets/Scenes");EditorSceneManager.SaveScene(scene,"Assets/Scenes/Reader.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Scenes/Reader.unity",true)};
        AssetDatabase.SaveAssets();
    }
    private static void Top(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    private static void Bottom(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(w,h);}
    private static void AnchorBar(RectTransform r,bool top,float height){r.anchorMin=new Vector2(0,top?1:0);r.anchorMax=new Vector2(1,top?1:0);r.pivot=new Vector2(0.5f,top?1:0);r.anchoredPosition=Vector2.zero;r.sizeDelta=new Vector2(0,height);}
    [MenuItem("PDFtoBook/Configure Android")]
    public static void ConfigureAndroid()
    {
        PlayerSettings.productName="PDFtoBook";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.pdftobook.reader");
        PlayerSettings.bundleVersion="0.1.1";PlayerSettings.Android.bundleVersionCode=2;
        PlayerSettings.defaultInterfaceOrientation=UIOrientation.Portrait;
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.targetSdkVersion=AndroidSdkVersions.AndroidApiLevel36;
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.Android.applicationEntry=AndroidApplicationEntry.Activity;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.androidIsGame=false;
        PlayerSettings.Android.forceInternetPermission=false;PlayerSettings.stripEngineCode=true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android,false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,new[]{UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3});
        var defines=PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.Android).Split(';').Where(x=>!x.StartsWith("UNITY_PIPELINE_") && x != "ENABLE_RUNTIME_PIPELINE_DEV" && x != "ENABLE_RUNTIME_PIPELINE").ToArray();
        PlayerSettings.SetScriptingDefineSymbols(NamedBuildTarget.Android,string.Join(";",defines));
        var cloud=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/UnityConnectSettings.asset").FirstOrDefault();
        if(cloud!=null){var serialized=new SerializedObject(cloud);foreach(var name in new[]{"m_Enabled","UnityAnalyticsSettings.m_Enabled","UnityAnalyticsSettings.m_InitializeOnStartup","UnityCrashSettings.m_Enabled","UnityCrashSettings.m_EnableCloudDiagnosticsReporting","UnityAdsSettings.m_Enabled","UnityAdsSettings.m_InitializeOnStartup"}){var property=serialized.FindProperty(name);if(property!=null&&property.propertyType==SerializedPropertyType.Boolean)property.boolValue=false;}serialized.ApplyModifiedPropertiesWithoutUndo();}
        var settings=AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
        if(settings!=null){var serialized=new SerializedObject(settings);var analytics=serialized.FindProperty("submitAnalytics");if(analytics!=null)analytics.boolValue=false;serialized.ApplyModifiedPropertiesWithoutUndo();}
        EditorUserBuildSettings.buildAppBundle=false;EditorUserBuildSettings.development=false;
        AssetDatabase.SaveAssets();
    }
}