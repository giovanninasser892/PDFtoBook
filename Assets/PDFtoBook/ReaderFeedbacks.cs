using DG.Tweening;
using MoreMountains.Feedbacks;
using MoreMountains.Tools;
using TMPro;
using UnityEngine;

namespace PDFtoBook
{
    /// <summary>Presentation only: counters, saves and completion decisions belong to ReaderApp.</summary>
    public sealed class ReaderFeedbacks : MonoBehaviour
    {
        public ReaderView view;
        public CanvasGroup libraryGroup,readerGroup,gainGroup,completionGroup;
        public GameObject transitionBlocker;
        public MMProgressBar libraryBar,readerBar;
        public MMF_Player incrementFeedbacks,completionFeedbacks;
        public TextMeshProUGUI gainLabel;
        public RectTransform completionRect;
        [Header("DOTween motion")]
        public float transitionDuration=0.32f;
        public float gainDuration=0.9f;
        public Ease popupEase=Ease.OutBack;
        private Sequence screenAnimation,gainAnimation,completionAnimation;
        private Vector2 libraryPosition,readerPosition;
        private bool initialized,screenInitialized,reading,hasMission;
        private int burstCount,lastPages,lastTarget;
        private string lastDay;
        private float lastGainTime=-10,strongFeedbackUntil;
        public bool IsTransitioning { get; private set; }

