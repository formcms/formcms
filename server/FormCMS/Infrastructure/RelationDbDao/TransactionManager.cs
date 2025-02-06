using System.Data;

namespace FormCMS.Infrastructure.RelationDbDao;

public class TransactionManager(IDbTransaction transaction):IDisposable
{
    private bool _isDisposed;
    
    public IDbTransaction? Transaction ()=> _isDisposed ? null : transaction;
    
    public void Commit()
    {
        transaction.Commit();
        Dispose();
    }

    public void Rollback()
    {
        transaction.Rollback();
        Dispose();
    }
    
    public void Dispose()
    {
        _isDisposed = true;
        transaction.Dispose();
    }
}