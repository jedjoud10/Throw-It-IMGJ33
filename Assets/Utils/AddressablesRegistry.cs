using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressableRegistry<T> where T : ScriptableObject {
    public Dictionary<string, T> data;
    
    public T this[string name] {
        get {
            if (this.data == null)
                return null;

            if (this.data.TryGetValue(name, out var data)) {
                return data;
            } else {
                Debug.LogError($"Could not find registry type '{name}'");
                return null;
            }
        }
    }

    public AddressableRegistry(string label) {
        var temp = new Dictionary<string, T>();
        data = temp;

        AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(label, (x) => {
            temp.TryAdd(x.name, x);
        });

        handle.WaitForCompletion();
    }
}

