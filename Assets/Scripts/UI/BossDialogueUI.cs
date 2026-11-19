using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Parry.BossClass;
using TMPro;
using UnityEngine;

namespace Parry.UIClass
{
    public sealed class BossDialogueUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text dialogueText;
        private readonly StringBuilder builder = new StringBuilder();
        private Coroutine dialogueCoroutine;

        public event Action OnDialogueCompleted;

        private void Awake() => Stop();

        public void Show(List<BossDialogueData> dialogues)
        {
            Stop();

            if (dialogues.Count == 0)
            {
                OnDialogueCompleted?.Invoke();
                return;
            }
            
            dialogueCoroutine = StartCoroutine(DialogueRoutine(dialogues));
        }

        private IEnumerator DialogueRoutine(List<BossDialogueData> dialogues)
        {
            var characterWait = new WaitForSecondsRealtime(0.1f);
            dialogueText.gameObject.SetActive(true);

            foreach (BossDialogueData dialogue in dialogues)
            {
                builder.Clear();
                dialogueText.text = string.Empty;
                string text = dialogue.text ?? string.Empty;

                for (int i = 0; i < text.Length; i++)
                {
                    if (i > 0)
                        yield return characterWait;

                    builder.Append(text[i]);
                    dialogueText.SetText(builder);
                }

                yield return new WaitForSecondsRealtime(dialogue.duration);
            }

            dialogueCoroutine = null;
            dialogueText.gameObject.SetActive(false);
            OnDialogueCompleted?.Invoke();
        }

        public void Stop()
        {
            if (dialogueCoroutine != null)
            {
                StopCoroutine(dialogueCoroutine);
                dialogueCoroutine = null;
            }

            builder.Clear();
            dialogueText.text = string.Empty;
            dialogueText.gameObject.SetActive(false);
        }

        private void OnDisable() => Stop();
    }
}
