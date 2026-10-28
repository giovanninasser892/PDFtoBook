using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PDFtoBook
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class ReaderButtonFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler, ISubmitHandler
    {
        [Range(0.8f,1f)] public float pressedScale=0.94f;
        public float pressDuration=0.07f;
        public float releaseDuration=0.26f;
        private UnityEngine.UI.Button button;
        private Vector3 restScale;
        private Tween animation;
        private bool pressed,ready;

        private void Awake(){button=GetComponent<UnityEngine.UI.Button>();restScale=transform.localScale;ready=true;}
        public void OnPointerDown(PointerEventData e)
        {
            if(e.button!=PointerEventData.InputButton.Left||!button.IsActive()||!button.IsInteractable())return;
            pressed=true;animation?.Kill();
            animation=transform.DOScale(restScale*pressedScale,pressDuration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
        public void OnPointerUp(PointerEventData e){if(e.button==PointerEventData.InputButton.Left)Release();}
        public void OnPointerExit(PointerEventData e){Release();}
        public void OnSubmit(BaseEventData e)
        {
            if(!button.IsActive()||!button.IsInteractable())return;
            animation?.Kill();transform.localScale=restScale*pressedScale;pressed=true;Release();
        }
        private void Release()
        {
            if(!pressed)return;
            pressed=false;animation?.Kill();
            animation=transform.DOScale(restScale,releaseDuration).SetEase(Ease.OutBack,2.2f).SetUpdate(true);
        }
        private void OnDisable(){if(!ready)return;animation?.Kill();pressed=false;transform.localScale=restScale;}
        private void OnDestroy(){animation?.Kill();}
    }
}