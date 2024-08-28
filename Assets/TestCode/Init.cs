using nn.account;
using nn.fs;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;

public class Init : MonoBehaviour
{
    Uid _nintendoSwitchUserID;
    FileHandle fileHandle = new FileHandle();
    bool _hasBeenInitialized;
    string gameDataFilePath;
    string optionsDataFilePath;
    string MOUNT_NAME ="Test";

#if UNITY_SWITCH
    public bool InitNintendoSwitchFiles(Uid userId)
    {

        _nintendoSwitchUserID = userId;

        print("InitNintendoSwitchFiles()");
        _hasBeenInitialized = true;
        gameDataFilePath = string.Format("{0}:/{1}", MOUNT_NAME, "gameData");
        optionsDataFilePath = string.Format("{0}:/{1}", MOUNT_NAME, "optionsData");

        nn.Result result = nn.fs.SaveData.Mount(MOUNT_NAME, userId);
        result.abortUnlessSuccess();

        return result.IsSuccess();
    }
#endif

    private object LoadDataNintendoSwitch(string filePath)
    {
        print("[NX] Data LOAD: " + filePath);
        object gameData = null;

        /*return gameData;*/



#if UNITY_SWITCH

        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            print("PATH NOT FOUND:" + filePath);
            //SaveDataNintendoSwitch(filePath, gameData);
            return null;
        }
        
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Read);
        result.abortUnlessSuccess();

        print("[NX] Data LOAD : ");
        long fileSize = 0;
        result = nn.fs.File.GetSize(ref fileSize, fileHandle);
        result.abortUnlessSuccess();

        print("[NX] Data LOAD [C]: ");
        byte[] data = new byte[fileSize]; // 스크립트 저장하려고 임의대로 넣어논거임 fileSize
        result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
        result.abortUnlessSuccess();
        print("[NX] Data LOAD [D]: ");
        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(MOUNT_NAME);


        BinaryFormatter bf = new BinaryFormatter();
        gameData = bf.Deserialize(new MemoryStream(data));
        print("[NX] Data LOAD [E]: " + filePath);
        print("[NX] Data LOAD [E2]: " + gameData);
        print("[NX] Data LOAD [E3]: " + gameData.ToString());

        // reader.
#endif

        return gameData;

    }

    public static byte[] ObjectToByteArray(object obj)
    {
        BinaryFormatter bf = new BinaryFormatter();
        using (var ms = new MemoryStream())
        {
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }
    }

    // Convert a byte array to an Object
    public static object ByteArrayToObject(byte[] arrBytes)
    {
        using (var memStream = new MemoryStream())
        {
            var binForm = new BinaryFormatter();
            memStream.Write(arrBytes, 0, arrBytes.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            var obj = binForm.Deserialize(memStream);
            return obj;
        }
    }


    private void SaveDataNintendoSwitch(string filePath, object data)
    {

        /* Debug.Log("[NX] Data SAVE [DISABLED]");
         return;*/

        
        nn.fs.FileSystem.Unmount(MOUNT_NAME);
        nn.Result result = nn.fs.SaveData.Mount(MOUNT_NAME, _nintendoSwitchUserID);

        

        byte[] dataByteArray = ObjectToByteArray(data);

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();



        nn.fs.EntryType entryType = 0;
        result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        print("[NX] Data LOAD [A-RESULT]: " + result.ToString());
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            Debug.Log("[NX] Data CREATING FILE:" + filePath);
            result = nn.fs.File.Create(filePath, dataByteArray.LongLength);
        }
        else
        {
            Debug.Log("[NX] Data REUSING FILE:" + filePath);
        }

        result.abortUnlessSuccess();

        
        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();
        
        const int offset = 0;
        result = nn.fs.File.Write(fileHandle, offset, dataByteArray, dataByteArray.LongLength, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();
        
        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(MOUNT_NAME);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();

        print("ENd CREATING DATA!!!");


        return;

    }
}
