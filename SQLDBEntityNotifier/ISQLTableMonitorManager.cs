using System.Threading.Tasks;
namespace SQLDBEntityNotifier
{
    public interface ISQLTableMonitorManager
    {
        Task Invoke();
    }
}
