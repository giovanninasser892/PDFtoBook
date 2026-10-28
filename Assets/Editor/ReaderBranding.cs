using System;
using System.IO;
using PDFtoBook;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Packages the supplied pixel art without filtering or changing its palette.</summary>
public static class ReaderBranding
{
    private const string LogoPath="Assets/Branding/PDFtoBookLogo.png";
    private const string IconsFolder="Assets/Branding/AndroidIcons";

    [MenuItem("PDFtoBook/Apply logo and green palette")]
    public static void Apply()
    {
        EnsureFont();
        ConfigureImport(LogoPath,true);
        ApplyIcons();
        ApplyScene();
        PlayerSettings.bundleVersion="0.3.0";
        PlayerSettings.Android.bundleVersionCode=4;
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    public static void EnsureFont()
    {
        const string path="Assets/Branding/Fonts/Not Jam UI 15 SDF.asset";
        if(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path)!=null)return;
        var source=AssetDatabase.LoadAssetAtPath<Font>("Assets/Branding/Fonts/Not Jam UI 15.ttf");
        if(source==null)throw new FileNotFoundException("Not Jam UI 15.ttf");
        var font=TMP_FontAsset.CreateFontAsset(source,90,9,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,false);
        font.name="Not Jam UI 15 SDF";
        var characters=new System.Text.StringBuilder();
        for(int i=32;i<=126;i++)characters.Append((char)i);
        for(int i=160;i<=255;i++)characters.Append((char)i);
        for(int i=8192;i<=8303;i++)characters.Append((char)i);
        characters.Append("€™□");
        font.TryAddCharacters(characters.ToString(),out string missing);
        if(!font.HasCharacters("áéíóúãõâêôçÁÉÍÓÚÃÕÂÊÔÇ"))throw new InvalidDataException("A fonte não contém os acentos necessários.");
        font.atlasPopulationMode=AtlasPopulationMode.Static;
        font.hideFlags=HideFlags.None;
        font.material.hideFlags=HideFlags.None; font.material.name=font.name+" Material";
        AssetDatabase.CreateAsset(font,path);
        AssetDatabase.AddObjectToAsset(font.material,font);
        foreach(var atlas in font.atlasTextures){atlas.hideFlags=HideFlags.None;atlas.filterMode=FilterMode.Bilinear;AssetDatabase.AddObjectToAsset(atlas,font);}
        EditorUtility.SetDirty(font);AssetDatabase.SaveAssets();
    }
    public static void ApplyScene()
    {
        var view=UnityEngine.Object.FindFirstObjectByType<ReaderView>(FindObjectsInactive.Include);
        if(view==null)return;
        view.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Branding/Fonts/Not Jam UI 15 SDF.asset");
        view.buttonSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Branding/UI/UI_Button.png");
        view.sliderSprite=AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Branding/UI/UI_ButtonSlider.png");
        if(view.font==null||view.buttonSprite==null||view.sliderSprite==null)throw new InvalidDataException("Fonte ou sprites da interface ausentes.");
        if(view.font.fallbackFontAssetTable==null)view.font.fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>();
        var fallback=TMP_Settings.defaultFontAsset;
        if(fallback!=null&&fallback!=view.font&&!view.font.fallbackFontAssetTable.Contains(fallback)){view.font.fallbackFontAssetTable.Add(fallback);EditorUtility.SetDirty(view.font);}
        foreach(var text in view.GetComponentsInChildren<TMP_Text>(true)){
            text.font=view.font;text.color=ReaderView.Ink;text.fontSize=Mathf.Max(15,Mathf.Round(text.fontSize/15)*15);EditorUtility.SetDirty(text);
        }
        foreach(var image in view.GetComponentsInChildren<UnityEngine.UI.Image>(true)) {
            if(image.name=="Logo"||image==view.pinOverlay||image==view.pinImage)continue;
            image.color=image.name=="Fill"||image.name=="Handle"||image.name=="ProgressLine"?Color.white:ReaderView.Background;
            EditorUtility.SetDirty(image);
        }
        foreach(var button in view.GetComponentsInChildren<UnityEngine.UI.Button>(true))view.StyleButton(button,ReaderView.RoleFor(button.name));
        foreach(var input in view.GetComponentsInChildren<TMP_InputField>(true)){
            var image=input.GetComponent<UnityEngine.UI.Image>();image.color=Color.white;image.sprite=view.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=0.5f;
            var outline=image.GetComponent<UnityEngine.UI.Outline>();if(outline!=null)UnityEngine.Object.DestroyImmediate(outline);
            input.fontAsset=view.font;input.customCaretColor=true;input.caretColor=Color.black;input.selectionColor=new Color(0,0,0,0.25f);
            input.textComponent.color=Color.black;if(input.placeholder!=null)input.placeholder.color=new Color(0,0,0,0.65f);
        }
        var handle=view.progress.handleRect.GetComponent<UnityEngine.UI.Image>();handle.sprite=view.sliderSprite;handle.type=UnityEngine.UI.Image.Type.Sliced;handle.color=Color.white;handle.pixelsPerUnitMultiplier=0.5f;
        view.progress.handleRect.sizeDelta=new Vector2(38,38);
        view.progress.GetComponent<UnityEngine.UI.Image>().color=new Color(1,1,1,0.25f);
        view.SetTheme(true);
        ((RectTransform)view.backButton.transform).sizeDelta=new Vector2(120,64);
        ReaderView.Fill(view.readerTitle.rectTransform,152,18,150,18);
        ((RectTransform)view.previousButton.transform).sizeDelta=new Vector2(150,62);
        view.pageLabel.rectTransform.anchoredPosition=new Vector2(180,102);view.pageLabel.rectTransform.sizeDelta=new Vector2(180,44);
        ((RectTransform)view.jumpButton.transform).anchoredPosition=new Vector2(380,94);((RectTransform)view.jumpButton.transform).sizeDelta=new Vector2(170,62);
        if(Camera.main!=null)Camera.main.backgroundColor=ReaderView.Background;
        var library=view.libraryPanel.transform;
        var logo=library.Find("Logo");
        if(logo!=null)UnityEngine.Object.DestroyImmediate(logo.gameObject);
        var eyebrow=library.Find("Eyebrow") as RectTransform;
        if(eyebrow!=null){eyebrow.anchoredPosition=new Vector2(28,-24);eyebrow.sizeDelta=new Vector2(664,36);}
        var title=library.Find("Title").GetComponent<TMP_Text>();
        title.fontSize=75;title.fontStyle=FontStyles.Bold;
        ((RectTransform)title.transform).sizeDelta=new Vector2(664,90);
        EditorUtility.SetDirty(view);
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);
    }

    public static void ApplyIcons()
    {
        Directory.CreateDirectory(IconsFolder);
        var source=new Texture2D(2,2,TextureFormat.RGBA32,false);
        if(!source.LoadImage(File.ReadAllBytes(LogoPath)))throw new InvalidDataException("Logo inválida.");
        var pixels=source.GetPixels32();
        int minX=source.width,minY=source.height,maxX=-1,maxY=-1;
        for(int y=0;y<source.height;y++)for(int x=0;x<source.width;x++)if(pixels[y*source.width+x].a>0){minX=Math.Min(minX,x);maxX=Math.Max(maxX,x);minY=Math.Min(minY,y);maxY=Math.Max(maxY,y);}
        if(maxX<minX)throw new InvalidDataException("A logo está vazia.");
        var bounds=new RectInt(minX,minY,maxX-minX+1,maxY-minY+1);
        var platform=NamedBuildTarget.Android;
        foreach(var kind in PlayerSettings.GetSupportedIconKinds(platform)) {
            var slots=PlayerSettings.GetPlatformIcons(platform,kind);
            for(int i=0;i<slots.Length;i++) {
                var slot=slots[i];
                if(slot.minLayerCount==2){
                    var bg=SaveIcon($"AdaptiveBackground-{slot.width}.png",Draw(source,pixels,bounds,slot.width,slot.height,true,false));
                    var fg=SaveIcon($"AdaptiveForeground-{slot.width}.png",Draw(source,pixels,bounds,slot.width,slot.height,false,true));
                    slot.SetTextures(new[]{bg,fg});
                } else {
                    var icon=SaveIcon($"Legacy-{slot.width}.png",Draw(source,pixels,bounds,slot.width,slot.height,true,true));
                    slot.SetTextures(new[]{icon});
                }
            }
            PlayerSettings.SetPlatformIcons(platform,kind,slots);
        }
        UnityEngine.Object.DestroyImmediate(source);
    }

    private static Texture2D Draw(Texture2D source,Color32[] pixels,RectInt bounds,int width,int height,bool background,bool foreground)
    {
        var result=new Texture2D(width,height,TextureFormat.RGBA32,false);
        var output=new Color32[width*height];
        Color32 fill=background?(Color32)ReaderView.Background:new Color32(0,0,0,0);
        for(int i=0;i<output.Length;i++)output[i]=fill;
        if(foreground){
            float fraction=background?0.78f:56f/108f;
            float scale=Mathf.Min(width*fraction/bounds.width,height*fraction/bounds.height);
            int w=Mathf.Max(1,Mathf.RoundToInt(bounds.width*scale)),h=Mathf.Max(1,Mathf.RoundToInt(bounds.height*scale));
            int left=(width-w)/2,bottom=(height-h)/2;
            for(int y=0;y<h;y++)for(int x=0;x<w;x++){
                int sx=bounds.x+Mathf.Min(bounds.width-1,x*bounds.width/w),sy=bounds.y+Mathf.Min(bounds.height-1,y*bounds.height/h);
                var pixel=pixels[sy*source.width+sx];
                if(pixel.a>0)output[(bottom+y)*width+left+x]=pixel;
            }
        }
        result.SetPixels32(output);result.Apply();return result;
    }

    private static Texture2D SaveIcon(string name,Texture2D texture)
    {
        string path=IconsFolder+"/"+name;
        File.WriteAllBytes(path,texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        ConfigureImport(path,false);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void ConfigureImport(string path,bool sprite)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new FileNotFoundException(path);
        importer.textureType=sprite?TextureImporterType.Sprite:TextureImporterType.Default;
        if(sprite)importer.spriteImportMode=SpriteImportMode.Single;
        importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.npotScale=TextureImporterNPOTScale.None;importer.alphaIsTransparency=true;
        importer.maxTextureSize=1024;importer.SaveAndReimport();
    }
}
