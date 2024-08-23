using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;

public class SaveLoadUility : MonoBehaviour
{
    public static SaveLoadUility inst;

    // Notice =  닌텐도 쓰기 가이드라인 1분 32회 + 여유 1회 추가 , 읽기 1초 1회
    private const float defaultSaveDealy = 60 / (32 + 1);
    private const float defaultLoadDealy = 1.1f;

    public Stack<string> saveDataPipeline = new Stack<string>();
    public Queue<string> loadQue = new Queue<string>();
    public float saveTimer;
    public float loadTimer;

    #region init Awake
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
    }
    #endregion

    void Start()
    {

    }

    void Update()
    {
        Save();
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

            Ui_Updater.inst.SaveCount++;
            
        }
        else if (saveTimer <= defaultSaveDealy)
        {
            saveTimer += Time.deltaTime;
        }
    }

    public void SaveData(string inputSaveData)
    {
        saveDataPipeline.Push(inputSaveData);
    }

    public void LoadDate()
    {
        Debug.Log("Load Data");
    }
}