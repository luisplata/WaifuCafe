namespace Customers.QueueUi
{
    public interface IQueueUiController
    {
        void Initialize(IQueueUiView view, IQueueUiService service);
        void Dispose();
    }
}

