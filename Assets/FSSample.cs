/*--------------------------------------------------------------------------------*
  Copyright (C)Nintendo All rights reserved.

  These coded instructions, statements, and computer programs contain proprietary
  information of Nintendo and/or its licensed developers and are protected by
  national and international copyright laws. They may not be disclosed to third
  parties or copied or duplicated in any form, in whole or in part, without the
  prior written consent of Nintendo.

  The content herein is highly confidential and should be handled accordingly.
 *--------------------------------------------------------------------------------*/

using System.IO;
using UnityEngine;
using nn;
using nn.account;
using nn.fs;
using UnityEngine.Switch;

public class FsSaveData : MonoBehaviour
{
    private UnityEngine.UI.Text textComponent;
    private System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();

    private nn.account.Uid userId; // 사용자 고유ID
    private const string mountName = "MySave";
    private const string fileName = "MySaveData";
    private static readonly string filePath = string.Format("{0}:/{1}", mountName, fileName);
    private nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();

    private nn.hid.NpadState npadState;
    private nn.hid.NpadId[] npadIds = { nn.hid.NpadId.Handheld, nn.hid.NpadId.No1 };
    private const int saveDataVersion = 1;
    private const int saveDataSize = 8;
    private int counter = 0;
    private int saveData = 0;
    private int loadData = 0;

    private void Start()
    {
        textComponent = GameObject.Find("/Canvas/Text").GetComponent<UnityEngine.UI.Text>();


        nn.account.Account.Initialize();
        nn.account.UserHandle userHandle = new nn.account.UserHandle();

        if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
        {
            nn.Nn.Abort("Failed to open preselected user.");
        }
        nn.Result result = nn.account.Account.GetUserId(ref userId, userHandle);
        result.abortUnlessSuccess();
        result = nn.fs.SaveData.Mount(mountName, userId);
        result.abortUnlessSuccess();

        InitializeSaveData();
        Load();

        //패드
        nn.hid.Npad.Initialize();
        nn.hid.Npad.SetSupportedStyleSet(nn.hid.NpadStyle.Handheld | nn.hid.NpadStyle.JoyDual);
        nn.hid.Npad.SetSupportedIdType(npadIds);
        npadState = new nn.hid.NpadState();
    }

    private void Update()
    {
        stringBuilder.Length = 0;
        if (counter - loadData >= 300)
        {
            Load();
            loadData = counter;
        }
        else if (counter - saveData >= 100)
        {
            SaveCounter();
            saveData = counter;
        }
        for (int i = 0; i < npadIds.Length; i++)
        {
            nn.hid.Npad.GetState(ref npadState, npadIds[i], nn.hid.Npad.GetStyleSet(npadIds[i]));
            if ((npadState.buttons & nn.hid.NpadButton.Y) != 0)
            {
                ResetSaveData();
            }
            else if ((npadState.buttons & nn.hid.NpadButton.B) != 0)
            {
                Load();
                loadData = counter;
            }
            else if ((npadState.buttons & nn.hid.NpadButton.A) != 0)
            {
                SaveCounter();
                saveData = counter;
            }
        }

        stringBuilder.AppendFormat("A:Save, B:Load, Y:Reset\nCounter: {0}\nSave data: {1}\nLoad data {2}",
            counter, saveData, loadData);
        counter++;

        textComponent.text = stringBuilder.ToString();
    }

    private void OnDestroy()
    {
        nn.fs.FileSystem.Unmount(mountName);
    }

    private void InitializeSaveData()
    {
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (result.IsSuccess())
        {
            return;
        }
        if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            result.abortUnlessSuccess();
        }

        byte[] data;
        using (BinaryWriter writer = new BinaryWriter(new MemoryStream(saveDataSize)))
        {
            writer.Write(saveDataVersion);
            writer.Write(counter);

            writer.BaseStream.Close();
            data = (writer.BaseStream as MemoryStream).GetBuffer();
            Debug.Assert(data.Length == saveDataSize);
        }

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        result = nn.fs.File.Create(filePath, saveDataSize);
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        const int offset = 0;
        result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
    }

    private void SaveCounter()
    {
        byte[] data;
        using (BinaryWriter writer = new BinaryWriter(new MemoryStream(sizeof(int))))
        {
            writer.Write(counter);

            writer.BaseStream.Close();
            data = (writer.BaseStream as MemoryStream).GetBuffer();
            Debug.Assert(data.Length == sizeof(int));
        }

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        nn.Result result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        const int offset = 4;
        result = nn.fs.File.Write(fileHandle, offset, data, data.LongLength, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();


        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
    }

    private void Load()
    {
        EntryType entryType = 0;
        Result result = FileSystem.GetEntryType(ref entryType, filePath);
        
        if (FileSystem.ResultPathNotFound.Includes(result))
        {
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

        using (BinaryReader reader = new BinaryReader(new MemoryStream(data)))
        {
            int version = reader.ReadInt32();
            Debug.Assert(version == saveDataVersion); // Save data version up
            counter = reader.ReadInt32();
        }
    }

    private void ResetSaveData()
    {
        counter = 0;
        SaveCounter();
        saveData = counter;
    }
}
