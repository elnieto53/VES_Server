using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WiderColumnQuestionnaire : Questionnaire
{
    public float relativeWidth;
    public GameObject board;
    public int numberOfSetups;
    private static List<string> options = new List<string>() { "Left", "Right" };

    public override List<string> GetOptions() => options;

    private GameObject LoadSymbol(GameObject symbol)
    {
        GameObject newSymbol = Instantiate(symbol, board.transform, false);
        SensibleElement[] newElements = newSymbol.GetComponentsInChildren<SensibleElement>();
        foreach (SensibleElement element in newElements)
            environmentManager.AddElement(element);
        newSymbol.SetActive(false);
        return newSymbol;
    }

    protected override void Init()
    {
        setups = new List<Setup>();
        
        for (int i = 0; i < numberOfSetups; i++)
        {
            GameObject symbol = LoadSymbol(Resources.Load(ResourcesPath.Prefabs.Environments.Symbols.widthCompSymbol) as GameObject);
            symbol.SetActive(false);
            setups.Add(new WiderColumnSetup(relativeWidth, symbol));
        }
            
    }

    public class WiderColumnSetup : Setup
    {
        private GameObject go;
        private int _answer;
        public int answer { get => _answer; }

        public void Init() => go.SetActive(true);

        public void DeInit() => go.SetActive(false);

        public WiderColumnSetup(float relativeWidth, GameObject symbol)
        {
            go = symbol;
            Transform leftColumn = go.transform.Find("Section 1");
            Transform rightColumn = go.transform.Find("Section 2");
            if (Random.value < 0.5f)
            {
                leftColumn.localScale = new Vector3(leftColumn.localScale.x, leftColumn.localScale.y, 1);
                rightColumn.localScale = new Vector3(rightColumn.localScale.x, rightColumn.localScale.y, relativeWidth);
                _answer = 1;
            }
            else
            {
                leftColumn.localScale = new Vector3(leftColumn.localScale.x, leftColumn.localScale.y, relativeWidth);
                rightColumn.localScale = new Vector3(rightColumn.localScale.x, rightColumn.localScale.y, 1);
                _answer = 0;
            }
        }
    }
}
