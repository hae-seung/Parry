using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Parry.AnimationClass
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Animator))]
    public sealed class AnimationClipPlayer : MonoBehaviour
    {
        private Animator animator;
        private PlayableGraph animationGraph;
        private AnimationPlayableOutput output;
        private AnimationClipPlayable clipPlayable;
        private bool holdLastFrame;

        public bool CanPlay =>
            isActiveAndEnabled && animator != null && animator.isActiveAndEnabled;
        public bool IsPlaying => clipPlayable.IsValid();

        public bool IsPlayingClip(AnimationClip clip) =>
            IsPlaying && clipPlayable.GetAnimationClip() == clip;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            animationGraph = PlayableGraph.Create(name + " Animation");
            animationGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            output = AnimationPlayableOutput.Create(animationGraph, "Animation", null);
        }

        private void Update()
        {
            if (IsPlaying && !clipPlayable.GetAnimationClip().isLooping &&
                clipPlayable.GetTime() >= clipPlayable.GetAnimationClip().length)
            {
                if (holdLastFrame)
                {
                    clipPlayable.SetTime(clipPlayable.GetAnimationClip().length);
                    clipPlayable.SetSpeed(0);
                }
                else
                {
                    Stop();
                }
            }
        }

        private void OnDisable() => Stop();

        private void OnDestroy()
        {
            if (animationGraph.IsValid())
                animationGraph.Destroy();
        }

        public bool Play(AnimationClip clip, bool holdLastFrame = false)
        {
            if (!CanPlay || clip == null)
                return false;

            AnimationClipPlayable previousClip = clipPlayable;
            this.holdLastFrame = holdLastFrame;
            
            clipPlayable = AnimationClipPlayable.Create(animationGraph, clip);
            clipPlayable.SetTime(0);
            
            output.SetSourcePlayable(clipPlayable);
            output.SetTarget(animator);
            
            if (previousClip.IsValid())
                animationGraph.DestroyPlayable(previousClip);
            
            animationGraph.Play();
            
            return true;
        }

        public void Stop()
        {
            if (!animationGraph.IsValid())
                return;

            //output이 animator 소유 해제
            output.SetTarget(null);
            output.SetSourcePlayable(Playable.Null);
            animationGraph.Stop();
            
            if (clipPlayable.IsValid())
                animationGraph.DestroyPlayable(clipPlayable);
            
            clipPlayable = default;
        }

    }
}
