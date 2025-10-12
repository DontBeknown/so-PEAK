// IStat.cs
public interface IStat
{
    float Current { get; }
    float Max { get; }
    float Percent { get; }
    event System.Action<float, float> OnChanged;
    void Add(float amount);
    void Subtract(float amount);
    void SetMax(float newMax);
    void Tick(float dt);
}