        private void Initialize()
        {
            if(initialized)return;
            libraryPosition=((RectTransform)view.libraryPanel.transform).anchoredPosition;
            readerPosition=((RectTransform)view.readerPanel.transform).anchoredPosition;
            gainGroup.alpha=completionGroup.alpha=0;
            gainGroup.blocksRaycasts=completionGroup.blocksRaycasts=false;
            transitionBlocker.SetActive(false);
            initialized=true;
        }
        public void ShowScreen(bool showReader)
        {
            Initialize();
            bool animate=Application.isPlaying&&screenInitialized&&reading!=showReader;
            screenInitialized=true;reading=showReader;
            screenAnimation?.Kill();
            RestoreScreen(libraryGroup,libraryPosition);RestoreScreen(readerGroup,readerPosition);
            view.libraryPanel.SetActive(!showReader);view.readerPanel.SetActive(showReader);
            if(showReader){view.topBar.SetActive(true);view.bottomBar.SetActive(true);}
            else HideTransient();
            SyncVisibleBars();
            var target=showReader?readerGroup:libraryGroup;
            var rect=(RectTransform)target.transform;
            var rest=showReader?readerPosition:libraryPosition;
            IsTransitioning=animate;
            transitionBlocker.SetActive(animate);
            if(!animate)return;
            transitionBlocker.transform.SetAsLastSibling();
            target.alpha=0;target.interactable=false;target.blocksRaycasts=false;
            rect.anchoredPosition=rest+new Vector2(showReader?42:-42,0);
            screenAnimation=DOTween.Sequence().SetUpdate(true)
                .Append(target.DOFade(1,transitionDuration*0.8f).SetEase(Ease.OutQuad))
                .Join(rect.DOAnchorPos(rest,transitionDuration).SetEase(Ease.OutBack,1.1f))
                .OnComplete(()=>{RestoreScreen(target,rest);IsTransitioning=false;transitionBlocker.SetActive(false);});
        }
        private static void RestoreScreen(CanvasGroup group,Vector2 position)
        {
            group.alpha=1;group.interactable=true;group.blocksRaycasts=true;
            ((RectTransform)group.transform).anchoredPosition=position;
        }
        public void SetMission(DailyMission mission,bool animate)
        {
            Initialize();
            if(hasMission&&mission.day==lastDay&&mission.pages==lastPages&&mission.target==lastTarget)return;
            bool reset=hasMission&&(mission.day!=lastDay||mission.pages<lastPages);
            if(reset)HideTransient();
            float fraction=mission.target>0?Mathf.Clamp01((float)mission.pages/mission.target):0;
            SetBar(libraryBar,fraction,animate&&!reset);
            SetBar(readerBar,fraction,animate&&!reset);
            lastDay=mission.day;lastPages=mission.pages;lastTarget=mission.target;hasMission=true;
        }
        private static void SetBar(MMProgressBar bar,float fraction,bool animate)
        {
            if(animate&&Application.isPlaying&&bar.gameObject.activeInHierarchy)bar.UpdateBar01(fraction);
            else bar.SetBar01(fraction);
        }
        public void SyncVisibleBars()
        {
            if(!hasMission)return;
            float fraction=lastTarget>0?Mathf.Clamp01((float)lastPages/lastTarget):0;
            if(libraryBar.gameObject.activeInHierarchy)libraryBar.SetBar01(fraction);
            if(readerBar.gameObject.activeInHierarchy)readerBar.SetBar01(fraction);
        }
        public void PageRead(bool completed)
        {
            if(!Application.isPlaying)return;
            Initialize();
            ShowGain();
            if(completed){
                incrementFeedbacks.StopFeedbacks();completionFeedbacks.StopFeedbacks();ResetFeedbackScales();
                completionFeedbacks.PlayFeedbacks();strongFeedbackUntil=Time.unscaledTime+0.7f;
                ShowCompletion();
            }else if(Time.unscaledTime>=strongFeedbackUntil){
                incrementFeedbacks.StopFeedbacks();ResetFeedbackScales();incrementFeedbacks.PlayFeedbacks();
            }
        }
        private void ShowGain()
        {
            burstCount=Time.unscaledTime-lastGainTime<0.45f?burstCount+1:1;lastGainTime=Time.unscaledTime;
            gainLabel.text="+"+burstCount;
            gainAnimation?.Kill();
            var rect=(RectTransform)gainGroup.transform;
            rect.anchoredPosition=new Vector2(0,260);rect.localScale=Vector3.one*0.45f;gainGroup.alpha=1;
            gainAnimation=DOTween.Sequence().SetUpdate(true)
                .Append(rect.DOScale(1,0.28f).SetEase(popupEase))
                .Join(rect.DOAnchorPosY(326,gainDuration).SetEase(Ease.OutQuad))
                .Insert(gainDuration-0.28f,gainGroup.DOFade(0,0.28f).SetEase(Ease.InQuad))
                .OnComplete(()=>gainGroup.alpha=0);
        }
        private void ShowCompletion()
        {
            completionAnimation?.Kill();
            completionRect.anchoredPosition=new Vector2(0,60);completionRect.localScale=Vector3.one*0.45f;completionGroup.alpha=0;
            completionAnimation=DOTween.Sequence().SetUpdate(true)
                .Append(completionGroup.DOFade(1,0.16f))
                .Join(completionRect.DOScale(1,0.55f).SetEase(popupEase,2.2f))
                .Join(completionRect.DOAnchorPosY(90,0.55f).SetEase(Ease.OutCubic))
                .AppendInterval(1.2f)
                .Append(completionGroup.DOFade(0,0.35f))
                .Join(completionRect.DOAnchorPosY(120,0.35f).SetEase(Ease.InQuad))
                .OnComplete(()=>completionGroup.alpha=0);
        }
        private void ResetFeedbackScales()
        {
            libraryBar.transform.localScale=readerBar.transform.localScale=Vector3.one;
            view.missionLabel.transform.localScale=view.readerMissionLabel.transform.localScale=Vector3.one;
        }
        private void HideTransient()
        {
            gainAnimation?.Kill();completionAnimation?.Kill();
            gainGroup.alpha=completionGroup.alpha=0;burstCount=0;lastGainTime=-10;
            if(Application.isPlaying){incrementFeedbacks.StopFeedbacks();completionFeedbacks.StopFeedbacks();ResetFeedbackScales();}
        }
        private void OnDisable()
        {
            if(!initialized)return;
            screenAnimation?.Kill();HideTransient();IsTransitioning=false;
            transitionBlocker.SetActive(false);RestoreScreen(libraryGroup,libraryPosition);RestoreScreen(readerGroup,readerPosition);
        }
    }
}