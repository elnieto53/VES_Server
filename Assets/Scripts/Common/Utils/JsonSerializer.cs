using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;

public class JsonSerializer<T> where T : new()
{
    public T data;

    public JsonSerializer(T data)
    {
        if (!typeof(T).IsSerializable && !typeof(ISerializable).IsAssignableFrom(typeof(T)))
            throw new InvalidOperationException("A serializable type is required");
        this.data = data;
    }

    public JsonSerializer() : this(new T()) { }

    public static T LoadDataFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new Exception("The file '" + filePath + "' could not be found");
        return JsonUtility.FromJson<T>(File.ReadAllText(filePath));
    }

    public static T LoadDataFromFile(string folderPath, string name) => LoadDataFromFile(folderPath + "/" + name + ".json");

    public void SaveDataToFile(string name, string folderPath, bool overwrite)
    {
        string filePath = folderPath + "/" + name + ".json";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        if (!overwrite)
        {
            //Avoid overwriting by appending an index to the file name
            int counter = 1;
            while (File.Exists(filePath))
                filePath = folderPath + "/" + name + "_" + (counter++).ToString() + ".json"; //e.g. "(filepath)/Alex_2.json"
        }

        File.WriteAllText(filePath, JsonUtility.ToJson(data, true));
        Debug.Log("Data saved in " + filePath);
    }

    public void SaveDataToFile(string filePath, bool overwrite) => SaveDataToFile(Path.GetFileNameWithoutExtension(filePath), Path.GetDirectoryName(filePath), overwrite);
}

[Serializable]
public class DataListContainer<T>
{
    public List<T> list;
    public DataListContainer() { list = new List<T>(); }
}