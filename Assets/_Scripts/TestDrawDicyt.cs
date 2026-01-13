using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using Sirenix.Serialization;

public class TestDrawDicyt : SerializedMonoBehaviour
{

    [OdinSerialize,DictionaryDrawerSettings(KeyLabel = "Key", ValueLabel = "Value")]
    private Dictionary<string, string> testDict = new Dictionary<string, string>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
