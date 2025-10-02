namespace com.faolline.dialoguesystem
{
    public interface IContext{}

    public sealed class DefaultContext : IContext
    {
        public static readonly DefaultContext Instance = new DefaultContext();
        private DefaultContext() { }
    }
}
