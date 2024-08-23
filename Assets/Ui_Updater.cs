using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class Ui_Updater : MonoBehaviour
{
    public static Ui_Updater inst;

    // Ref
    private GameObject UIcanvas;
    private TMP_Text[] saveInfoText; // 0:StackCount / 1:SaveTimer / 2:SaveCompleteCounter / 3: Current AutoSave Mode Stats / 4: FPS
    private Button[] Btns;

    // 슬라이더
    private Slider autoSaveIntervalSlider;
    private float sliderValue;
    private TMP_Text sliderValueText;

    int saveCount;
    public int SaveCount
    {
        get { return saveCount; }
        set
        {
            saveCount = value;
            saveInfoText[2].text = $"Save Complete Count : <color=yellow><b>{saveCount}";
        }
    }

    bool isAutoSave;
    public bool IsAutoSave
    {
        get { return isAutoSave; }
        set
        {
            isAutoSave = value;
            Btns[0].interactable = !value;
            saveInfoText[3].text = $"Auto Save Mode : <color={(value ? "blue" : "red")}><b>{value}";
        }
    }

    private void Awake()
    {
        if (inst == null)
        {
            inst = this;
        }
        else
        {
            Destroy(this);
        }

        UIcanvas = GameObject.Find("=== [ Canvas ]/Canvas").gameObject;

        // 1. Ui Info Text Init
        saveInfoText = new TMP_Text[UIcanvas.transform.Find("SaveVerticalLayOut").childCount];
        for (int index = 0; index < saveInfoText.Length; index++)
        {
            saveInfoText[index] = UIcanvas.transform.Find("SaveVerticalLayOut").GetChild(index).GetComponent<TMP_Text>();
        }

        // 2. Btn Init
        Btns = new Button[UIcanvas.transform.Find("Btns").childCount];
        for (int index = 0; index < Btns.Length; index++)
        {
            Btns[index] = UIcanvas.transform.Find("Btns").GetChild(index).GetComponent<Button>();
            switch (index)
            {
                case 0: // Save 버튼
                    Btns[index].onClick.AddListener(() => { SaveLoadUility.inst.SaveData(saveCount.ToString()); });
                    break;

                case 1:  // Auto Save 버튼
                    Btns[index].onClick.AddListener(() => { IsAutoSave = !IsAutoSave; });
                    break;

                case 2: // Load 버튼
                    Btns[index].onClick.AddListener(() => { SaveLoadUility.inst.LoadDate(); });
                    break;
            }
        }

        // 3. Slider 
        autoSaveIntervalSlider = UIcanvas.transform.Find("AutoSaveSlider/Slider").GetComponent<Slider>();
        sliderValueText = UIcanvas.transform.Find("AutoSaveSlider/ValueText").GetComponent<TMP_Text>();
        sliderValue = autoSaveIntervalSlider.value;

        autoSaveIntervalSlider.onValueChanged.AddListener((value) =>
        {
            sliderValue = value;
            sliderValueText.text = $"{sliderValue.ToString("F1")} Sec";
        });

        IsAutoSave = false;
        SaveCount = 0;

    }
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UI_Text_Updater();
    }



    public float currentFps;
    float fpsUpdaterTimer;
    [SerializeField, Range(0f, 0.5f)] float FPSInterval;

    private void UI_Text_Updater()
    {
        fpsUpdaterTimer += Time.deltaTime;

        //Container Counter
        saveInfoText[0].text = $"Current Save Contatiner Counter : {SaveLoadUility.inst.saveDataPipeline.Count}";

        // Save Timer
        saveInfoText[1].text = $"Save Timer : <b>{SaveLoadUility.inst.saveTimer.ToString("F1")} Sec";

        // FPS Text
        if (fpsUpdaterTimer > FPSInterval)
        {
            fpsUpdaterTimer = 0;
            currentFps += (Time.deltaTime - currentFps) * 0.1f;
            float curFPS = 1f / currentFps;
            saveInfoText[4].text = $"FPS : <color=yellow><b> {curFPS.ToString("F1")}";
        }
    }
}
