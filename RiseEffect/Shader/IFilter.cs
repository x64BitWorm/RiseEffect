namespace RiseEffect.Shader
{
    public interface IFilter
    {
        public Shader InitializeFilter();
        public void UseFilter();
    }
}
