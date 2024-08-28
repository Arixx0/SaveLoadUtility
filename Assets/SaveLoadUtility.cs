using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using UnityEngine;
using System.Threading.Tasks;
using System.Collections;


#if UNITY_SWITCH

using nn.account;
using nn.fs;
using nn.hid;
using UnityEngine.Switch;

#endif


public class SaveLoadUtility : MonoBehaviour
{
    #region Ref

    public static SaveLoadUtility inst;
    public enum formatType { Binary, XmlType }
    public formatType saveType;

    private const float defaultSaveDelay = 60f / (32f + 1f);   // Notice =  ���ٵ� ���� ���̵���� 1�� 32ȸ + ���� 1ȸ �߰� , �б� 1�� 1ȸ
    private const float defaultLoadDelay = 1.1f;
    private const long saveDataFileSizeOffset = 2; // ���� ������ ���ϻ���� �� �� ũ�� ����

    private Uid userId;
    private const string mountName = "NTDSave";
    private const string fileName = "MySaveData";
    private static readonly string filePath = string.Format("{0}:/{1}", mountName, fileName);
    private FileHandle fileHandle = new FileHandle();
    private UserHandle userHandle = new UserHandle();

    private NpadState npadState;
    private NpadId[] npadIds = { NpadId.Handheld, NpadId.No1 };


    public Queue<string> saveDataWaitQueue = new Queue<string>();

    public float DefaulSaveDealy { get { return defaultSaveDelay; } }

    public float DefaultLoadDealy { get { return defaultLoadDelay; } }

    #endregion


    private void Awake()
    {
        #region Singlton
        if (inst == null)
        {
            inst = this;
        }
        else
        {
            Destroy(this);
        }
        #endregion

        loadingDelay = new WaitForSeconds(defaultLoadDelay);
    }
    void Start()
    {
#if UNITY_SWITCH

        Init_Switch_FileSystem();

#endif


        IsLoading = false;
    }

    private void OnDestroy()
    {
#if UNITY_SWITCH

        FileSystem.Unmount(mountName);

#endif
    }

    #region _Initialize Switch

    // Only Switch Invoke Init
    private void Init_Switch_FileSystem()
    {
        Account.Initialize();

        if (!Account.TryOpenPreselectedUser(ref userHandle))
        {
            nn.Nn.Abort("Failed to open preselected user.");
        }
        nn.Result result = Account.GetUserId(ref userId, userHandle);
        result.abortUnlessSuccess();

        result = nn.fs.SaveData.Mount(mountName, userId);
        result.abortUnlessSuccess();

        // File Init
        InitializeSaveData();

        // Pad Init
        Npad.Initialize();
        Npad.SetSupportedStyleSet(NpadStyle.Handheld | NpadStyle.JoyDual);
        Npad.SetSupportedIdType(npadIds);

        npadState = new NpadState();
    }

    private void InitializeSaveData()
    {
        EntryType entryType = 0;
        nn.Result result = FileSystem.GetEntryType(ref entryType, filePath);

        // ���� �� ���丮�� �������
        if (result.IsSuccess())
        {
            Debug.LogError("Init : Success Find File and Directory");
            return;
        }

        // ��θ� ã���� ���°��
        if (!FileSystem.ResultPathNotFound.Includes(result))
        {
            result.abortUnlessSuccess();
        }

        string initData = Ui_Updater.inst.MakeSampleSavaDataValue();
        byte[] data = SaveFileToAry(formatType.Binary, initData);

        Notification.EnterExitRequestHandlingSection();

        result = nn.fs.File.Create(filePath, data.LongLength * saveDataFileSizeOffset); // ���� ũ�⸦ ����ȭ�� �������� ũ��� ����
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, OpenFileMode.Write);
        result.abortUnlessSuccess();

