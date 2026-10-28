using System;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using PDFtoBook;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ReaderMotionSetup
{
    public static void Apply()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Saia do Play Mode antes de configurar os efeitos.");
        var view=UnityEngine.Object.FindFirstObjectByType<ReaderView>(FindObjectsInactive.Include);
        if(view==null)throw new InvalidOperationException("Abra a cena Reader.");
        var fx=view.GetComponent<ReaderFeedbacks>();if(fx==null)fx=view.gameObject.AddComponent<ReaderFeedbacks>();
        fx.view=view;view.feedbacks=fx;
        fx.libraryGroup=Group(view.libraryPanel);fx.readerGroup=Group(view.readerPanel);
        fx.libraryBar=Progress((RectTransform)view.missionFill.transform.parent,view.missionFill);
        var readerTrack=view.bottomBar.transform.Find("DailyProgress") as RectTransform;
        if(readerTrack==null){
            var image=ReaderView.Panel("DailyProgress",view.bottomBar.transform,new Color(1,1,1,0.22f));image.raycastTarget=false;
            readerTrack=(RectTransform)image.transform;
            readerTrack.anchorMin=readerTrack.anchorMax=Vector2.zero;readerTrack.pivot=Vector2.zero;
            readerTrack.anchoredPosition=new Vector2(222,180);readerTrack.sizeDelta=new Vector2(482,6);
            var fill=ReaderView.Panel("Fill",readerTrack,Color.white);fill.raycastTarget=false;
        }
        fx.readerBar=Progress(readerTrack,readerTrack.Find("Fill").GetComponent<UnityEngine.UI.Image>());
        fx.gainGroup=Bubble(view,"ReadGain",new Vector2(144,70),new Vector2(0.5f,0),new Vector2(0,260),out var gainRect);
        fx.gainLabel=gainRect.GetComponentInChildren<TextMeshProUGUI>(true);
        if(fx.gainLabel==null){fx.gainLabel=view.Text("Number",gainRect,"+1",45,Color.white);fx.gainLabel.alignment=TextAlignmentOptions.Center;}
        fx.completionGroup=Bubble(view,"MissionComplete",new Vector2(620,156),new Vector2(0.5f,0.5f),new Vector2(0,60),out var completeRect);
        fx.completionRect=completeRect;
        if(completeRect.Find("Heading")==null){
            var heading=view.Text("Heading",completeRect,"Missão concluída!",45,Color.white);ReaderView.Fill(heading.rectTransform,12,66,12,12);heading.alignment=TextAlignmentOptions.Center;heading.textWrappingMode=TextWrappingModes.NoWrap;
            var detail=view.Text("Detail",completeRect,"Meta de leitura do dia alcançada",24,Color.white);ReaderView.Fill(detail.rectTransform,12,18,12,92);detail.fontSize=24;detail.alignment=TextAlignmentOptions.Center;
        }
        fx.incrementFeedbacks=Player(view,"PageIncrementFeedbacks",fx,false);
        fx.completionFeedbacks=Player(view,"MissionCompleteFeedbacks",fx,true);
        var blocker=view.transform.Find("TransitionBlocker");
        if(blocker==null){var image=ReaderView.Panel("TransitionBlocker",view.transform,Color.clear);fx.transitionBlocker=image.gameObject;}
        else fx.transitionBlocker=blocker.gameObject;
        fx.transitionBlocker.SetActive(false);
        fx.gainGroup.alpha=fx.completionGroup.alpha=0;
        foreach(var button in view.GetComponentsInChildren<UnityEngine.UI.Button>(true))view.StyleButton(button,ReaderView.RoleFor(button.name));
        ConfigurePostProcessing(view);
        PlayerSettings.bundleVersion="0.3.0";PlayerSettings.Android.bundleVersionCode=4;
        EditorUtility.SetDirty(view);EditorUtility.SetDirty(fx);
        foreach(var component in new UnityEngine.Object[]{fx.libraryGroup,fx.readerGroup,fx.libraryBar,fx.readerBar,fx.incrementFeedbacks,fx.completionFeedbacks})EditorUtility.SetDirty(component);
        EditorSceneManager.MarkSceneDirty(view.gameObject.scene);EditorSceneManager.SaveScene(view.gameObject.scene);AssetDatabase.SaveAssets();
    }
    private static CanvasGroup Group(GameObject obj){var group=obj.GetComponent<CanvasGroup>();return group!=null?group:obj.AddComponent<CanvasGroup>();}
    private static MMProgressBar Progress(RectTransform track,UnityEngine.UI.Image fill)
    {
        var bar=track.GetComponent<MMProgressBar>();if(bar==null)bar=track.gameObject.AddComponent<MMProgressBar>();
        bar.ForegroundBar=fill.transform;bar.FillMode=MMProgressBar.FillModes.Anchor;
        bar.TimeScale=MMProgressBar.TimeScales.UnscaledTime;bar.BarFillMode=MMProgressBar.BarFillModes.FixedDuration;
        bar.LerpForegroundBar=true;bar.LerpForegroundBarDurationIncreasing=0.32f;bar.LerpForegroundBarDurationDecreasing=0.2f;
        bar.LerpForegroundBarCurveIncreasing=new AnimationCurve(new Keyframe(0,0,0,3),new Keyframe(1,1,0,0));
        bar.BumpScaleOnChange=false;bar.BumpOnIncrease=false;bar.BumpOnDecrease=false;bar.ChangeColorWhenBumping=false;
        fill.rectTransform.offsetMin=fill.rectTransform.offsetMax=Vector2.zero;
        return bar;
    }
    private static CanvasGroup Bubble(ReaderView view,string name,Vector2 size,Vector2 anchor,Vector2 position,out RectTransform rect)
    {
        rect=view.transform.Find(name) as RectTransform;
        if(rect==null){
            var image=ReaderView.Panel(name,view.transform,new Color(0,0,0,0.94f));image.raycastTarget=false;
            image.sprite=view.buttonSprite;image.type=UnityEngine.UI.Image.Type.Sliced;image.pixelsPerUnitMultiplier=0.5f;
            rect=(RectTransform)image.transform;
        }
        rect.anchorMin=rect.anchorMax=anchor;rect.pivot=new Vector2(0.5f,0.5f);rect.sizeDelta=size;rect.anchoredPosition=position;
        var group=Group(rect.gameObject);group.interactable=false;group.blocksRaycasts=false;return group;
    }
    private static MMF_Player Player(ReaderView view,string name,ReaderFeedbacks fx,bool complete)
    {
        var child=view.transform.Find(name);
        var obj=child!=null?child.gameObject:new GameObject(name);obj.transform.SetParent(view.transform,false);
        var player=obj.GetComponent<MMF_Player>();if(player==null)player=obj.AddComponent<MMF_Player>();
        player.InitializationMode=MMFeedbacks.InitializationModes.Start;
        player.AutoPlayOnEnable=player.AutoPlayOnStart=false;player.CanPlayWhileAlreadyPlaying=true;
        player.ForceTimescaleMode=true;player.ForcedTimescaleMode=TimescaleModes.Unscaled;player.PlayerTimescaleMode=TimescaleModes.Unscaled;
        if(player.FeedbacksList!=null&&player.FeedbacksList.Count>0)return player;
        float duration=complete?0.65f:0.28f;
        Scale(player,fx.readerBar.transform,complete?2.3f:1.65f,duration,true,"Pulso da barra de leitura");
        Scale(player,fx.libraryBar.transform,complete?2.3f:1.65f,duration,true,"Pulso da barra da biblioteca");
        Scale(player,view.readerMissionLabel.transform,complete?1.13f:1.06f,duration,false,"Pulso do contador de leitura");
        Scale(player,view.missionLabel.transform,complete?1.13f:1.06f,duration,false,"Pulso do contador diário");
        return player;
    }
    private static void Scale(MMF_Player player,Transform target,float peak,float duration,bool vertical,string label)
    {
        var feedback=(MMF_Scale)player.AddFeedback(typeof(MMF_Scale));
        feedback.Label=label;feedback.AnimateScaleTarget=target;feedback.Mode=MMF_Scale.Modes.Absolute;
        feedback.AnimateScaleDuration=duration;feedback.RemapCurveZero=1;feedback.RemapCurveOne=peak;
        feedback.AnimateX=!vertical;feedback.AnimateY=true;feedback.AnimateZ=false;feedback.UniformScaling=!vertical;
        feedback.AllowAdditivePlays=true;
        var curve=new AnimationCurve(new Keyframe(0,0),new Keyframe(0.28f,1),new Keyframe(0.62f,-0.12f),new Keyframe(1,0));
        feedback.AnimateScaleTweenX=new MMTweenType(curve);feedback.AnimateScaleTweenY=new MMTweenType(curve);feedback.AnimateScaleTweenZ=new MMTweenType(curve);
        feedback.Timing.TimescaleMode=TimescaleModes.Unscaled;
    }
    private static void ConfigurePostProcessing(ReaderView view)
    {
        var pipeline=GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if(pipeline==null)throw new InvalidOperationException("URP precisa estar ativo para configurar o bloom.");
        pipeline.supportsHDR=true;EditorUtility.SetDirty(pipeline);
        var camera=Camera.main;if(camera==null)throw new InvalidOperationException("Main Camera ausente.");
        camera.allowHDR=true;camera.cullingMask=1<<LayerMask.NameToLayer("UI");
        var data=camera.GetUniversalAdditionalCameraData();data.renderPostProcessing=true;data.volumeLayerMask=1;data.antialiasing=AntialiasingMode.None;
        var canvas=view.GetComponentInParent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        foreach(var transform in canvas.GetComponentsInChildren<Transform>(true))transform.gameObject.layer=LayerMask.NameToLayer("UI");
        const string path="Assets/Settings/ReaderPostProcessing.asset";
        var profile=AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if(profile==null){profile=ScriptableObject.CreateInstance<VolumeProfile>();AssetDatabase.CreateAsset(profile,path);}
        if(!profile.TryGet<Bloom>(out var bloom))bloom=profile.Add<Bloom>(true);
        bloom.active=true;bloom.threshold.Override(0.95f);bloom.intensity.Override(0.06f);bloom.scatter.Override(0.12f);
        bloom.downscale.Override(BloomDownscaleMode.Half);bloom.maxIterations.Override(2);bloom.highQualityFiltering.Override(false);
        if(!profile.TryGet<Tonemapping>(out var tone))tone=profile.Add<Tonemapping>(true);
        tone.active=true;tone.mode.Override(TonemappingMode.None);
        foreach(var effect in profile.components){effect.hideFlags=HideFlags.None;if(!AssetDatabase.Contains(effect))AssetDatabase.AddObjectToAsset(effect,profile);EditorUtility.SetDirty(effect);}
        var existing=UnityEngine.Object.FindObjectsByType<Volume>(FindObjectsInactive.Include,FindObjectsSortMode.None);
        Volume volume=null;foreach(var item in existing)if(item.name=="Reader Post Processing")volume=item;
        if(volume==null)volume=new GameObject("Reader Post Processing").AddComponent<Volume>();
        volume.gameObject.layer=0;volume.isGlobal=true;volume.weight=1;volume.priority=100;volume.sharedProfile=profile;
        var renderer=new SerializedObject(pipeline);var list=renderer.FindProperty("m_RendererDataList");
        for(int i=0;list!=null&&i<list.arraySize;i++){
            var asset=list.GetArrayElementAtIndex(i).objectReferenceValue;if(asset==null)continue;
            var settings=new SerializedObject(asset);var post=settings.FindProperty("m_PostProcessData")??settings.FindProperty("postProcessData");
            if(post!=null&&post.objectReferenceValue==null){var ids=AssetDatabase.FindAssets("t:PostProcessData");if(ids.Length==0)throw new InvalidOperationException("URP PostProcessData ausente.");post.objectReferenceValue=AssetDatabase.LoadAssetAtPath<PostProcessData>(AssetDatabase.GUIDToAssetPath(ids[0]));settings.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(asset);}
        }
        EditorUtility.SetDirty(profile);EditorUtility.SetDirty(volume);EditorUtility.SetDirty(camera);EditorUtility.SetDirty(data);EditorUtility.SetDirty(canvas);
    }
}