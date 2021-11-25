using System.Collections;
using UnityEngine;

public class NextInLine : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;
   public KMColorblindMode Colorblind;
   public TextMesh CBText;
   public GameObject[] Hatch;
   public KMSelectable Button;
   public KMSelectable WireSelectable;
   public Material[] Colors;
   public GameObject[] Wire;
   public GameObject[] LEDsGame;

   static int moduleIdCounter = 1;
   int moduleId;
   private bool moduleSolved;

   int[][] ColorOrder = new int[8][] { //ROYGBKWA
        //          0  1  2  3  4  5  6  7
		    new int[7] {4, 2, 1, 3, 6, 5, 7}, // R
        new int[7] {7, 0, 3, 6, 5, 2, 4}, // O
        new int[7] {1, 7, 4, 5, 3, 6, 0}, // Y
        new int[7] {2, 4, 5, 7, 1, 0, 6}, // G
        new int[7] {0, 1, 6, 2, 7, 3, 5}, // B
        new int[7] {3, 6, 7, 1, 0, 4, 2}, // K
        new int[7] {5, 3, 2, 0, 4, 7, 1}, // W
        new int[7] {6, 5, 0, 4, 2, 1, 3}  // A
    };
   int[] StreakNumbers = { 37, 45, 52, 60, 67, 75, 82, 98 };
   int CurrentColor;
   int PreviousColor;
   int Iteration = -1;
   int Streak;

   string[] ColorNames = { "red", "orange", "yellow", "green", "blue", "black", "white", "gray" };
   string[] CBColorNames = { "R", "O", "Y", "G", "B", "K", "W", "A" };

   bool Active;
   bool Open;
   bool ILikeYaCutG;
   bool Wrong;
   bool Animating = true;
   bool OneTime;
   bool CB;

   void Awake () {
      for (int i = 0; i < LEDsGame.Length; i++)
         LEDsGame[i].SetActive(false);
      Wire[1].gameObject.SetActive(false);
      CB = Colorblind.ColorblindModeActive;
      moduleId = moduleIdCounter++;
      Button.OnInteract += delegate () { ButtonPress(); return false; };
      WireSelectable.OnInteract += delegate () { WireCut(); return false; };
      GetComponent<KMBombModule>().OnActivate += OnActivate;
   }

   #region Calculations

   void Start () {
      CurrentColor = Random.Range(0, 8);
      WireColorSetter();
      Debug.LogFormat("[Next In Line #{0}] At iteration {1}, the color is {2}. This is the first stage though. Cut it.", moduleId, Iteration + 2, ColorNames[CurrentColor]);
      PreviousColor = CurrentColor;
   }

   void OnActivate () {
      StartCoroutine(HatchAnimation());
      Active = true;
   }

   void WireCut () {
      if (Animating || moduleSolved)
         return;
      ILikeYaCutG = true;
      Streak = 0;
      Wire[0].gameObject.SetActive(false);
      Wire[1].gameObject.SetActive(true);
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSnip, Wire[0].transform);
   }

   void WireColorSetter () {
      if (Iteration == -1)
         goto FirstStageSkip;
      if (ILikeYaCutG)
         PreviousColor = CurrentColor;
      CurrentColor = Random.Range(0, 100) <= StreakNumbers[Streak] ? ColorOrder[PreviousColor][Iteration] : Random.Range(0, 8);
      FirstStageSkip:
      Wire[0].GetComponent<MeshRenderer>().material = Colors[CurrentColor];
      Wire[1].GetComponent<MeshRenderer>().material = Colors[CurrentColor];
      CBText.text = CB ? CBColorNames[CurrentColor] : "";
   }

   IEnumerator StagePass () {
      if (Iteration == 6) {
         GetComponent<KMBombModule>().HandlePass();
         moduleSolved = true;
         Active = false;
         yield return null;
      }
      if (Wrong) {
         GetComponent<KMBombModule>().HandleStrike();
         Iteration = 0;
      }
      else {
         Iteration++;
      }
      StartCoroutine(HatchAnimation());
      float t = 0f;
      while (t < 1f) {
         yield return null;
         t += Time.deltaTime;
      }
      t = 0f;
      if (!moduleSolved) {
         WireColorSetter();
         Wire[0].gameObject.SetActive(true);
         Wire[1].gameObject.SetActive(false);
         StartCoroutine(HatchAnimation());
         Debug.LogFormat("[Next In Line #{0}] At iteration {1}, the color is {2}.", moduleId, Iteration + 2, ColorNames[CurrentColor]);
         if (ColorOrder[PreviousColor][Iteration] == CurrentColor)
            Debug.LogFormat("[Next In Line #{0}] Cut it.", moduleId);
         else
            Debug.LogFormat("[Next In Line #{0}] Don't cut it.", moduleId);
         while (t < 1f) {
            yield return null;
            t += Time.deltaTime;
         }
      }
      if (!ILikeYaCutG && !Wrong) {
         Streak++;
         Streak %= 8; //In case some mumbo jumbo shit happens
      }
      Debug.LogFormat("<Next In Line #{0}> Odds of cutting are {1}%.", moduleId, StreakNumbers[Streak]);
      Wrong = false;
      ILikeYaCutG = false;
      Animating = false;
   }

   void ButtonPress () {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Button.transform);
      if (Animating || moduleSolved)
         return;
      if (Iteration == -1 && !ILikeYaCutG) {
         GetComponent<KMBombModule>().HandleStrike();
         return;
      }
      Animating = true;
      if (Iteration == -1 && ILikeYaCutG)
         StartCoroutine(StagePass());
      else if (ColorOrder[PreviousColor][Iteration] == CurrentColor && ILikeYaCutG)
         StartCoroutine(StagePass());
      else if (ColorOrder[PreviousColor][Iteration] != CurrentColor && !ILikeYaCutG)
         StartCoroutine(StagePass());
      else {
         Wrong = true;
         StartCoroutine(StagePass());
      }
   }

   #endregion

   #region Animations/Design

   IEnumerator HatchAnimation () {
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.WireSequenceMechanism, transform);
      float t = 0f;
      for (int j = 0; j < 50; j++) {
         if (Open) {
            Hatch[0].transform.localPosition -= new Vector3(0, 0.000592f, 0);
            Hatch[1].transform.localPosition += new Vector3(0, 0, 0.00088f);
         }
         else {
            Hatch[0].transform.localPosition += new Vector3(0, 0.000592f, 0);
            Hatch[1].transform.localPosition -= new Vector3(0, 0, 0.00088f);
         }
         while (t < 0.01f) {
            yield return null;
            t += Time.deltaTime;
         }
         t = 0f;
      }
      if (Open) {
         Hatch[0].transform.localPosition = new Vector3(0, -0.0198f, 0);
         Hatch[1].transform.localPosition = new Vector3(0, 0.0042f, 0);
      }
      else {
         Hatch[0].transform.localPosition = new Vector3(0, 0.0098f, 0);
         Hatch[1].transform.localPosition = new Vector3(0, 0.0042f, -0.044f);
      }
      Open = !Open;
      if (!OneTime)
      {
         OneTime = true;
         Animating = false;
      }
   }

   bool[] ShowingSegments (int Input) {
      bool[] Output = new bool[7];
      switch (Input) {    //tm tl    tr     mm     bl    br    bm
         case 0: Output = new bool[] { true, true, true, false, true, true, true }; break;
         case 1: Output = new bool[] { false, false, true, false, false, true, false }; break;
         case 2: Output = new bool[] { true, false, true, true, true, false, true }; break;
         case 3: Output = new bool[] { true, false, true, true, false, true, true }; break;
         case 4: Output = new bool[] { false, true, true, true, false, true, false }; break;
         case 5: Output = new bool[] { true, true, false, true, false, true, true }; break;
         case 6: Output = new bool[] { true, true, false, true, true, true, true }; break;
         case 7: Output = new bool[] { true, false, true, false, false, true, false }; break;
         case 8: Output = new bool[] { true, true, true, true, true, true, true }; break;
         case 9: Output = new bool[] { true, true, true, true, false, true, true }; break;
      }
      return Output;
   }

   void Update () {
      if (Active) {
         for (int i = 0; i < 7; i++) {
            LEDsGame[i].gameObject.SetActive(ShowingSegments(Iteration + 2)[i]);
         }
      }
      else if (moduleSolved) {
         for (int i = 0; i < LEDsGame.Length; i++) {
            LEDsGame[i].SetActive(false);
         }
      }
   }

   #endregion

   #region TP

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} Cut to cut the wire. Use !{0} Next to press the button. Use !{0} Colorblind to toggle colorblind mode.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      yield return null;
      Command = Command.ToUpper();
      if (Command == "CUT") {
         WireSelectable.OnInteract();
      }
      else if (Command == "NEXT") {
         Button.OnInteract();
      }
      else if (Command == "COLORBLIND") {
         CB = !CB;
         CBText.text = CB ? CBColorNames[CurrentColor] : "";
      }
      else {
         yield return "sendtochaterror I don't understand!";
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      while (!moduleSolved) {
         while (Animating) yield return true;
         if (Iteration == -1 || ColorOrder[PreviousColor][Iteration] == CurrentColor) {
            yield return ProcessTwitchCommand("Cut");
            yield return new WaitForSecondsRealtime(0.1f);
         }
         yield return ProcessTwitchCommand("Next");
      }
   }

   #endregion
}