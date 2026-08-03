using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class TestModule { }

[System.Serializable]
public class ModuleA : TestModule { public float value; }

[System.Serializable]
public class ModuleB : TestModule { public int count; }

public class SerializeRefTest : MonoBehaviour
{
    [SerializeReference]
    public List<TestModule> modules = new List<TestModule>();
}