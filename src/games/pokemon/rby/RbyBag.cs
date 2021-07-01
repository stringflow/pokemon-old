using System;
using System.Collections;
using System.Collections.Generic;

public class RbyBag : IEnumerable<RbyItemStack> {
    public Rby Game;
    public RbyItemStack[] Items;
    public int NumItems;

    public int IndexOf(string name) {
        return IndexOf(Game.Items[name]);
    }

    public int IndexOf(RbyItem item) {
        for(int i = 0; i < NumItems; i++) {
            if(Items[i].Item == item) {
                return i;
            }
        }

        return -1;
    }

    public bool Contains(string name) {
        return Contains(Game.Items[name]);
    }

    public bool Contains(RbyItem item) {
        return IndexOf(item) != -1;
    }

    public RbyItemStack this[int index] {
        get { return Items[index]; }
        set { Items[index] = value; }
    }

    public RbyItemStack this[RbyItem item] {
        get { return Items[IndexOf(item)]; }
        set { Items[IndexOf(item)] = value; }
    }

    public RbyItemStack this[string name] {
        get { return Items[IndexOf(Game.Items[name])]; }
        set { Items[IndexOf(Game.Items[name])] = value; }
    }

    public IEnumerator<RbyItemStack> GetEnumerator() {
        foreach(var item in Items) {
            if(item != null) yield return item;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        throw new NotImplementedException();
    }
}
