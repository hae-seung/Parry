namespace Parry.AnimationClass
{
    public sealed class EffectLifetime : PooledObject
    {
        public void EndEffect() => Release();
    }
}
