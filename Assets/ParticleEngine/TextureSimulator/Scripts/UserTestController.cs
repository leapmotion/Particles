using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;
using Leap.Unity.Animation;
using Leap.Unity.Interaction;
using Leap.Unity.GraphicalRenderer;

public class UserTestController : MonoBehaviour {

  public TextureSimulator sim;
  public InteractionButton nextButton;
  public LeapTextGraphic textLabel;
  public StreamingFolder textFolder;
  public StreamingFolder ecosystemFolder;
  public StreamingFolder dataFolder;
  public StreamingFolder musicFolder;

  [Header("Audio Settings")]
  public AudioSource mainAudioSource;
  public AudioSource secondAudioSource;
  public float transitionTime = 1;

  private List<string> _ecosystemPaths;
  private List<string> _scriptPaths = new List<string>();
  private LoadData _currLoadData;
  private int _currEcosystem = -1;
  private int _currScript = 0;

  private Tween _audioTween;
  private string _currAudioPath;

  void Start() {
    _ecosystemPaths = Directory.GetFiles(ecosystemFolder.Path).
                                Where(p => p.EndsWith(".json")).
                                OrderBy(p => p).
                                ToList();

    sim.OnEcosystemMidTransition += onSimulationTransitionMid;
  }

  void Update() {
    if (Input.GetKeyDown(KeyCode.RightArrow)) {
      OnNext();
    }

    if (Input.GetKeyDown(KeyCode.LeftArrow)) {
      OnPrev();
    }

    if (Input.GetKeyDown(KeyCode.DownArrow)) {
      OnRestart();
    }
  }

  [ContextMenu("Generate Example Json")]
  private void generateExampleJson() {
    File.WriteAllText("Example.json", JsonUtility.ToJson(new LoadData(), prettyPrint: true));
  }

  public void OnNext() {
    if (_currScript == _scriptPaths.Count - 1 && _currEcosystem == _ecosystemPaths.Count - 1) {
      return;
    }

    _currScript++;
    if (_currScript >= _scriptPaths.Count) {
      _currEcosystem++;
      _currScript = 0;

      loadScripts();
      transition(forceReset: false);
    }

    updateText();
  }

  public void OnPrev() {
    if (_currEcosystem <= 0 && _currScript <= 0) {
      return;
    }

    _currScript--;
    if (_currScript < 0) {
      _currEcosystem--;
      loadScripts();
      _currScript = _scriptPaths.Count - 1;

      transition(forceReset: true);
    }

    updateText();
  }

  public void OnRestart() {
    //No reset before we started!
    if (_currEcosystem < 0) return;

    _currScript = 0;
    transition(forceReset: true);
    updateText();
  }

  private void loadScripts() {
    string currEcosystemPath = _ecosystemPaths[_currEcosystem];
    string ecosystemName = Path.GetFileNameWithoutExtension(currEcosystemPath);
    _scriptPaths = Directory.GetFiles(textFolder.Path).
                             Where(p => p.Contains(ecosystemName)).
                             Where(p => p.EndsWith(".txt")).
                             OrderBy(p => p.Replace(ecosystemName, "")).
                             ToList();
  }

  private void transition(bool forceReset = false) {
    string dataPath = Path.Combine(dataFolder.Path, Path.GetFileName(_ecosystemPaths[_currEcosystem]));
    _currLoadData = JsonUtility.FromJson<LoadData>(File.ReadAllText(dataPath));

    var desc = JsonUtility.FromJson<TextureSimulator.SimulationDescription>(File.ReadAllText(_ecosystemPaths[_currEcosystem]));
    if (forceReset) {
      sim.RestartSimulation(desc, TextureSimulator.ResetBehavior.ResetPositions);
    } else {
      sim.RestartSimulation(desc, _currLoadData.transitionBehavior);
    }

    sim.handCollisionEnabled = _currLoadData.collisionEnabled;
    sim.handInfluenceEnabled = _currLoadData.graspingEnabled;

    StartCoroutine(loadAudioCoroutine());
  }

  private IEnumerator loadAudioCoroutine() {
    if (_currLoadData.backgroundMusic == _currAudioPath) {
      Debug.Log("Did not load audio because it was the same.");
      yield break;
    }

    _currAudioPath = _currLoadData.backgroundMusic;

    string path = Path.Combine(musicFolder.Path, _currLoadData.backgroundMusic);
    Debug.Log("Loading audio from: " + path);

    WWW www = new WWW("file://" + path);
    yield return www;

    if (www.isDone && string.IsNullOrEmpty(www.error)) {
      secondAudioSource.clip = www.GetAudioClip(threeD: false, stream: false);
      secondAudioSource.volume = 0;
      secondAudioSource.Play();
      secondAudioSource.loop = true;
      Debug.Log("Loaded audio!");
    } else {
      secondAudioSource.clip = null;
      Debug.Log("Failed to load audio: " + www.error);
    }

    www.Dispose();

    if (_audioTween.isValid) {
      _audioTween.Stop();
    }

    _audioTween = Tween.Single().
                        Value(1, 0, t => mainAudioSource.volume = t).
                        Value(0, 1, t => secondAudioSource.volume = t).
                        OverTime(transitionTime).
                        OnReachEnd(() => {
                          mainAudioSource.Stop();
                          Utils.Swap(ref mainAudioSource, ref secondAudioSource);
                        }).
                        Play();
  }

  private void updateText() {
    textLabel.text = File.ReadAllText(_scriptPaths[_currScript]);
  }

  private void onSimulationTransitionMid() {
    sim.transform.localScale = Vector3.one * _currLoadData.simulationScale;
    sim.colorMode = _currLoadData.colorMode;
  }

  public class LoadData {
    public TextureSimulator.ResetBehavior transitionBehavior = TextureSimulator.ResetBehavior.FadeInOut;
    public TextureSimulator.ColorMode colorMode = TextureSimulator.ColorMode.BySpecies;
    public float simulationScale = 1;
    public float timeScale = 1;
    public string backgroundMusic = "";
    public bool graspingEnabled = true;
    public bool collisionEnabled = true;
  }

}
