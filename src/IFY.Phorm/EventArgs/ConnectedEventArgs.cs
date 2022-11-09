using IFY.Phorm.Connectivity;

namespace IFY.Phorm.EventArgs
{
    public sealed class ConnectedEventArgs : System.EventArgs
    {
        /// <summary>
        /// The connection that has been created.
        /// </summary>
        public IPhormDbConnection Connection { get; internal set; } = null!;
    }
}
