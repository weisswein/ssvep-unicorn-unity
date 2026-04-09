using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SsvepExperimentController : MonoBehaviour
{
    [System.Serializable]
    public class SsvepClass
    {
        public int classId;
        public float frequencyHz;
        public string label;
    }

    public UnicornTriggerSender triggerSender;
    public SsvepEventLogger eventLogger;

    public List<SsvepClass> classes = new List<SsvepClass>()
    {
        new SsvepClass(){ classId = 1, frequencyHz = 8.57f, label = "Left" },
        new SsvepClass(){ classId = 2, frequencyHz = 10.0f, label = "Right" },
        new SsvepClass(){ classId = 3, frequencyHz = 12.0f, label = "Up" },
        new SsvepClass(){ classId = 4, frequencyHz = 15.0f, label = "Down" },
    };

    public int repeatsPerClass = 5;
    public float preStartDelay = 3f;
    public float trialDuration = 3f;
    public float restDuration = 1f;

    private List<SsvepClass> trialSequence = new List<SsvepClass>();

    private IEnumerator Start()
    {
        BuildTrialSequence();

        Debug.Log("[Experiment] Start in 3 sec...");
        yield return new WaitForSeconds(preStartDelay);

        for (int i = 0; i < trialSequence.Count; i++)
        {
            int trialId = i + 1;
            SsvepClass c = trialSequence[i];

            // ここで対応する刺激オブジェクトを点滅開始
            StartStimulus(c);

            eventLogger.LogEvent(trialId, "start", c.classId, c.frequencyHz);
            triggerSender.SendTrigger(c.classId);

            Debug.Log($"[Trial {trialId}] START class={c.classId}, freq={c.frequencyHz}");
            yield return new WaitForSeconds(trialDuration);

            StopStimulus(c);

            eventLogger.LogEvent(trialId, "end", c.classId, c.frequencyHz);
            triggerSender.SendTrigger(100 + c.classId);

            Debug.Log($"[Trial {trialId}] END");
            yield return new WaitForSeconds(restDuration);
        }

        Debug.Log("[Experiment] Finished.");
    }

    private void BuildTrialSequence()
    {
        trialSequence.Clear();
        foreach (var c in classes)
        {
            for (int i = 0; i < repeatsPerClass; i++)
                trialSequence.Add(c);
        }

        // 簡易シャッフル
        for (int i = 0; i < trialSequence.Count; i++)
        {
            int j = Random.Range(i, trialSequence.Count);
            var tmp = trialSequence[i];
            trialSequence[i] = trialSequence[j];
            trialSequence[j] = tmp;
        }
    }

    private void StartStimulus(SsvepClass c)
    {
        // TODO:
        // Unityの点滅オブジェクト/Shader/UIに接続
        // ここではフックだけ置いている
    }

    private void StopStimulus(SsvepClass c)
    {
        // TODO:
        // 点滅停止処理
    }
}