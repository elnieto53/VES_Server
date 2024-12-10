using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class FolderExplorerGUI : MonoBehaviour
{
    private Dropdown foldersDropdown;
    private Text selectedPathText;
    private Button acceptButton;

    /*Returns the file and folder paths*/
    public Action<String> fileSelectedCallback;
    private string currentPath;


    // Start is called before the first frame update
    void Start()
    {
        foldersDropdown = transform.Find("FoldersDropdown").GetComponent<Dropdown>();
        acceptButton = transform.Find("AcceptButton").GetComponent<Button>();
        selectedPathText = transform.Find("SelectedPathText").GetComponent<Text>();

        foldersDropdown.ClearOptions();
        selectedPathText.text = currentPath;

        foldersDropdown.onValueChanged.AddListener(OnPathSelection);
        acceptButton.onClick.AddListener(delegate { fileSelectedCallback?.Invoke(currentPath); Destroy(gameObject); });

        Init(Application.dataPath, ExampleCallback);
    }

    private void ExampleCallback(string filePath)
    {
        Debug.Log("Returned folder path: " + filePath);
    }

    public void Init(string currentPath, Action<String> callback)
    {
        this.currentPath = currentPath;
        fileSelectedCallback = callback;
        RefreshFolderDropdown();
    }

    private void RefreshFolderDropdown()
    {
        List<string> folderNames = new List<string>();
        string[] folders = Directory.GetDirectories(currentPath);
        folderNames.Add("(No Directories Selected)");
        folderNames.Add("...");
        for (int i = 0; i < folders.Length; i++)
        {
            folders[i].Replace(currentPath, " ");
            folderNames.Add(folders[i].Replace(currentPath, " "));
        }
        foldersDropdown.ClearOptions();
        foldersDropdown.AddOptions(folderNames);
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
            currentPath = (currentPath + foldersDropdown.options[value].text).Replace("\\", "/");
        }
        selectedPathText.text = currentPath;
        RefreshFolderDropdown();
    }
}