        const int offset = 0;
        result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        Notification.LeaveExitRequestHandlingSection();
    }

    #endregion

    #region Save

    private Task saveTask;


    // 1. Input Data in Queue

    /// <summary>
    /// NX File Save Function
    /// </summary>
    /// <param name="inputSaveData">string SaveData</param>
    public async void SaveData(string inputSaveData)
    {
#if UNITY_SWITCH

        saveDataWaitQueue.Enqueue(inputSaveData);

        // ù ��° ȣ�� �ÿ��� ���� �۾��� ����
        if (saveTask == null)
        {
            saveTask = ProcessSaveQueue();
            await saveTask;
            saveTask = null;
        }
#else

        Debug.LogError("This Class need NintendoSwitch H/W, Cheking System");

#endif 
    }

    // 2. Que Data Processing
    private async Task ProcessSaveQueue()
    {
        bool isFirstSave = true;

        while (saveDataWaitQueue.Count > 0)
        {
            string saveData = saveDataWaitQueue.Dequeue();

            if (!string.IsNullOrEmpty(saveData))
            {
                // ù ��° ������ �ƴ� ��쿡�� ������ ����

                if (!isFirstSave)
                {
                    await Task.Delay(TimeSpan.FromSeconds(defaultSaveDelay));
                }
                else
                {
                    isFirstSave = false;
                }

                SaveDataToFile(saveData);
            }
        }

        // ����������, ���� �۾��� �Ϸ�� �Ŀ��� �����̸� �����Ͽ� ��� ������� �ʵ��� ��
        await Task.Delay(TimeSpan.FromSeconds(defaultSaveDelay));
    }

    // Data Convert to File Funtion
    private void SaveDataToFile(string savedata)
    {
        byte[] data = SaveFileToAry(formatType.Binary, savedata);

        Notification.EnterExitRequestHandlingSection();

        nn.Result result;
        EntryType entryType = 0;

        result = FileSystem.GetEntryType(ref entryType, filePath);

        // File Check
        if (FileSystem.ResultPathNotFound.Includes(result))
        {
            result = nn.fs.File.Create(filePath, data.LongLength * saveDataFileSizeOffset);
            result.abortUnlessSuccess();
        }

        else // ** ���� �����ϴ� ������ �����Ϸ��� ���Ϻ��� ������ ���� ���� **
        {
            long currentFileSize = 0;
            result = nn.fs.File.GetSize(ref currentFileSize, fileHandle);
            result.abortUnlessSuccess();

            if (currentFileSize < data.LongLength)
            {
                nn.fs.File.Delete(filePath);
                result.abortUnlessSuccess();

                result = nn.fs.File.Create(filePath, data.LongLength * saveDataFileSizeOffset);
                result.abortUnlessSuccess();
            }
        }

        result = nn.fs.File.Open(ref fileHandle, filePath, OpenFileMode.Write);
        result.abortUnlessSuccess();

        const int offset = 0;
        result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        Notification.LeaveExitRequestHandlingSection();

        // UI ������Ʈ
        Ui_Updater.inst.Set_CenterText("Save Complete");
        Ui_Updater.inst.SaveCount++;
    }

#endregion

    #region Load

    // �ε� ���̵���� ������

    private bool isLoading;
    private Coroutine loadingCoroutine;
    private WaitForSeconds loadingDelay;

    public bool IsLoading
    {
        get => isLoading;
        set
        {
            if (isLoading == value) return;

            isLoading = value;
            Ui_Updater.inst.LoadStateText(isLoading);

            if (isLoading && loadingCoroutine == null)
            {
                loadingCoroutine = StartCoroutine(HandleLoadingDelay());
            }
            else if (!isLoading && loadingCoroutine != null)
            {
                StopCoroutine(loadingCoroutine);
                loadingCoroutine = null;
            }
        }
    }

    private IEnumerator HandleLoadingDelay()
    {
        yield return loadingDelay;
        IsLoading = false;
    }


    public void LoadData()
    {
        if (IsLoading) return;

        IsLoading = true;

        EntryType entryType = 0;
        nn.Result result = FileSystem.GetEntryType(ref entryType, filePath);

        if (FileSystem.ResultPathNotFound.Includes(result))
        {
            Ui_Updater.inst.loadTextUpdate("Result Not Found");
            return;
        }
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, OpenFileMode.Read);
        result.abortUnlessSuccess();

        long fileSize = 0;
        result = nn.fs.File.GetSize(ref fileSize, fileHandle);
        result.abortUnlessSuccess();

        byte[] data = new byte[fileSize];
        result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);

        switch (saveType)   // Save file output value
        {
            case formatType.Binary:
                using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
                {
                    Ui_Updater.inst.loadTextUpdate(reader.ReadString());
                }
                break;

            case formatType.XmlType:
                XmlSerializer xmlSerializer = new XmlSerializer(typeof(string));
                using (MemoryStream memoryStream = new MemoryStream(data))
                {
                    Ui_Updater.inst.loadTextUpdate((string)xmlSerializer.Deserialize(memoryStream));
                }
                break;
        }
    }

    #endregion

    #region Utility

    /// <summary>
    /// Return To byte[] of SaveData ( Type : BainryAry, XML )
    /// </summary>
    /// <param name="returnType"> 0 : Bainry / 1 : Xml </param>
    /// <param name="savedata"> Input savedata (string type) </param>
    /// <returns></returns>
    public byte[] SaveFileToAry(formatType returnType, string savedata)
    {
        switch (returnType)
        {
            case formatType.Binary: // Bin

                using (MemoryStream memoryStream = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(memoryStream))
                {
                    writer.Write(savedata);
                    writer.Flush(); // Ensure all data is written to the stream
                    return memoryStream.ToArray();
                }

            case formatType.XmlType: // Xml => Bin

                XmlSerializer xmlSerializer = new XmlSerializer(typeof(string));

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    xmlSerializer.Serialize(memoryStream, savedata);
                    return memoryStream.ToArray();
                }

            default:
                throw new ArgumentOutOfRangeException(nameof(returnType), "Unsupported format type");
        }
    }

    #endregion

}