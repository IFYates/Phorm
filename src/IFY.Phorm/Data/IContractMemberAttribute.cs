namespace IFY.Phorm.Data
{
    /// <summary>
    /// An attribute on a contract member
    /// </summary>
    public interface IContractMemberAttribute
    {
        /// <summary>
        /// For attributes that will need the member context for later use.
        /// </summary>
        /// <param name="context">The current context of the contract member.</param>
        void SetContext(object? context);
    }
}
