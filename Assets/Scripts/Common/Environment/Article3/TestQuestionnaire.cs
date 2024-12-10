using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class TestQuestionnaire : Questionnaire
{
    public GameObject board;
    public List<GameObject> symbols;
    public bool shuffle;
    private static System.Random rng = new System.Random();
    private List<string> options;

    public override List<string> GetOptions() => options;

    private GameObject LoadSymbol(GameObject symbol)
    {
        GameObject newSymbol = Instantiate(symbol, board.transform, false);
        SensibleElement[] newElements = newSymbol.GetComponentsInChildren<SensibleElement>();
        foreach(SensibleElement element in newElements)
            environmentManager.AddElement(element);
        newSymbol.SetActive(false);
        return newSymbol;
    }


    protected override void Init()
    {
        options = new List<string>(symbols.Select(o => o.name));
        setups = new List<Setup>();
        
        for (int i= 0; i < symbols.Count; i++)
            setups.Add(new SymbolSetup(LoadSymbol(symbols[i]), options.FindIndex(p => p.Equals(symbols[i].name))));
        if (shuffle)
            Shuffle(setups);
    }


    private static void Shuffle<T>(IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }


    public class SymbolSetup : Setup
    {
        GameObject symbol;
        private int _answer;
        public int answer { get => _answer; }

        public void Init() => symbol.SetActive(true);

        public void DeInit() => symbol.SetActive(false);

        public SymbolSetup(GameObject symbol, int answer)
        {
            _answer = answer;
            this.symbol = symbol;
        }
    }

}
