//
//  AppLovinDownloadHandler.cs
//  AppLovin MAX Unity Plugin
//
//  Created by Santosh Bagadi on 7/26/19.
//  Copyright © 2019 AppLovin. All rights reserved.
//

#if !UNITY_2017_2_OR_NEWER

using System;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class AppLovinDownloadHandler : DownloadHandlerScript
{
    // Required by DownloadHandler base class. Called when you address the 'bytes' property.
    protected override byte[] GetData()
    {
        return null;
    }

    private FileStream fileStream;

    public AppLovinDownloadHandler(string path) : base(new byte[2048])
    {
        var downloadDirectory = Path.GetDirectoryName(path);
        if (!Directory.Exists(downloadDirectory))
        {
            Directory.CreateDirectory(downloadDirectory);
        }

        try
        {
            //Open the current file to write to
            fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        }
        catch (Exception exception)
        {
            MaxSdkLogger.UserError(string.Format("Failed to create file at {0}\n{1}", path, exception.Message));
        }
    }

    protected override bool ReceiveData(byte[] byteFromServer, int dataLength)
    {
        if (byteFromServer == null || byteFromServer.Length < 1 || fileStream == null)
        {
            return false;
        }

        try
        {
            //Write the current data to the file
            fileStream.Write(byteFromServer, 0, dataLength);
        }
        catch (Exception exception)
        {
            fileStream.Close();
            fileStream = null;
            MaxSdkLogger.UserError(string.Format("Failed to download file{0}", exception.Message));
        }

        return true;
    }

    protected override void CompleteContent()
    {
        fileStream.Close();
    }
}

#endif
