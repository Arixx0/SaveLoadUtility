using System;
using System.Collections;
using System.Text;
using TMPro;
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
    // Save  
    private Slider autoSaveIntervalSlider;
    private float savesliderValue;
    private TMP_Text savesliderValueText;

    // Load
    private Slider autoLoadIntervalSlider;
    private float loadSliderValue;
    private TMP_Text loadSliderValueText;

    /*private float sliderValue*/
    //private TMP_Text sliderValueText;


    // LoadData Viewr
    private TMP_Text inputKeyText;
    private TMP_Text loaddataText;
    private TMP_Text loadStateText;
    private TMP_Text glovalText;
    int saveCount;
    public int SaveCount
    {
        get { return saveCount; }
        set
        {
            saveCount = value;
            saveInfoText[1].text = $"Save Complete Count : <color=yellow><b>{saveCount}";
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
            saveInfoText[2].text = $"Auto Save Mode : <color={(value ? "blue" : "red")}><b>{value}";
        }
    }

    bool isAutoLoad;
    public bool IsAutoLoad
    {
        get { return isAutoLoad; }
        set
        {
            isAutoLoad = value;
            Btns[2].interactable = !value;
            saveInfoText[3].text = $"Auto Load Mode : <color={(value ? "blue" : "red")}><b>{value}";
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

        inputKeyText = UIcanvas.transform.Find("CompleteText").GetComponent<TMP_Text>();
        loaddataText = UIcanvas.transform.Find("LoadTextBOX/Text").GetComponent<TMP_Text>();
        loaddataText.text = string.Empty;

        loadStateText = UIcanvas.transform.Find("LoadTextBOX/Stats").GetComponent<TMP_Text>();
        glovalText = UIcanvas.transform.Find("GlobalValue").GetComponent<TMP_Text>();
   


        // 2. Btn Init
        Btns = new Button[UIcanvas.transform.Find("Btns").childCount];
        for (int index = 0; index < Btns.Length; index++)
        {
            Btns[index] = UIcanvas.transform.Find("Btns").GetChild(index).GetComponent<Button>();
            switch (index)
            {
                case 0: // Save 버튼
                    Btns[index].onClick.AddListener(() =>
                    {
                        string saveValue = MakeSampleSavaDataValue();
                        SaveLoadUtility.inst.SaveData(saveValue);
                    });
                    break;

                case 1:  // Auto Save 버튼
                    Btns[index].onClick.AddListener(() => { IsAutoSave = !IsAutoSave; });
                    break;

                case 2: // Load 버튼
                    Btns[index].onClick.AddListener(() => { SaveLoadUtility.inst.LoadData(); });
                    break;

                case 3: // AutoLoad 버튼
                    Btns[index].onClick.AddListener(() => { IsAutoLoad = !IsAutoLoad; });
                    break;
            }
        }

        // 3. Slider 

        // 3-1 Save
        autoSaveIntervalSlider = UIcanvas.transform.Find("AutoSaveSlider/Slider").GetComponent<Slider>();
        savesliderValueText = UIcanvas.transform.Find("AutoSaveSlider/ValueText").GetComponent<TMP_Text>();
        autoSaveIntervalSlider.onValueChanged.AddListener((value) =>
        {
            savesliderValue = value;
            savesliderValueText.text = $"{savesliderValue.ToString("F1")} Sec";
        });

        // 3-2 Load
        autoLoadIntervalSlider = UIcanvas.transform.Find("AutoLoadSlider/Slider").GetComponent<Slider>();
        loadSliderValueText = UIcanvas.transform.Find("AutoLoadSlider/ValueText").GetComponent<TMP_Text>();
        autoLoadIntervalSlider.onValueChanged.AddListener((value) =>
        {
            loadSliderValue = value;
            loadSliderValueText.text = $"{loadSliderValue.ToString("F1")} Sec";
        });

        // 4. 초기값 설정
        autoSaveIntervalSlider.value = 5f;
        autoLoadIntervalSlider.value = 5f;
        IsAutoSave = false;
        IsAutoLoad = false;
        SaveCount = 0;

    }
    void Start()
    {
        glovalText.text = $"Global Save Dealy : {SaveLoadUtility.inst.DefaulSaveDealy.ToString("F1")}  /  Global Load Delay : {SaveLoadUtility.inst.DefaultLoadDealy.ToString("F1")}";
    }

    // Update is called once per frame
    float autoSaveTimer = 0f;
    float autoLoadTimer = 0f;
    void Update()
    {
        UI_Text_Updater();
        AutoMode();
    }

    private void AutoMode()
    {
        if (isAutoSave)
        {
            autoSaveTimer += Time.deltaTime;

            if (autoSaveTimer > savesliderValue)
            {
                autoSaveTimer = 0f;
                string saveValue = MakeSampleSavaDataValue();
                SaveLoadUtility.inst.SaveData(saveValue);
            }
        }

        if (IsAutoLoad)
        {
            autoLoadTimer += Time.deltaTime;

            if (autoLoadTimer > loadSliderValue)
            {
                autoLoadTimer = 0f;
                SaveLoadUtility.inst.LoadData();
            }
        }
    }


    public float currentFps;
    float fpsUpdaterTimer;
    float curFPS;
    StringBuilder formattedTime = new StringBuilder();

   [SerializeField, Range(0f, 0.5f)] float FPSInterval;

    private void UI_Text_Updater()
    {
        fpsUpdaterTimer += Time.deltaTime;

        //Container Counter
        saveInfoText[0].text = $"Current Save Contatiner Counter : {SaveLoadUtility.inst.saveDataWaitQueue.Count}";

        
        // FPS Text
        if (fpsUpdaterTimer > FPSInterval)
        {
            fpsUpdaterTimer = 0;
            curFPS = 1.0f / Time.deltaTime;
            saveInfoText[4].text = $"FPS : <color=yellow><b> {curFPS.ToString("F1")}";
        }

        // Application Play Time
        TimeSpan timeSpan = TimeSpan.FromSeconds(Time.time);
        formattedTime.Clear();

        if (timeSpan.TotalMinutes < 1)
        {
            formattedTime.Append($"{timeSpan.Seconds}s");
        }
        else if (timeSpan.TotalHours < 1)
        {
            formattedTime.Append($"{timeSpan.Minutes}m {timeSpan.Seconds}s");
        }
        else
        {
            formattedTime.Append($"{(int)timeSpan.TotalHours}h {timeSpan.Minutes}m {timeSpan.Seconds}s");
        }

        saveInfoText[5].text = $"App PlayTime : <color=yellow><b>{formattedTime}</b></color>";
    }

    Coroutine textco;
    public void Set_CenterText(string text)
    {
        if (textco != null)
        {
            StopCoroutine(textco);
        }

        inputKeyText.text = text;

        textco = StartCoroutine(Set_CompleteText());
    }

    IEnumerator Set_CompleteText()
    {
        inputKeyText.gameObject.SetActive(true);

        yield return new WaitForSeconds(1.5f);

        inputKeyText.gameObject.SetActive(false);
    }

    // 실질적 로드된 데이터 표기
    public void loadTextUpdate(string LoadedData)
    {
        loaddataText.text = $"{LoadedData}";
    }

    // 로딩 준비 텍스트
    public void LoadStateText(bool value)
    {
        if (value)
        {
            loadStateText.text = $"<color=red> Loading.. Wait";
        }
        else
        {
            loadStateText.text = $"<color=green> Load Ready";
        }
        
    }

    public string MakeSampleSavaDataValue()
    {
        return $"Last SaveCount : <color=blue><b>{SaveCount}</b></color> / Time : <color=blue><b>{Time.time.ToString("F1")}";
    }

}
