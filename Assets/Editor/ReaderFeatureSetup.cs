using System;
using PDFtoBook;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Adds mission/pin controls to the existing scene, preserving its other objects.</summary>
public static class ReaderFeatureSetup
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Saia do Play Mode antes de editar a cena.");
        var view=UnityEngine.Object.FindFirstObjectByType<ReaderView>(FindObjectsInactive.Include);
        if(view==null)throw new InvalidOperationException("Abra a cena Reader.");
        ReaderBranding.EnsureFont();
        view.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Branding/Fonts/Not Jam UI 15 SDF.asset");
        if(view.font.fallbackFontAssetTable==null)view.font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
        var fallback=TMP_Settings.defaultFontAsset;
        if(fallback!=null&&!view.font.fallbackFontAssetTable.Contains(fallback))view.font.fallbackFontAssetTable.Add(fallback);
        foreach(var text in view.GetComponentsInChildren<TMP_Text>(true)){
            text.font=view.font;
            text.fontSize=Mathf.Max(15,Mathf.Round(text.fontSize/15)*15);
            EditorUtility.SetDirty(text);
        }
        foreach(var input in view.GetComponentsInChildren<TMP_InputField>(true)){input.fontAsset=view.font;EditorUtility.SetDirty(input);}
        var library=view.libraryPanel.transform;
        var logo=library.Find("Logo");
        if(logo!=null)UnityEngine.Object.DestroyImmediate(logo.gameObject);
        var eyebrow=library.Find("Eyebrow").GetComponent<TextMeshProUGUI>();
        Top(eyebrow.rectTransform,28,24,664,36);eyebrow.fontSize=30;
        var title=library.Find("Title").GetComponent<TextMeshProUGUI>();
        Top(title.rectTransform,28,70,664,90);title.fontSize=75;title.fontStyle=FontStyles.Bold;
        var subtitle=library.Find("Subtitle") as RectTransform;
        if(subtitle!=null)Top(subtitle,28,166,664,44);

        var mission=library.Find("DailyMission") as RectTransform;
        if(mission==null){
            mission=ReaderView.Rect("DailyMission",library);
            var label=view.Text("Heading",mission,"MISSÃO DIÁRIA",25,Color.white);Top(label.rectTransform,0,0,460,34);
            view.missionLabel=view.Text("Count",mission,"0 páginas · Defina uma meta",25,Color.white);Top(view.missionLabel.rectTransform,0,36,476,40);
            var reset=view.Text("Reset",mission,"Reinicia às 00:00",20,Color.white);Top(reset.rectTransform,0,82,476,28);
            view.missionButton=view.Button("DailyGoal",mission,"Meta");
            var button=(RectTransform)view.missionButton.transform;
            button.anchorMin=button.anchorMax=new Vector2(1,1);button.pivot=new Vector2(1,1);button.anchoredPosition=new Vector2(0,-16);button.sizeDelta=new Vector2(166,60);
            var track=ReaderView.Panel("Track",mission,new Color(1,1,1,0.2f));track.raycastTarget=false;
            var bar=(RectTransform)track.transform;bar.anchorMin=Vector2.zero;bar.anchorMax=new Vector2(1,0);bar.pivot=Vector2.zero;bar.anchoredPosition=Vector2.zero;bar.sizeDelta=new Vector2(0,4);
            view.missionFill=ReaderView.Panel("Fill",track.transform,Color.white);view.missionFill.raycastTarget=false;
            var fill=(RectTransform)view.missionFill.transform;fill.anchorMax=new Vector2(0,1);fill.offsetMin=fill.offsetMax=Vector2.zero;
        }
        Top(mission,28,400,664,124);
        view.missionLabel.fontSize=24;
        mission.Find("Reset").GetComponent<TMP_Text>().fontSize=24;
        foreach(var button in view.GetComponentsInChildren<UnityEngine.UI.Button>(true))view.StyleButton(button,ReaderView.RoleFor(button.name));
        var list=view.bookList.parent.parent as RectTransform;
        ReaderView.Fill(list,28,88,28,550);

        if(view.pinButton==null){view.pinButton=view.Button("Pin",view.bottomBar.transform,"Pin");Bottom((RectTransform)view.pinButton.transform,16,188,190,44);}
        if(view.readerMissionLabel==null){view.readerMissionLabel=view.Text("DailyCount",view.bottomBar.transform,"Hoje: 0 páginas",25,Color.white);Bottom(view.readerMissionLabel.rectTransform,222,188,482,44);}
        var bottom=(RectTransform)view.bottomBar.transform;bottom.sizeDelta=new Vector2(bottom.sizeDelta.x,244);
        ReaderView.Fill(view.pageViewport,0,244,0,110);

        const string pinPath="Assets/Branding/UI/UI_Pin.png";
        var importer=AssetImporter.GetAtPath(pinPath) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Sprite UI_Pin ausente.");
        importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;importer.SaveAndReimport();
        view.pinSprite=AssetDatabase.LoadAssetAtPath<Sprite>(pinPath);
        if(view.pinOverlay==null){view.pinOverlay=ReaderView.Panel("PinPlacement",view.pageRect,new Color(0.1f,0.35f,0.7f,0.45f));view.pinOverlay.raycastTarget=false;}
        if(view.pinImage==null){
            var rect=ReaderView.Rect("ReadingPin",view.pageRect);
            rect.pivot=new Vector2(6.5f/16f,0.5f/16f);rect.sizeDelta=new Vector2(48,48);
            view.pinImage=rect.gameObject.AddComponent<UnityEngine.UI.Image>();view.pinImage.raycastTarget=false;
        }
        ((RectTransform)view.pinImage.transform).pivot=new Vector2(6.5f/16f,0.5f/16f);
        view.pinImage.sprite=view.pinSprite;view.pinImage.color=Color.white;view.pinImage.preserveAspect=true;
        view.SetPin(null,false);
        view.UpdateMission(new DailyMission());
        EditorUtility.SetDirty(view);EditorUtility.SetDirty(view.font);
        PlayerSettings.bundleVersion="0.3.0";PlayerSettings.Android.bundleVersionCode=4;
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
        EditorSceneManager.SaveScene(view.gameObject.scene);
        AssetDatabase.SaveAssets();
    }
    private static void Top(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=new Vector2(0,1);r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    private static void Bottom(RectTransform r,float x,float y,float width,float height){r.anchorMin=r.anchorMax=Vector2.zero;r.pivot=Vector2.zero;r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);}
}