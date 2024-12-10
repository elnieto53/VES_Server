using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// This utility class is meant for large IO operations of stream data.
/// </summary>
/// <typeparam name="T"></typeparam>
public class JsonStreamSerializer<T>
{
    private JsonSerializer<DataContainer> jsonManager;
    private DataContainer dataContainer;
    private Thread ioThread;

    private BlockingCollection<Command> commands;

    public delegate void DataLoadedCallback(List<T> data);

    private abstract class Command
    {
        public enum ID { AddDatum, SaveData }
        public ID id { get; protected set; }

        public class AddDatum : Command
        {
            public T datum;
            public AddDatum(T datum)
            {
                this.datum = datum;
                id = ID.AddDatum;
            }
        }

        public class SaveData : Command
        {
            public string name, filePath;
            public bool overwrite;
            public Action callback;
            public SaveData(string name, string filePath, bool overwrite, Action callback)
            {
                this.name = name;
                this.filePath = filePath;
                this.overwrite = overwrite;
                this.callback = callback;
                id = ID.SaveData;
            }
        }
    }


    [Serializable]
    public class DataContainer
    {
        public List<T> data;
        public DataContainer() { data = new List<T>(); }
    }


    public JsonStreamSerializer()
    {
        if (!typeof(T).IsValueType)
            throw new InvalidOperationException("A value Type is required");
        dataContainer = new DataContainer();
        jsonManager = new JsonSerializer<DataContainer>();
        commands = new BlockingCollection<Command>();
    }

    public void Init()
    {
        ioThread = new Thread(MainTask);
        ioThread.Start();
    }

    public void Add(T datum) => commands.Add(new Command.AddDatum(datum));

    public void SaveDataToFile(string name, string filePath, bool overwrite, Action callback)
        => commands.Add(new Command.SaveData(name, filePath, overwrite, callback));

    public static void LoadData(string name, string folderPath, DataLoadedCallback callback)
    {
        Thread newIOThread = new Thread(() => callback?.Invoke(JsonSerializer<DataContainer>.LoadDataFromFile(folderPath, name).data));
        newIOThread.Start();
    }

    private void MainTask()
    {
        Command command;
        while (true)
        {
            command = commands.Take();
            /*BEWARE: avoided pattern matching for its CPU overhead*/
            switch (command.id)
            {
                case Command.ID.AddDatum:
                    dataContainer.data.Add(((Command.AddDatum)command).datum);
                    break;
                case Command.ID.SaveData:
                    Command.SaveData commandData = (Command.SaveData)command;
                    jsonManager.data = dataContainer;
                    jsonManager.SaveDataToFile(commandData.name, commandData.filePath, commandData.overwrite);
                    commandData.callback?.Invoke();
                    break;
            }
        }
    }

    public void DeInit() => ioThread.Abort();
}