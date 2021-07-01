using System;
using System.Collections;
using System.Collections.Generic;

/*
    A bidirectional Dictionary. Example:
        map["one"] = 1;
        map["two"] = 2;
        map[3] = "three";

        map[1] returns "one"
        map["one"] returns 1
        map[2] returns "two"
        map["two"] returns 2
        map[3] returns "three"
        map["three"] returns 3
*/
public class BiDictionary<T1, T2> {

    public Dictionary<T1, T2> Forwards = new Dictionary<T1, T2>();
    public Dictionary<T2, T1> Backwards = new Dictionary<T2, T1>();

    public T2 this[T1 value1] {
        get { return Forwards[value1]; }
        set { Add(value1, value); }
    }

    public T1 this[T2 value2] {
        get { return Backwards[value2]; }
        set { Add(value, value2); }
    }

    public void Add(T1 value1, T2 value2) {
        Forwards[value1] = value2;
        Backwards[value2] = value1;
    }

    public bool Contains(T1 value1) {
        return Forwards.ContainsKey(value1);
    }

    public bool Contains(T2 value2) {
        return Backwards.ContainsKey(value2);
    }

    public void Remove(T1 value1) {
        T2 value2 = Forwards[value1];
        Forwards.Remove(value1);
        Backwards.Remove(value2);
    }

    public void Remove(T2 value2) {
        T1 value1 = Backwards[value2];
        Forwards.Remove(value1);
        Backwards.Remove(value2);
    }
}

/*
    A List that can be indexed through different attributes.
    TODO: Perhaps a better way of doing this is via attributes
*/
public class DataList<T> : IEnumerable<T> {

    public Func<T, string> NameCallback;
    public Func<T, int> IndexCallback;
    public Func<T, (int, int)> PositionCallback;

    public List<T> Elements = new List<T>();
    public Dictionary<string, T> Names = new Dictionary<string, T>();
    public Dictionary<int, T> Indices = new Dictionary<int, T>();
    public Dictionary<(int, int), T> Positions = new Dictionary<(int, int), T>();

    public void Add(T value) {
        Elements.Add(value);
        if(NameCallback != null) Names[NameCallback(value)] = value;
        if(IndexCallback != null) Indices[IndexCallback(value)] = value;
        if(PositionCallback != null) Positions[PositionCallback(value)] = value;
    }

    public void Remove(T value) {
        Elements.Remove(value);
        if(NameCallback != null) Names.Remove(NameCallback(value));
        if(IndexCallback != null) Indices.Remove(IndexCallback(value));
        if(PositionCallback != null) Positions.Remove(PositionCallback(value));
    }

    public void Remove(string str) {
        if(NameCallback != null && Names.ContainsKey(str)) {
            Remove(Names[str]);
        }
    }

    public void Remove(int index) {
        if(IndexCallback != null && Indices.ContainsKey(index)) {
            Remove(Indices[index]);
        }
    }

    public void Remove((int, int) position) {
        if(PositionCallback != null && Positions.ContainsKey(position)) {
            Remove(Positions[position]);
        }
    }

    public void Remove(int x, int y) {
        Remove((x, y));
    }

    public IEnumerator<T> GetEnumerator() {
        return Elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
        throw new NotImplementedException();
    }

    public T this[string str] {
        get { return Names.GetValueOrDefault(str, default); }
    }

    public T this[int index] {
        get { return Indices.GetValueOrDefault(index, default); }
    }

    public T this[(int, int) position] {
        get { return Positions.GetValueOrDefault(position, default); }
    }

    public T this[int x, int y] {
        get { return Positions.GetValueOrDefault((x, y), default); }
    }
}