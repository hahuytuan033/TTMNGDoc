using UnityEngine;
public sealed class FeatureUIRewardProbe : ScriptableObject
{
    public int Count;
    public void Grant() { Count++; }
}
