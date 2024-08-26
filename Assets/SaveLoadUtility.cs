using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using nn.account;
using nn.fs;
using UnityEngine.Switch;

public class SaveLoadUtility : MonoBehaviour
{
    public static SaveLoadUtility inst;

    // Notice =  닌텐도 쓰기 가이드라인 1분 32회 + 여유 1회 추가 , 읽기 1초 1회
    private const float defaultSaveDealy = 60f / (32f + 1f);
    private const float defaultLoadDealy = 1.1f;

    private nn.account.Uid userId;
    private const string mountName = "MySave";
    private const string fileName = "MySaveData.bin";
    private static readonly string filePath = string.Format("{0}:/{1}", mountName, fileName);
    private nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();

    private nn.hid.NpadState npadState;
    private nn.hid.NpadId[] npadIds = { nn.hid.NpadId.Handheld, nn.hid.NpadId.No1 };
    private const int saveDataVersion = 1;
    private const int saveDataSize = 8;

    //
    public Queue<string> saveDataPipeline = new Queue<string>();
    public Queue<string> loadQue = new Queue<string>();
    public float saveTimer;
    public float loadTimer;


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
    void Start()
    {
#if UNITY_EDITOR

#else
        InitSwitch_FS();
#endif
    }


    // Only Switch Invoke Init
    private void InitSwitch_FS()
    {
        Account.Initialize();
        UserHandle userHandle = new UserHandle();

        if (!Account.TryOpenPreselectedUser(ref userHandle))
        {
            nn.Nn.Abort("Failed to open preselected user.");
        }
        nn.Result result = Account.GetUserId(ref userId, userHandle);
        result.abortUnlessSuccess();
        result = nn.fs.SaveData.Mount(mountName, userId);
        result.abortUnlessSuccess();

        InitializeSaveData();

        nn.hid.Npad.Initialize();
        nn.hid.Npad.SetSupportedStyleSet(nn.hid.NpadStyle.Handheld | nn.hid.NpadStyle.JoyDual);
        nn.hid.Npad.SetSupportedIdType(npadIds);
        npadState = new nn.hid.NpadState();
    }

    private void InitializeSaveData()
    {
        EntryType entryType = 0;
        nn.Result result = FileSystem.GetEntryType(ref entryType, filePath);

        // 파일 및 디렉토리가 있을경우
        if (result.IsSuccess())
        {
            return;
        }

        // 경로를 찾을수 없는경우
        if (!FileSystem.ResultPathNotFound.Includes(result))
        {
            result.abortUnlessSuccess();
        }

        string initialData = "-1"; // 초기 데이터 문자열

        byte[] data;
        using (MemoryStream memoryStream = new MemoryStream())
        {
            XmlSerializer xmlfile = new XmlSerializer(typeof(string));
            xmlfile.Serialize(memoryStream, initialData);

            data = memoryStream.ToArray();
        }

        // 시작 Enter
        Notification.EnterExitRequestHandlingSection();

        result = nn.fs.File.Create(filePath, data.LongLength); // 파일 크기를 직렬화된 데이터의 크기로 설정
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, OpenFileMode.Write);
        result.abortUnlessSuccess();

        const int offset = 0;
        result = nn.fs.File.Write(fileHandle, offset, data, data.Length, WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        // 종료 Leave
        Notification.LeaveExitRequestHandlingSection();
        Debug.LogError("Init 끝까지 완료");
    }



    void Update()
    {
        Save();
    }

    public void Save()
    {
#if UNITY_EDITOR
        if (saveTimer > defaultSaveDealy && saveDataPipeline.Count > 0)
        {
            Debug.Log(defaultSaveDealy);
            saveTimer = 0f;
            string savedatatemp = saveDataPipeline.Dequeue();
            XmlSerializer xmlfile = new XmlSerializer(typeof(string));

            using (MemoryStream memoryStream = new MemoryStream())
            {
                xmlfile.Serialize(memoryStream, savedatatemp);

                //바이너리 파일로 저장
                byte[] xmlData = memoryStream.ToArray();
                System.IO.File.WriteAllBytes("test", xmlData);
                
            }
        }
        else if (saveTimer <= defaultSaveDealy)
        {
            saveTimer += UnityEngine.Time.deltaTime;
        }
        #else
        if (saveTimer > defaultSaveDealy && saveDataPipeline.Count > 0)
        {
            //saveTimer = 0f;
            //string savedatatemp = saveDataPipeline.Dequeue();

            //byte[] data;
            //using (BinaryWriter writer = new BinaryWriter(new MemoryStream(sizeof(int))))
            //{
            //    writer.Write(savedatatemp);

            //    writer.BaseStream.Close();
            //    data = (writer.BaseStream as MemoryStream).GetBuffer();
            //    Debug.Assert(data.Length == sizeof(int));
            //}

            //UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

            //nn.Result result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
            //result.abortUnlessSuccess();

            //const int offset = 4;
            //result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, nn.fs.WriteOption.Flush);
            //result.abortUnlessSuccess();

            //nn.fs.File.Close(fileHandle);
            //result = nn.fs.FileSystem.Commit(mountName);
            //result.abortUnlessSuccess();


            //UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();


            string savedatatemp = saveDataPipeline.Dequeue();

            // Step 2 & 3: Convert string data to XML and then to a binary format
            XmlSerializer xmlSerializer = new XmlSerializer(typeof(string));
            byte[] data;

            using (MemoryStream memoryStream = new MemoryStream())
            {
                xmlSerializer.Serialize(memoryStream, savedatatemp);
                data = memoryStream.ToArray(); // Convert XML to byte array
            }

            // Step 4: Save binary data to the Nintendo Switch file system
#if UNITY_SWITCH
            UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();
#endif

            nn.Result result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
            result.abortUnlessSuccess();

            const int offset = 0;
            result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, nn.fs.WriteOption.Flush);
            result.abortUnlessSuccess();

            nn.fs.File.Close(fileHandle);
            result = nn.fs.FileSystem.Commit(mountName);
            result.abortUnlessSuccess();

#if UNITY_SWITCH
            UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif
        }
        else if (saveTimer <= defaultSaveDealy)
        {
            saveTimer += UnityEngine.Time.deltaTime;
        }
#endif 
    }



    public void SaveData(string inputSaveData)
    {
        saveDataPipeline.Enqueue(inputSaveData);
    }

    public void LoadDate()
    {
        Debug.Log("Load Data");
    }
}