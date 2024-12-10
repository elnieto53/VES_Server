using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FileExplorerGUI : MonoBehaviour
{
    private Dropdown foldersDropdown;
    private Dropdown filesDropdown;
    private Button acceptButton;
    private Button cancelButton;
    private Button createFileButton;
    private InputField newFileNameInputField;
    private InputField newFileContentInputField;
    private Text currentFolderText;

    /*Returns the file and folder paths*/
    public Action<string> fileSelectedCallback;
    private string currentPath;
    private string fileSearchPattern = null;


    private void ExampleCallback(string filePath)
    {
        Debug.Log("Returned FilePath: " + filePath);
        Debug.Log("Parent folder: " + Directory.GetParent(filePath).FullName);
    }

    public void Init(string currentPath, Action<string> callback, string fileSearchPattern)
    {
        foldersDropdown = transform.Find("FoldersDropdown").GetComponent<Dropdown>();
        filesDropdown = transform.Find("FilesDropdown").GetComponent<Dropdown>();
        acceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        cancelButton = transform.Find("CancelButton").GetComponent<Button>();
        createFileButton = transform.Find("CreateFileButton").GetComponent<Button>();
        newFileNameInputField = transform.Find("NewFileNameInputField").GetComponent<InputField>();
        newFileContentInputField = transform.Find("NewFileContentInputField").GetComponent<InputField>();
        currentFolderText = transform.Find("CurrentFolderText").GetComponent<Text>();

        this.fileSearchPattern = fileSearchPattern;

        foldersDropdown.ClearOptions();
        filesDropdown.ClearOptions();

        foldersDropdown.onValueChanged.AddListener(OnPathSelection);
        acceptButton.onClick.AddListener(OnClickAccept);
        cancelButton.onClick.AddListener(delegate { Destroy(gameObject); });
        createFileButton.onClick.AddListener(delegate { CreateNewFile(newFileContentInputField.text); });
        newFileContentInputField.characterLimit = 256000;

        this.currentPath = currentPath;
        if (!Directory.Exists(currentPath))
            Directory.CreateDirectory(currentPath);
        fileSelectedCallback = callback;
        RefreshFolderDropdown();
        RefreshFilesFolder();
    }

    public void Init(string currentPath, Action<string> callback) => Init(currentPath, callback, null);

    private void CreateNewFile(string content)
    {
        string newFilePath = currentPath + "/" + newFileNameInputField.text + ".json";
        if (!File.Exists(newFilePath))
            File.WriteAllText(newFilePath, content);

        RefreshFilesFolder();
    }

    private void OnClickAccept()
    {
        string path;
        if(filesDropdown.options.Count > 0)
        {
            path = currentPath + "/" + filesDropdown.options[filesDropdown.value].text;
        }
        else
        {
            path = "";
        }
        fileSelectedCallback?.Invoke(path);
        Destroy(gameObject);
    }

    private void RefreshFolderDropdown()
    {
        List<string> folderNames = new List<string>();
        string[] folders = Directory.GetDirectories(currentPath);
        folderNames.Add(currentPath);
        folderNames.Add("...");
        for (int i = 0; i < folders.Length; i++)
        {
            folders[i].Replace(currentPath, "");
            folderNames.Add(folders[i].Replace(currentPath, ""));
        }
        currentFolderText.text = GetFolderName(currentPath);
        foldersDropdown.ClearOptions();
        foldersDropdown.AddOptions(folderNames);
    }

    private void RefreshFilesFolder()
    {
        List<string> fileNames = new List<string>();
        string[] files = fileSearchPattern == null ? Directory.GetFiles(currentPath) : Directory.GetFiles(currentPath, fileSearchPattern);
        fileNames.AddRange(files.Select(o => Path.GetFileName(o)));

        filesDropdown.ClearOptions();
        filesDropdown.AddOptions(fileNames);
    }

    private string GetFolderName(string folderPath)
    {
        folderPath = folderPath.Replace("\\", "/");
        string parentPath = Directory.GetParent(folderPath).FullName.Replace("\\", "/");
        return folderPath.Replace(parentPath, "");
    }


    void OnPathSelection(int value)
    {
        if (value == 0)
            return;
        if (value == 1)
        {
            currentPath = Directory.GetParent(currentPath).FullName;
        }
        else
        {
            currentPath = String.Concat(currentPath + foldersDropdown.options[value].text).Replace("\\", "/");
        }
        RefreshFolderDropdown();
        RefreshFilesFolder();
    }
}
