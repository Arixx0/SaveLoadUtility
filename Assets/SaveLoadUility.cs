using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadUility : MonoBehaviour
{
    private const float defaultSaveDealy = 60 / (32 + 1);
    private const float defaultLoadDealy = 1.1f;

    Stack<string> saveDataPipeline = new Stack<string>();
    Queue<string> loadQue = new Queue<string>();
    float saveTimer;

    // Notice =  닌텐도 쓰기 가이드라인 1분 32회 + 여유 1회 추가 , 읽기 1초 1회


    GameObject UIcanvas;
    TMP_Text[] saveInfoText; // 0:StackCount / 1:SaveTimer / 2:SaveCompleteCounter / 3: Current AutoSave Mode Stats / 4: FPS
    Button[] Btns;

    // 슬라이더
    Slider autoSaveIntervalSlider;
    float sliderValue;
    TMP_Text sliderValueText;
    

    float loadTimer;
    int saveCount;
    int SaveCount
    {
        get { return saveCount; }
        set
        {
            saveCount = value;
            saveInfoText[2].text = $"Save Complete Count : <color=yellow><b>{saveCount}";
        }
    }
    bool isAutoSave;
    bool IsAutoSave
    {
        get { return isAutoSave; } 
        set
        {
            isAutoSave = value;
            Btns[0].interactable = !value;
            saveInfoText[3].text = $"Auto Save Mode : <color={(value ? "blue" : "red")}><b>{value}";
        }
    }

    float currentFps;
    [SerializeField, Range(0f, 0.5f)] float FPSInterval;
    #region init Awake
    private void Awake()
    {
        UIcanvas = GameObject.Find("Canvas").gameObject;

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
                    Btns[index].onClick.AddListener(() => { SaveData(saveCount.ToString());});
                    break;

                case 1:  // Auto Save 버튼
                    Btns[index].onClick.AddListener(() => { IsAutoSave = !IsAutoSave; });
                    break;

                case 2: // Load 버튼
                    Btns[index].onClick.AddListener(() => { LoadDate(); });
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


    }
    #endregion

    void Start()
    {
        IsAutoSave = false;
        SaveCount = 0;
    }


    void Update()
    {
        Save();
        UI_Text_Updater();
    }


    float fpsUpdaterTimer;
    private void UI_Text_Updater()
    {
        fpsUpdaterTimer += Time.deltaTime;

        // Save Timer
        saveInfoText[1].text = $"Save Timer : <b>{saveTimer.ToString("F1")} Sec";

        // FPS Text
        if (fpsUpdaterTimer > FPSInterval)
        {
            fpsUpdaterTimer = 0;
            currentFps += (Time.deltaTime - currentFps) * 0.1f;
            float curFPS = 1f / currentFps;
            saveInfoText[4].text = $"FPS : <color=yellow><b> {curFPS.ToString("F1")}";
        }
    }



    public void Save()
    {
        if (saveTimer > defaultSaveDealy && saveDataPipeline.Count > 0)
        {
            saveTimer = 0f;
            string savedatatemp = saveDataPipeline.Pop();
            XmlSerializer xmlfile = new XmlSerializer(typeof(string));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                xmlfile.Serialize(memoryStream, savedatatemp);

                // 바이너리 파일로 저장
                byte[] xmlData = memoryStream.ToArray();
                File.WriteAllBytes("savedata.binary", xmlData);
            }
        }
        else if (saveTimer <= defaultSaveDealy)
        {
            saveTimer += Time.deltaTime;
        }
    }

    private void SaveData(string inputSaveData)
    {
        saveDataPipeline.Push(inputSaveData);
        SaveCount++;
    }

    private void LoadDate()
    {
        Debug.Log("Load Data");
    }
}