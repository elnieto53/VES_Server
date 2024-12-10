using System;
using System.Collections.Generic;

public class DataMapping<A, T> where T : new()
{
    private List<(Predicate<A> filter, T data)> mapping { get; set; } = null;
    public IList<(Predicate<A> filter, T data)> iMapping { get => mapping; }

    public DataMapping(List<(Predicate<A> filter, T data)> mapping) => this.mapping = mapping;

    public bool TryGetData(A element, out T data)
    {
        data = new T();
        for (int i = 0; i < mapping.Count; i++)
        {
            if (mapping[i].filter(element))
            {
                data = mapping[i].data;
                return true;
            }
        }
        return false;
    }
}

/*Util*/
public class PathContainer
{
    public string path { get; set; }
    public PathContainer() => path = "";
    public PathContainer(string path) => this.path = path;
}